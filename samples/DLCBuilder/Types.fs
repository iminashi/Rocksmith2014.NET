namespace DLCBuilder

open Rocksmith2014.DLCProject
open Rocksmith2014.Common.Manifest
open Avalonia.Media.Imaging
open System

[<AutoOpen>]
module Types =
    type Configuration =
        { ProfilePath : string
          TestFolderPath : string
          ProjectsFolderPath : string
          CharterName : string
          ShowAdvanced : bool }
    
        static member Default =
            { ProfilePath = String.Empty
              TestFolderPath = String.Empty
              ProjectsFolderPath = String.Empty
              CharterName = String.Empty
              ShowAdvanced = true }

    type State =
        { Project : DLCProject
          Config : Configuration
          CoverArt : Bitmap option
          SelectedArrangement : Arrangement option
          SelectedTone : Tone option
          ShowSortFields : bool
          ShowJapaneseFields : bool }

    type Msg =
    | SelectOpenArrangement
    | SelectCoverArt
    | SelectAudioFile
    | SelectCustomFont
    | AddArrangements of files : string[] option
    | AddCoverArt of fileName : string option
    | AddAudioFile of fileName : string option
    | AddCustomFontFile of fileName : string option
    | ArrangementSelected of selected : Arrangement option
    | ToneSelected of selected : Tone option
    | DeleteArrangement
    | DeleteTone
    | ImportTones
    | CreatePreviewAudio
    | ShowSortFields of shown : bool
    | ShowJapaneseFields of shown : bool
    | EditInstrumental of edit : (Instrumental -> Instrumental)
    | EditVocals of edit : (Vocals -> Vocals)
    | EditProject of edit : (DLCProject -> DLCProject)
