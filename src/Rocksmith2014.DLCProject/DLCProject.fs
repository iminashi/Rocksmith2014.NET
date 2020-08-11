namespace Rocksmith2014.DLCProject

open Rocksmith2014.Common.Manifest
open System

type AudioFile = { Path : string; Volume : float }
type SortableString =
    { Value: string; SortValue: string }

    static member makeSimple(value) = { Value = value; SortValue = value }
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
      // TODO: Move to arrangement
      CentOffset : float
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
          AudioFile = { Path = String.Empty; Volume = -7. }
          AudioPreviewFile = { Path = String.Empty; Volume = -8. }
          CentOffset = 0.
          Arrangements = []
          Tones = [] }
