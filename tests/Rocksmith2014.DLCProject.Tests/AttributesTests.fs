module AttributesTests

open Expecto
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest
open System
open Rocksmith2014.SNG

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
      TuningPitch = 440.0
      Arrangements = []
      Tones = [] }

[<Tests>]
let someTests =
  testList "Attribute Tests" [

    testCase "Partition is set correctly" <| fun _ ->
        let sng = SNG.Empty
        let lead1 =
            { XML = "some_lead1.xml"
              ArrangementName = ArrangementName.Lead
              RouteMask = RouteMask.Lead
              ScrollSpeed = 13
              MasterID = 12345
              PersistentID = Guid.NewGuid() }
        let lead2 =
            { XML = "some_lead2.xml"
              ArrangementName = ArrangementName.Lead
              RouteMask = RouteMask.Lead
              ScrollSpeed = 13
              MasterID = 12346
              PersistentID = Guid.NewGuid() }

        let project = { testProject with Arrangements = [ Instrumental lead1; Instrumental lead2 ] }

        let header1 = SongHeader(project, lead1, sng)
        let attr1 = SongAttributes(project, lead1, header1, sng)
        let header2 = SongHeader(project, lead2, sng)
        let attr2 = SongAttributes(project, lead2, header2, sng)

        Expect.equal attr1.SongPartition 1 "Partition for first lead arrangement is 1"
        Expect.equal attr2.SongPartition 2 "Partition for second lead arrangement is 2"
  ]
