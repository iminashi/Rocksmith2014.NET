module DLCBuilder.RecentFilesList

open Rocksmith2014.Common
open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

let private recentFilePath =
    Path.Combine(Configuration.appDataFolder, "recent.json")

let private jsonOptions =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    options

/// Saves the recent files list into a file.
let save (recentList: string list) = async {
    Directory.CreateDirectory(Path.GetDirectoryName recentFilePath) |> ignore
    use file = File.Create recentFilePath
    do! JsonSerializer.SerializeAsync(file, recentList, jsonOptions) }

/// Updates the list with a new filename.
let update newFile oldList =
    let updatedList =
        let list = List.remove newFile oldList
        newFile::list
        |> List.truncate 3

    // Save the list if it changed
    if updatedList <> oldList then
        save updatedList |> Async.Start

    updatedList

/// Loads a recent files list from a file.
let load () =
    recentFilePath
    |> File.tryMap (fun path -> async {
        use file = File.OpenRead path
        return! JsonSerializer.DeserializeAsync<string list>(file, jsonOptions) } )
    |> Option.defaultValue (async { return [] })

