module ManifestTests

open Expecto
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open System

let project =
    { Version = 1.
      DLCKey = "SomeTest"
      ArtistName = SortableString.Create "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.Create "Title"
      AlbumName = SortableString.Create "Album"
      Year = 2020
      AlbumArtFile = "cover.dds"
      AudioFile = { Path = "audio.wem"; Volume = 1. }
      AudioPreviewFile = { Path = "audio_preview.wem"; Volume = 1. }
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

        let attr = createAttributes project (FromVocals arr)
        let jsonString =
            Manifest.create [ attr ]
            |> Manifest.toJsonString

        Expect.isNotEmpty jsonString "JSON string is not empty"

    testCase "Can be read from JSON" <| fun _ ->
        let attr =
             { XML = "lyrics.xml"
               Japanese = false
               CustomFont = None
               MasterID = 123456
               PersistentID = Guid.NewGuid() }
            |> FromVocals
            |> createAttributes project

        let jsonString =
            Manifest.create [ attr ]
            |> Manifest.toJsonString

        let mani = Manifest.fromJsonString jsonString

        Expect.isTrue (mani.Entries.ContainsKey attr.PersistentID) "Manifest contains same key"
  ]
