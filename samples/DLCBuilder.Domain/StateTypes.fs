[<AutoOpen>]
module DLCBuilder.Types

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DD
open Rocksmith2014.DLCProject
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing
open System
open OnlineUpdate
open System.IO

[<RequireQualifiedAccess>]
type FocusedSetting =
    | ProfilePath
    | TestFolder
    | DLCFolder

type PreviewAudioCreationData =
    { SourceFile: string
      MaxPreviewStart: TimeSpan }

type ProfileCleanerRecordsRemoved =
    { PlayNext: int
      Songs: int
      ScoreAttack: int
      Stats: int }

[<RequireQualifiedAccess>]
type ProfileCleanerStep =
    | Idle
    | ReadingIds of progress: float
    | CleaningProfile
    | Completed of wasDryRun: bool * result: ProfileCleanerRecordsRemoved

type ProfileCleanerState =
    { CurrentStep: ProfileCleanerStep
      IsDryRun: bool }

    static member Default =
        { CurrentStep = ProfileCleanerStep.Idle
          IsDryRun = false }

type OverlayContents =
    | NoOverlay
    | AbnormalExitMessage
    | AboutMessage
    | ConfigEditor of focus: FocusedSetting option
    | DeleteConfirmation of files: string list
    | ErrorMessage of message: string * moreInfo: string option
    | IdRegenerationConfirmation of arrangements: Arrangement list * reply: AsyncReply
    | ImportToneSelector of tones: Tone array
    | IssueViewer of arrangement: Arrangement
    | JapaneseLyricsCreator of JapaneseLyricsCreator.LyricsCreatorState
    | PitchShifter
    | SelectPreviewStart of data: PreviewAudioCreationData
    | ToneCollection of state: ToneCollection.ToneCollectionState
    | ToneEditor
    | UpdateInformationDialog of update: UpdateInformation
    | AdditionalMetaDataEditor
    | LyricsViewer of lyrics: string * isJapanese: bool
    | InstrumentalXmlDetailsViewer of xml: InstrumentalArrangement * fileName: string
    | ProfileCleaner

[<RequireQualifiedAccess>]
type OverlayCloseMethod =
    | OverlayButton
    | EscapeKey
    | ClickedOutside

type PreviewAudioCreation =
    | InitialSetup
    | SetupStartTime of data: PreviewAudioCreationData
    | CreateFile of data: PreviewAudioCreationData
    | FileCreated of path: string

type VolumeTarget =
    | MainAudio
    | PreviewAudio
    | CustomAudio of audioPath: string * arrId: Guid

type LongTask =
    | BuildPackage
    | WemConversion of filesBeingConverted: string array
    | PsarcImport
    | PsarcUnpack
    | WemToOggConversion
    | ArrangementCheckAll
    | ArrangementCheckOne
    | AutomaticPreviewCreation
    | VolumeCalculation of VolumeTarget
    | FileDownload of id: DownloadId

type StatusMessage =
    | TaskWithoutProgress of task: LongTask
    | TaskWithProgress of task: LongTask * progress: float
    | MessageString of id: Guid * message: string
    | UpdateMessage of updateInfo: UpdateInformation

[<RequireQualifiedAccess>]
type BuildCompleteType =
    | Test
    | TestNewVersion of version: string
    | Release of targetDirectory: string
    | PitchShifted
    | ReplacePsarc

type TimeComponent =
    | Minutes of float
    | Seconds of float
    | Milliseconds of float

type ProjectEdit =
    | SetDLCKey of string
    | SetVersion of string
    | SetAlbumArt of string
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
    | SetPreviewStartTime of TimeComponent
    | SetPitchShift of int16
    | SetAuthor of string

type ToneEdit =
    | SetName of toneName: string
    | SetKey of toneKey: string
    | SetVolume of volume: float
    | ChangeDescriptor of index: int * descriptor: ToneDescriptor
    | AddDescriptor
    | RemoveDescriptor
    | SetPedal of ToneGear.GearData
    | SetKnobValue of knobKey: string * value: float32
    | RemovePedal
    | MovePedal of gearSlot: ToneGear.GearSlot * direction: MoveDirection

type VocalsEdit =
    | SetIsJapanese of bool
    | SetCustomFont of string option
    | SetVocalsMasterId of int
    | SetVocalsPersistentId of Guid

