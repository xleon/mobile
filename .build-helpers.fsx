module BuildHelpers

open Fake
open Fake.XamarinHelper
open System
open System.IO
open System.Linq
open System.Xml
open Fake.FileUtils

let Exec command args =
    let result = Shell.Exec("mono", "--runtime=v4.0 " + command + " " + args)
    if result <> 0 then failwithf "%s exited with error %d" command result

let RestorePackages packageConfigFile =
    let toolFolder = Path.Combine("", ".ci/teamcity/tools/NuGet/NuGet.exe")
    Exec toolFolder ("restore " + packageConfigFile + " -PackagesDirectory packages")

let RestoreXamComponents projectFile =
      log "Restoring componentes!"
      RestoreComponents (fun defaults ->
        {defaults with
            ToolPath = ".ci/teamcity/tools/xpkg/xamarin-component.exe"
            }) projectFile

let RunNUnitTests dllPath xmlPath =
    Exec "packages/NUnit.Runners.2.6.4/tools/nunit-console.exe" (dllPath + " -xml=" + xmlPath)
    TeamCityHelper.sendTeamCityNUnitImport xmlPath

let GetAndroidFileName projectFolder =
    let LoadXmlNode (projectPath:string) =
        let xmlDoc = new XmlDocument ()
        xmlDoc.Load (projectPath)
        xmlDoc.DocumentElement

    let node = LoadXmlNode projectFolder
    let package = getAttribute "package" node
    let version = getAttribute "android:versionName" node
    let build = getAttribute "android:versionCode" node
    (package + "_" + version + "_" + build + ".apk")

let ChangeFileName (file:#FileInfo, fileName:string) =
    let newName = Path.Combine (file.DirectoryName, fileName)
    mv file.FullName newName
    newName
