namespace Rocksmith2014.DLCProject

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

type DLCProject =
    { Version : string
      DLCKey : string
      ArtistName : SortableString
      JapaneseArtistName : string option
      JapaneseTitle : string option
      Title : SortableString
      AlbumName : SortableString
      Year : int
      AlbumArtFile : string
      AudioFile : AudioFile
      AudioPreviewFile : AudioFile
      AudioPreviewStartTime : float option
      Arrangements : Arrangement list
      Tones : Tone list }

    static member Empty =
        { Version = "1"
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
          Arrangements = []
          Tones = [] }

module DLCProject =
    type Dto() =
        member val Version : string = String.Empty with get, set
        member val DLCKey : string = String.Empty with get, set
        member val ArtistName : SortableString = SortableString.Empty with get, set
        member val JapaneseArtistName : string = null with get, set
        member val JapaneseTitle : string = null with get, set
        member val Title : SortableString = SortableString.Empty with get, set
        member val AlbumName : SortableString = SortableString.Empty with get, set
        member val Year : int = DateTime.Now.Year with get, set
        member val AlbumArtFile : string = String.Empty with get, set
        member val AudioFile : AudioFile = AudioFile.Empty with get, set
        member val AudioPreviewFile = AudioFile.Empty with get, set
        member val AudioPreviewStartTime = Nullable<float>() with get, set
        member val Arrangements : Arrangement array = Array.empty with get, set
        member val Tones : ToneDto array = Array.empty with get, set

    let private toDto (project: DLCProject) =
        let tones =
            project.Tones
            |> List.map Tone.toDto
            |> Array.ofList

        Dto(Version = project.Version,
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
            AudioPreviewStartTime = Option.toNullable project.AudioPreviewStartTime,
            Arrangements = Array.ofList project.Arrangements,
            Tones = tones)

    let private fromDto (dto: Dto) =
        { Version = dto.Version
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
          AudioPreviewStartTime = Option.ofNullable dto.AudioPreviewStartTime
          Arrangements = dto.Arrangements |> List.ofArray
          Tones = dto.Tones |> List.ofArray |> List.map Tone.fromDto }

    let private toAbsolutePath (baseDir: string) (fileName: string) =
        if String.IsNullOrWhiteSpace fileName then
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
                    Instrumental { i with XML = abs i.XML
                                          CustomAudio = Option.map (fun x -> { x with Path = abs x.Path }) i.CustomAudio }
                | Vocals v ->
                    Vocals { v with XML = abs v.XML
                                    CustomFont = Option.map abs v.CustomFont }
                | Showlights s ->
                    Showlights { s with XML = abs s.XML })

        { project with Arrangements = arrangements
                       AlbumArtFile = abs project.AlbumArtFile
                       AudioFile = { project.AudioFile with Path = abs project.AudioFile.Path }
                       AudioPreviewFile = { project.AudioPreviewFile with Path = abs project.AudioPreviewFile.Path } }

    let private toRelativePath (relativeTo: string) (path: string) =
        if String.IsNullOrWhiteSpace path then
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
                    Instrumental { i with XML = rel i.XML
                                          CustomAudio = Option.map (fun x -> { x with Path = rel x.Path }) i.CustomAudio }
                | Vocals v ->
                    Vocals { v with XML = rel v.XML
                                    CustomFont = Option.map rel v.CustomFont }
                | Showlights s ->
                    Showlights { s with XML = rel s.XML })

        { project with Arrangements = arrangements
                       AlbumArtFile = rel project.AlbumArtFile
                       AudioFile = { project.AudioFile with Path = rel project.AudioFile.Path }
                       AudioPreviewFile = { project.AudioPreviewFile with Path = rel project.AudioPreviewFile.Path } }

    /// Saves a project with the given filename.
    let save (fileName: string) (project: DLCProject) = async {
        use file = File.Create fileName
        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        options.Converters.Add(JsonFSharpConverter())
        let p =
            toRelativePaths (Path.GetDirectoryName fileName) project
            |> toDto
        do! JsonSerializer.SerializeAsync<Dto>(file, p, options) }

    /// Loads a project from a file with the given filename.
    let load (fileName: string) = async {
        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        options.Converters.Add(JsonFSharpConverter())
        use file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan ||| FileOptions.Asynchronous)
        let! project = JsonSerializer.DeserializeAsync<Dto>(file, options)
        return toAbsolutePaths (Path.GetDirectoryName fileName) (fromDto project)  }

    /// Updates the tone names for the instrumental arrangements in the project from the XML files.
    let updateToneInfo (project: DLCProject) =
        let arrs =
            project.Arrangements
            |> List.map (function
                | Instrumental inst -> Arrangement.updateToneInfo inst false |> Instrumental
                | other -> other)

        { project with Arrangements = arrs }

    /// Returns true if the audio files for the project exist.
    let audioFilesExist (project: DLCProject) =
        File.Exists project.AudioFile.Path
        &&
        File.Exists project.AudioPreviewFile.Path

    /// Returns the paths to the audio files that need converting to wem.
    let getFilesThatNeedConverting (project: DLCProject) =
        seq { project.AudioFile
              project.AudioPreviewFile
              yield! project.Arrangements
                     |> List.choose (function Instrumental i -> i.CustomAudio | _ -> None) }
        |> Seq.map (fun audio -> audio.Path)
        |> Seq.filter (fun path -> not <| File.Exists(Path.ChangeExtension(path, "wem")))
