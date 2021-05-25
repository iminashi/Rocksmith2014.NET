module DLCBuilder.OnlineUpdate

open Octokit
open System
open System.Net.Http
open System.IO
open System.IO.Compression

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

let private tryGetLatestRelease () = async {
    try
        let github = GitHubClient(ProductHeaderValue("rs2014-dlc-builder"))
        let! release = github.Repository.Release.GetLatest("iminashi", "Rocksmith2014.NET")
        return (Some release)
    with e ->
        Console.WriteLine $"Getting latest release failed with: {e.Message}"
        return None }

let private getAvailableUpdateInformation (release: Release) =
    let latestVersion = Version(release.TagName.Substring 1)
    let currentVersion = AppVersion.current

    let availableUpdate =
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

    let asset =
        release.Assets
        |> Seq.tryFind (fun ass -> ass.Name.Contains("win", StringComparison.OrdinalIgnoreCase))

    match availableUpdate, asset with
    | Some update, Some asset ->
        { AvailableUpdate = update
          UpdateVersion = latestVersion
          ReleaseDate = release.CreatedAt
          Changes = release.Body
          AssetUrl = asset.BrowserDownloadUrl }
        |> Some
    | _ ->
        None

let checkForUpdates () = async {
    let! release = tryGetLatestRelease()
    return release |> Option.bind getAvailableUpdateInformation }

let private client = new HttpClient()

let private downloadFile (targetPath: string) (sourceUrl: string) = async {
    let! response = client.GetAsync sourceUrl
    response.EnsureSuccessStatusCode() |> ignore
    use! stream = response.Content.ReadAsStreamAsync()
    use file = File.Create targetPath
    do! stream.CopyToAsync file }

let downloadUpdate (targetPath: string) (update: UpdateInformation) = async {
    do! downloadFile targetPath update.AssetUrl

    let extractDir = Path.Combine(Path.GetDirectoryName targetPath, "update")
    if Directory.Exists extractDir then
        Directory.Delete(extractDir, recursive=true)
    Directory.CreateDirectory extractDir |> ignore

    ZipFile.ExtractToDirectory(targetPath, extractDir)
    return extractDir }
