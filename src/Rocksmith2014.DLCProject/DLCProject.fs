namespace Rocksmith2014.DLCProject

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

type DLCProject =
    { Version: string
      Author: string option
      DLCKey: string
      ArtistName: SortableString
      JapaneseArtistName: string option
      JapaneseTitle: string option
      Title: SortableString
      AlbumName: SortableString
      Year: int
      AlbumArtFile: string
      AudioFile: AudioFile
      AudioPreviewFile: AudioFile
      AudioPreviewStartTime: TimeSpan option
      PitchShift: int16 option
      IgnoredIssues: Set<string>
      Arrangements: Arrangement list
      Tones: Tone list }

    static member Empty =
        { Version = "1"
          Author = None
          DLCKey = String.Empty
          ArtistName = SortableString.Empty
          JapaneseArtistName = None
          JapaneseTitle = None
          Title = SortableString.Empty
          AlbumName = SortableString.Empty
          Year = DateTime.Now.Year
          AlbumArtFile = String.Empty
          AudioFile = AudioFile.Empty
          AudioPreviewFile = AudioFile.Empty
          AudioPreviewStartTime = None
          PitchShift = None
          IgnoredIssues = Set.empty
          Arrangements = []
          Tones = [] }

module DLCProject =
    type InstrumentalDto() =
        member val ID: Guid = Guid.NewGuid() with get, set
        member val XML: string = String.Empty with get, set
        member val Name: ArrangementName = ArrangementName.Lead with get, set
        member val RouteMask: RouteMask = RouteMask.None with get, set
        member val Priority: ArrangementPriority = ArrangementPriority.Main with get, set
        member val ScrollSpeed: float = 1.3 with get, set
        member val BassPicked: bool = false with get, set
        member val Tuning: int16 array = Array.empty with get, set
        member val TuningPitch: float = 440.0 with get, set
        member val BaseTone: string = String.Empty with get, set
        member val Tones: string array = Array.empty with get, set
        member val CustomAudio: AudioFile option = None with get, set
        member val ArrangementProperties: ArrangementPropertiesOverride.ArrPropFlags option = None with get, set
        member val MasterID: int = 0 with get, set
        member val PersistentID: Guid = Guid.Empty with get, set

    type VocalsDto() =
        member val ID: Guid = Guid.NewGuid() with get, set
        member val XML: string = String.Empty with get, set
        member val Japanese: bool = false with get, set
        member val CustomFont: string = null with get, set
        member val MasterID: int = 0 with get, set
        member val PersistentID: Guid = Guid.Empty with get, set

    type ShowlightsDto() =
        member val ID: Guid = Guid.NewGuid() with get, set
        member val XML: string = String.Empty with get, set

    [<RequireQualifiedAccess>]
    type ArrangementDto =
        | Instrumental of InstrumentalDto
        | Vocals of VocalsDto
        | Showlights of ShowlightsDto

    let private arrangementToDto (arr: Arrangement) =
        match arr with
        | Showlights sl ->
            ShowlightsDto(
                ID = ArrangementId.Value sl.Id,
                XML = sl.XmlPath
            )
            |> ArrangementDto.Showlights
        | Vocals v ->
            VocalsDto(
                ID = ArrangementId.Value v.Id,
                XML = v.XmlPath,
                Japanese = v.Japanese,
                CustomFont = Option.toObj v.CustomFont,
                MasterID = v.MasterId,
                PersistentID = v.PersistentId
            )
            |> ArrangementDto.Vocals
        | Instrumental i ->
            InstrumentalDto(
                ID = ArrangementId.Value i.Id,
                XML = i.XmlPath,
                Name = i.Name,
                RouteMask = i.RouteMask,
                Priority = i.Priority,
                ScrollSpeed = i.ScrollSpeed,
                BassPicked = i.BassPicked,
                Tuning = i.Tuning,
                TuningPitch = i.TuningPitch,
                BaseTone = i.BaseTone,
                Tones = Array.ofList i.Tones,
                CustomAudio = i.CustomAudio,
                ArrangementProperties = i.ArrangementProperties,
                MasterID = i.MasterId,
                PersistentID = i.PersistentId
            )
            |> ArrangementDto.Instrumental

    let arrangementFromDto (arr: ArrangementDto) =
        match arr with
        | ArrangementDto.Showlights sl ->
            Showlights { Id = ArrangementId sl.ID; XmlPath = sl.XML }
        | ArrangementDto.Vocals v ->
            {
                Id = ArrangementId v.ID
                XmlPath = v.XML
                Japanese = v.Japanese
                CustomFont = Option.ofObj v.CustomFont
                MasterId = v.MasterID
                PersistentId = v.PersistentID
            }
            |> Vocals
        | ArrangementDto.Instrumental i ->
            {
                Id = ArrangementId i.ID
                XmlPath = i.XML
                Name = i.Name
                RouteMask = i.RouteMask
                Priority = i.Priority
                ScrollSpeed = i.ScrollSpeed
                BassPicked = i.BassPicked
                Tuning = i.Tuning
                TuningPitch = i.TuningPitch
                BaseTone = i.BaseTone
                Tones = Array.toList i.Tones
                CustomAudio = i.CustomAudio
                ArrangementProperties = i.ArrangementProperties
                MasterId = i.MasterID
                PersistentId = i.PersistentID
            }
            |> Instrumental

    type Dto() =
        member val Version: string = String.Empty with get, set
        member val Author: string = String.Empty with get, set
        member val DLCKey: string = String.Empty with get, set
        member val ArtistName: SortableString = SortableString.Empty with get, set
        member val JapaneseArtistName: string = null with get, set
        member val JapaneseTitle: string = null with get, set
        member val Title: SortableString = SortableString.Empty with get, set
        member val AlbumName: SortableString = SortableString.Empty with get, set
        member val Year: int = DateTime.Now.Year with get, set
        member val AlbumArtFile: string = String.Empty with get, set
        member val AudioFile: AudioFile = AudioFile.Empty with get, set
        member val AudioPreviewFile = AudioFile.Empty with get, set
        member val AudioPreviewStartTime = Nullable<float>() with get, set
        member val PitchShift = Nullable<int16>() with get, set
        member val IgnoredIssues: string array = Array.empty with get, set
        member val Arrangements: ArrangementDto array = Array.empty with get, set
        member val Tones: ToneDto array = Array.empty with get, set

    let private toDto (project: DLCProject) =
        let tones =
            project.Tones
            |> List.map Tone.toDto
            |> Array.ofList

        let previewStart =
            project.AudioPreviewStartTime
            |> Option.map (fun x -> float x.TotalSeconds)
            |> Option.toNullable

        let arrangements =
            project.Arrangements
            |> List.map arrangementToDto
            |> Array.ofList

        Dto(
            Version = project.Version,
            Author = Option.toObj project.Author,
            DLCKey = project.DLCKey,
            ArtistName = project.ArtistName,
            JapaneseArtistName = Option.toObj project.JapaneseArtistName,
            JapaneseTitle = Option.toObj project.JapaneseTitle,
            Title = project.Title,
            AlbumName = project.AlbumName,
            Year = project.Year,
            AlbumArtFile = project.AlbumArtFile,
            AudioFile = project.AudioFile,
            AudioPreviewFile = project.AudioPreviewFile,
            AudioPreviewStartTime = previewStart,
            PitchShift = Option.toNullable project.PitchShift,
            IgnoredIssues = Set.toArray project.IgnoredIssues,
            Arrangements = arrangements,
            Tones = tones
        )

    let private fromDto (dto: Dto) =
        { Version = dto.Version
          Author = Option.ofString dto.Author
          DLCKey = dto.DLCKey
          ArtistName = dto.ArtistName
          JapaneseArtistName = Option.ofString dto.JapaneseArtistName
          JapaneseTitle = Option.ofString dto.JapaneseTitle
          Title = dto.Title
          AlbumName = dto.AlbumName
          Year = dto.Year
          AlbumArtFile = dto.AlbumArtFile
          AudioFile = dto.AudioFile
          AudioPreviewFile = dto.AudioPreviewFile
          AudioPreviewStartTime = dto.AudioPreviewStartTime |> Option.ofNullable |> Option.map TimeSpan.FromSeconds
          PitchShift = Option.ofNullable dto.PitchShift
          IgnoredIssues = dto.IgnoredIssues |> Set.ofArray
          Arrangements = dto.Arrangements |> List.ofArray |> List.map arrangementFromDto
          Tones = dto.Tones |> List.ofArray |> List.map Tone.fromDto }

    let private toAbsolutePath (baseDir: string) (fileName: string) =
        if String.IsNullOrWhiteSpace(fileName) || Path.IsPathFullyQualified(fileName) then
            fileName
        else
            Path.Combine(baseDir, fileName)

    /// Converts the paths in the project to absolute paths.
    let toAbsolutePaths (baseDir: string) (project: DLCProject) =
        let abs = toAbsolutePath baseDir
        let arrangements =
            project.Arrangements
            |> List.map (function
                | Instrumental i ->
                    { i with
                        XmlPath = abs i.XmlPath
                        CustomAudio = Option.map (fun x -> { x with Path = abs x.Path }) i.CustomAudio }
                    |> Instrumental
                | Vocals v ->
                    { v with
                        XmlPath = abs v.XmlPath
                        CustomFont = Option.map abs v.CustomFont }
                    |> Vocals
                | Showlights s ->
                    Showlights { s with XmlPath = abs s.XmlPath })

        { project with
            Arrangements = arrangements
            AlbumArtFile = abs project.AlbumArtFile
            AudioFile =
                { project.AudioFile with
                    Path = abs project.AudioFile.Path }
            AudioPreviewFile =
                { project.AudioPreviewFile with
                    Path = abs project.AudioPreviewFile.Path } }

    let private toRelativePath (relativeTo: string) (path: string) =
        if String.IsNullOrWhiteSpace(path) then
            path
        else
            Path.GetRelativePath(relativeTo, path)

    /// Converts the paths in the project relative to the given path.
    let toRelativePaths (path: string) (project: DLCProject) =
        let rel = toRelativePath path

        let arrangements =
            project.Arrangements
            |> List.map (function
                | Instrumental i ->
                    { i with
                        XmlPath = rel i.XmlPath
                        CustomAudio = Option.map (fun x -> { x with Path = rel x.Path }) i.CustomAudio }
                    |> Instrumental
                | Vocals v ->
                    { v with
                        XmlPath = rel v.XmlPath
                        CustomFont = Option.map rel v.CustomFont }
                    |> Vocals
                | Showlights s ->
                    Showlights { s with XmlPath = rel s.XmlPath })

        { project with
            Arrangements = arrangements
            AlbumArtFile = rel project.AlbumArtFile
            AudioFile =
                { project.AudioFile with
                    Path = rel project.AudioFile.Path }
            AudioPreviewFile =
                { project.AudioPreviewFile with
                    Path = rel project.AudioPreviewFile.Path } }

    /// Saves a project with the given filename.
    let save (path: string) (project: DLCProject) =
        backgroundTask {
            use file = File.Create(path)
            let options = FSharpJsonOptions.Create(indent = true, ignoreNull = JsonIgnoreCondition.WhenWritingNull)
            let dto =
                toRelativePaths (Path.GetDirectoryName(path)) project
                |> toDto
            do! JsonSerializer.SerializeAsync<Dto>(file, dto, options)
        }

    /// Loads a project from a file with the given filename.
    let load (path: string) =
        backgroundTask {
            let options = FSharpJsonOptions.Create(indent = true, ignoreNull = JsonIgnoreCondition.WhenWritingNull)
            use file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan ||| FileOptions.Asynchronous)
            let! project = JsonSerializer.DeserializeAsync<Dto>(file, options)
            return toAbsolutePaths (Path.GetDirectoryName(path)) (fromDto project)
        }

    /// Updates the tone names for the instrumental arrangements in the project from the XML files.
    let updateToneInfo (project: DLCProject) =
        let arrs =
            project.Arrangements
            |> List.map (function
                | Instrumental inst when File.Exists(inst.XmlPath) ->
                    Arrangement.updateToneInfo inst false
                    |> Instrumental
                | other ->
                    other)

        { project with Arrangements = arrs }

    /// Returns true if the audio files for the project exist.
    let audioFilesExist (project: DLCProject) =
        File.Exists(project.AudioFile.Path)
        && File.Exists(project.AudioPreviewFile.Path)

    /// Returns a sequence of the audio files in the project.
    let getAudioFiles (project: DLCProject) =
        seq {
            project.AudioFile
            project.AudioPreviewFile
            yield! project.Arrangements
                   |> List.choose (function Instrumental i -> i.CustomAudio | _ -> None)
        }

    let private needsConversion (convertIfNewerThan: TimeSpan) (path: string) =
        let wemPath = Path.ChangeExtension(path, "wem")

        if String.endsWith "wem" path then
            // Never convert if audio explicitly set to wem file
            false
        elif not <| File.Exists(wemPath) then
            // Convert if wem file does not exist
            true
        else
            let sourceAudioDate = FileInfo(path).LastWriteTime
            let wemAudioDate = FileInfo(wemPath).LastWriteTime

            // Convert if source file is newer then the existing wem file
            sourceAudioDate - wemAudioDate >= convertIfNewerThan

    /// Returns the paths to the audio files that need converting to wem.
    let getFilesThatNeedConverting (convertIfNewerThan: TimeSpan) (project: DLCProject) =
        getAudioFiles project
        |> Seq.map (fun audio -> audio.Path)
        |> Seq.filter (needsConversion convertIfNewerThan)
