module DLCBuilder.BuildValidator

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System.IO

/// Validates the project for missing files and other errors.
let validate (project: DLCProject) =
    let toneKeyGroups =
        project.Tones
        |> List.groupBy (fun x -> x.Key)

    let vocalGroups =
        project.Arrangements
        |> List.choose (function Vocals v -> Some v | _ -> None)
        |> List.groupBy (fun x -> x.Japanese)

    if not <| File.Exists project.AlbumArtFile then
        Error "albumArtNotFound"
    elif toneKeyGroups |> List.exists (fun (_, list) -> list.Length > 1) then
        Error "multipleTonesSameKey"
    elif vocalGroups |> List.exists (fun (_, list) -> list.Length > 1) then
        Error "conflictingVocals"
    elif not <| File.Exists project.AudioPreviewFile.Path then
        Error "previewNotFound"
    else
        Ok ()
