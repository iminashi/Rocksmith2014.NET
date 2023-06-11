module ProfileCleaner

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.PSARC
open Microsoft.Extensions.FileProviders
open Newtonsoft.Json.Linq
open System
open System.IO
open System.Reflection

[<Struct>]
type IdReadingProgress =
    { CurrentFilePath: string
      TotalFiles: int }

type IdData =
    { Ids: Set<string>
      Keys: Set<string> }

let private readEmbeddedFileLines (file: string) =
    let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    use embeddedFile = embeddedProvider.GetFileInfo(file).CreateReadStream()
    use reader = new StreamReader(embeddedFile)
    [ while not reader.EndOfStream do reader.ReadLine() ]

/// Reads the on-disc IDs and keys from the prepared text files.
let private readOnDiscIdsAndKeys () =
    {| OnDiscIds = Set.ofList (readEmbeddedFileLines "onDiscIDs.txt")
       OnDiscKeys = Set.ofList (readEmbeddedFileLines "onDiscKeys.txt") |}

/// Reads the IDs and keys from a PSARC with the given path.
let private readIDs (path: string) =
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
let private gatherDLCData (reportProgress: IdReadingProgress -> unit) (maxDegreeOfParallelism: int) (directory: string) =
    async {
        let! results =
            let searchPattern =
                let platform = if OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() then "m" else "p"
                $"*_{platform}.psarc"

            let files =
                Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories)
                |> Array.filter (fun path -> not (Path.GetFileNameWithoutExtension(path).Contains("rs1compatibility")))

            files
            |> Array.map (fun path ->
                async {
                    reportProgress
                        { CurrentFilePath = path
                          TotalFiles = files.Length }

                    return! readIDs path |> Async.AwaitTask
                })
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism)

        let ids, keys =
            results
            |> List.ofArray
            |> List.unzip

        return List.concat ids, List.concat keys
    }

/// Returns IDs and keys collected from PSARCs from the given directory together with on-disc IDs and keys.
let gatherIdAndKeyData (reportProgress: IdReadingProgress -> unit) (maxDegreeOfParallelism: int) (dlcDirectory: string) =
    async {
        let onDisc = readOnDiscIdsAndKeys()
        let! dlcIds, dlcKeys = gatherDLCData reportProgress maxDegreeOfParallelism dlcDirectory

        return
            { Ids = Set.union onDisc.OnDiscIds (Set.ofList dlcIds)
              Keys = Set.union onDisc.OnDiscKeys (Set.ofList dlcKeys) }
    }

/// Removes the children whose names are not in the IDs set from the JToken.
let private filterJTokenIds (ids: Set<string>) (token: JToken) =
    let filtered =
        token
        |> Seq.filter (fun x -> not <| ids.Contains((x :?> JProperty).Name))
        |> Seq.toArray

    filtered |> Array.iter (fun x -> x.Remove())
    filtered.Length

/// Removes the elements that are not in the keys set from the JArray.
let private filterJArrayKeys (keys: Set<string>) (array: JArray) =
    array
    |> Seq.filter (fun x -> not <| keys.Contains(x.Value<string>()))
    |> Seq.toArray
    |> Array.iter (array.Remove >> ignore)

/// Returns functions for filtering IDs and keys from the profile JSON.
let getFilteringFunctions (data: IdData) =
    let filterIds = filterJTokenIds data.Ids
    let filterKeys = filterJArrayKeys data.Keys

    filterIds, filterKeys

let backupProfile (profilePath: string) =
    File.Copy(profilePath, $"%s{profilePath}.backup", overwrite = true)
