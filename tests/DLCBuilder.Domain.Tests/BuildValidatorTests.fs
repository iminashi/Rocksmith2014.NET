module BuildValidatorTests

open DLCBuilder
open Expecto
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open System

let tone =
    let gear =
        let pedal =
            { Type = ""
              KnobValues = Map.empty
              Key = ""
              Category = None
              Skin = None
              SkinIndex = None }

        { Amp = pedal
          Cabinet = pedal
          Racks =  [||]
          PrePedals =  [||]
          PostPedals =  [||] }

    { GearList = gear
      ToneDescriptors = [||]
      NameSeparator = " - "
      Volume = 17.
      MacVolume = None
      Key = "tone_key"
      Name = "tone_name"
      SortOrder = None }

let vocals =
    { XML = ""
      Japanese = false
      CustomFont = None
      MasterID = 1
      PersistentID = Guid.NewGuid() }

let instrumental =
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

let validProject =
    { Version = "1"
      DLCKey = "Abcdefghijklmn"
      ArtistName = SortableString.Create "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.Create "Title"
      AlbumName = SortableString.Create "Album"
      Year = DateTime.Now.Year
      AlbumArtFile = "DLCBuilder.Tests.exe"
      AudioFile = { AudioFile.Empty with Path = "DLCBuilder.Tests.exe" }
      AudioPreviewFile = { AudioFile.Empty with Path = "DLCBuilder.Tests.exe" }
      AudioPreviewStartTime = None
      PitchShift = None
      Arrangements = List.empty
      Tones = List.empty }

let expectError errorType result =
    match result with
    | Ok () ->
        failwith "Expected error."
    | Error error ->
        Expect.equal error errorType "Validation error is correct"

[<Tests>]
let tests =
    testList "Build Validator Tests" [
        testCase "Valid project passes" <| fun _ ->
            let result = BuildValidator.validate validProject

            Expect.isOk result "Validation passed"

        testCase "Detects invalid DLC key" <| fun _ ->
            let project = { validProject with DLCKey = "a" }

            BuildValidator.validate project
            |> expectError InvalidDLCKey

        testCase "Detects empty artist name" <| fun _ ->
            let project = { validProject with ArtistName = { validProject.ArtistName with Value = "" } }

            BuildValidator.validate project
            |> expectError ArtistNameEmpty

        testCase "Detects missing title sort value" <| fun _ ->
            let project = { validProject with Title = { validProject.Title with SortValue = "" } }

            BuildValidator.validate project
            |> expectError TitleEmpty

        testCase "Detects missing album art file" <| fun _ ->
            let project = { validProject with AlbumArtFile = "na" }

            BuildValidator.validate project
            |> expectError AlbumArtNotFound

        testCase "Detects missing preview audio file" <| fun _ ->
            let project = { validProject with AudioPreviewFile = { validProject.AudioPreviewFile with Path = "na" } }

            BuildValidator.validate project
            |> expectError PreviewNotFound

        testCase "Detects missing base tone key" <| fun _ ->
            let invalidInstrumental = { instrumental with BaseTone = "" }
            let project = { validProject with Arrangements = [ Instrumental invalidInstrumental ] }

            BuildValidator.validate project
            |> expectError MissingBaseToneKey

        testCase "Detects same persistent ID" <| fun _ ->
            let project = { validProject with Arrangements = [ Instrumental instrumental; Instrumental instrumental ] }

            BuildValidator.validate project
            |> expectError SamePersistentID

        testCase "Detects same key on multiple tones" <| fun _ ->
            let project = { validProject with Tones = [ tone; tone ] }

            BuildValidator.validate project
            |> expectError MultipleTonesSameKey

        testCase "Detects conflicting vocals arrangements" <| fun _ ->
            let vocals2 = { vocals with PersistentID = Guid.NewGuid() }
            let project = { validProject with Arrangements = [ Vocals vocals; Vocals vocals2 ] }

            BuildValidator.validate project
            |> expectError ConflictingVocals
    ]
