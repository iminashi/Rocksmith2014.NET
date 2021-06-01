module DLCBuilder.OnlineUpdate

open Octokit
open System
open System.Net.Http
open System.IO
open System.IO.Compression
open System.Diagnostics

[<RequireQualifiedAccess>]
type AvailableUpdate =
    | Major
    | Minor
    | BugFix

type UpdateInformation =
    { AvailableUpdate : AvailableUpdate
      UpdateVersion : Version
      ReleaseDate : DateTimeOffset
      Changes : string
      AssetUrl : string }

let private isMac = OperatingSystem.IsMacOS()

/// Attempts to get the latest release from GitHub.
let private tryGetLatestRelease () = async {
    try
        let github = GitHubClient(ProductHeaderValue("rs2014-dlc-builder"))
        let! release = github.Repository.Release.GetLatest("iminashi", "Rocksmith2014.NET")
        return (Ok release)
    with e ->
        return (Error $"Getting latest release failed with: {e.Message}") }

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
    let platform = if isMac then "mac" else "win"
    release.Assets
    |> Seq.tryFind (fun ass ->
        ass.Name.Contains(platform, StringComparison.OrdinalIgnoreCase))

/// Returns the update information for the given release if it is newer than the current version.
let private getAvailableUpdateInformation (release: Release) =
    let latestVersion = Version(release.TagName.Substring 1)
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
let checkForUpdates () = async {
    let! release = tryGetLatestRelease()
    return release |> Result.map getAvailableUpdateInformation }

let private client = new HttpClient()

/// Downloads a file from the source URL to the target path.
let private downloadFile (targetPath: string) (sourceUrl: string) = async {
    let! response = client.GetAsync sourceUrl
    response.EnsureSuccessStatusCode() |> ignore
    use! stream = response.Content.ReadAsStreamAsync()
    use file = File.Create targetPath
    do! stream.CopyToAsync file }

/// Downloads the update asset, unzips it and returns the directory it was extracted to.
let downloadUpdate (targetPath: string) (update: UpdateInformation) = async {
    do! downloadFile targetPath update.AssetUrl

    let extractDir = Path.Combine(Path.GetDirectoryName targetPath, Guid.NewGuid().ToString())
    if Directory.Exists extractDir then
        Directory.Delete(extractDir, recursive=true)
    Directory.CreateDirectory extractDir |> ignore

    ZipFile.ExtractToDirectory(targetPath, extractDir)
    File.Delete targetPath

    return extractDir }

/// Downloads the update and starts the Updater process.
let downloadAndApplyUpdate (update: UpdateInformation) = async {
    let downloadPath = Path.Combine(Path.GetTempPath(), "dlc-builder-update.zip")
    let targetFolder = Path.GetDirectoryName(AppContext.BaseDirectory)

    let! updateFolder = downloadUpdate downloadPath update
    let updaterPath = Path.Combine(updateFolder, "Updater", "Updater")

    let startInfo = ProcessStartInfo(FileName = updaterPath, Arguments = $"\"{updateFolder}\" \"{targetFolder}\"")
    use updater = new Process(StartInfo = startInfo)
    updater.Start() |> ignore
    Environment.Exit 0 }
