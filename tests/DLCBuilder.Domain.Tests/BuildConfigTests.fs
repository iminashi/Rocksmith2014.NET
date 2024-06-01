module BuildConfigTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DD
open Rocksmith2014.DLCProject
open System

let private customAppId = AppId 77777UL

let generalConfig =
    { Configuration.Default with
        CharterName = "test charter"
        CustomAppId = Some customAppId
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
      AudioFileLength = None
      AudioPreviewFile = AudioFile.Empty
      AudioPreviewStartTime = None
      PitchShift = None
      IgnoredIssues = Set.empty
      Arrangements = List.empty
      Tones = List.empty
      Author = None }

let platforms = [ PC; Mac ]

[<Tests>]
let tests =
    testList "Build Configuration Tests" [
        testCase "Creates valid configuration for test build" <| fun _ ->
            let buildConfig = BuildConfig.create Test generalConfig None project platforms

            Expect.equal buildConfig.AppId customAppId "Custom app ID is correct"
            Expect.equal buildConfig.ApplyImprovements generalConfig.ApplyImprovements "Apply improvements is correct"
            Expect.equal buildConfig.Author generalConfig.CharterName "Author is correct"
            Expect.stringStarts buildConfig.BuilderVersion "DLC Builder " "Builder version is correct"
            Expect.equal buildConfig.GenerateDD generalConfig.GenerateDD "Generate DD is correct"
            Expect.isNone buildConfig.IdResetConfig "ID reset config is none"
            Expect.equal buildConfig.Platforms platforms "Platforms are correct"
            Expect.equal buildConfig.SaveDebugFiles generalConfig.SaveDebugFiles "Save debug files is correct"

            Expect.equal buildConfig.DDConfig.LevelCountGeneration generalConfig.DDLevelCountGeneration "DD level count generation is correct"
            Expect.equal buildConfig.DDConfig.PhraseSearchThreshold (Some 80) "DD phrase search threshold is correct"

        testCase "Creates valid configuration for release build" <| fun _ ->
            let generalConfig = { generalConfig with DDPhraseSearchEnabled = false; DDLevelCountGeneration = LevelCountGeneration.MLModel }
            let customAuthor = "Moai Moahashi"
            let project = { project with Author = Some customAuthor }

            let buildConfig = BuildConfig.create Release generalConfig None project [ PC ]

            Expect.equal buildConfig.AppId AppId.CherubRock "App ID defaulted to Cherub Rock"
            Expect.equal buildConfig.ApplyImprovements generalConfig.ApplyImprovements "Apply improvements is correct"
            Expect.equal buildConfig.Author customAuthor "Author is correct"
            Expect.stringStarts buildConfig.BuilderVersion "DLC Builder " "Builder version is correct"
            Expect.isTrue buildConfig.GenerateDD "Generate DD defaulted to true"
            Expect.isNone buildConfig.IdResetConfig "ID reset config is none"
            Expect.equal buildConfig.Platforms [ PC ] "Platforms are correct"
            Expect.isFalse buildConfig.SaveDebugFiles "Save debug files defaulted to false"

            Expect.equal buildConfig.DDConfig.LevelCountGeneration generalConfig.DDLevelCountGeneration "DD level count generation is correct"
            Expect.equal buildConfig.DDConfig.PhraseSearchThreshold None "DD phrase search threshold is correct"
    ]
