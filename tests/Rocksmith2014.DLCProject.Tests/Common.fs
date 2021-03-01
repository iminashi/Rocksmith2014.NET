[<AutoOpen>]
module Common

open System
open Rocksmith2014.DLCProject

let testLead =
    { XML = "instrumental.xml"
      Name = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      Priority = ArrangementPriority.Main
      TuningPitch = 440.
      Tuning = [|0s;0s;0s;0s;0s;0s|]
      BaseTone = "Base_Tone"
      Tones = ["Tone_1"; "Tone_2"; "Tone_3"]
      ScrollSpeed = 1.3
      BassPicked = false
      MasterID = 12345
      PersistentID = Guid.NewGuid()
      CustomAudio = None }

let testVocals =
    { XML = "vocals.xml"
      Japanese = false
      CustomFont = None
      MasterID = 54321
      PersistentID = Guid.NewGuid() }

let testJVocals =
    { XML = "jvocals.xml"
      Japanese = true
      CustomFont = Some "font.dds"
      MasterID = 123456
      PersistentID = Guid.NewGuid() }

let testProject =
    { Version = "1.0"
      DLCKey = "SomeTest"
      ArtistName = SortableString.Create "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.Create "Title"
      AlbumName = SortableString.Create "Album"
      Year = 2020
      AlbumArtFile = "cover.dds"
      AudioFile = { Path = "audio.ogg"; Volume = 1. }
      AudioPreviewFile = { Path = "audio_preview.wav"; Volume = 1. }
      AudioPreviewStartTime = None
      Arrangements = [ Instrumental testLead ]
      Tones = [] }
