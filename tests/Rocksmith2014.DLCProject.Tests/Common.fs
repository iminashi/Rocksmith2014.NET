[<AutoOpen>]
module Common

open System
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject

let testLead =
    { Instrumental.Empty with
          XmlPath = "instrumental.xml"
          BaseTone = "Base_Tone"
          Tones = [ "Tone_1"; "Tone_2"; "Tone_3"; "Tone_4" ]
          MasterId = 12345 }

let testLeadCapo =
    { Instrumental.Empty with
          XmlPath = "instrumental_capo.xml"
          TuningPitch = 452.89
          Tuning = [| -1s;-2s;-3s;-4s;-5s;-6s |]
          BaseTone = "Base_Tone"
          Tones = [ "Tone_1"; "Tone_2"; "Tone_3" ]
          MasterId = 12345 }

let testVocals =
    { Id = ArrangementId.New
      XmlPath = "vocals.xml"
      Japanese = false
      CustomFont = None
      MasterId = 54321
      PersistentId = Guid.NewGuid() }

let testJVocals =
    { Id = ArrangementId.New
      XmlPath = "jvocals.xml"
      Japanese = true
      CustomFont = Some "font.dds"
      MasterId = 123456
      PersistentId = Guid.NewGuid() }

let testTone =
    let testGear =
        let pedal =
          { Type = "test"
            KnobValues = Map.empty
            Key = "test"
            Category = None
            Skin = None
            SkinIndex = None }

        { Amp = pedal
          Cabinet = pedal
          Racks = Array.empty
          PrePedals = Array.empty
          PostPedals = Array.empty }

    { GearList= testGear
      ToneDescriptors = [| "Clean" |]
      NameSeparator = " - "
      Volume = -20.
      MacVolume = None
      Key = "Tone_1"
      Name = "tone_name"
      SortOrder = None }

let testProject =
    { Version = "1.0"
      DLCKey = "SomeTest"
      ArtistName = SortableString.Create("Artist", "ArtistSort")
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.Create("Title", "TitleSort")
      AlbumName = SortableString.Create("Album", "AlbumSort")
      Year = 2020
      AlbumArtFile = "cover.dds"
      AudioFile = { Path = "audio.ogg"; Volume = 1. }
      AudioFileLength = None
      AudioPreviewFile = { Path = "audio_preview.wav"; Volume = 1. }
      AudioPreviewStartTime = None
      PitchShift = None
      IgnoredIssues = Set.empty
      Arrangements = [ Instrumental testLead ]
      Tones = [ testTone; { testTone with Key = "Tone_2" } ]
      Author = None }
