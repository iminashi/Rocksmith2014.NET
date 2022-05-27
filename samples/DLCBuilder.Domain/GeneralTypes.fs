[<AutoOpen>]
module DLCBuilder.GeneralTypes

open Rocksmith2014.Common
open System

type Locale =
    { Name: string
      ShortName: string }

    override this.ToString() = this.Name
    static member Default = { Name = "English"; ShortName = "en" }

type AsyncReply(f: bool -> unit) =
    member _.Reply(answer) = f answer

type IBitmapLoader =
    abstract member InvalidateCache : unit -> unit
    abstract member TryLoad : string -> bool

type IStringLocalizer =
    abstract member Translate : string -> string
    abstract member TranslateFormat : string * obj array -> string
    abstract member ChangeLocale : Locale -> unit
    abstract member LocaleFromShortName : string -> Locale

type AudioConversionType =
    | ToWav
    | ToOgg

    member this.ToExtension = match this with ToWav -> "wav" | ToOgg -> "ogg"

type MoveDirection = Up | Down

type PsarcQuickEditData =
    { PsarcPath: string
      TempDirectory: string
      AppId: AppId option }

type BuildType =
    | Test
    | Release
    | PitchShifted
    | ReplacePsarc of PsarcQuickEditData

type PsarcImportType =
    | Normal of createdProjectFilePath: string
    | Quick of data: PsarcQuickEditData

type LoadedProjectOrigin =
    | FromFile of path: string
    | FromPsarcImport of importType: PsarcImportType
    | FromToolkitTemplateImport

    member this.ReloadTonesFromArrangementFiles =
        match this with
        | FromFile _ ->
            true
        | _ ->
            false

    member this.QuickEditData =
        match this with
        | FromPsarcImport (Quick info) ->
            Some info
        | _ ->
            None

    member this.ProjectPath =
        match this with
        | FromFile path
        | FromPsarcImport (Normal path) ->
            Some path
        | _ ->
            None

type ArrangementAddingError =
    | MaxInstrumentals
    | MaxShowlights
    | MaxVocals

type BuildValidationError =
    | InvalidDLCKey
    | TitleEmpty
    | ArtistNameEmpty
    | AlbumArtNotFound
    | PreviewNotFound
    | MultipleTonesSameKey
    | ConflictingVocals
    | MissingBaseToneKey
    | SamePersistentID

type DownloadId = { Id: Guid; LocString: string }
