module ProfileCleaner

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.PSARC
open Microsoft.Extensions.FileProviders
open Newtonsoft.Json.Linq
open System.IO
open System.Reflection

[<Struct>]
type IdReadingProgress =
    { FileName: string
      Progress: float }

let private readFromAppDir (file: string) =
    let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    use embeddedFile = embeddedProvider.GetFileInfo(file).CreateReadStream()
    use reader = new StreamReader(embeddedFile)
    [ while not reader.EndOfStream do reader.ReadLine() ]

/// Reads the on-disc IDs and keys from the prepared text files.
let readOnDiscIdsAndKeys () =
    {| OnDiscIds = Set.ofList (readFromAppDir "onDiscIDs.txt")
       OnDiscKeys = Set.ofList (readFromAppDir "onDiscKeys.txt") |}

/// Reads the IDs and keys from a PSARC with the given path.
let readIDs (path: string) =
    backgroundTask {
        use psarc = PSARC.OpenFile(path)

        use! headerStream =
            psarc.Manifest
            |> List.find (String.endsWith "hsan")
            |> psarc.GetEntryStream

        let! manifest = Manifest.fromJsonStream headerStream

        let ids, songKeys =
            manifest.Entries
            |> Map.toList
            |> List.map (fun (id, entry) -> id, entry.Attributes.SongKey)
            |> List.unzip

        return ids, List.distinct songKeys
    }

/// Reads IDs and keys from psarcs in the given directory and its subdirectories.
let gatherDLCData (reportProgress: IdReadingProgress -> unit) (directory: string) =
    async {
        let! results =
            let files =
                Directory.EnumerateFiles(directory, "*.psarc", SearchOption.AllDirectories)
                |> Seq.filter (fun x -> not <| (x.Contains("inlay") || x.Contains("rs1compatibility")))
                |> Seq.toArray

            files
            |> Array.mapi (fun i path ->
                async {
                    reportProgress
                        { FileName = Path.GetRelativePath(directory, path)
                          Progress = float (i + 1) / float files.Length }

                    return! readIDs path |> Async.AwaitTask
                })
            |> Async.Sequential

        let ids, keys =
            results
            |> List.ofArray
            |> List.unzip

        return List.collect id ids, List.collect id keys
    }

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

let backupProfile (profilePath: string) =
    File.Copy(profilePath, $"%s{profilePath}.backup", overwrite = true)

//[<EntryPoint>]
//let main argv =
//    if argv.Length < 2 then
//        Console.WriteLine "Give as arguments: path to profile file and path to DLC directory."
//    else
//        backgroundTask {
//            let profilePath = argv[0]
//            let dlcDirectory = argv[1]
//            let isVerbose = Array.tryItem 2 argv = Some "-v"

//            Console.Clear()
//            let cursorVisibleOld = Console.CursorVisible
//            Console.CursorVisible <- false

//            let! ids, keys =
//                async {
//                    let odIds, odKeys = readOnDiscIdsAndKeys ()
//                    let! dlcIds, dlcKeys = gatherDLCData ignore isVerbose dlcDirectory
//                    return Set.union odIds (Set.ofList dlcIds), Set.union odKeys (Set.ofList dlcKeys)
//                }

//            let filterIds = filterJTokenIds ids
//            let filterKeys = filterJArrayKeys keys
//            let printStats section num =
//                printfn "%-9s: %i record%s removed" section num (if num = 1 then "" else "s")

//            printfn "Reading profile..."

//            let! profile, id = Profile.readAsJToken profilePath

//            printfn "Debloating profile..."

//            filterIds profile["Playnexts"].["Songs"] |> printStats "Playnexts"
//            filterIds profile["Songs"] |> printStats "Songs"
//            filterIds profile["SongsSA"] |> printStats "Songs SA"
//            filterIds profile["Stats"].["Songs"] |> printStats "Stats"

//            profile["SongListsRoot"]["SongLists"] :?> JArray
//            |> Seq.iter (fun songList -> songList :?> JArray |> filterKeys)

//            profile["FavoritesListRoot"]["FavoritesList"] :?> JArray
//            |> filterKeys

//            printfn "Saving profile file..."

//            backupProfile profilePath
//            printfn "Backup file created."

//            do! Profile.saveJToken profilePath id profile
//            printfn "Profile saved."
//            Console.CursorVisible <- cursorVisibleOld
//        }
//        |> fun t -> t.Wait()
//    0
