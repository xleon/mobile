#r @"packages/FAKE.4.1.4/tools/FakeLib.dll"
#load ".build-helpers.fsx"
open Fake
open System
open System.IO
open System.Linq
open BuildHelpers
open Fake.XamarinHelper
open Fake.FileUtils

Target "clean" (fun _ ->
    let dirs = !! "./**/bin/"
                  ++ "./**/obj/"
    CleanDirs dirs
)

Target "core-build" (fun () ->
    RestorePackages "Phoebe/packages.config"
    RestorePackages "Tests/packages.config"
    MSBuild "Phoebe/bin/Desktop/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Phoebe/Phoebe.Desktop.csproj" ] |> ignore
    MSBuild "Tests/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Tests/Tests.csproj" ] |> ignore
)

Target "core-tests" (fun () ->
    RunNUnitTests "Tests/bin/Debug/Tests.dll" "Tests/bin/Debug/TestResult.xml"
)

Target "android-build" (fun () ->
    let buildParamsFile = getBuildParam "buildParamsFile"
    if (System.String.Empty <> buildParamsFile)
      then cp buildParamsFile "Phoebe/Build.cs"

    RestorePackages "Mobile.Android.sln"
    MSBuild "Joey/bin/Release" "Build" [ ("Configuration", "Release") ] [ "Mobile.Android.sln" ] |> ignore
)

Target "android-package" (fun () ->
    // Android build parameters
    let keyStorePath = getBuildParamOrDefault "keyStorePath" "toggl.keystore"
    let keyStorePassword = getBuildParamOrDefault "keyStorePassword" ""
    let keyStoreAlias = getBuildParamOrDefault "keyStoreAlias" "toggl"
    let fileName = GetAndroidReleaseName "Joey/Properties/AndroidManifest.xml"

    let ChangeFileName (file:#FileInfo) =
        let newName = Path.Combine(file.DirectoryName, fileName)
        file.MoveTo (newName)
        newName

    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "Joey/Joey.csproj" // Project file and not Android solution!
            Configuration = "Release"
            OutputPath = "Joey/bin/Release"
        })
    |> AndroidSignAndAlign (fun defaults ->
        {defaults with
            KeystorePath = keyStorePath
            KeystorePassword = keyStorePassword
            KeystoreAlias = keyStoreAlias
            // If zipalign tool is not added to system path
            // you should uncomment this line and configure
            // the correct path.
            // ZipalignPath = "/Users/xxx/Library/Developers/Xamarin/android-sdk-macosx/build-tools/23.0.0/zipalign"
        })
    |> ChangeFileName
    |> TeamCityHelper.PublishArtifact
)

Target "ios-build" (fun () ->
    let buildParamsFile = getBuildParam "buildParamsFile"
    if (System.String.Empty <> buildParamsFile)
      then cp buildParamsFile "Phoebe/Build.cs"

    RestorePackages "Mobile.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Mobile.iOS.sln"
            Configuration = "Debug|iPhoneSimulator"
            Target = "Build"
        })
)

Target "ios-adhoc" (fun () ->
    // Setup conf files.
    let buildParamsFile = getBuildParam "buildParamsFile"
    if (System.String.Empty <> buildParamsFile)
      then cp buildParamsFile "Phoebe/Build.cs"

    let googleXml = getBuildParam "googleServicesXml"
    if (System.String.Empty <> googleXml)
      then cp googleXml "Ross/GoogleService-Info.plist"

    let infoXml = getBuildParam "infoXml"
    if (System.String.Empty <> infoXml)
      then cp infoXml "Ross/Info.plist"

    RestorePackages "Mobile.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Mobile.iOS.sln"
            Configuration = "Ad-Hoc|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("Ross", "bin", "iPhone", "Ad-Hoc"), "*.ipa").First()
    let newReleaseName = Path.Combine("Ross", "bin", "iPhone", "Ad-Hoc", (GetiOSReleaseName "Ross/Info.plist" + ".ipa"))
    Rename newReleaseName appPath
    TeamCityHelper.PublishArtifact newReleaseName
)

Target "ios-appstore" (fun () ->
    // Setup conf files.
    // Too many conf parameters. This will change in the future.
    let xamApiKey = getBuildParam "xamApiKey"

    let buildParamsFile = getBuildParam "buildParamsFile"
    if (System.String.Empty <> buildParamsFile)
      then cp buildParamsFile "Phoebe/Build.cs"

    let googleXml = getBuildParam "googleServicesXml"
    if (System.String.Empty <> googleXml)
      then cp googleXml "Ross/GoogleService-Info.plist"

    let infoXml = getBuildParam "infoXml"
    if (System.String.Empty <> infoXml)
      then cp infoXml "Ross/Info.plist"

    RestorePackages "Mobile.iOS.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Mobile.iOS.sln"
            Configuration = "AppStore|iPhone"
            Target = "Build"
        })

    // Zip .app file
    let outputFolder = Path.Combine("Ross", "bin", "iPhone", "AppStore")
    let zipFilePath = (GetiOSReleaseName "Ross/Info.plist" + ".zip")
    pushd outputFolder
    let zipArgs = String.Format("-r -y {0} {1}", zipFilePath, "Ross.app")
    let result = Shell.Exec ("zip", zipArgs)
    if result <> 0 then failwithf "zip exited with error" result
    else TeamCityHelper.PublishArtifact zipFilePath
    popd ()

    // Upload dSYM to Xamarin Insights
    let dSYMPath = Path.Combine("Ross", "bin", "iPhone", "AppStore", "Ross.app.dSYM")
    let dSYMPathZip = Path.Combine("Ross", "bin", "iPhone", "AppStore", "Ross.app.dSYM.zip")
    let zipArgs = String.Format("-r -y {0} {1}", dSYMPathZip, dSYMPath)
    Shell.Exec("zip", zipArgs) |> ignore
    let curlArgs = String.Format("-i -F 'dsym=@{0};type=application/zip' https://xaapi.xamarin.com/api/dsym?apikey={1}", dSYMPathZip, xamApiKey)
    Shell.Exec("curl", curlArgs) |> ignore
)

"clean"
  ==> "core-build"
  ==> "core-tests"

"clean"
  ==> "android-build"
  ==> "android-package"

"clean"
  ==> "ios-build"

"clean"
  ==> "ios-adhoc"

"clean"
  ==> "ios-appstore"

RunTarget()
