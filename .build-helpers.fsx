#r @"packages/FAKE.4.12.0/tools/FakeLib.dll"

open Fake
open Fake.XamarinHelper
open System
open System.IO
open System.Linq
open System.Xml
open Fake.FileUtils

let Exec command args =
    log (command + " " + args)
    let result = Shell.Exec ("mono", "--runtime=v4.0 " + command + " " + args)
    if result <> 0 then failwithf "%s exited with error %d" command result

let RestorePackages packageConfigFile =
    let toolFolder = Path.Combine("", ".ci/teamcity/tools/NuGet/NuGet.exe")
    Exec toolFolder ("restore " + packageConfigFile + " -PackagesDirectory packages")

let RunNUnitTests dllPath xmlPath =
    Exec "packages/NUnit.Runners.2.6.4/tools/nunit-console.exe" (dllPath + " -xml=" + xmlPath)
    TeamCityHelper.sendTeamCityNUnitImport xmlPath

let GetAndroidReleaseName xmlPath =
    let LoadXmlNode (xmlPath:string) =
        let xmlDoc = new XmlDocument ()
        xmlDoc.Load (xmlPath)
        xmlDoc.DocumentElement

    let node = LoadXmlNode xmlPath
    let package = getAttribute "package" node
    let version = getAttribute "android:versionName" node
    let build = getAttribute "android:versionCode" node
    (package + "_" + version + "_" + build + ".apk")

let GetiOSReleaseName (xmlPath:string) =
    let xmlDoc = new XmlDocument ()
    xmlDoc.Load (xmlPath)
    let bundleNode = xmlDoc.SelectSingleNode ("//plist/dict/key[.='CFBundleIdentifier']/following-sibling::*[1]")
    let versionNode = xmlDoc.SelectSingleNode ("//plist/dict/key[.='CFBundleShortVersionString']/following-sibling::*[1]")
    let buildNode = xmlDoc.SelectSingleNode ("//plist/dict/key[.='CFBundleVersion']/following-sibling::*[1]")
    (bundleNode.InnerText + "_" + versionNode.InnerText + "_" + buildNode.InnerText)

let ChangeFileName (file:FileInfo, fileName:string) =
    let newName = Path.Combine (file.DirectoryName, fileName)
    mv file.FullName newName
    newName
