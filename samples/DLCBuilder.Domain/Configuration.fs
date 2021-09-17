namespace DLCBuilder

open Rocksmith2014.Common
open Rocksmith2014.DD
open System
open System.IO
open System.Text.Json

type Configuration =
    { ReleasePlatforms: Platform Set
      ProfilePath: string
      TestFolderPath: string
      ProjectsFolderPath: string
      CharterName: string
      ShowAdvanced: bool
      GenerateDD: bool
      DDPhraseSearchEnabled: bool
      DDPhraseSearchThreshold: int
      DDLevelCountGeneration: LevelCountGeneration
      RemoveDDOnImport: bool
      ApplyImprovements: bool
      SaveDebugFiles: bool
      AutoVolume: bool
      AutoSave: bool
      ConvertAudio: AudioConversionType
      OpenFolderAfterReleaseBuild: bool
      LoadPreviousOpenedProject: bool
      PreviousOpenedProject: string
      Locale: Locale
      WwiseConsolePath: string option
      CustomAppId: string option }

    static member Default =
        { ReleasePlatforms = Set([ PC; Mac ])
          ProfilePath = String.Empty
          TestFolderPath = String.Empty
          ProjectsFolderPath = String.Empty
          CharterName = String.Empty
          ShowAdvanced = false
          GenerateDD = true
          DDPhraseSearchEnabled = true
          DDPhraseSearchThreshold = 80
          DDLevelCountGeneration = LevelCountGeneration.Simple
          RemoveDDOnImport = false
          ApplyImprovements = true
          SaveDebugFiles = false
          AutoVolume = true
          AutoSave = false
          ConvertAudio = NoConversion
          OpenFolderAfterReleaseBuild = true
          LoadPreviousOpenedProject = false
          PreviousOpenedProject = String.Empty
          Locale = Locale.Default
          WwiseConsolePath = None
          CustomAppId = None }

module Configuration =
    type Dto() =
        member val ReleasePC: bool = true with get, set
        member val ReleaseMac: bool = true with get, set
        member val ProfilePath: string = String.Empty with get, set
        member val TestFolderPath: string = String.Empty with get, set
        member val ProjectsFolderPath: string = String.Empty with get, set
        member val CharterName: string = String.Empty with get, set
        member val ShowAdvanced: bool = false with get, set
        member val GenerateDD: bool = true with get, set
        member val DDPhraseSearchEnabled: bool = true with get, set
        member val DDPhraseSearchThreshold: int = 80 with get, set
        member val DDLevelCountGeneration: int = 0 with get, set
        member val RemoveDDOnImport: bool = false with get, set
        member val ApplyImprovements: bool = true with get, set
        member val SaveDebugFiles: bool = false with get, set
        member val AutoVolume: bool = true with get, set
        member val AutoSave: bool = false with get, set
        member val ConvertAudio: int = 0 with get, set
        member val OpenFolderAfterReleaseBuild: bool = true with get, set
        member val LoadPreviousOpenedProject: bool = false with get, set
        member val PreviousOpenedProject: string = String.Empty with get, set
        member val Locale: string = Locale.Default.ShortName with get, set
        member val WwiseConsolePath: string = String.Empty with get, set
        member val CustomAppId: string = String.Empty with get, set

    let appDataFolder =
        let dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rs2-dlcbuilder")
        (Directory.CreateDirectory dir).FullName

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
                Set([ PC; Mac ])
            else
                Set([ if dto.ReleasePC then PC
                      if dto.ReleaseMac then Mac ])

        let threshold = Math.Clamp(dto.DDPhraseSearchThreshold, 0, 100)

        let convertAudio =
            match dto.ConvertAudio with
            | 1 -> ToOgg
            | 2 -> ToWav
            | _ -> NoConversion

        let levelCountGeneration =
            match dto.DDLevelCountGeneration with
            | 1 -> LevelCountGeneration.MLModel
            | _ -> LevelCountGeneration.Simple

        { ReleasePlatforms = platforms
          ProfilePath = dto.ProfilePath
          TestFolderPath = dto.TestFolderPath
          ProjectsFolderPath = dto.ProjectsFolderPath
          CharterName = dto.CharterName
          ShowAdvanced = dto.ShowAdvanced
          GenerateDD = dto.GenerateDD
          DDPhraseSearchEnabled = dto.DDPhraseSearchEnabled
          DDPhraseSearchThreshold = threshold
          DDLevelCountGeneration = levelCountGeneration
          RemoveDDOnImport = dto.RemoveDDOnImport
          ApplyImprovements = dto.ApplyImprovements
          SaveDebugFiles = dto.SaveDebugFiles
          AutoVolume = dto.AutoVolume
          AutoSave = dto.AutoSave
          ConvertAudio = convertAudio
          OpenFolderAfterReleaseBuild = dto.OpenFolderAfterReleaseBuild
          LoadPreviousOpenedProject = dto.LoadPreviousOpenedProject
          PreviousOpenedProject = dto.PreviousOpenedProject
          Locale = t.LocaleFromShortName dto.Locale
          WwiseConsolePath = Option.ofString dto.WwiseConsolePath
          CustomAppId = Option.ofString dto.CustomAppId }

    /// Converts a configuration into a configuration DTO.
    let private toDto (config: Configuration) =
        let convertAudio =
            match config.ConvertAudio with
            | NoConversion -> 0
            | ToOgg -> 1
            | ToWav -> 2

        let levelCountGeneration =
            match config.DDLevelCountGeneration with
            | LevelCountGeneration.Simple -> 0
            | LevelCountGeneration.MLModel -> 1

        Dto(
            ReleasePC = (config.ReleasePlatforms |> Set.contains PC),
            ReleaseMac = (config.ReleasePlatforms |> Set.contains Mac),
            ProfilePath = config.ProfilePath,
            TestFolderPath = config.TestFolderPath,
            ProjectsFolderPath = config.ProjectsFolderPath,
            CharterName = config.CharterName,
            ShowAdvanced = config.ShowAdvanced,
            Locale = config.Locale.ShortName,
            GenerateDD = config.GenerateDD,
            DDPhraseSearchEnabled = config.DDPhraseSearchEnabled,
            DDPhraseSearchThreshold = config.DDPhraseSearchThreshold,
            DDLevelCountGeneration = levelCountGeneration,
            RemoveDDOnImport = config.RemoveDDOnImport,
            ApplyImprovements = config.ApplyImprovements,
            AutoVolume = config.AutoVolume,
            AutoSave = config.AutoSave,
            ConvertAudio = convertAudio,
            OpenFolderAfterReleaseBuild = config.OpenFolderAfterReleaseBuild,
            LoadPreviousOpenedProject = config.LoadPreviousOpenedProject,
            PreviousOpenedProject = config.PreviousOpenedProject,
            SaveDebugFiles = config.SaveDebugFiles,
            WwiseConsolePath = Option.toObj config.WwiseConsolePath,
            CustomAppId = Option.toObj config.CustomAppId
        )

    /// Loads a configuration from the file defined in configFilePath.
    let load (t: IStringLocalizer) = async {
        if not <| File.Exists configFilePath then
            return Configuration.Default
        else
            try
                use file = File.OpenRead configFilePath
                let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
                let! dto = JsonSerializer.DeserializeAsync<Dto>(file, options)
                return fromDto t dto
            with _ ->
                return Configuration.Default }

    /// Saves the configuration to the file defined in configFilePath.
    let save (config: Configuration) = async {
        use file = File.Create configFilePath
        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        do! JsonSerializer.SerializeAsync(file, toDto config, options) }
