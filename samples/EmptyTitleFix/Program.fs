open System
open System.IO
open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest

let makeManifestData manifest =
    let mem = MemoryStreamPool.Default.GetStream()
    async { do! Manifest.toJsonStream mem manifest } |> Async.RunSynchronously
    mem

let fixHsan entry =
    let headerData =
        async { return! Manifest.fromJsonStream entry.Data }
        |> Async.RunSynchronously

    headerData.Entries
    |> Map.iter (fun _ a -> a.Attributes.JapaneseSongName <- null)

    { entry with Data = makeManifestData headerData }

let fixJson entry =
    let attrs =
        async { return! Manifest.fromJsonStream entry.Data }
        |> Async.RunSynchronously
        |> Manifest.getSingletonAttributes

    attrs.JapaneseSongName <- null
    let newJson = Manifest.create [ attrs ]

    { entry with Data = makeManifestData newJson }

/// Fixes empty Japanese song names by setting the attribute to null in the manifests.
let fixManifests (psarcs: seq<PSARC>) =
    psarcs
    |> Seq.iter (fun psarc ->
        psarc.Edit(EditOptions.Default, (fun namedEntries ->
            let updatedManifests =
                namedEntries
                |> List.ofSeq
                |> List.map (fun entry ->
                    if entry.Name.EndsWith "hsan" then fixHsan entry
                    elif entry.Name.EndsWith "json" then fixJson entry
                    else entry)

            namedEntries.Clear()
            namedEntries.AddRange updatedManifests))
        |> Async.RunSynchronously
        (psarc :> IDisposable).Dispose()
    )

/// Returns the attributes of the first arrangement found.
let getAttributes (psarc: PSARC) = async {
    // Use the first (non-vocals) JSON file to determine if a fix is needed
    let jsonFile = psarc.Manifest |> Seq.find (fun x -> x.EndsWith "json" && not <| x.Contains "vocals")
    use mem = MemoryStreamPool.Default.GetStream()
    do! psarc.InflateFile(jsonFile, mem)
    let! mani = Manifest.fromJsonStream mem
    return Manifest.getSingletonAttributes mani }

/// Returns a sequence of PSARCs where the Japanese song name is an empty string.
let findFixablePsarcs directory =
    Directory.EnumerateFiles(directory, "*.psarc", SearchOption.AllDirectories)
    |> Seq.filter (fun x -> not <| x.Contains("inlay") && not <| x.Contains("rs1compatibility"))
    |> Seq.choose (fun path ->
        let psarc = PSARC.ReadFile path

        let attributes =
            getAttributes psarc
            |> Async.RunSynchronously

        if attributes.JapaneseSongName = String.Empty then
            printfn "Fixing %s" (Path.GetRelativePath(directory, path))
            Some psarc
        else
            (psarc :> IDisposable).Dispose()
            None)

[<EntryPoint>]
let main argv =
    if argv.Length <> 1 then
        Console.WriteLine "Give as argument a path to a directory that contains PSARC files that need fixing."
    else
        findFixablePsarcs argv.[0]
        |> fixManifests
    0
