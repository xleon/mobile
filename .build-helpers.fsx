module BuildHelpers

open Fake
open Fake.XamarinHelper
open System
open System.IO
open System.Linq

let Exec command args =
    let result = Shell.Exec("mono", "--runtime=v4.0 " + command + " " + args)
    if result <> 0 then failwithf "%s exited with error %d" command result

let RestorePackages packageConfigFile =
    let toolFolder = Path.Combine("", ".ci/teamcity/tools/NuGet/NuGet.exe")
    printfn "File: %s" packageConfigFile
    Exec toolFolder ("restore " + packageConfigFile + " -PackagesDirectory packages")

let RestoreComponents solutionFile =
      let toolFolder = Path.Combine("", ".ci/teamcity/tools/xpkg/xamarin-component.exe")
      Exec toolFolder ("restore " + solutionFile)

let RunNUnitTests dllPath xmlPath =
    Exec "packages/NUnit.Runners.2.6.4/tools/nunit-console.exe" (dllPath + " -xml=" + xmlPath)
    TeamCityHelper.sendTeamCityNUnitImport xmlPath
