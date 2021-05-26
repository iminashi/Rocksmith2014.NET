#r "nuget: Fake.Core.ReleaseNotes"
#r "nuget: Fake.Api.GitHub"
#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.IO.FileSystem"
#r "nuget: Fake.IO.Zip"

open Fake.Api
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open System
open System.IO

let gitOwner = "iminashi"
let gitName = "Rocksmith2014.NET"

let publishDir = Path.Combine(__SOURCE_DIRECTORY__, "publish")
let samplesDir = Path.Combine(__SOURCE_DIRECTORY__, "samples")
let dlcBuilderProject = Path.Combine(samplesDir, "DLCBuilder", "DLCBuilder.fsproj")
let msBuildParams = { MSBuild.CliArguments.Create() with Properties = [ "PublishSingleFile", "true" ] }

let cleanPublishDirectory () = Directory.Delete(publishDir, recursive = true)

let createConfig runTime directory (arg: DotNet.PublishOptions) =
    let targetDir = Path.Combine(publishDir, directory)

    { arg with OutputPath = Some targetDir
               Configuration = DotNet.BuildConfiguration.Release
               Runtime = Some runTime
               SelfContained = Some (runTime = "osx-x64")
               MSBuildParams = msBuildParams }

let publishUpdaterWin () =
    let project = Path.Combine(samplesDir, "Updater", "Updater.fsproj")

    DotNet.publish (createConfig "win-x64" "updater-win") project

let publishBuilderWin () =
    let targetDir = Path.Combine(publishDir, "dlcbuilder-win")

    DotNet.publish (createConfig "win-x64" "dlcbuilder-win") dlcBuilderProject

    // Copy the updater
    Directory.CreateDirectory(Path.Combine(targetDir, "Updater")) |> ignore
    File.Copy(Path.Combine(publishDir, "updater-win", "Updater.exe"),
              Path.Combine(targetDir, "Updater", "Updater.exe"), overwrite = true)

let publishBuilderMac () =
    let targetDir = Path.Combine(publishDir, "dlcbuilder-mac")

    DotNet.publish (createConfig "osx-x64" "dlcbuilder-mac") dlcBuilderProject

    // Copy the (temporary) setup instructions
    File.Copy(Path.Combine(samplesDir, "DLCBuilder", "MacSetup.txt"),
              Path.Combine(targetDir, "MacSetup.txt"), overwrite = true)

let release =
    Path.Combine(samplesDir, "DLCBuilder", "RELEASE_NOTES.md")
    |> ReleaseNotes.load

let createZipArchives () =
    [ "win"; "mac" ]
    |> List.map (fun platform ->
        let targetFile = Path.Combine(publishDir, $"DLCBuilder-{platform}-{release.NugetVersion}.zip")
        let dir = Path.Combine(publishDir, $"dlcbuilder-{platform}")
        !! (sprintf "%s/**" dir)
        |> Zip.zip dir targetFile
        targetFile)

let createGitHubRelease files =
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
    |> GitHub.uploadFiles files
    |> GitHub.publishDraft
    |> Async.RunSynchronously

cleanPublishDirectory()
publishUpdaterWin()
publishBuilderWin()
publishBuilderMac()

createZipArchives()
|> createGitHubRelease
