[<AutoOpen>]
module Common

open System
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject

let testLead =
    { XML = "instrumental.xml"
      Name = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      Priority = ArrangementPriority.Main
      TuningPitch = 440.
      Tuning = [|0s;0s;0s;0s;0s;0s|]
      BaseTone = "Base_Tone"
      Tones = ["Tone_1"; "Tone_2"; "Tone_3"; "Tone_4"]
      ScrollSpeed = 1.3
      BassPicked = false
      MasterID = 12345
      PersistentID = Guid.NewGuid()
      CustomAudio = None }

let testLeadCapo =
    { XML = "instrumental_capo.xml"
      Name = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      Priority = ArrangementPriority.Main
      TuningPitch = 452.89
      Tuning = [|-1s;-2s;-3s;-4s;-5s;-6s|]
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
      AudioPreviewFile = { Path = "audio_preview.wav"; Volume = 1. }
      AudioPreviewStartTime = None
      PitchShift = None
      Arrangements = [ Instrumental testLead ]
      Tones = [ testTone; { testTone with Key = "Tone_2" } ] }
