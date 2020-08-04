module AttributesTests

open Expecto
open Rocksmith2014.SNG
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open System

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

[<Tests>]
let someTests =
  testList "Attribute Tests" [

    testCase "Partition is set correctly" <| fun _ ->
        let sng = SNG.Empty
        let lead1 =
            { XML = "instrumental.xml"
              ArrangementName = ArrangementName.Lead
              RouteMask = RouteMask.Lead
              ScrollSpeed = 13
              MasterID = 12345
              PersistentID = Guid.NewGuid() }
        let lead2 =
            { XML = "instrumental.xml"
              ArrangementName = ArrangementName.Lead
              RouteMask = RouteMask.Lead
              ScrollSpeed = 13
              MasterID = 12346
              PersistentID = Guid.NewGuid() }

        let project = { testProject with Arrangements = [ Instrumental lead1; Instrumental lead2 ] }

        //let header1 = createAttributesHeader project (InstrumentalConversion (lead1, sng))
        let attr1 = createAttributes project (InstrumentalConversion (lead1, sng))
        //let header2 = createAttributesHeader project (InstrumentalConversion (lead2, sng))
        let attr2 = createAttributes project (InstrumentalConversion (lead2, sng))

        Expect.equal attr1.SongPartition (Nullable(1)) "Partition for first lead arrangement is 1"
        Expect.equal attr2.SongPartition (Nullable(2)) "Partition for second lead arrangement is 2"
  ]
