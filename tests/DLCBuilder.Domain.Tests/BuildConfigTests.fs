module BuildConfigTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open Rocksmith2014.DD

let generalConfig =
    { Configuration.Default with
        CharterName = "test charter"
        CustomAppId = Some "custom id"
        GenerateDD = false
        SaveDebugFiles = true }

let project =
    { Version = "1"
      DLCKey = "DLCKey"
      ArtistName = SortableString.Create "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.Create "Title"
      AlbumName = SortableString.Create "Album"
      Year = DateTime.Now.Year
      AlbumArtFile = ""
      AudioFile = AudioFile.Empty
      AudioPreviewFile = AudioFile.Empty
      AudioPreviewStartTime = None
      PitchShift = None
      Arrangements = List.empty
      Tones = List.empty }

let platforms = [ PC; Mac ]

[<Tests>]
let tests =
    testList "Build Configuration Tests" [
        testCase "Creates valid configuration for test build" <| fun _ ->
            let buildConfig = BuildConfig.create Test generalConfig project platforms

            Expect.equal buildConfig.AppId "custom id" "Custom app ID is correct"
            Expect.equal buildConfig.ApplyImprovements generalConfig.ApplyImprovements "Apply improvements is correct"
            Expect.equal buildConfig.Author generalConfig.CharterName "Author is correct"
            Expect.stringStarts buildConfig.BuilderVersion "DLC Builder " "Builder version is correct"
            Expect.equal buildConfig.GenerateDD generalConfig.GenerateDD "Generate DD is correct"
            Expect.isNone buildConfig.IdResetConfig "ID reset config is none"
            Expect.equal buildConfig.Platforms platforms "Platforms are correct"
            Expect.equal buildConfig.SaveDebugFiles generalConfig.SaveDebugFiles "Save debug files is correct"

            Expect.equal buildConfig.DDConfig.LevelCountGeneration generalConfig.DDLevelCountGeneration "DD level count generation is correct"
            Expect.equal buildConfig.DDConfig.PhraseSearch (PhraseSearch.WithThreshold 80) "DD phrase search is correct"

        testCase "Creates valid configuration for release build" <| fun _ ->
            let generalConfig = { generalConfig with DDPhraseSearchEnabled = false; DDLevelCountGeneration = LevelCountGeneration.MLModel }

            let buildConfig = BuildConfig.create Release generalConfig project [ PC ]

            Expect.equal buildConfig.AppId "248750" "App ID defaulted to Cherub Rock"
            Expect.equal buildConfig.ApplyImprovements generalConfig.ApplyImprovements "Apply improvements is correct"
            Expect.equal buildConfig.Author generalConfig.CharterName "Author is correct"
            Expect.stringStarts buildConfig.BuilderVersion "DLC Builder " "Builder version is correct"
            Expect.isTrue buildConfig.GenerateDD "Generate DD defaulted to true"
            Expect.isNone buildConfig.IdResetConfig "ID reset config is none"
            Expect.equal buildConfig.Platforms [ PC ] "Platforms are correct"
            Expect.isFalse buildConfig.SaveDebugFiles "Save debug files defaulted to false"

            Expect.equal buildConfig.DDConfig.LevelCountGeneration generalConfig.DDLevelCountGeneration "DD level count generation is correct"
            Expect.equal buildConfig.DDConfig.PhraseSearch PhraseSearch.SearchDisabled "DD phrase search is correct"
    ]
