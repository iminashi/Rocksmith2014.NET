#load "publish.fsx"

open System.IO
open System.Runtime.InteropServices
open Fake.IO.FileSystemOperators
open Fake.IO
open Fake.Core
open Publish

let createAppBundle buildDir =
    // Create the app bundle directory structure and copy the build contents
    let contentsDir = publishDir </> "DLC Builder.app" </> "Contents"
    let resourceDir = contentsDir </> "Resources"
    Directory.CreateDirectory(contentsDir) |> ignore
    Directory.Move(buildDir, contentsDir </> "MacOS")
    Directory.CreateDirectory(resourceDir) |> ignore

    // Run chmod on the executables if not building on Windows
    if not <| RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
        printfn "Setting executable permissions..."
        Shell.cd (contentsDir </> "MacOS")
        [ "DLCBuilder"; "./Tools/ww2ogg"; "./Tools/revorb" ]
        |> List.iter (chmod "+x")

    // Copy the icon
    let macSourceDir = dlcBuilderDir </> "macOS"
    File.Copy(macSourceDir </> "icon.icns", resourceDir </> "icon.icns")

    // Create Info.plist with the program version set
    File.readAsString (macSourceDir </> "Info.plist")
    |> String.replace "%VERSION%" release.NugetVersion
    |> File.writeString false (contentsDir </> "Info.plist")

cleanPublishDirectory()
publishBuilder MacOS
|> createAppBundle
createZipArchive MacOS
|> addFileToRelease
