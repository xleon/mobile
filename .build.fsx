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
    MSBuild "Phoebe/bin/Desktop/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Phoebe/Phoebe.Desktop.csproj" ] |> ignore
    MSBuild "Tests/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "Tests/Tests.csproj" ] |> ignore
)

Target "core-tests" (fun () ->
    RestorePackages "Tests/packages.config"
    RunNUnitTests "Tests/bin/Debug/Tests.dll" "Tests/bin/Debug/TestResult.xml"
)

"core-build"
  ==> "core-tests"

RunTarget()
