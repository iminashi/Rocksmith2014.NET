module ManifestTests

open Expecto
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest
open System

let project =
    { DLCKey = "some_test"
      AppID = 248750
      ArtistName = "Artist"
      ArtistNameSort = "artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = "Title"
      TitleSort = "title"
      AlbumName = "Album"
      AlbumNameSort = "album"
      Year = 2020
      AlbumArtFile = "cover.dds"
      AudioFile = "audio.wem"
      AudioPreviewFile = "audio_preview.wem"
      TuningPitch = 440.0
      Arrangements = []
      Tones = [] }

[<Tests>]
let someTests =
  testList "Manifest Tests" [

    testCase "Can be converted to JSON" <| fun _ ->
        let arr =
            { XML = "lyrics.xml"
              Japanese = false
              CustomFont = None
              MasterID = 123456
              PersistentID = Guid.NewGuid() }

        let attr = VocalsAttributes(project, Vocals arr)
        let jsonString =
            Manifest.create [ attr ]
            |> Manifest.toJson

        Expect.isNotEmpty jsonString "JSON string is not empty"

    testCase "Can be read from JSON" <| fun _ ->
        let arr =
            { XML = "lyrics.xml"
              Japanese = false
              CustomFont = None
              MasterID = 123456
              PersistentID = Guid.NewGuid() }

        let attr = VocalsAttributes(project, Vocals arr)
        let jsonString =
            Manifest.create [ attr ]
            |> Manifest.toJson

        let mani = Manifest.fromJson jsonString

        Expect.isTrue (mani.Entries.ContainsKey attr.PersistentID) "Manifest contains same key"
  ]
