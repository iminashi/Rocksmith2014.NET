[<AutoOpen>]
module DLCBuilder.Types

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.XML.Processing
open Avalonia.Controls
open Avalonia.Media.Imaging
open System

type OverlayContents =
    | NoOverlay
    | ErrorMessage of message : string * moreInfo : string option
    | ImportToneSelector of tones : Tone array
    | SelectPreviewStart of audioLength : TimeSpan
    | ConfigEditor
    | IssueViewer of issues : ArrangementChecker.Issue list
    | ToneEditor

type PreviewAudioCreation =
    | SetupStartTime
    | CreateFile
    | FileCreated of path : string

type MoveDirection = Up | Down

type VolumeTarget =
    | MainAudio
    | PreviewAudio
    | CustomAudio of audioPath : string

type LongTask =
    | BuildPackage
    | WemConversion
    | PsarcImport
    | ArrangementCheck
    | VolumeCalculation of VolumeTarget

type BuildType = Test | Release

type ProjectEdit =
    | SetDLCKey of string
    | SetVersion of string
    | SetArtistName of string
    | SetArtistNameSort of string
    | SetJapaneseArtistName of string option
    | SetTitle of string
    | SetTitleSort of string
    | SetJapaneseTitle of string option
    | SetAlbumName of string
    | SetAlbumNameSort of string
    | SetYear of int
    | SetAudioVolume of float
    | SetPreviewVolume of float
    | SetPreviewStartTime of float

type ToneEdit =
    | SetName of string
    | SetKey of string
    | SetVolume of float
    | ChangeDescriptor of index:int * descriptor:ToneDescriptor
    | AddDescriptor
    | RemoveDescriptor
    | SetPedal of ToneGear.GearData
    | SetKnobValue of knobKey:string * value:float32
    | RemovePedal

type VocalsEdit =
    | SetIsJapanese of bool
    | SetCustomFont of string option

type ConfigEdit =
    | SetCharterName of string
    | SetProfilePath of string
    | SetTestFolderPath of string
    | SetProjectsFolderPath of string
    | SetWwiseConsolePath of string
    | SetAutoVolume of bool
    | SetShowAdvanced of bool
    | SetRemoveDDOnImport of bool
    | SetGenerateDD of bool
    | SetDDPhraseSearchEnabled of bool
    | SetDDPhraseSearchThreshold of int
    | SetApplyImprovements of bool
    | SetSaveDebugFiles of bool
    | SetCustomAppId of string option
    | SetConvertAudio of AudioConversionType
    | AddReleasePlatform of Platform
    | RemoveReleasePlatform of Platform

type InstrumentalEdit =
    | SetArrangementName of ArrangementName
    | SetPriority of ArrangementPriority
    | SetRouteMask of RouteMask
    | SetBassPicked of bool
    | SetTuning of stringIndex:int * tuningValue:int16
    | SetTuningPitch of float
    | SetBaseTone of string
    | SetScrollSpeed of float
    | SetMasterId of int
    | SetPersistentId of Guid
    | SetCustomAudioPath of string option
    | SetCustomAudioVolume of float
    | UpdateToneInfo
    | GenerateNewIds

type State =
    { Project : DLCProject
      SavedProject : DLCProject
      RecentFiles : string list
      Config : Configuration
      CoverArt : Bitmap option
      SelectedArrangement : Arrangement option
      SelectedToneIndex : int
      SelectedGear : ToneGear.GearData option
      SelectedGearType : ToneGear.GearType
      ShowSortFields : bool
      ShowJapaneseFields : bool
      Overlay : OverlayContents
      ImportTones : Tone list
      OpenProjectFile : string option
      CurrentPlatform : Platform
      RunningTasks : LongTask Set
      ArrangementIssues : Map<string, ArrangementChecker.Issue list> }

type Msg =
    | OpenFileDialog of locTitle : string * filter : (unit -> ResizeArray<FileDialogFilter>) * msg : (string -> Msg)
    | OpenFolderDialog of locTitle : string * msg : (string -> Msg)
    | ConditionalCmdDispatch of opt : string option * msg : (string -> Msg)
    | SelectOpenArrangement
    | SelectImportPsarcFolder of psarcFile : string
    | ImportPsarc of psarcFile : string * targetFolder : string option
    | ImportToolkitTemplate of fileName : string
    | ImportTonesFromFile of fileName : string
    | ImportProfileTones
    | ImportTonesChanged of item : obj
    | ImportSelectedTones
    | ImportTones of tones : Tone list
    | NewProject
    | OpenProject of fileName : string
    | ProjectSaveOrSaveAs
    | ProjectSaveAs
    | ProjectSaved of targetFile : string
    | SaveProject of fileName : string option
    | AddArrangements of files : string[] option
    | SetCoverArt of fileName : string
    | SetAudioFile of fileName : string
    | SetConfiguration of config : Configuration
    | SetRecentFiles of string list
    | SetSelectedArrangement of selected : Arrangement option
    | SetSelectedToneIndex of index : int
    | SetSelectedGear of ToneGear.GearData option
    | SetSelectedGearType of ToneGear.GearType
    | DeleteArrangement
    | DeleteTone
    | DuplicateTone
    | MoveTone of MoveDirection
    | CreatePreviewAudio of PreviewAudioCreation
    | ShowSortFields of shown : bool
    | ShowJapaneseFields of shown : bool
    | EditInstrumental of InstrumentalEdit
    | EditVocals of VocalsEdit
    | EditTone of ToneEdit
    | EditProject of ProjectEdit
    | EditConfig of ConfigEdit
    | CloseOverlay
    | ExportSelectedTone
    | ExportTone of tone : Tone * targetPath : string option
    | ShowToneEditor
    | ShowConfigEditor
    | ShowIssueViewer
    | ShowImportToneSelector of tones : Tone array
    | ProjectLoaded of project : DLCProject * projectFile : string
    | Build of BuildType
    | BuildComplete of unit
    | CheckArrangements
    | CheckCompleted of Map<string, ArrangementChecker.Issue list>
    | ConvertToWem
    | ConvertToWemCustom
    | CalculateVolumes
    | CalculateVolume of target : VolumeTarget
    | VolumeCalculated of volume : float * target : VolumeTarget
    | ChangeLocale of locale : Locale
    | ErrorOccurred of e : exn
    | TaskFailed of e : exn * failedTask : LongTask
    | HotKeyMsg of Msg
