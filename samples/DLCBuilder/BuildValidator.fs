module DLCBuilder.BuildValidator

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System.IO

/// Validates the project for missing files and other errors.
let validate (loc: Localization) (project: DLCProject) =
    let wemAudio = Path.ChangeExtension(project.AudioFile.Path, "wem")
    let wemPreview =
        if String.notEmpty project.AudioPreviewFile.Path then
            Path.ChangeExtension(project.AudioFile.Path, "wem")
        else
            let dir = Path.GetDirectoryName project.AudioFile.Path
            let fn = Path.GetFileNameWithoutExtension project.AudioFile.Path
            Path.Combine(dir, sprintf "%s_preview.wem" fn)

    let toneKeyGroups =
        project.Tones
        |> List.groupBy (fun x -> x.Key)

    let vocalGroups =
        project.Arrangements
        |> List.choose (function Vocals v -> Some v | _ -> None)
        |> List.groupBy (fun x -> x.Japanese)

    if not <| File.Exists project.AlbumArtFile then
        Error (loc.GetString "albumArtNotFound")
    elif toneKeyGroups |> List.exists (fun (_, list) -> list.Length > 1)  then
        Error (loc.GetString "multipleTonesSameKey")
    elif vocalGroups |> List.exists (fun (_, list) -> list.Length > 1) then
        Error (loc.GetString "conflictingVocals")
    elif not <| File.Exists wemAudio then
        Error (loc.GetString "wemAudioNotFound")
    elif not <| File.Exists wemPreview then
        Error (loc.GetString "wemPreviewNotFound")
    else
        Ok ()
