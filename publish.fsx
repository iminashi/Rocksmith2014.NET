#r "nuget: Fake.Core.ReleaseNotes"
#r "nuget: Fake.Api.GitHub"
#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.IO.FileSystem"
#r "nuget: Fake.IO.Zip"
//Required for FAKE to work on Linux
#r "nuget: MSBuild.StructuredLogger"

open Fake.Api
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open System
open System.IO
open System.Runtime.InteropServices

type TargetPlatForm =
    | Windows
    | MacOS
    override this.ToString() =
        match this with
        | Windows -> "win"
        | MacOS -> "mac"

let gitOwner = "iminashi"
let gitName = "Rocksmith2014.NET"

let publishDir = __SOURCE_DIRECTORY__ </> "publish"
let samplesDir = __SOURCE_DIRECTORY__ </> "samples"
let dlcBuilderDir = samplesDir </> "DLCBuilder"
let msBuildParams trim =
    let properties =
        [ "PublishSingleFile", "true"
          if trim then
            "PublishTrimmed", "true"
            "TrimMode", "link" ]
    { MSBuild.CliArguments.Create() with Properties = properties }

let release =
    Path.Combine(dlcBuilderDir, "RELEASE_NOTES.md")
    |> ReleaseNotes.load

let cleanPublishDirectory () =
    if Directory.Exists publishDir then
        Directory.Delete(publishDir, recursive = true)

let createConfig appName platform trim (arg: DotNet.PublishOptions) =
    let targetDir = publishDir </> $"{appName}-{platform}"
    let runTime, isMac =
        match platform with
        | Windows -> "win-x64", false
        | MacOS -> "osx-x64", true

    { arg with OutputPath = Some targetDir
               Configuration = DotNet.BuildConfiguration.Release
               Runtime = Some runTime
               SelfContained = Some isMac
               MSBuildParams = msBuildParams trim }

let chmod arg file =
    CreateProcess.fromRawCommand "chmod" [ arg; file ]
    |> Proc.run
    |> ignore

let zip workingDir targetFile dirToZip =
    Shell.cd workingDir
    CreateProcess.fromRawCommand "zip" [ "-r"; targetFile; dirToZip ]
    |> Proc.run
    |> ignore

let publishUpdater platform =
    samplesDir </> "Updater" </> "Updater.fsproj"
    |> DotNet.publish (createConfig "updater" platform (platform = MacOS))

let createMacAppBundle buildDir =
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

let publishBuilder platform =
    dlcBuilderDir </> "DLCBuilder.fsproj"
    |> DotNet.publish (createConfig "dlcbuilder" platform false)

    let targetDir = publishDir </> $"dlcbuilder-{platform}"
    let updaterExecutable =
        match platform with
        | MacOS -> "Updater"
        | Windows -> "Updater.exe"

    match platform with
    | Windows ->
        // Copy the updater into a subfolder
        Directory.CreateDirectory(targetDir </> "Updater") |> ignore
        File.Copy(publishDir </> $"updater-{platform}" </> updaterExecutable,
                  targetDir </> "Updater" </> updaterExecutable, overwrite = true)
    | MacOS ->
        // Copy the updater into the target folder
        //File.Copy(publishDir </> $"updater-{platform}" </> updaterExecutable,
        //          targetDir </> updaterExecutable, overwrite = true)
        createMacAppBundle targetDir

let createZipArchive platform =
    let targetFile = publishDir </> $"DLCBuilder-{platform}-{release.NugetVersion}.zip"
    let workingDir, dirToZip =
        match platform with
        | Windows ->
            let dir = publishDir </> "dlcbuilder-win"
            dir, dir
        | MacOS ->
            publishDir, "DLC Builder.app"

    if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
        !! $"{dirToZip}/**" |> Zip.zip workingDir targetFile
    else
        // Using FAKE's zip loses the executable permissions
        zip workingDir targetFile dirToZip

    targetFile

let createGitHubRelease file =
    let token =
        match Environment.GetEnvironmentVariable "github_token" with
        | null -> failwith "github_token environment variable is not set."
        | s -> s

    let tagName = $"v{release.NugetVersion}"

    let setParams (p: GitHub.CreateReleaseParams) =
        { p with Name = $"DLC Builder {tagName}"
                 Body = String.Join("\n", release.Notes)
                 Draft = true
                 Prerelease = release.SemVer.PreRelease <> None }

    GitHub.createClientWithToken token
    |> GitHub.createRelease gitOwner gitName tagName setParams
    |> GitHub.uploadFile file
    |> GitHub.publishDraft
    |> Async.RunSynchronously

let addFileToRelease file =
    let token =
        match Environment.GetEnvironmentVariable "github_token" with
        | null -> failwith "github_token environment variable is not set."
        | s -> s

    let tagName = $"v{release.NugetVersion}"

    GitHub.createClientWithToken token
    |> GitHub.getReleaseByTag gitOwner gitName tagName
    |> GitHub.uploadFile file
    |> Async.RunSynchronously
