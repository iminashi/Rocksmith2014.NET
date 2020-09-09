open System
open System.IO
open Rocksmith2014.Common
open Rocksmith2014.PSARC
open Rocksmith2014.DLCProject.Manifest
open Newtonsoft.Json
open Newtonsoft.Json.Linq

let readOnDiscIdsAndKeys () =
    Set.ofArray (File.ReadAllLines "onDiscIDs.txt"),
    Set.ofArray (File.ReadAllLines "onDiscKeys.txt")
    
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

let saveProfile (originalPath: string) id (profile: JToken) =
    use json = MemoryStreamPool.Default.GetStream()
    use streamWriter = new StreamWriter(json, NewLine = "\n")
    use writer = new JsonTextWriter(streamWriter,
                                    Formatting = Formatting.Indented,
                                    Indentation = 0,
                                    StringEscapeHandling = StringEscapeHandling.EscapeNonAscii)

    profile.WriteTo writer
    writer.Flush()

    Profile.write (originalPath + ".processed") id json |> Async.RunSynchronously

let readProfile path =
    use profileFile = File.OpenRead path
    use mem = MemoryStreamPool.Default.GetStream()
    let _, id, _ = Profile.decrypt profileFile mem |> Async.RunSynchronously

    mem.Position <- 0L
    use textReader = new StreamReader(mem)
    use reader = new JsonTextReader(textReader)

    JToken.ReadFrom reader, id

[<EntryPoint>]
let main argv =
    if argv.Length <> 2 then
        Console.WriteLine "Give as arguments: path to profile file and path to DLC directory."
        0
    else
        let profilePath = argv.[0]
        let dlcDirectory = argv.[1]

        let ids, keys =
            let odIds, odKeys = readOnDiscIdsAndKeys()
            let dlcIds, dlcKeys = gatherDLCData dlcDirectory
            Set.union odIds (Set.ofList dlcIds), 
            Set.union odKeys (Set.ofList dlcKeys)

        Console.WriteLine "Reading profile..."

        let profile, id = readProfile profilePath

        let filterJTokenIds (token: JToken) =
            token
            |> Seq.filter (fun x -> not <| ids.Contains((x :?> JProperty).Name))
            |> Seq.toArray
            |> Array.iter (fun x -> x.Remove())

        let filterJArrayKeys (array: JArray) =
            array
            |> Seq.filter (fun x -> not <| keys.Contains(x.Value<string>()))
            |> Seq.toArray
            |> Array.iter (array.Remove >> ignore)

        Console.WriteLine "Debloating profile..."

        filterJTokenIds (profile.["Playnexts"].["Songs"])
        filterJTokenIds (profile.["Songs"])
        filterJTokenIds (profile.["Stats"].["Songs"])

        profile.["SongListsRoot"].["SongLists"] :?> JArray
        |> Seq.iter (fun songList -> songList :?> JArray |> filterJArrayKeys) 

        profile.["FavoritesListRoot"].["FavoritesList"] :?> JArray
        |> filterJArrayKeys

        Console.WriteLine "Saving profile file..."

        saveProfile profilePath id profile

        0
