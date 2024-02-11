module BuildValidatorTests

open DLCBuilder
open Expecto
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open System.IO

let vocals =
    { Id = ArrangementId.New
      XmlPath = ""
      Japanese = false
      CustomFont = None
      MasterId = 1
      PersistentId = Guid.NewGuid() }

let toneKey1 = "Tone_1"

let instrumental =
    { Instrumental.Empty with
          XmlPath = "instrumental.xml"
          BaseTone = "Base_Tone"
          Tones = [ toneKey1; "Tone_2"; "Tone_3"; "Tone_4" ]
          MasterId = 12345 }

let existingFile = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.*") |> Seq.head

let validProject =
    { Version = "1"
      DLCKey = "Abcdefghijklmn"
      ArtistName = SortableString.Create "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.Create "Title"
      AlbumName = SortableString.Create "Album"
      Year = DateTime.Now.Year
      AlbumArtFile = existingFile
      AudioFile = { AudioFile.Empty with Path = existingFile }
      AudioPreviewFile = { AudioFile.Empty with Path = existingFile }
      AudioPreviewStartTime = None
      PitchShift = None
      IgnoredIssues = Set.empty
      Arrangements = [ Instrumental instrumental ]
      Tones = List.empty
      Author = None }

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

        testCase "Detects project without any arrangements" <| fun _ ->
            let project = { validProject with Arrangements = List.empty }

            BuildValidator.validate project
            |> expectError NoArrangements

        testCase "Detects project without main audio file" <| fun _ ->
            let project = { validProject with AudioFile = AudioFile.Empty }

            BuildValidator.validate project
            |> expectError MainAudioFileNotSet

        testCase "Detects invalid DLC key" <| fun _ ->
            let project = { validProject with DLCKey = "a" }

            BuildValidator.validate project
            |> expectError InvalidDLCKey

        testCase "Detects empty artist name" <| fun _ ->
            let project = { validProject with ArtistName = { validProject.ArtistName with Value = "" } }

            BuildValidator.validate project
            |> expectError ArtistNameEmpty

        testCase "Detects missing album art file" <| fun _ ->
            let project = { validProject with AlbumArtFile = "na" }

            BuildValidator.validate project
            |> expectError AlbumArtNotFound

        testCase "Detects missing main audio file" <| fun _ ->
            let project = { validProject with AudioFile = { validProject.AudioFile with Path = "na" } }

            BuildValidator.validate project
            |> expectError MainAudioFileNotFound

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
            let project =
                let tone = { testTone with Key = toneKey1 }
                let instrumental2 = { instrumental with PersistentId = Guid.NewGuid(); MasterId = Random.Shared.Next() }
                { validProject with
                    Arrangements = [ Instrumental instrumental; Instrumental instrumental2 ]
                    Tones = [ tone; tone ] }

            BuildValidator.validate project
            |> expectError (MultipleTonesSameKey toneKey1)

        testCase "Detects conflicting vocals arrangements" <| fun _ ->
            let vocals2 = { vocals with PersistentId = Guid.NewGuid() }
            let project = { validProject with Arrangements = [ Vocals vocals; Vocals vocals2 ] }

            BuildValidator.validate project
            |> expectError ConflictingVocals
    ]
