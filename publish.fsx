#r "nuget: Fake.Core.ReleaseNotes"
#r "nuget: Fake.Api.GitHub"
#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.IO.FileSystem"
#r "nuget: Fake.IO.Zip"

open Fake.Api
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open System
open System.IO
open System.Runtime.InteropServices

type TargetPlatform =
    | Windows
    | MacOS
    | Linux
    override this.ToString() =
        match this with
        | Windows -> "win"
        | MacOS -> "mac"
        | Linux -> "linux"

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

    { MSBuild.CliArguments.Create() with
        Properties = properties
        // https://github.com/fsprojects/FAKE/issues/2744
        DisableInternalBinLog = true }

let release =
    Path.Combine(dlcBuilderDir, "RELEASE_NOTES.md")
    |> ReleaseNotes.load

let cleanPublishDirectory () =
    if Directory.Exists(publishDir) then
        Directory.Delete(publishDir, recursive=true)

let createConfig appName platform trim (arg: DotNet.PublishOptions) =
    let targetDir = publishDir </> $"{appName}-{platform}"
    let runtime =
        match platform with
        | Windows -> "win-x64"
        | MacOS -> "osx-x64"
        | Linux -> "linux-x64"

    { arg with OutputPath = Some targetDir
               Configuration = DotNet.BuildConfiguration.Release
               Runtime = Some runtime
               SelfContained = Some(runtime <> "win-x64")
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

let publishBuilder platform =
    dlcBuilderDir </> "DLCBuilder.fsproj"
    |> DotNet.publish (createConfig "dlcbuilder" platform false)

    // Return the target directory
    publishDir </> $"dlcbuilder-{platform}"

let createZipArchive platform =
    let targetFile = publishDir </> $"DLCBuilder-{platform}-{release.NugetVersion}.zip"
    let dirToZip =
        match platform with
        | Windows ->
            "dlcbuilder-win"
        | MacOS ->
            "DLC Builder.app"
        | Linux ->
            "dlcbuilder-linux"

    if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
        let dirToZip = publishDir </> dirToZip
        let workingDir = dirToZip
        !! $"{dirToZip}/**" |> Zip.zip workingDir targetFile
    else
        // Using FAKE's zip loses the executable permissions
        zip publishDir targetFile dirToZip

    targetFile

let getGitHubToken () =
    match Environment.GetEnvironmentVariable("github_token") with
    | null -> failwith "github_token environment variable is not set."
    | s -> s

let createGitHubRelease file =
    let token = getGitHubToken ()
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
    let token = getGitHubToken ()
    let tagName = $"v{release.NugetVersion}"

    GitHub.createClientWithToken token
    |> GitHub.getReleaseByTag gitOwner gitName tagName
    |> GitHub.uploadFile file
    |> Async.RunSynchronously
