namespace Rocksmith2014.DLCProject

type DLCProject =
    { DLCKey : string
      AppID : int
      ArtistName : string
      ArtistNameSort : string
      JapaneseArtistName : string option
      JapaneseTitle : string option
      Title : string
      TitleSort : string
      AlbumName : string
      AlbumNameSort : string
      Year : int
      AlbumArtFile : string
      AudioFile : string
      AudioPreviewFile : string
      TuningPitch : float
      Arrangements : Arrangement list
      Tones : Tone list }
