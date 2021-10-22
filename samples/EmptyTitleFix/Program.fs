open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.PSARC
open System
open System.IO

/// Serializes the manifest data into a memory stream.
let makeManifestData manifest =
    async {
        let mem = MemoryStreamPool.Default.GetStream()
        do! Manifest.toJsonStream mem manifest
        return mem
    }

/// Fixes a manifest entry by setting the JapaneseSongName attribute to null.
let fixManifest entry =
    async {
        let! manifest = Manifest.fromJsonStream entry.Data

        manifest.Entries
        |> Map.iter (fun _ a -> a.Attributes.JapaneseSongName <- null)

        let! data = makeManifestData manifest
        return { entry with Data = data }
    }

/// Calls fixManifest on the manifest entries.
let mapEntry (entry: NamedEntry) =
    match entry with
    | { Name = HasExtension (".hsan" | ".json") } ->
        fixManifest entry
        |> Async.RunSynchronously
    | _ ->
        entry

/// Fixes empty Japanese song names by setting the attribute to null in the manifests.
let fixManifests (psarcs: seq<PSARC>) =
    psarcs
    |> Seq.map (fun psarc ->
        async {
            do! psarc.Edit(EditOptions.Default, List.map mapEntry)
            (psarc :> IDisposable).Dispose()
        })

/// Returns the attributes of the first arrangement found.
let getAttributes (psarc: PSARC) =
    async {
        // Use the first (non-vocals) JSON file to determine if a fix is needed
        let jsonFile =
            psarc.Manifest
            |> List.find (fun x -> x.EndsWith("json") && not <| x.Contains("vocals"))

        use! stream = psarc.GetEntryStream(jsonFile)
        let! mani = Manifest.fromJsonStream stream
        return Manifest.getSingletonAttributes mani
    }

/// Returns a sequence of PSARCs where the Japanese song name is an empty string.
let findFixablePsarcs directory =
    Directory.EnumerateFiles(directory, "*.psarc", SearchOption.AllDirectories)
    |> Seq.filter (fun x -> not <| x.Contains("inlay") && not <| x.Contains("rs1compatibility"))
    |> Seq.map (fun path ->
        async {
            let psarc = PSARC.ReadFile(path)

            let! attributes = getAttributes psarc

            if attributes.JapaneseSongName = String.Empty then
                printfn "Fixing %s" (Path.GetRelativePath(directory, path))
                return Some psarc
            else
                (psarc :> IDisposable).Dispose()
                return None
        })

[<EntryPoint>]
let main argv =
    if argv.Length <> 1 then
        Console.WriteLine "Give as argument a path to a directory that contains PSARC files that need fixing."
    else
        async {
            let! psarcs =
                findFixablePsarcs argv.[0]
                |> Async.Sequential

            do! psarcs
                |> Array.choose id
                |> fixManifests
                |> Async.Sequential
                |> Async.Ignore
        }
        |> Async.RunSynchronously
    0
