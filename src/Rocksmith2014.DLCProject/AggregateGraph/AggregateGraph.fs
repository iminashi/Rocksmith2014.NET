module Rocksmith2014.DLCProject.AggregateGraph

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System
open System.IO
open System.Text

type Graph = { Items: ResizeArray<GraphItem> }

[<Literal>]
let CanonicalXBlock = "/gameXblocks/nsongs"

[<Literal>]
let CanonicalXmlSong = "/songs/arr"

let private rand = Random()
let private zeroes = Array.zeroCreate<byte> 8

let private newLLID () = Guid(rand.Next(), 0s, 0s, zeroes)

let private getPlatformTag = function
    | PC -> Tag.DX9
    | Mac -> Tag.MacOS

let create (platform: Platform) (project: DLCProject) =
    let dlcName = project.DLCKey.ToLower()

    let items = ResizeArray<GraphItem>()

    let xbl =
        { Name = dlcName
          Canonical = CanonicalXBlock
          RelPath = sprintf "test/%s.xblock" dlcName
          Tags = [ Tag.EmergentWorld; Tag.XWorld ]
          UUID = Guid.NewGuid()
          LogPath = None; LLID = None }
    items.Add(xbl)

    for arrangement in project.Arrangements do
        match arrangement with 
        | Showlights ->
            let sl =
                let name = sprintf "%s_showlights" dlcName
                let path = sprintf "%s/%s.xml" CanonicalXmlSong name
                { Name = name
                  Canonical = CanonicalXmlSong
                  RelPath = path
                  LogPath = Some path
                  Tags = [ Tag.Application; Tag.XML ]
                  UUID = Guid.NewGuid()
                  LLID = Some (newLLID()) }
            items.Add(sl)

        | _ ->
            let name = "" // TODO

            let json =
                let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
                let path = sprintf "%s/%s.json" canonical name
                { Name = name
                  Canonical = canonical
                  RelPath = path
                  Tags = [ Tag.Database; Tag.JsonDB ]
                  UUID = Guid.NewGuid()
                  LogPath = None; LLID = None }
            items.Add(json)

            let sng =
                let canonical = sprintf "/songs/bin/%s" (Platform.getPath platform 1)
                let path = sprintf "%s/%s.sng" canonical name
                { Name = name
                  Canonical = canonical
                  RelPath = path
                  LogPath = Some path
                  Tags = [ Tag.Application; Tag.MusicgameSong ]
                  UUID = Guid.NewGuid()
                  LLID = Some (newLLID()) }
            items.Add(sng)

    let hsan =
        let name = sprintf "songs_dlc_%s" dlcName
        let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
        { Name = name
          Canonical = canonical
          RelPath = sprintf "%s/%s.hsan" canonical name
          Tags = [ Tag.Database; Tag.HsanDB]
          UUID = Guid.NewGuid()
          LLID = None; LogPath = None }
    items.Add(hsan)

    [ sprintf "album_%s_256" dlcName; sprintf "album_%s_128" dlcName; sprintf "album_%s_64" dlcName  ]
    |> List.iter (fun art ->
        let dds =
            let canonical = "/gfxassets/album_art"
            let path = sprintf "%s/%s.dds" canonical art
            { Name = art
              Canonical = canonical
              RelPath = path
              LogPath = Some path
              Tags = [ Tag.DDS; Tag.Image ]
              UUID = Guid.NewGuid()
              LLID = Some (newLLID()) }
        items.Add(dds))

    project.Arrangements
    |> List.tryPick (function Vocals v -> v.CustomFont | _ -> None)
    |> Option.iter (fun _ ->
        let dds =
            let name = sprintf "lyrics_%s" dlcName
            let canonical = sprintf "/assets/ui/lyrics/%s" dlcName
            let path = sprintf "%s/%s.dds" canonical name
            { Name = name
              Canonical = canonical
              RelPath = path
              LogPath = Some path
              Tags = [ Tag.DDS; Tag.Image ]
              UUID = Guid.NewGuid()
              LLID = Some (newLLID()) }
        items.Add(dds))

    let bnkMain =
        let canonical = sprintf "/audio/%s" (Platform.getPath platform 0)
        let name = sprintf "song_%s" dlcName
        { Canonical = canonical 
          Name = name 
          RelPath = sprintf "%s/%s.bnk" canonical name
          LogPath = Some(sprintf "/audio/%s.bnk" dlcName)
          Tags = [ Tag.Audio; Tag.WwiseSoundBank; getPlatformTag platform ]
          UUID = Guid.NewGuid()
          LLID = Some(newLLID()) }
    items.Add(bnkMain)

    let bnkPreview =
        let canonical = sprintf "/audio/%s" (Platform.getPath platform 0)
        let name = sprintf "song_%s_preview" dlcName
        { Canonical = canonical 
          Name = name 
          RelPath = sprintf "%s/%s.bnk" canonical name
          LogPath = Some(sprintf "/audio/%s.bnk" dlcName)
          Tags = [ Tag.Audio; Tag.WwiseSoundBank; getPlatformTag platform ]
          UUID = Guid.NewGuid()
          LLID = Some(newLLID()) }
    items.Add(bnkPreview)

    { Items = items }

let serialize (output: Stream) (graph: Graph) =
    use writer = new StreamWriter(output, Encoding.UTF8, -1, true)
    graph.Items
    |> Seq.iteri (fun i item ->
        if i <> 0 then writer.WriteLine()
        GraphItem.write writer item)
