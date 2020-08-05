module AttributesTests

open Expecto
open System
open Rocksmith2014.SNG
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open Rocksmith2014.XML
open Rocksmith2014.Conversion
open Rocksmith2014.Common

let testProject =
    { DLCKey = "SomeTest"
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
      CentOffset = 0.0
      Arrangements = []
      Tones = [] }

let testArr = InstrumentalArrangement.Load("instrumental.xml")
let testSng = ConvertInstrumental.xmlToSng testArr

let testLead =
    { XML = "instrumental.xml"
      ArrangementName = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      ScrollSpeed = 13
      MasterID = 12345
      PersistentID = Guid.NewGuid() }

[<Tests>]
let someTests =
  testList "Attribute Tests" [

    testCase "Partition is set correctly" <| fun _ ->
        let lead2 = { testLead with MasterID = 12346; PersistentID = Guid.NewGuid() }

        let project = { testProject with Arrangements = [ Instrumental testLead; Instrumental lead2 ] }

        let attr1 = createAttributes project (InstrumentalConversion (testLead, testSng))
        let attr2 = createAttributes project (InstrumentalConversion (lead2, testSng))

        Expect.equal attr1.SongPartition (Nullable(1)) "Partition for first lead arrangement is 1"
        Expect.equal attr2.SongPartition (Nullable(2)) "Partition for second lead arrangement is 2"

    testCase "Chord templates are created" <| fun _ ->
        let project = { testProject with Arrangements = [ Instrumental testLead ] }
        let emptyNameId = testSng.Chords |> Array.findIndex (fun c -> String.IsNullOrEmpty c.Name)

        let attr = createAttributes project (InstrumentalConversion (testLead, testSng))

        Expect.isNonEmpty attr.ChordTemplates "Chord templates array is not empty"
        Expect.isFalse (attr.ChordTemplates |> Array.exists (fun (c: Attributes.ChordTemplate) -> c.ChordId = int16 emptyNameId)) "Chord template with empty name is removed"

    testCase "Sections are created" <| fun _ ->
        let project = { testProject with Arrangements = [ Instrumental testLead ] }

        let attr = createAttributes project (InstrumentalConversion (testLead, testSng))

        Expect.equal attr.Sections.Length testSng.Sections.Length "Section count is same"
        Expect.equal attr.Sections.[0].UIName "$[34298] Riff [1]" "UI name is correct"

    testCase "Phrases are created" <| fun _ ->
        let project = { testProject with Arrangements = [ Instrumental testLead ] }

        let attr = createAttributes project (InstrumentalConversion (testLead, testSng))

        Expect.equal attr.Phrases.Length testSng.Phrases.Length "Phrase count is same"
  ]
