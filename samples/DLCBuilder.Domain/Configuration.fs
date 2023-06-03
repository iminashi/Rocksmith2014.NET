namespace DLCBuilder

open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.DD
open System
open System.IO
open System.Text.Json

[<RequireQualifiedAccess>]
type BaseToneNamingScheme =
    | Default
    | TitleAndArrangement

    override this.ToString() =
        match this with
        | BaseToneNamingScheme.Default ->
            "DefaultBaseToneNamingScheme"
        | BaseToneNamingScheme.TitleAndArrangement ->
            "TitleAndArrangementBaseToneNamingScheme"

type Configuration =
    { ReleasePlatforms: Platform Set
      ProfilePath: string
      TestFolderPath: string
      DlcFolderPath: string
      CharterName: string
      ShowAdvanced: bool
      GenerateDD: bool
      DDPhraseSearchEnabled: bool
      DDPhraseSearchThreshold: int
      DDLevelCountGeneration: LevelCountGeneration
      RemoveDDOnImport: bool
      CreateEOFProjectOnImport: bool
      ApplyImprovements: bool
      SaveDebugFiles: bool
      AutoVolume: bool
      AutoSave: bool
      ComparePhraseLevelsOnTestBuild: bool
      ConvertAudio: AudioConversionType option
      OpenFolderAfterReleaseBuild: bool
      LoadPreviousOpenedProject: bool
      PreviousOpenedProject: string
      Locale: Locale
      WwiseConsolePath: string option
      FontGeneratorPath: string option
      CustomAppId: AppId option
      BaseToneNamingScheme: BaseToneNamingScheme }

    static member Default =
        { ReleasePlatforms = Set([ PC; Mac ])
          ProfilePath = String.Empty
          TestFolderPath = String.Empty
          DlcFolderPath = String.Empty
          CharterName = String.Empty
          ShowAdvanced = false
          GenerateDD = true
          DDPhraseSearchEnabled = true
          DDPhraseSearchThreshold = 80
          DDLevelCountGeneration = LevelCountGeneration.Simple
          RemoveDDOnImport = false
          CreateEOFProjectOnImport = false
          ApplyImprovements = true
          SaveDebugFiles = false
          AutoVolume = true
          AutoSave = false
          ComparePhraseLevelsOnTestBuild = false
          ConvertAudio = None
          OpenFolderAfterReleaseBuild = true
          LoadPreviousOpenedProject = false
          PreviousOpenedProject = String.Empty
          Locale = Locale.Default
          WwiseConsolePath = None
          FontGeneratorPath = None
          CustomAppId = None
          BaseToneNamingScheme = BaseToneNamingScheme.Default }

