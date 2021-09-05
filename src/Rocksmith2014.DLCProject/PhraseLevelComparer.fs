module Rocksmith2014.DLCProject.PhraseLevelComparer

open Rocksmith2014.SNG
open System
open System.Collections.Generic
open System.IO
open System.Text.Json

type PhraseName = string

type ProjectLevels = IReadOnlyDictionary<Guid, IReadOnlyDictionary<PhraseName, int>>

let [<Literal>] private PhraseLevelFile = ".phrase-levels"

let private tryGetStoredLevels directory =
    let levelsFile = Path.Combine(directory, PhraseLevelFile)
    if File.Exists levelsFile then
        try
            let text = File.ReadAllText levelsFile
            Some <| JsonSerializer.Deserialize<ProjectLevels> text
        with _ ->
            None
    else
        None

let private savePhraseLevels directory (phraseLevels: ProjectLevels) =
    let levelsFile = Path.Combine(directory, PhraseLevelFile)
    let text = JsonSerializer.Serialize phraseLevels
    File.WriteAllText(levelsFile, text)

let private createLevelDictionary (arrangements: (Arrangement * SNG) list) : ProjectLevels =
    arrangements
    |> List.choose (fun (arr, sng) ->
        Arrangement.pickInstrumental arr
        |> Option.map (fun inst -> inst, sng))
    |> List.map (fun (inst, sng) ->
        let phraseLevels =
            sng.Phrases
            |> Array.map (fun x -> x.Name, x.MaxDifficulty)
            |> readOnlyDict
        inst.PersistentID, phraseLevels)
    |> readOnlyDict

/// Compares the level counts of the arrangements to the stored level counts.
let compareLevels (stored: ProjectLevels) (arrangements: (Arrangement * SNG) list) =
    arrangements
    |> List.choose (function
        | Instrumental inst, sng ->
            option {
                let! storedLevels = Dictionary.tryGetValue inst.PersistentID stored
                if sng.Phrases |> Array.exists (fun phrase ->
                    storedLevels
                    |> Dictionary.tryGetValue phrase.Name
                    |> Option.exists (fun storedMaxDiff -> storedMaxDiff > phrase.MaxDifficulty))
                then
                    return inst.PersistentID }
        | _ ->
            None)

/// Returns a list of persistent IDs of the arrangements whose IDs should be regenerated.
let compareToExisting directory (arrangements: (Arrangement * SNG) list) =
    tryGetStoredLevels directory
    |> Option.map (fun stored -> compareLevels stored arrangements)
    |> Option.defaultValue List.empty

/// Saves the arrangement levels to a file in the given directory.
let saveLevels directory (arrangements: (Arrangement * SNG) list) =
    createLevelDictionary arrangements
    |> savePhraseLevels directory
