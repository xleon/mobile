#r @"packages/FAKE.4.1.4/tools/FakeLib.dll"
#load ".build-helpers.fsx"
open Fake
open System
open System.IO
open System.Linq
open BuildHelpers
open Fake.XamarinHelper

Target "core-build" (fun () ->
    RestorePackages "Phoebe/packages.config"

    MSBuild "Phoebe/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Phoebe/Phoebe.csproj" ] |> ignore
)

Target "core-tests" (fun () ->
    RunNUnitTests "Phoebe/bin/Debug/Phoebe.Tests.dll" "src/TipCalc/bin/Debug/testresults.xml"
)

Target "ios-build" (fun () ->
  RestorePackages "Ross/packages.config"

  RestoreComponents "Mobile.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Mobile.sln"
            Configuration = "Debug|iPhoneSimulator"
            Target = "Build"
        })
)

Target "ios-adhoc" (fun () ->
    RestorePackages "Ross/packages.config"

    RestoreComponents "Mobile.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Mobile.sln"
            Configuration = "Ad-Hoc|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("Ross", "bin", "iPhone", "Ad-Hoc"), "*.ipa").First()

    TeamCityHelper.PublishArtifact appPath
)

Target "ios-appstore" (fun () ->
    RestorePackages "Ross/packages.config"

    RestoreComponents "Mobile.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "TipCalc.iOS.sln"
            Configuration = "AppStore|iPhone"
            Target = "Build"
        })

    let outputFolder = Path.Combine("Ross", "bin", "iPhone", "AppStore")
    let appPath = Directory.EnumerateDirectories(outputFolder, "*.app").First()
    let zipFilePath = Path.Combine(outputFolder, "Ross.iOS.zip")
    let zipArgs = String.Format("-r -y '{0}' '{1}'", zipFilePath, appPath)

    Exec "zip" zipArgs

    TeamCityHelper.PublishArtifact zipFilePath
)

Target "ios-uitests" (fun () ->
    let appPath = Directory.EnumerateDirectories(Path.Combine("Ross", "bin", "iPhoneSimulator", "Debug"), "*.app").First()

    RunUITests appPath
)

Target "ios-testcloud" (fun () ->
    RestorePackages "Ross/packages.config"

    RestoreComponents "Mobile.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "Mobile.sln"
            Configuration = "Debug|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("Ross", "bin", "iPhone", "Debug"), "*.ipa").First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

Target "android-build" (fun () ->
    RestorePackages "Joey/packages.config"

    xsbuild "Joey/bin/Release" "Build" [ ("Configuration", "Release") ] [ "Mobile.sln" ] |> ignore
)

Target "android-package" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "Joey/Joey.csproj"
            Configuration = "Release"
            OutputPath = "Joey/bin/Release"
        })
    |> AndroidSignAndAlign (fun defaults ->
        {defaults with
            KeystorePath = "keystore"
            KeystorePassword = "pass" // TODO: don't store this in the build script for a real app!
            KeystoreAlias = "alias"
        })
    |> fun file -> TeamCityHelper.PublishArtifact file.FullName
)

Target "android-uitests" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "Joey/Joey.csproj"
            Configuration = "Release"
            OutputPath = "Joey/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("Joey", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    RunUITests appPath
)

Target "android-testcloud" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "Joey/Joey.csproj"
            Configuration = "Release"
            OutputPath = "Joey/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("Joey", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

"core-build"
  ==> "core-tests"

"ios-build"
  ==> "ios-uitests"

"android-build"
  ==> "android-uitests"

"android-build"
  ==> "android-testcloud"

"android-build"
  ==> "android-package"

RunTarget()
