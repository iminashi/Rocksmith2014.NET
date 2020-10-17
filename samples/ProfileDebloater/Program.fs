open System
open System.IO
open Rocksmith2014.Common
open Rocksmith2014.PSARC
open Rocksmith2014.DLCProject.Manifest
open Newtonsoft.Json
open Newtonsoft.Json.Linq

let readFromAppDir file =
    File.ReadAllLines (Path.Combine(AppContext.BaseDirectory, file))

/// Reads the on-disc IDs and keys from the prepared text files.
let readOnDiscIdsAndKeys () =
    Set.ofArray (readFromAppDir "onDiscIDs.txt"),
    Set.ofArray (readFromAppDir "onDiscKeys.txt")
    
/// Reads IDs and keys from psarcs in the given directory and its subdirectories.
let gatherDLCData (directory: string) =
    let ids, keys =
        Directory.EnumerateFiles(directory, "*.psarc", SearchOption.AllDirectories)
        |> Seq.filter (fun x -> not <| x.Contains("inlay") && not <| x.Contains("rs1compatibility"))
        |> Seq.map (fun path ->
            printfn "Reading IDs from %s" (Path.GetRelativePath(directory, path))

            use psarc = PSARC.ReadFile path
            let headerFile = psarc.Manifest |> Seq.find (fun x -> x.EndsWith "hsan")
            use mem = MemoryStreamPool.Default.GetStream()
            psarc.InflateFile(headerFile, mem) |> Async.RunSynchronously
            let manifest = async { return! Manifest.fromJsonStream mem } |> Async.RunSynchronously

            let ids, songKeys =
                manifest.Entries
                |> Map.toList
                |> List.map (fun (id, entry) -> id, entry.Attributes.SongKey)
                |> List.unzip
            ids, List.distinct songKeys)
        |> Seq.toList
        |> List.unzip
    List.collect id ids, List.collect id keys

/// Saves the profile data, backing up the existing profile file.
let saveProfile (originalPath: string) id (profile: JToken) =
    use json = MemoryStreamPool.Default.GetStream()
    use streamWriter = new StreamWriter(json, NewLine = "\n")
    use writer = new JsonTextWriter(streamWriter,
                                    Formatting = Formatting.Indented,
                                    Indentation = 0,
                                    StringEscapeHandling = StringEscapeHandling.EscapeNonAscii)

    profile.WriteTo writer
    writer.Flush()

    let backUp = originalPath + ".backup"
    if File.Exists backUp then File.Delete backUp
    File.Copy(originalPath, backUp)

    Profile.write originalPath id json |> Async.RunSynchronously

/// Reads a profile from the given path.
let readProfile path =
    use profileFile = File.OpenRead path
    use mem = MemoryStreamPool.Default.GetStream()
    let _, id, _ = Profile.decrypt profileFile mem |> Async.RunSynchronously

    mem.Position <- 0L
    use textReader = new StreamReader(mem)
    use reader = new JsonTextReader(textReader)

    JToken.ReadFrom reader, id

/// Removes the children whose names are not in the IDs set from the JToken.
let filterJTokenIds (ids: Set<string>) (token: JToken) =
    let filtered =
        token
        |> Seq.filter (fun x -> not <| ids.Contains((x :?> JProperty).Name))
        |> Seq.toArray
    filtered |> Array.iter (fun x -> x.Remove())
    filtered.Length

/// Removes the elements that are not in the keys set from the JArray.
let filterJArrayKeys (keys: Set<string>) (array: JArray) =
    array
    |> Seq.filter (fun x -> not <| keys.Contains(x.Value<string>()))
    |> Seq.toArray
    |> Array.iter (array.Remove >> ignore)

[<EntryPoint>]
let main argv =
    if argv.Length <> 2 then
        Console.WriteLine "Give as arguments: path to profile file and path to DLC directory."
    else
        let profilePath = argv.[0]
        let dlcDirectory = argv.[1]

        let ids, keys =
            let odIds, odKeys = readOnDiscIdsAndKeys()
            let dlcIds, dlcKeys = gatherDLCData dlcDirectory
            Set.union odIds (Set.ofList dlcIds), 
            Set.union odKeys (Set.ofList dlcKeys)

        let filterIds = filterJTokenIds ids
        let filterKeys = filterJArrayKeys keys
        let printstats section num = printfn "%-9s: %i records removed" section num

        Console.WriteLine "Reading profile..."

        let profile, id = readProfile profilePath

        Console.WriteLine "Debloating profile..."

        filterIds profile.["Playnexts"].["Songs"] |> printstats "Playnexts"
        filterIds profile.["Songs"] |> printstats "Songs"
        filterIds profile.["SongsSA"] |> printstats "Songs SA"
        filterIds profile.["Stats"].["Songs"] |> printstats "Stats"

        profile.["SongListsRoot"].["SongLists"] :?> JArray
        |> Seq.iter (fun songList -> songList :?> JArray |> filterKeys) 

        profile.["FavoritesListRoot"].["FavoritesList"] :?> JArray
        |> filterKeys

        Console.WriteLine "Saving profile file..."

        saveProfile profilePath id profile

    0
