module Rocksmith2014.DLCProject.Partitioner

open Rocksmith2014.DLCProject
open System

let create (project: DLCProject) =
    let groups =
        project.Arrangements
        |> List.choose (function Instrumental i -> Some i | _ -> None)
        |> List.groupBy (fun a -> a.ArrangementName)
        |> Map.ofList

    fun (arrangement: Arrangement) ->
        match arrangement with
        | Vocals v when v.Japanese -> 1, "jvocals"
        | Vocals _ -> 1, "vocals"
        | Showlights _ -> 1, "showlights"
        | Instrumental inst ->
            let name = inst.ArrangementName.ToString().ToLowerInvariant()
            let group = groups.[inst.ArrangementName]
            let part = 1 + (group |> List.findIndex (fun x -> Object.ReferenceEquals(x, inst)))
            if part = 1 then
                part, name
            else
                part, sprintf "%s%i" name part
