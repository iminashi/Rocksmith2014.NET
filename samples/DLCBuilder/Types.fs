[<AutoOpen>]
module DLCBuilder.Types

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.XML.Processing
open Avalonia.Controls.Selection
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
    | DeleteConfirmation of files : string list
    | AbnormalExitMessage
    | PitchShifter

type PreviewAudioCreation =
    | SetupStartTime
    | CreateFile
    | FileCreated of path : string

type MoveDirection = Up | Down

type VolumeTarget =
    | MainAudio
    | PreviewAudio
    | CustomAudio of audioPath : string * arrId : Guid

type LongTask =
    | BuildPackage
    | WemConversion
    | PsarcImport
    | PsarcUnpack
    | ArrangementCheck
    | VolumeCalculation of VolumeTarget

type StatusMessage =
    | TaskWithoutProgress of taks:LongTask
    | TaskWithProgress of task:LongTask * progress:float
    | MessageString of id:Guid * message:string

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
    | SetPitchShift of int16

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
    | SetOpenFolderAfterReleaseBuild of bool
    | SetLoadPreviousProject of bool
    | SetAutoSave of bool
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

type State =
    { Project : DLCProject
      SavedProject : DLCProject
      RecentFiles : string list
      Config : Configuration
      CoverArt : Bitmap option
      SelectedArrangementIndex : int
      SelectedToneIndex : int
      SelectedGear : ToneGear.GearData option
      ManuallyEditingKnobKey : string option
      SelectedGearSlot : ToneGear.GearSlot
      ShowSortFields : bool
      ShowJapaneseFields : bool
      Overlay : OverlayContents
      SelectedImportTones : SelectionModel<Tone>
      OpenProjectFile : string option
      CurrentPlatform : Platform
      StatusMessages : StatusMessage list
      RunningTasks : LongTask Set
      ArrangementIssues : Map<string, ArrangementChecker.Issue list> }

type ToolsMsg =
    | UnpackPSARC of file : string
    | RemoveDD of files : string array

[<RequireQualifiedAccess>]
type Dialog =
    | OpenProject
    | SaveProjectAs
    | ToolkitImport
    | PsarcImport
    | PsarcImportTargetFolder of psarcPath : string
    | PsarcUnpack
    | RemoveDD
    | TestFolder
    | ProfileFile
    | ProjectFolder
    | AddArrangements
    | ToneImport
    | WwiseConsole
    | CoverArt
    | AudioFile of isCustom : bool
    | CustomFont
    | ExportTone of tone : Tone

[<RequireQualifiedAccess>]
type FileFilter =
    | Audio
    | XML
    | Image
    | DDS
    | Profile
    | Project
    | PSARC
    | ToolkitTemplate
    | ToneImport
    | ToneExport
    | WwiseConsoleApplication

type BuildValidationError =
    | InvalidDLCKey
    | TitleEmpty
    | ArtistNameEmpty
    | AlbumArtNotFound
    | PreviewNotFound
    | MultipleTonesSameKey
    | ConflictingVocals

type Msg =
    | ImportPsarc of psarcFile : string * targetFolder : string
    | PsarcImported of project : DLCProject * projectFile : string
    | ImportToolkitTemplate of fileName : string
    | ImportTonesFromFile of fileName : string
    | ImportProfileTones
    | ImportSelectedTones
    | ImportTones of tones : Tone list
    | NewProject
    | OpenProject of fileName : string
    | ProjectLoaded of project : DLCProject * projectFile : string
    | ProjectSaveOrSaveAs
    | SaveProjectAs
    | SaveProject of fileName : string
    | ProjectSaved of targetFile : string
    | AutoSaveProject
    | AddArrangements of files : string array
    | SetCoverArt of fileName : string
    | SetAudioFile of fileName : string
    | SetConfiguration of config : Configuration * enableLoad : bool * wasNormalExit : bool
    | SetRecentFiles of files : string list
    | SetSelectedArrangementIndex of index : int
    | SetSelectedToneIndex of index : int
    | SetSelectedGear of ToneGear.GearData option
    | SetSelectedGearSlot of ToneGear.GearSlot
    | SetManuallyEditingKnobKey of string option
    | DeleteTestBuilds
    | DeleteConfirmed of files : string list
    | DeleteArrangement
    | DeleteTone
    | DuplicateTone
    | MoveTone of MoveDirection
    | MoveArrangement of MoveDirection
    | CreatePreviewAudio of PreviewAudioCreation
    | ShowSortFields of shown : bool
    | ShowJapaneseFields of shown : bool
    | GenerateNewIds
    | GenerateAllIds
    | EditInstrumental of InstrumentalEdit
    | EditVocals of VocalsEdit
    | EditTone of ToneEdit
    | EditProject of ProjectEdit
    | EditConfig of ConfigEdit
    | CloseOverlay
    | ExportSelectedTone
    | ExportTone of tone : Tone * targetPath : string
    | OpenPreviousProjectConfirmed
    | ShowOverlay of OverlayContents
    | ShowToneEditor
    | ShowIssueViewer
    | ShowImportToneSelector of tones : Tone array
    | BuildPitchShifted
    | Build of BuildType
    | BuildComplete of BuildType
    | WemConversionComplete of unit
    | CheckArrangements
    | TaskProgressChanged of task : LongTask * progress : float
    | AddStatusMessage of locString : string
    | RemoveStatusMessage of id : Guid
    | CheckCompleted of Map<string, ArrangementChecker.Issue list>
    | PsarcUnpacked
    | ConvertToWem
    | ConvertToWemCustom
    | CalculateVolumes
    | CalculateVolume of target : VolumeTarget
    | VolumeCalculated of volume : float * target : VolumeTarget
    | ChangeLocale of locale : Locale
    | ErrorOccurred of e : exn
    | TaskFailed of e : exn * failedTask : LongTask
    | ToolsMsg of ToolsMsg
    | ShowDialog of Dialog
    | HotKeyMsg of Msg
    | CloseApplication
