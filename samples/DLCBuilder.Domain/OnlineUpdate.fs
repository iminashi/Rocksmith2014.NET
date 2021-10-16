module DLCBuilder.OnlineUpdate

open Octokit
open System
open System.Diagnostics

[<RequireQualifiedAccess>]
type AvailableUpdate =
    | Major
    | Minor
    | BugFix

type UpdateInformation =
    { AvailableUpdate: AvailableUpdate
      UpdateVersion: Version
      ReleaseDate: DateTimeOffset
      Changes: string
      AssetUrl: string }

/// Attempts to get the latest release from GitHub.
let private tryGetLatestRelease () =
    async {
        try
            let github = GitHubClient(ProductHeaderValue("rs2014-dlc-builder"))
            let! release = github.Repository.Release.GetLatest("iminashi", "Rocksmith2014.NET")
            return Ok release
        with e ->
            return Error $"Getting latest release failed with: {e.Message}"
    }

let private getAvailableUpdate (latestVersion: Version) =
    let currentVersion = AppVersion.current

    if latestVersion.Major > currentVersion.Major then
        Some AvailableUpdate.Major
    elif latestVersion.Major = currentVersion.Major
         && latestVersion.Minor > currentVersion.Minor
    then
        Some AvailableUpdate.Minor
    elif latestVersion.Major = currentVersion.Major
         && latestVersion.Minor = currentVersion.Minor
         && latestVersion.Build > currentVersion.Build
    then
        Some AvailableUpdate.BugFix
    else
        None

let private tryGetReleaseAsset (release: Release) =
    let platform =
        PlatformSpecific.Value(mac="mac", windows="win", linux="linux")

    release.Assets
    |> Seq.tryFind (fun ass -> ass.Name |> String.containsIgnoreCase platform)

/// Returns the update information for the given release if it is newer than the current version.
let private getAvailableUpdateInformation (release: Release) =
    let latestVersion = Version(release.TagName.Substring(1))
    let availableUpdate = getAvailableUpdate latestVersion
    let asset = tryGetReleaseAsset release

    (availableUpdate, asset)
    ||> Option.map2 (fun update asset ->
        { AvailableUpdate = update
          UpdateVersion = latestVersion
          ReleaseDate = release.CreatedAt
          Changes = release.Body
          AssetUrl = asset.BrowserDownloadUrl })

/// Fetches the latest release and returns the information for the available update.
let checkForUpdates () =
    async {
        let! release = tryGetLatestRelease ()
        return release |> Result.map getAvailableUpdateInformation
    }

/// Starts the installer process from the given path.
let applyUpdate updatePath =
    let startInfo = ProcessStartInfo(FileName = updatePath, Arguments = $"/SILENT /CLOSEAPPLICATIONS")
    use update = new Process(StartInfo = startInfo)
    update.Start() |> ignore

    Environment.Exit(0)