type ConfigEdit =
    | SetCharterName of string
    | SetProfilePath of string
    | SetTestFolderPath of string
    | SetDlcFolderPath of string
    | SetWwiseConsolePath of string
    | SetFontGeneratorPath of string
    | SetAutoVolume of bool
    | SetAutoAudioConversion of bool
    | SetShowAdvanced of bool
    | SetRemoveDDOnImport of bool
    | SetCreateEOFProjectOnImport of bool
    | SetGenerateDD of bool
    | SetComparePhraseLevelsOnTestBuild of bool
    | SetDDPhraseSearchEnabled of bool
    | SetDDPhraseSearchThreshold of int
    | SetDDLevelCountGeneration of LevelCountGeneration
    | SetApplyImprovements of bool
    | SetSaveDebugFiles of bool
    | SetCustomAppId of AppId option
    | SetConvertAudio of AudioConversionType option
    | SetOpenFolderAfterReleaseBuild of bool
    | SetLoadPreviousProject of bool
    | SetAutoSave of bool
    | SetBaseToneNaming of BaseToneNamingScheme
    | AddReleasePlatform of Platform
    | RemoveReleasePlatform of Platform
    | SetProfileCleanerParallelism of int

[<RequireQualifiedAccess>]
type ArrPropOp =
    | Enable of ArrangementPropertiesOverride.ArrPropFlags
    | Disable of ArrangementPropertiesOverride.ArrPropFlags

type InstrumentalEdit =
    | SetArrangementName of ArrangementName
    | SetPriority of ArrangementPriority
    | SetRouteMask of RouteMask
    | SetBassPicked of bool
    | SetTuning of stringIndex: int * tuningValue: int16
    | ChangeTuning of stringIndex: int * direction: MoveDirection
    | ChangeTuningAll of direction: MoveDirection
    | SetTuningPitch of float
    | SetBaseTone of string
    | SetScrollSpeed of float
    | SetMasterId of int
    | SetPersistentId of Guid
    | SetCustomAudioPath of string option
    | SetCustomAudioVolume of float
    | UpdateToneInfo
    | ToggleArrangementPropertiesOverride of ArrangementProperties
    | ToggleArrangementProperty of ArrPropOp

type ToolsMsg =
    | ConvertWemToOgg of files: string array
    | ConvertAudioToWem of files: string array
    | UnpackPSARC of paths: string array * targetRootDirectory: string
    | PackDirectoryIntoPSARC of directory: string * targetFile: string
    | RemoveDD of files: string array
    | InjectTonesIntoProfile of files: string array
    | StartProfileCleaner
    | ProfileCleanerProgressChanged of progress: float
    | IdDataReadingCompleted of data: ProfileCleaner.IdData
    | ProfileCleaned of result: ProfileCleanerRecordsRemoved
    | SetProfileCleanerDryRun of bool

[<RequireQualifiedAccess>]
type Dialog =
    | OpenProject
    | SaveProjectAs
    | ToolkitImport
    | PsarcImportQuick
    | PsarcImport
    | PsarcImportTargetFolder of psarcPath: string
    | PsarcUnpack
    | PsarcUnpackTargetFolder of psarcPaths: string array
    | PsarcPackDirectory
    | PsarcPackTargetFile of directory: string
    | WemFiles
    | AudioFileConversion
    | RemoveDD
    | TestFolder
    | DlcFolder
    | ProfileFile
    | AddArrangements
    | ToneImport
    | ToneInject
    | WwiseConsole
    | FontGeneratorPath
    | CoverArt
    | AudioFile of isCustom: bool
    | PreviewFile
    | CustomFont
    | ExportTone of tone: Tone
    | SaveJapaneseLyrics

