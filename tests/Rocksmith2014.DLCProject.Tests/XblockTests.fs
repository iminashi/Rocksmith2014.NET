module XblockTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.XBlock
open System
open System.IO

let vocals =
    { XML = "vocals.xml"
      Japanese = true
      CustomFont = Some "font.dds"
      MasterID = 123456
      PersistentID = Guid.NewGuid() }

let testProject =
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
      Arrangements = [ Vocals vocals ] }

[<Tests>]
let someTests =
  testList "XBlock Tests" [

    testCase "Can be created" <| fun _ ->
        let x = XBlock.create PC testProject

        Expect.isNonEmpty x.EntitySet "Entity set has been populated"

    testCase "Can be serialized" <| fun _ ->
        let set = { Value = "urn:database:hsan-db:songs_dlc_test" }
        let property =
            { Name = "Header"
              Set = set }
        let entity =
            { Id = "2b689bf502f744d39aced3f4728aa6b0"
              ModelName = "RSEnumerable_Song"
              Name = "Test_Bass"
              Iterations = 0
              Properties = [| property |] }
        let game = { EntitySet = [| entity |] }

        use stream = new MemoryStream()
        XBlock.serialize stream game
        stream.Position <- 0L
        let xml = using (new StreamReader(stream)) (fun reader -> reader.ReadToEnd())

        Expect.isNotEmpty xml "XML string is not empty"

    testCase "Can be deserialized" <| fun _ ->
        use file = File.OpenRead "test.xblock"

        let xblock = XBlock.deserialize file

        Expect.equal xblock.EntitySet.[0].ModelName "RSEnumerable_Song" "Model name is correct"
  ]
