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

let publishBuilder platform =
    dlcBuilderDir </> "DLCBuilder.fsproj"
    |> DotNet.publish (createConfig "dlcbuilder" platform false)

    // Return the target directory
    publishDir </> $"dlcbuilder-{platform}"

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
