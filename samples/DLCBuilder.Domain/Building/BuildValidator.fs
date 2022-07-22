module DLCBuilder.BuildValidator

open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open System.IO

let private validators = [
    InvalidDLCKey,    fun project -> project.DLCKey.Length < DLCKey.MinimumLength
    TitleEmpty,       fun project -> String.IsNullOrEmpty project.Title.Value
    ArtistNameEmpty,  fun project -> String.IsNullOrEmpty project.ArtistName.Value
    AlbumArtNotFound, fun project -> not <| File.Exists project.AlbumArtFile
    PreviewNotFound,  fun project -> not <| File.Exists project.AudioPreviewFile.Path
    MissingBaseToneKey,
    fun project ->
        project.Arrangements
        |> List.choose Arrangement.pickInstrumental
        |> List.exists (fun inst -> String.IsNullOrWhiteSpace(inst.BaseTone))

    SamePersistentID,
    fun project ->
        let allIds =
            project.Arrangements
            |> List.choose (function
                | Instrumental i -> Some i.PersistentID
                | Vocals v -> Some v.PersistentID
                | _ -> None)
        let distinctIds = List.distinct allIds
        distinctIds.Length <> allIds.Length

    MultipleTonesSameKey,
    fun project ->
        project.Tones
        |> List.groupBy (fun x -> x.Key)
        |> List.exists (fun (_, list) -> list.Length > 1)

    ConflictingVocals,
    fun project ->
        project.Arrangements
        |> List.choose Arrangement.pickVocals
        |> List.groupBy (fun x -> x.Japanese)
        |> List.exists (fun (_, list) -> list.Length > 1) ]

/// Validates the project for missing files and other errors.
let validate (project: DLCProject) =
    (Ok(), validators)
    ||> List.fold (fun acc (error, isInvalid) ->
        acc
        |> Result.bind (fun () ->
            if isInvalid project then
                Error error
            else
                Ok()))