type Msg =
    | OpenWithShell of path: string
    | IgnoreIssueForProject of issueCode: string
    | EnableIssueForProject of issueCode: string
    | ConfirmIdRegeneration of arrIds: Guid list * reply: AsyncReply
    | SetNewArrangementIds of Map<Guid, Arrangement>
    | ImportPsarcQuick of psarcFile: string
    | ImportPsarc of psarcFile: string * targetFolder: string
    | PsarcImported of project: DLCProject * importType: PsarcImportType
    | ImportToolkitTemplate of fileName: string
    | ImportTonesFromFile of fileName: string
    | ImportProfileTones
    | ImportTones of tones: Tone list
    | ImportSelectedTones
    | SetSelectedImportTones of selectedTones: Tone list
    | NewProject
    | OpenProject of fileName: string
    | ProjectLoaded of project: DLCProject * origin: LoadedProjectOrigin
    | ProjectSaveOrSaveAs
    | SaveProjectAs
    | SaveProject of fileName: string
    | ProjectSaved of targetFile: string
    | AutoSaveProject
    | OpenProjectFolder
    | AddArrangements of files: string array
    | SetAudioFile of path: string
    | SetPreviewAudioFile of path: string
    | SetConfiguration of config: Configuration * enableLoad: bool * wasAbnormalExit: bool
    | SetRecentFiles of files: string list
    | ProgramClosing
    | SetAvailableUpdate of update: Result<UpdateInformation option, string>
    | SetToneRepository of repository: ToneGear.Repository
    | SetSelectedArrangementIndex of index: int
    | SetSelectedToneIndex of index: int
    | SetSelectedGearSlot of ToneGear.GearSlot
    | SetManuallyEditingKnobKey of string option
    | CheckForUpdates
    | UpdateCheckCompleted of update: Result<UpdateInformation option, string>
    | DismissUpdateMessage
    | ShowUpdateInformation
    | DownloadUpdate
    | DeleteTestBuilds
    | DeleteConfirmed of files: string list
    | DeleteSelectedArrangement
    | DeleteSelectedTone
    | AddNewTone
    | DuplicateTone
    | AddToneToCollection
    | MoveTone of MoveDirection
    | ShowToneCollection
    | MoveArrangement of MoveDirection
    | CreatePreviewAudio of PreviewAudioCreation
    | AutoPreviewCreated of previewPath: string * continuation: Msg
    | ShowSortFields of shown: bool
    | ShowJapaneseFields of shown: bool
    | GenerateNewIds
    | GenerateAllIds
    | ApplyLowTuningFix
    | EditInstrumental of InstrumentalEdit
    | EditVocals of VocalsEdit
    | EditTone of ToneEdit
    | EditProject of ProjectEdit
    | EditConfig of ConfigEdit
    | CloseOverlay of closeMethod: OverlayCloseMethod
    | ExportSelectedTone
    | ExportTone of tone: Tone * targetPath: string
    | OpenPreviousProjectConfirmed
    | ShowOverlay of OverlayContents
    | ShowToneEditor
    | ShowIssueViewer
    | ShowImportToneSelector of tones: Tone array
    | ShowLyricsViewer
    | ShowInstrumentalXmlDetailsViewer
    | Build of BuildType
    | BuildComplete of BuildCompleteType
    | WemConversionComplete of filesConverted: string array
    | CheckArrangement of arrangement: Arrangement
    | CheckArrangements
    | TaskProgressChanged of task: LongTask * progress: float
    | AddStatusMessage of message: string
    | RemoveStatusMessage of id: Guid
    | CheckOneCompleted of string * Issue list
    | CheckAllCompleted of issues: Map<string, Issue list>
    | PsarcUnpacked
    | WemToOggConversionCompleted
    | ConvertToWem
    | ConvertToWemCustom
    | CalculateVolumes
    | CalculateVolume of target: VolumeTarget
    | VolumeCalculated of volume: float * target: VolumeTarget
    | ChangeLocale of locale: Locale
    | ErrorOccurred of e: exn
    | TaskFailed of e: exn * failedTask: LongTask
    | ToolsMsg of ToolsMsg
    | ToneCollectionMsg of ToneCollection.Msg
    | ShowDialog of Dialog
    | HotKeyMsg of Msg
    | ShowJapaneseLyricsCreator
    | LyricsCreatorMsg of JapaneseLyricsCreator.Msg
    | LoadMultipleFiles of paths: string seq
    | OfficialTonesDatabaseDownloaded of downloadTask: LongTask
    | UpdateDownloaded of installerPath: string
    | SetEditedPsarcAppId of appId: string
    | ReadAudioLength
    | SetAudioLength of audioLength: TimeSpan option
    | StartFontGenerator
    | FontGenerated of arrangementId: Guid * glyphsXmlPath: string

type State =
    { Project: DLCProject
      SavedProject: DLCProject
      RecentFiles: string list
      Config: Configuration
      SelectedArrangementIndex: int
      SelectedToneIndex: int
      SelectedImportTones: Tone list
      ManuallyEditingKnobKey: string option
      SelectedGearSlot: ToneGear.GearSlot
      ShowSortFields: bool
      ShowJapaneseFields: bool
      Overlay: OverlayContents
      OpenProjectFile: string option
      CurrentPlatform: Platform
      StatusMessages: StatusMessage list
      RunningTasks: LongTask Set
      ArrangementIssues: Map<string, Issue list>
      AvailableUpdate: UpdateInformation option
      ToneGearRepository: ToneGear.Repository option
      QuickEditData: PsarcQuickEditData option
      ImportedBuildToolVersion: string option
      AudioLength: TimeSpan option
      FontGenerationWatcher: FileSystemWatcher option
      ProfileCleanerState: ProfileCleanerState
      /// For forcing a view update if the user loads the same album art file, but the file has been modified.
      AlbumArtLoadTime: DateTime option
      Localizer: IStringLocalizer
      AlbumArtLoader: IBitmapLoader
      DatabaseConnector: ToneCollection.IDatabaseConnector }