module Configuration =
    type Dto() =
        member val ReleasePC: bool = true with get, set
        member val ReleaseMac: bool = true with get, set
        member val ProfilePath: string = String.Empty with get, set
        member val TestFolderPath: string = String.Empty with get, set
        member val DlcFolderPath: string = String.Empty with get, set
        member val CharterName: string = String.Empty with get, set
        member val ShowAdvanced: bool = false with get, set
        member val GenerateDD: bool = true with get, set
        member val DDPhraseSearchEnabled: bool = true with get, set
        member val DDPhraseSearchThreshold: int = 80 with get, set
        member val DDLevelCountGeneration: int = 0 with get, set
        member val RemoveDDOnImport: bool = false with get, set
        member val CreateEOFProjectOnImport: bool = false with get, set
        member val ApplyImprovements: bool = true with get, set
        member val SaveDebugFiles: bool = false with get, set
        member val AutoVolume: bool = true with get, set
        member val AutoSave: bool = false with get, set
        member val ComparePhraseLevelsOnTestBuild: bool = false with get, set
        member val ConvertAudio: int = 0 with get, set
        member val OpenFolderAfterReleaseBuild: bool = true with get, set
        member val LoadPreviousOpenedProject: bool = false with get, set
        member val PreviousOpenedProject: string = String.Empty with get, set
        member val Locale: string = Locale.Default.ShortName with get, set
        member val WwiseConsolePath: string = String.Empty with get, set
        member val FontGeneratorPath: string = String.Empty with get, set
        member val CustomAppId: string = String.Empty with get, set
        member val BaseToneNaming: int = 1 with get, set

    let appDataFolder =
        let dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rs2-dlcbuilder")
        Directory.CreateDirectory(dir).FullName

    let private configFilePath =
        Path.Combine(appDataFolder, "config.json")

    let exitCheckFilePath =
        Path.Combine(appDataFolder, "exit.check")

    let crashLogPath =
        Path.Combine(appDataFolder, "crash_log.txt")

    /// Converts a configuration DTO into a configuration record.
    let private fromDto (t: IStringLocalizer) (dto: Dto) =
        let platforms =
            if not (dto.ReleasePC || dto.ReleaseMac) then
                [ PC; Mac ]
            else
                [ if dto.ReleasePC then PC
                  if dto.ReleaseMac then Mac ]

        let threshold = Math.Clamp(dto.DDPhraseSearchThreshold, 0, 100)

        let convertAudio =
            match dto.ConvertAudio with
            | 1 -> Some ToOgg
            | 2 -> Some ToWav
            | _ -> None

        let levelCountGeneration =
            match dto.DDLevelCountGeneration with
            | 1 -> LevelCountGeneration.MLModel
            | _ -> LevelCountGeneration.Simple

        let baseToneNaming =
            match dto.BaseToneNaming with
            | 1 -> BaseToneNamingScheme.Default
            | _ -> BaseToneNamingScheme.TitleAndArrangement

        { ReleasePlatforms = set platforms
          ProfilePath = dto.ProfilePath
          TestFolderPath = dto.TestFolderPath
          DlcFolderPath = dto.DlcFolderPath
          CharterName = dto.CharterName
          ShowAdvanced = dto.ShowAdvanced
          GenerateDD = dto.GenerateDD
          DDPhraseSearchEnabled = dto.DDPhraseSearchEnabled
          DDPhraseSearchThreshold = threshold
          DDLevelCountGeneration = levelCountGeneration
          RemoveDDOnImport = dto.RemoveDDOnImport
          CreateEOFProjectOnImport = dto.CreateEOFProjectOnImport
          ApplyImprovements = dto.ApplyImprovements
          SaveDebugFiles = dto.SaveDebugFiles
          AutoVolume = dto.AutoVolume
          AutoSave = dto.AutoSave
          ComparePhraseLevelsOnTestBuild = dto.ComparePhraseLevelsOnTestBuild
          ConvertAudio = convertAudio
          OpenFolderAfterReleaseBuild = dto.OpenFolderAfterReleaseBuild
          LoadPreviousOpenedProject = dto.LoadPreviousOpenedProject
          PreviousOpenedProject = dto.PreviousOpenedProject
          Locale = t.LocaleFromShortName dto.Locale
          WwiseConsolePath = Option.ofString dto.WwiseConsolePath
          FontGeneratorPath = Option.ofString dto.FontGeneratorPath
          CustomAppId = AppId.ofString dto.CustomAppId
          BaseToneNamingScheme = baseToneNaming }

    /// Converts a configuration into a configuration DTO.
    let private toDto (config: Configuration) =
        let convertAudio =
            match config.ConvertAudio with
            | None -> 0
            | Some ToOgg -> 1
            | Some ToWav -> 2

        let levelCountGeneration =
            match config.DDLevelCountGeneration with
            | LevelCountGeneration.Simple -> 0
            | LevelCountGeneration.MLModel -> 1
            | LevelCountGeneration.Constant _ -> 2 // Should never be here

        let customAppId =
            config.CustomAppId |> Option.map AppId.toString |> Option.toObj

        let baseToneNaming =
            match config.BaseToneNamingScheme with
            | BaseToneNamingScheme.Default -> 1
            | BaseToneNamingScheme.TitleAndArrangement -> 2

        Dto(
            ReleasePC = (config.ReleasePlatforms |> Set.contains PC),
            ReleaseMac = (config.ReleasePlatforms |> Set.contains Mac),
            ProfilePath = config.ProfilePath,
            TestFolderPath = config.TestFolderPath,
            DlcFolderPath = config.DlcFolderPath,
            CharterName = config.CharterName,
            ShowAdvanced = config.ShowAdvanced,
            Locale = config.Locale.ShortName,
            GenerateDD = config.GenerateDD,
            DDPhraseSearchEnabled = config.DDPhraseSearchEnabled,
            DDPhraseSearchThreshold = config.DDPhraseSearchThreshold,
            DDLevelCountGeneration = levelCountGeneration,
            RemoveDDOnImport = config.RemoveDDOnImport,
            CreateEOFProjectOnImport = config.CreateEOFProjectOnImport,
            ApplyImprovements = config.ApplyImprovements,
            AutoVolume = config.AutoVolume,
            AutoSave = config.AutoSave,
            ComparePhraseLevelsOnTestBuild = config.ComparePhraseLevelsOnTestBuild,
            ConvertAudio = convertAudio,
            OpenFolderAfterReleaseBuild = config.OpenFolderAfterReleaseBuild,
            LoadPreviousOpenedProject = config.LoadPreviousOpenedProject,
            PreviousOpenedProject = config.PreviousOpenedProject,
            SaveDebugFiles = config.SaveDebugFiles,
            WwiseConsolePath = Option.toObj config.WwiseConsolePath,
            FontGeneratorPath = Option.toObj config.FontGeneratorPath,
            CustomAppId = customAppId,
            BaseToneNaming = baseToneNaming
        )

    /// Loads a configuration from the file defined in configFilePath.
    let load (t: IStringLocalizer) =
        backgroundTask {
            if not <| File.Exists(configFilePath) then
                return Configuration.Default
            else
                try
                    use file = File.OpenRead(configFilePath)
                    let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
                    let! dto = JsonSerializer.DeserializeAsync<Dto>(file, options)
                    return fromDto t dto
                with _ ->
                    return Configuration.Default
        }

    /// Saves the configuration to the file defined in configFilePath.
    let save (config: Configuration) =
        backgroundTask {
            use file = File.Create(configFilePath)
            let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
            do! JsonSerializer.SerializeAsync(file, toDto config, options)
        }
