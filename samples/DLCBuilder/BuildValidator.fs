module DLCBuilder.BuildValidator

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System.IO

let private validators = [
    InvalidDLCKey,    fun project -> project.DLCKey.Length < DLCKey.MinimumLength
    TitleEmpty,       fun project -> SortableString.IsEmpty project.Title
    ArtistNameEmpty,  fun project -> SortableString.IsEmpty project.ArtistName
    AlbumArtNotFound, fun project -> not <| File.Exists project.AlbumArtFile
    PreviewNotFound,  fun project -> not <| File.Exists project.AudioPreviewFile.Path
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
        |> Result.bind (fun () -> if isInvalid project then Error error else Ok()))
