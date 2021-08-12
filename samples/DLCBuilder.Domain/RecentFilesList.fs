module DLCBuilder.RecentFilesList

open Rocksmith2014.Common
open System
open System.IO
open System.Text.Json

let [<Literal>] private MaxFiles = 5

let private recentFilePath =
    Path.Combine(Configuration.appDataFolder, "recent.json")

/// Saves the recent files list into a file.
let save (recentList: string list) = async {
    try
        use file = File.Create recentFilePath
        do! JsonSerializer.SerializeAsync(file, recentList, FSharpJsonOptions.Create())
    with ex ->
        Console.WriteLine ex.Message }

/// Updates the list with a new filename.
let update newFile oldList =
    let updatedList =
        let list = List.remove newFile oldList
        newFile::list
        |> List.truncate MaxFiles

    updatedList

/// Loads a recent files list from a file.
let load () =
    recentFilePath
    |> File.tryMap (fun path -> async {
        use file = File.OpenRead path
        let! recent = JsonSerializer.DeserializeAsync<string list>(file, FSharpJsonOptions.Create())
        return List.filter File.Exists recent } )
    |> Option.defaultValue (async { return List.empty })
