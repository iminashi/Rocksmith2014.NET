namespace Rocksmith2014.DLCProject

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

type AudioFile = { Path : string; Volume : float }

type SortableString =
    { Value: string; SortValue: string }

    static member Create(value, ?sort) =
        let sort = defaultArg sort null
        { Value = value
          SortValue =
            if String.IsNullOrWhiteSpace sort then
                StringValidator.removeArticles value
            else
                sort
            |> StringValidator.sortField }

    static member Empty = { Value = String.Empty; SortValue = String.Empty }

type DLCProject =
    { Version: string
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
          AudioFile = { Path = String.Empty; Volume = -8. }
          AudioPreviewFile = { Path = String.Empty; Volume = -7. }
          Arrangements = []
          Tones = [] }

module DLCProject =
    let private toAbsolutePath (baseDir: string) (fileName: string) =
        if String.IsNullOrWhiteSpace fileName then
            fileName
        else
            Path.Combine(baseDir, fileName)

    let private toAbsolutePaths (baseDir: string) (project: DLCProject) =
        let abs = toAbsolutePath baseDir
        let arrangements =
            project.Arrangements
            |> List.map (fun x ->
                match x with
                | Instrumental i -> Instrumental { i with XML = abs i.XML }
                | Vocals v -> Vocals { v with XML = abs v.XML; CustomFont = Option.map abs v.CustomFont }
                | Showlights s -> Showlights { s with XML = abs s.XML })

        { project with Arrangements = arrangements
                       AlbumArtFile = abs project.AlbumArtFile
                       AudioFile = { project.AudioFile with Path = abs project.AudioFile.Path }
                       AudioPreviewFile = { project.AudioPreviewFile with Path = abs project.AudioPreviewFile.Path } }

    let private toRelativePath (relativeTo: string) (path: string) =
        if String.IsNullOrWhiteSpace path then
            path
        else
            Path.GetRelativePath(relativeTo, path)

    let private toRelativePaths (path: string) (project: DLCProject) =
        let rel = toRelativePath path
        let arrangements =
            project.Arrangements
            |> List.map (fun x ->
                match x with
                | Instrumental i -> Instrumental { i with XML = rel i.XML }
                | Vocals v -> Vocals { v with XML = rel v.XML; CustomFont = Option.map rel v.CustomFont }
                | Showlights s -> Showlights { s with XML = rel s.XML })

        { project with Arrangements = arrangements
                       AlbumArtFile = rel project.AlbumArtFile
                       AudioFile = { project.AudioFile with Path = rel project.AudioFile.Path }
                       AudioPreviewFile = { project.AudioPreviewFile with Path = rel project.AudioPreviewFile.Path } }

    let save (fileName: string) (project: DLCProject) = async {
        use file = File.Create fileName
        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        options.Converters.Add(JsonFSharpConverter())
        let p = toRelativePaths (Path.GetDirectoryName fileName) project
        do! JsonSerializer.SerializeAsync(file, p, options) }

    let load (fileName: string) = async {
        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        options.Converters.Add(JsonFSharpConverter())
        use file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan ||| FileOptions.Asynchronous)
        let! project = JsonSerializer.DeserializeAsync<DLCProject>(file, options)
        return toAbsolutePaths (Path.GetDirectoryName fileName) project  }

module DLCKey =
    let create (charterName: string) (artist: string) (title: string) =
        let prefix =
            let name = StringValidator.dlcKey charterName
            if String.IsNullOrWhiteSpace name || name.Length < 2 then
                String([| RandomGenerator.nextAlphabet(); RandomGenerator.nextAlphabet() |])
            else
                name.Substring(0, 2)
        let validArtist = StringValidator.dlcKey artist
        let validTitle = StringValidator.dlcKey title

        prefix
        + validArtist.Substring(0, Math.Min(5, validArtist.Length))
        + validTitle.Substring(0, Math.Min(5, validTitle.Length))
