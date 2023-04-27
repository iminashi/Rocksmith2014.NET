module DLCBuilder.RecentFilesList

open Rocksmith2014.Common
open System
open System.IO
open System.Text.Json

let [<Literal>] private MaxFiles = 5

let private recentFilePath =
    Path.Combine(Configuration.appDataFolder, "recent.json")

/// Saves the recent files list into a file.
let save (recentList: string list) =
    backgroundTask {
        try
            use file = File.Create(recentFilePath)
            do! JsonSerializer.SerializeAsync(file, recentList, FSharpJsonOptions.Create())
        with ex ->
            Console.WriteLine(ex.Message)
    }

/// Updates the list with a new filename.
let update newFile oldList =
    let updatedList =
        let list = List.remove newFile oldList
        newFile :: list |> List.truncate MaxFiles

    updatedList

/// Loads a recent files list from a file.
let load () =
    backgroundTask {
        if File.Exists(recentFilePath) then
            use file = File.OpenRead(recentFilePath)
            let! recent = JsonSerializer.DeserializeAsync<string list>(file, FSharpJsonOptions.Create())
            return List.filter File.Exists recent
        else
            return List.Empty
    }
