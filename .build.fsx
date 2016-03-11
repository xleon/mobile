#r @"packages/FAKE.4.4.2/tools/FakeLib.dll"
#load ".build-helpers.fsx"
open Fake
open System
open System.IO
open System.Linq
open ``build-helpers``
open Fake.XamarinHelper
open Fake.FileUtils

Target "clean" (fun _ ->
    let dirs = !! "./**/bin/"
                  ++ "./**/obj/"
    CleanDirs dirs
)

Target "core-build" (fun () ->
    MSBuild "Tests/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Mobile.UnitTest.sln" ] |> ignore
)

Target "core-tests" (fun () ->
    RunNUnitTests "Tests/bin/Debug/Tests.dll" "Tests/bin/Debug/TestResult.xml"
)

Target "android-package" (fun () ->
    let buildParamsFile = getBuildParam "buildParamsFile"
    if (System.String.Empty <> buildParamsFile)
      then cp buildParamsFile "Phoebe/Build.cs"

    RestorePackages "Mobile.Android.sln"
    // Build solution to include Android Wear
    MSBuild "" "Build" [ ("Configuration", "Release") ] [ "Mobile.Android.sln" ] |> ignore
    // Package project (wear apk will be included)
    MSBuild "Joey/bin/Release" "PackageForAndroid" [ ("Configuration", "Release") ]  [ "Joey/Joey.csproj" ] |> ignore
)

Target "android-signalign" (fun () ->
    // Android build parameters
    let keyStorePath = getBuildParamOrDefault "keyStorePath" "toggl.keystore"
    let keyStorePassword = getBuildParamOrDefault "keyStorePassword" ""
    let keyStoreAlias = getBuildParamOrDefault "keyStoreAlias" "toggl"
    let fileName = GetAndroidReleaseName "Joey/Properties/AndroidManifest.xml"

    // path configurations
    let basePath = "Joey/bin/Release/com.toggl.timer"
    let unsignedApk = basePath + ".apk"
    let unsignedWearableApk = basePath + "/res/raw/wearable_app.apk"
    let signedWearableApk = basePath + "_weareable_signed.apk"

    let ChangeFileName (file:FileInfo) =
        let newName = Path.Combine(file.DirectoryName, fileName)
        file.MoveTo (newName)
        newName

    // Unpack the unsigned apk
    let unpackArgs = String.Format("d -s -o {0} {1}", basePath, unsignedApk)
    Shell.Exec ("apktool", unpackArgs) |> ignore

     // Sign wearable apk
    let jarsignerArgs = String.Format("-verbose -keystore {0} -storepass {1} {2} {3}", keyStorePath, keyStorePassword, unsignedWearableApk, keyStoreAlias)
    Shell.Exec ("jarsigner", jarsignerArgs) |> ignore

    // Pack solution again
    let packArgs = String.Format("b -o {0} {1}", signedWearableApk, basePath)
    Shell.Exec ("apktool", packArgs) |> ignore

    // Sign whole .apk and finish process
    let apkFileInfo = new FileInfo (signedWearableApk);
    AndroidSignAndAlign (fun defaults ->
        {defaults with
            KeystorePath = keyStorePath
            KeystorePassword = keyStorePassword
            KeystoreAlias = keyStoreAlias
        }) apkFileInfo
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
            Configuration = "Debug"
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
            Configuration = "Ad-Hoc"
            Target = "Build"
            Platform = "iPhone"
            BuildIpa = true
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
            Configuration = "AppStore"
            Target = "Build"
            Platform = "iPhone"
        })

    // Zip .app file
    let outputFolder = Path.Combine("Ross", "bin", "iPhone", "AppStore")
    let zipFilePath = (GetiOSReleaseName "Ross/Info.plist" + ".zip")
    pushd outputFolder
    let zipArgs = String.Format("-r -y {0} {1}", zipFilePath, "Ross.app")
    let result = Shell.Exec ("zip", zipArgs)
    popd ()

    // Publish on Teamcity
    let binaryPath = Path.Combine("Ross", "bin", "iPhone", "AppStore", zipFilePath)
    if result <> 0 then failwithf "zip exited with error %i" result
    else TeamCityHelper.PublishArtifact binaryPath

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
  ==> "android-package"
  ==> "android-signalign"

"clean"
  ==> "ios-build"

"clean"
  ==> "ios-adhoc"

"clean"
  ==> "ios-appstore"

RunTarget()
