namespace DLCBuilder

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Avalonia.Controls
open Avalonia.Media.Imaging
open System

[<AutoOpen>]
module Types =
    type OverlayContents =
    | NoOverlay
    | ErrorMessage of message : string
    | ImportToneSelector of tones : Tone array
    | SelectPreviewStart of audioLength : TimeSpan
    | ConfigEditor

    type PreviewAudioCreation =
    | SetupStartTime
    | CreateFile
    | FileCreated of path : string

    type MoveDirection = Up | Down

    type State =
        { Project : DLCProject
          SavedProject : DLCProject
          Config : Configuration
          CoverArt : Bitmap
          SelectedArrangement : Arrangement option
          SelectedTone : Tone option
          ShowSortFields : bool
          ShowJapaneseFields : bool
          Overlay : OverlayContents
          ImportTones : Tone list
          PreviewStartTime : TimeSpan
          OpenProjectFile : string option
          CurrentPlatform : Platform
          BuildInProgress : bool
          Localization : ILocalization }

    type Msg =
    | OpenFileDialog of locTitle : string * filter : (ILocalization -> ResizeArray<FileDialogFilter>) * msg : (string -> Msg)
    | OpenFolderDialog of locTitle : string * msg : (string -> Msg)
    | ConditionalCmdDispatch of opt : string option * msg : (string -> Msg)
    | SelectOpenArrangement
    | ProjectSaveAs
    | SelectImportPsarcFolder of psarcFile : string
    | ImportPsarc of psarcFile : string * targetFolder : string option
    | ImportToolkitTemplate of fileName : string
    | OpenProject of fileName : string
    | SaveProject of fileName : string option
    | AddArrangements of files : string[] option
    | AddCoverArt of fileName : string
    | AddAudioFile of fileName : string
    | AddCustomFontFile of fileName : string
    | AddProfilePath of path : string
    | AddTestFolderPath of path : string
    | AddProjectsFolderPath of path : string
    | ImportTonesFromFile of fileName : string
    | ArrangementSelected of selected : Arrangement option
    | ToneSelected of selected : Tone option
    | DeleteArrangement
    | DeleteTone
    | MoveTone of MoveDirection
    | ImportProfileTones
    | CreatePreviewAudio of PreviewAudioCreation
    | PreviewAudioStartChanged of time : float
    | ShowSortFields of shown : bool
    | ShowJapaneseFields of shown : bool
    | EditInstrumental of edit : (State -> Instrumental -> Instrumental)
    | EditVocals of edit : (Vocals -> Vocals)
    | EditTone of edit : (Tone -> Tone)
    | EditProject of edit : (DLCProject -> DLCProject)
    | EditConfig of edit : (Configuration -> Configuration)
    | CloseOverlay
    | ImportTonesChanged of item : obj
    | ImportSelectedTones
    | ShowConfigEditor
    | ProjectSaveOrSaveAs
    | SaveConfiguration
    | SetConfiguration of config : Configuration
    | ProjectLoaded of project : DLCProject * projectFile : string option
    | BuildTest
    | BuildRelease
    | BuildComplete of unit
    | ConvertToWem
    | ShowImportToneSelector of tones : Tone array
    | ChangeLocale of locale : Locale
    | ErrorOccurred of e : exn
