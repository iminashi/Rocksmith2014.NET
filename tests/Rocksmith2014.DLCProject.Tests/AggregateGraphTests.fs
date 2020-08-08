module AggregateGraphTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System.IO
open System

let vocals =
    { XML = "vocals.xml"
      Japanese = true
      CustomFont = Some "font.dds"
      MasterID = 123456
      PersistentID = Guid.Empty }

let sl = { XML = "showlights.xml" }

let project =
    { DLCKey = "SomeTest"
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
      Arrangements = [ Vocals vocals; Showlights sl ] }

[<Tests>]
let someTests =
  testList "Aggregate Graph Tests" [

    testCase "Can be serialized" <| fun _ ->
        use stream = new MemoryStream()
        AggregateGraph.create PC project
        |> AggregateGraph.serialize stream

        stream.Position <- 0L
        let str = using (new StreamReader(stream)) (fun reader -> reader.ReadToEnd())

        Expect.isNotEmpty str "Aggregate graph string is not empty"
  ]
