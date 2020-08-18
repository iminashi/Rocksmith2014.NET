namespace DLCBuilder

open Rocksmith2014.DLCProject
open Rocksmith2014.Common.Manifest
open Avalonia.Media.Imaging
open System

[<AutoOpen>]
module Types =
    type OverlayContents =
    | NoOverlay
    | ErrorMessage of message : string
    | SelectImportTones of tones : Tone array
    | SelectPreviewStart of audioLength : TimeSpan
    | ConfigEditor

    type PreviewAudioCreation =
    | SetupStartTime
    | CreateFile
    | FileCreated of path : string

    type State =
        { Project : DLCProject
          Config : Configuration
          CoverArt : Bitmap option
          SelectedArrangement : Arrangement option
          SelectedTone : Tone option
          ShowSortFields : bool
          ShowJapaneseFields : bool
          Overlay : OverlayContents
          ImportTones : Tone list
          PreviewStartTime : TimeSpan }

    type Msg =
    | SelectOpenArrangement
    | SelectCoverArt
    | SelectAudioFile
    | SelectCustomFont
    | SelectProfilePath
    | SelectTestFolderPath
    | SelectProjectsFolderPath
    | AddArrangements of files : string[] option
    | AddCoverArt of fileName : string option
    | AddAudioFile of fileName : string option
    | AddCustomFontFile of fileName : string option
    | AddProfilePath of path : string option
    | AddTestFolderPath of path : string option
    | AddProjectsFolderPath of path : string option
    | ArrangementSelected of selected : Arrangement option
    | ToneSelected of selected : Tone option
    | DeleteArrangement
    | DeleteTone
    | ImportProfileTones
    | CreatePreviewAudio of PreviewAudioCreation
    | PreviewAudioStartChanged of time : float
    | ShowSortFields of shown : bool
    | ShowJapaneseFields of shown : bool
    | EditInstrumental of edit : (Instrumental -> Instrumental)
    | EditVocals of edit : (Vocals -> Vocals)
    | EditTone of edit : (Tone -> Tone)
    | EditProject of edit : (DLCProject -> DLCProject)
    | EditConfig of edit : (Configuration -> Configuration)
    | CloseOverlay
    | ImportTonesChanged of item : obj
    | ImportSelectedTones
    | ShowConfigEditor
    | SaveConfiguration
    | SetConfiguration of config : Configuration
    | ErrorOccurred of e : exn
