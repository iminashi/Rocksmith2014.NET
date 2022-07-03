module BuildValidatorTests

open DLCBuilder
open Expecto
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open System.IO

let vocals =
    { XML = ""
      Japanese = false
      CustomFont = None
      MasterID = 1
      PersistentID = Guid.NewGuid() }

let instrumental =
    { Instrumental.Empty with 
          XML = "instrumental.xml"
          BaseTone = "Base_Tone"
          Tones = [ "Tone_1"; "Tone_2"; "Tone_3"; "Tone_4" ]
          MasterID = 12345 }

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
      Arrangements = List.empty
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
            let project = { validProject with Tones = [ testTone; testTone ] }

            BuildValidator.validate project
            |> expectError MultipleTonesSameKey

        testCase "Detects conflicting vocals arrangements" <| fun _ ->
            let vocals2 = { vocals with PersistentID = Guid.NewGuid() }
            let project = { validProject with Arrangements = [ Vocals vocals; Vocals vocals2 ] }

            BuildValidator.validate project
            |> expectError ConflictingVocals
    ]
