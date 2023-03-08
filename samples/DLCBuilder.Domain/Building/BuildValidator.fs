module DLCBuilder.BuildValidator

open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open System.IO

let private toneIsUsedInArrangement project toneKey =
    project.Arrangements
    |> List.exists (function
        | Instrumental inst ->
            List.contains toneKey inst.AllTones
        | _ ->
            false)

let private validators =
    [
        fun project ->
            if project.DLCKey.Length < DLCKey.MinimumLength then Error InvalidDLCKey else Ok()

        fun project ->
            if String.IsNullOrEmpty(project.Title.Value) then Error TitleEmpty else Ok()

        fun project ->
            if String.IsNullOrEmpty(project.ArtistName.Value) then Error ArtistNameEmpty else Ok()

        fun project ->
            if File.Exists(project.AlbumArtFile) then Ok() else Error AlbumArtNotFound

        fun project ->
            if File.Exists(project.AudioPreviewFile.Path) then Ok() else Error PreviewNotFound

        fun project ->
            let isError =
                project.Arrangements
                |> List.choose Arrangement.pickInstrumental
                |> List.exists (fun inst -> String.IsNullOrWhiteSpace(inst.BaseTone))

            if isError then Error MissingBaseToneKey else Ok()

        fun project ->
            let allIds =
                project.Arrangements
                |> List.choose (function
                    | Instrumental i -> Some i.PersistentID
                    | Vocals v -> Some v.PersistentID
                    | _ -> None)

            let distinctIds = List.distinct allIds
            if distinctIds.Length <> allIds.Length then Error SamePersistentID else Ok()

        fun project ->
            let conflictingKey =
                project.Tones
                |> List.groupBy (fun x -> x.Key)
                |> List.tryFind (fun (key, list) ->
                    list.Length > 1
                    && String.notEmpty key
                    && toneIsUsedInArrangement project key)
                |> Option.map fst

            match conflictingKey with
            | Some key -> Error (MultipleTonesSameKey key)
            | None -> Ok()

        fun project ->
            let isError =
                project.Arrangements
                |> List.choose Arrangement.pickVocals
                |> List.groupBy (fun x -> x.Japanese)
                |> List.exists (fun (_, list) -> list.Length > 1)

            if isError then Error ConflictingVocals else Ok()
    ]

/// Validates the project for missing files and other errors.
let validate (project: DLCProject) =
    (Ok(), validators)
    ||> List.fold (fun state validator -> Result.bind (fun () -> validator project) state)
