namespace Rocksmith2014.DLCProject

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open System

type AudioFile = { Path : string; Volume : float }

type SortableString =
    { Value: string; SortValue: string }

    static member Create(value, ?sort) =
        let sort = defaultArg sort null
        { Value = value
          SortValue =
            if String.IsNullOrWhiteSpace sort then value else sort
            |> StringValidator.sortField }

    static member Empty = { Value = String.Empty; SortValue = String.Empty }

type DLCProject =
    { Version: float
      DLCKey : string
      //AppID : int = 221680 
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
        { Version = 1.
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
