#r "nuget: Fake.Installer.InnoSetup"

#load "publish.fsx"

open Publish
open System.IO
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.Installer

let createInstaller buildDir =
    let scriptPath = buildDir </> "setup.iss"
    let installerDir = buildDir </> "installer"

    File.readAsString (dlcBuilderDir </> "setup.iss")
    |> String.replace "%VERSION%" release.NugetVersion
    |> File.writeString false scriptPath

    InnoSetup.build (fun p -> 
        { p with
            ToolPath = @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
            OutputFolder = installerDir
            ScriptFile = scriptPath
        })

    Directory.EnumerateFiles(installerDir, "*.exe")
    |> Seq.head

cleanPublishDirectory ()
publishBuilder Windows
|> createInstaller
|> createGitHubRelease
