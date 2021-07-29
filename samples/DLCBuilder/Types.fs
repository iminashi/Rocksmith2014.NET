[<AutoOpen>]
module DLCBuilder.Types

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DD
open Rocksmith2014.XML.Processing
open Avalonia.Controls.Selection
open Avalonia.Media.Imaging
open System
open OnlineUpdate
open DLCBuilder.ToneCollection

type OverlayContents =
    | NoOverlay
    | ErrorMessage of message : string * moreInfo : string option
    | ImportToneSelector of tones : Tone array
    | SelectPreviewStart of audioLength : TimeSpan
    | ConfigEditor
    | IssueViewer of issues : ArrangementChecker.Issue list
    | ToneEditor
    | ToneCollection of state : ToneCollectionState
    | DeleteConfirmation of files : string list
    | AbnormalExitMessage
    | PitchShifter
    | AboutMessage
    | UpdateInformationDialog of update : UpdateInformation

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
    | WemToOggConversion
    | ArrangementCheck
    | VolumeCalculation of VolumeTarget

type StatusMessage =
    | TaskWithoutProgress of task:LongTask
    | TaskWithProgress of task:LongTask * progress:float
    | MessageString of id:Guid * message:string
    | UpdateMessage of updateInfo:UpdateInformation

type BuildType = Test | Release | PitchShifted

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
    | SetName of toneName:string
    | SetKey of toneKey:string
    | SetVolume of volume:float
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
    | SetDDLevelCountGeneration of LevelCountGeneration
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
    | ChangeTuning of stringIndex:int * direction:MoveDirection
    | ChangeTuningAll of direction:MoveDirection
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
      ArrangementIssues : Map<string, ArrangementChecker.Issue list>
      AvailableUpdate : UpdateInformation option
      ToneGearRepository: ToneGear.Repository option }

type ToolsMsg =
    | ConvertWemToOgg of files : string array
    | ConvertAudioToWem of files : string array
    | UnpackPSARC of file : string
    | PackDirectoryIntoPSARC of directory : string * targetFile : string
    | RemoveDD of files : string array
    | InjectTonesIntoProfile of files : string array

[<RequireQualifiedAccess>]
type Dialog =
    | OpenProject
    | SaveProjectAs
    | ToolkitImport
    | PsarcImport
    | PsarcImportTargetFolder of psarcPath : string
    | PsarcUnpack
    | PsarcPackDirectory
    | PsarcPackTargetFile of directory : string
    | WemFiles
    | AudioFileConversion
    | RemoveDD
    | TestFolder
    | ProfileFile
    | ProjectFolder
    | AddArrangements
    | ToneImport
    | ToneInject
    | WwiseConsole
    | CoverArt
    | AudioFile of isCustom : bool
    | CustomFont
    | ExportTone of tone : Tone

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
    | SetConfiguration of config : Configuration * enableLoad : bool * wasAbnormalExit : bool
    | SetRecentFiles of files : string list
    | SetAvailableUpdate of update : Result<UpdateInformation option, string>
    | SetToneRepository of repository : ToneGear.Repository
    | SetSelectedArrangementIndex of index : int
    | SetSelectedToneIndex of index : int
    | SetSelectedGear of ToneGear.GearData option
    | SetSelectedGearSlot of ToneGear.GearSlot
    | SetManuallyEditingKnobKey of string option
    | CheckForUpdates
    | UpdateCheckCompleted of update : Result<UpdateInformation option, string>
    | DismissUpdateMessage
    | ShowUpdateInformation
    | UpdateAndRestart
    | UpdateFailed of messageId : Guid * error : exn
    | DeleteTestBuilds
    | DeleteConfirmed of files : string list
    | DeleteArrangement
    | DeleteTone
    | AddNewTone
    | DuplicateTone
    | AddToneToCollection
    | MoveTone of MoveDirection
    | ShowToneCollection
    | MoveArrangement of MoveDirection
    | CreatePreviewAudio of PreviewAudioCreation
    | ShowSortFields of shown : bool
    | ShowJapaneseFields of shown : bool
    | GenerateNewIds
    | GenerateAllIds
    | ApplyLowTuningFix
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
    | Build of BuildType
    | BuildComplete of BuildType
    | WemConversionComplete of unit
    | CheckArrangements
    | TaskProgressChanged of task : LongTask * progress : float
    | AddStatusMessage of locString : string
    | RemoveStatusMessage of id : Guid
    | CheckCompleted of Map<string, ArrangementChecker.Issue list>
    | PsarcUnpacked
    | WemToOggConversionCompleted
    | ConvertToWem
    | ConvertToWemCustom
    | CalculateVolumes
    | CalculateVolume of target : VolumeTarget
    | VolumeCalculated of volume : float * target : VolumeTarget
    | ChangeLocale of locale : Locale
    | ErrorOccurred of e : exn
    | TaskFailed of e : exn * failedTask : LongTask
    | ToolsMsg of ToolsMsg
    | ToneCollectionMsg of ToneCollection.Msg
    | ShowDialog of Dialog
    | HotKeyMsg of Msg
