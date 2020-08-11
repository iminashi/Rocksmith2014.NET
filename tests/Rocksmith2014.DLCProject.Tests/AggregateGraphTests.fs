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

let lead =
    { XML = "lead.xml"
      ArrangementName = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      ScrollSpeed = 13
      MasterID = 987654
      PersistentID = Guid.NewGuid() }

let project =
    { Version = 1.
      DLCKey = "SomeTest"
      ArtistName = SortableString.makeSimple "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.makeSimple "Title"
      AlbumName = SortableString.makeSimple "Album"
      Year = 2020
      AlbumArtFile = "cover.dds"
      AudioFile = { Path = "audio.wem"; Volume = 1. }
      AudioPreviewFile = { Path = "audio_preview.wem"; Volume = 1. }
      CentOffset = 0.0
      Arrangements = [ Vocals vocals; Showlights sl; Instrumental lead ]
      Tones = [] }

[<Tests>]
let someTests =
  testList "Aggregate Graph Tests" [

    test "Can be serialized" {
        use stream = new MemoryStream()
        AggregateGraph.create PC project
        |> AggregateGraph.serialize stream

        stream.Position <- 0L
        let str = using (new StreamReader(stream)) (fun reader -> reader.ReadToEnd())

        Expect.isNotEmpty str "Aggregate graph string is not empty" }

    test "Graph items are created correctly: Main sound bank (PC)" { 
        let a = AggregateGraph.create PC project
        let bnk = a.Items |> List.find (fun x -> x.Name = "song_sometest")

        Expect.equal bnk.Canonical "/audio/windows" "Canonical is correct"
        Expect.equal bnk.RelPath "/audio/windows/song_sometest.bnk" "Relative path is correct"
        Expect.stringEnds ((Option.get bnk.LLID).ToString()) "-0000-0000-0000-000000000000" "LLID ending is all zeroes"
        Expect.equal (Option.get bnk.LogPath) "/audio/song_sometest.bnk" "Logical path is correct"
        Expect.sequenceEqual bnk.Tags [ "audio"; "wwise-sound-bank"; "dx9" ] "Has correct tags" }

    test "Graph items are created correctly: Preview sound bank (Mac)" { 
        let a = AggregateGraph.create Mac project
        let bnk = a.Items |> List.find (fun x -> x.Name = "song_sometest_preview")

        Expect.equal bnk.Canonical "/audio/mac" "Canonical is correct"
        Expect.equal bnk.RelPath "/audio/mac/song_sometest_preview.bnk" "Relative path is correct"
        Expect.equal (Option.get bnk.LogPath) "/audio/song_sometest_preview.bnk" "Logical path is correct"
        Expect.sequenceEqual bnk.Tags [ "audio"; "wwise-sound-bank"; "macos" ] "Has correct tags" }

    test "Graph items are created correctly: Lead arrangement SNG" { 
        let a = AggregateGraph.create PC project
        let item =
            a.Items
            |> List.find (fun x -> x.Name = "sometest_lead" && x.Tags = [ "application"; "musicgame-song" ])

        Expect.equal item.Canonical "/songs/bin/generic" "Canonical is correct"
        Expect.equal item.RelPath "/songs/bin/generic/sometest_lead.sng" "Relative path is correct"
        Expect.equal (Option.get item.LogPath) "/songs/bin/generic/sometest_lead.sng" "Logical path is correct" }

    test "Graph items are created correctly: Lead arrangement JSON" { 
        let a = AggregateGraph.create PC project
        let item =
            a.Items
            |> List.find (fun x -> x.Name = "sometest_lead" && x.Tags = [ "database"; "json-db" ])

        Expect.equal item.Canonical "/manifests/songs_dlc_sometest" "Canonical is correct"
        Expect.equal item.RelPath "/manifests/songs_dlc_sometest/sometest_lead.json" "Relative path is correct" }

    test "Graph items are created correctly: Show lights" { 
        let a = AggregateGraph.create PC project
        let item = a.Items |> List.find (fun x -> x.Name = "sometest_showlights")

        Expect.equal item.Canonical "/songs/arr" "Canonical is correct"
        Expect.equal item.RelPath "/songs/arr/sometest_showlights.xml" "Relative path is correct"       
        Expect.equal (Option.get item.LogPath) "/songs/arr/sometest_showlights.xml" "Logical path is correct"
        Expect.sequenceEqual item.Tags [ "application"; "xml" ] "Has correct tags" }

    test "Graph items are created correctly: Album art medium" { 
        let a = AggregateGraph.create PC project
        let item = a.Items |> List.find (fun x -> x.Name = "album_sometest_128")

        Expect.equal item.Canonical "/gfxassets/album_art" "Canonical is correct"
        Expect.equal item.RelPath "/gfxassets/album_art/album_sometest_128.dds" "Relative path is correct"       
        Expect.equal (Option.get item.LogPath) "/gfxassets/album_art/album_sometest_128.dds" "Logical path is correct"
        Expect.sequenceEqual item.Tags [ "dds"; "image" ] "Has correct tags" }

    test "Graph items are created correctly: X-Block" { 
        let a = AggregateGraph.create PC project
        let item = a.Items |> List.find (fun x -> x.Name = "sometest")

        Expect.equal item.Canonical "/gamexblocks/nsongs" "Canonical is correct"
        Expect.equal item.RelPath "/gamexblocks/nsongs/sometest.xblock" "Relative path is correct"       
        Expect.sequenceEqual item.Tags [ "emergent-world"; "x-world" ] "Has correct tags" }

    test "Graph items are created correctly: HSAN" { 
        let a = AggregateGraph.create PC project
        let item = a.Items |> List.find (fun x -> x.Name = "songs_dlc_sometest")

        Expect.equal item.Canonical "/manifests/songs_dlc_sometest" "Canonical is correct"
        Expect.equal item.RelPath "/manifests/songs_dlc_sometest/songs_dlc_sometest.hsan" "Relative path is correct"       
        Expect.sequenceEqual item.Tags [ "database"; "hsan-db" ] "Has correct tags" }
  ]
