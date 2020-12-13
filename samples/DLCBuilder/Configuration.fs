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
      Locale : Locale
      WwiseConsolePath : string option }

    static member Default =
        { ReleasePlatforms = [ PC; Mac ]
          ProfilePath = String.Empty
          TestFolderPath = String.Empty
          ProjectsFolderPath = String.Empty
          CharterName = String.Empty
          ShowAdvanced = false
          Locale = Locales.English
          WwiseConsolePath = None }

module Configuration =
    type Dto() =
        member val ReleasePC : bool = true with get, set
        member val ReleaseMac : bool = true with get, set
        member val ProfilePath : string = String.Empty with get, set
        member val TestFolderPath : string = String.Empty with get, set
        member val ProjectsFolderPath : string = String.Empty with get, set
        member val CharterName : string = String.Empty with get, set
        member val ShowAdvanced : bool = false with get, set
        member val Locale : string = "en" with get, set
        member val WwiseConsolePath : string = String.Empty with get, set

    let private configFilePath =
        let appData = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rs2-dlcbuilder")
        IO.Path.Combine(appData, "config.json")

    /// Converts a configuration DTO into a configuration record.
    let private fromDto (dto: Dto) =
        let platforms =
            if not (dto.ReleasePC || dto.ReleaseMac) then
                [ PC; Mac ]
            else
                [ if dto.ReleasePC then PC
                  if dto.ReleaseMac then Mac ]

        { ReleasePlatforms = platforms
          ProfilePath = dto.ProfilePath
          TestFolderPath = dto.TestFolderPath
          ProjectsFolderPath = dto.ProjectsFolderPath
          CharterName = dto.CharterName
          ShowAdvanced = dto.ShowAdvanced
          Locale = Locales.fromShortName dto.Locale
          WwiseConsolePath = Option.ofString dto.WwiseConsolePath }

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
            WwiseConsolePath = Option.toObj config.WwiseConsolePath)
    
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
