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

type IExitHandler =
    abstract member Exit : unit -> unit

type AudioConversionType =
    | ToWav
    | ToOgg

type MoveDirection = Up | Down

type PsarcQuickEditData =
    { PsarcPath: string
      TempDirectory: string
      AppId: AppId option
      BuildToolVersion: string option }

type PsarcNormalImportData =
    { ProjectFilePath: string
      BuildToolVersion: string option }

type BuildType =
    | Test
    | Release
    | PitchShifted
    | ReplacePsarc of PsarcQuickEditData

type PsarcImportType =
    | Normal of data: PsarcNormalImportData
    | Quick of data: PsarcQuickEditData

type LoadedProjectOrigin =
    | FromFile of path: string
    | FromPsarcImport of importType: PsarcImportType
    | FromToolkitTemplateImport

    member this.ShouldReloadTonesFromArrangementFiles =
        match this with
        | FromFile _ ->
            true
        | _ ->
            false

    member this.QuickEditData =
        match this with
        | FromPsarcImport (Quick data) ->
            Some data
        | _ ->
            None

    member this.ProjectPath =
        match this with
        | FromFile path
        | FromPsarcImport (Normal { ProjectFilePath = path }) ->
            Some path
        | _ ->
            None

    member this.BuildToolVersion =
        match this with
        | FromPsarcImport (Quick { BuildToolVersion = v } | Normal { BuildToolVersion = v }) ->
            v
        | _ ->
            None

type ArrangementAddingError =
    | MaxInstrumentals
    | MaxShowlights
    | MaxVocals

type BuildValidationError =
    | NoArrangements
    | MainAudioFileNotSet
    | MainAudioFileNotFound
    | InvalidDLCKey
    | TitleEmpty
    | ArtistNameEmpty
    | AlbumArtNotFound
    | MultipleTonesSameKey of conflictingKey: string
    | ConflictingVocals
    | MissingBaseToneKey
    | SamePersistentID

type DownloadId = { Id: Guid; LocString: string }
