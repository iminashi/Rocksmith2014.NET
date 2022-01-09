module DLCBuilder.ArrangementNameUtils

open Rocksmith2014.DLCProject
open System

type ArrangementNameExtraInfo =
    | NameOnly
    | WithExtra

let private getArrangementNumber arr project =
    match arr with
    | Instrumental inst ->
        let groups =
            project.Arrangements
            |> List.choose Arrangement.pickInstrumental
            |> List.groupBy (fun a -> a.Priority, a.Name)
            |> Map.ofList

        let group = groups[inst.Priority, inst.Name]

        if group.Length > 1 then
            let index =
                group
                |> List.findIndex (fun x -> x.PersistentID = inst.PersistentID)

            sprintf " %i" (1 + index)
        else
            String.Empty
    | _ ->
        String.Empty

/// Returns the translated name for the arrangement.
let translateName project info arr =
    match arr with
    | Instrumental inst ->
        let baseName =
            let n, p = Arrangement.getNameAndPrefix arr

            if p.Length > 0 then
                $"{translate p} {translate n}"
            else
                translate n

        let arrNumber = getArrangementNumber arr project
        let baseName = $"{baseName}{arrNumber}"

        match info with
        | NameOnly ->
            baseName
        | WithExtra ->
            let extra =
                match inst with
                | { Name = ArrangementName.Combo } ->
                    let c = translate "ComboArr" in $" ({c})"
                | { RouteMask = RouteMask.Bass; BassPicked = true } ->
                    let p = translate "Picked" in $" ({p})"
                | _ ->
                    String.Empty

            $"{baseName}{extra}"
    | _ ->
        Arrangement.getNameAndPrefix arr
        |> fst
        |> translate
