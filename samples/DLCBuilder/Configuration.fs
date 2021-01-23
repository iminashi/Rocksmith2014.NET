namespace DLCBuilder

open Rocksmith2014.Common
open System
open System.IO
open System.Text.Json

type Configuration =
    { ReleasePlatforms : Platform list
      ProfilePath : string
      TestFolderPath : string
      ProjectsFolderPath : string
      CharterName : string
      ShowAdvanced : bool
      GenerateDD : bool
      DDPhraseSearchEnabled : bool
      DDPhraseSearchThreshold : int
      ApplyImprovements : bool
      SaveDebugFiles : bool
      AutoVolume : bool
      Locale : Locale
      WwiseConsolePath : string option
      CustomAppId : string option }

    static member Default =
        { ReleasePlatforms = [ PC; Mac ]
          ProfilePath = String.Empty
          TestFolderPath = String.Empty
          ProjectsFolderPath = String.Empty
          CharterName = String.Empty
          ShowAdvanced = false
          GenerateDD = true
          DDPhraseSearchEnabled = true
          DDPhraseSearchThreshold = 80
          ApplyImprovements = true
          SaveDebugFiles = false
          AutoVolume = true
          Locale = Locales.English
          WwiseConsolePath = None
          CustomAppId = None }

module Configuration =
    type Dto() =
        member val ReleasePC : bool = true with get, set
        member val ReleaseMac : bool = true with get, set
        member val ProfilePath : string = String.Empty with get, set
        member val TestFolderPath : string = String.Empty with get, set
        member val ProjectsFolderPath : string = String.Empty with get, set
        member val CharterName : string = String.Empty with get, set
        member val ShowAdvanced : bool = false with get, set
        member val GenerateDD : bool = true with get, set
        member val DDPhraseSearchEnabled : bool = true with get, set
        member val DDPhraseSearchThreshold : int = 80 with get, set
        member val ApplyImprovements : bool = true with get, set
        member val SaveDebugFiles : bool = false with get, set
        member val AutoVolume : bool = true with get, set
        member val Locale : string = "en" with get, set
        member val WwiseConsolePath : string = String.Empty with get, set
        member val CustomAppId : string = String.Empty with get, set

    let appDataFolder =
        IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rs2-dlcbuilder")

    let private configFilePath =
        IO.Path.Combine(appDataFolder, "config.json")

    /// Converts a configuration DTO into a configuration record.
    let private fromDto (dto: Dto) =
        let platforms =
            if not (dto.ReleasePC || dto.ReleaseMac) then
                [ PC; Mac ]
            else
                [ if dto.ReleasePC then PC
                  if dto.ReleaseMac then Mac ]

        let threshold = Math.Clamp(dto.DDPhraseSearchThreshold, 0, 100)

        { ReleasePlatforms = platforms
          ProfilePath = dto.ProfilePath
          TestFolderPath = dto.TestFolderPath
          ProjectsFolderPath = dto.ProjectsFolderPath
          CharterName = dto.CharterName
          ShowAdvanced = dto.ShowAdvanced
          GenerateDD = dto.GenerateDD
          DDPhraseSearchEnabled = dto.DDPhraseSearchEnabled
          DDPhraseSearchThreshold = threshold
          ApplyImprovements = dto.ApplyImprovements
          SaveDebugFiles = dto.SaveDebugFiles
          AutoVolume = dto.AutoVolume
          Locale = Locales.fromShortName dto.Locale
          WwiseConsolePath = Option.ofString dto.WwiseConsolePath
          CustomAppId = Option.ofString dto.CustomAppId }

    /// Converts a configuration into a configuration DTO.
    let private toDto (config: Configuration) =
        Dto(ReleasePC = (config.ReleasePlatforms |> List.contains PC),
            ReleaseMac = (config.ReleasePlatforms |> List.contains Mac),
            ProfilePath = config.ProfilePath,
            TestFolderPath = config.TestFolderPath,
            ProjectsFolderPath = config.ProjectsFolderPath,
            CharterName = config.CharterName,
            ShowAdvanced = config.ShowAdvanced,
            Locale = config.Locale.ShortName,
            GenerateDD = config.GenerateDD,
            DDPhraseSearchEnabled = config.DDPhraseSearchEnabled,
            DDPhraseSearchThreshold = config.DDPhraseSearchThreshold,
            ApplyImprovements = config.ApplyImprovements,
            AutoVolume = config.AutoVolume,
            SaveDebugFiles = config.SaveDebugFiles,
            WwiseConsolePath = Option.toObj config.WwiseConsolePath,
            CustomAppId = Option.toObj config.CustomAppId)
    
    /// Loads a configuration from the file defined in configFilePath.
    let load () = async {
        if not <| File.Exists configFilePath then
            return Configuration.Default
        else
            try
                use file = File.OpenRead configFilePath
                let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
                let! dto = JsonSerializer.DeserializeAsync<Dto>(file, options)
                return fromDto dto
            with _ -> return Configuration.Default }
    
    /// Saves the configuration to the file defined in configFilePath.
    let save (config: Configuration) = async {
        Directory.CreateDirectory(Path.GetDirectoryName configFilePath) |> ignore
        use file = File.Create configFilePath
        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        do! JsonSerializer.SerializeAsync(file, toDto config, options) }
