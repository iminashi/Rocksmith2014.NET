module Rocksmith2014.DLCProject.AggregateGraph

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System.IO
open System.Text

type Graph = { Items: GraphItem list }

let [<Literal>] private canonicalXBlock = "/gamexblocks/nsongs"
let [<Literal>] private canonicalXmlSong = "/songs/arr"
let [<Literal>] private canonicalAlbumArt = "/gfxassets/album_art"

let create (platform: Platform) (project: DLCProject) =
    let dlcName = project.DLCKey.ToLowerInvariant()
    let partition = Partitioner.create project

    let items = [
        yield GraphItem.normal dlcName canonicalXBlock "xblock" [ Tag.EmergentWorld; Tag.XWorld ]

        for arrangement in project.Arrangements do
            match arrangement with 
            | Showlights ->
                let name = sprintf "%s_showlights" dlcName
                yield GraphItem.llid name canonicalXmlSong "xml" [ Tag.Application; Tag.XML ]

            | _ ->
                let name = partition arrangement |> snd

                let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
                yield GraphItem.normal name canonical "json" [ Tag.Database; Tag.JsonDB ]

                let canonical = sprintf "/songs/bin/%s" (Platform.getPath platform 1)
                yield GraphItem.llid name canonical "sng" [ Tag.Application; Tag.MusicgameSong ]

        let name = sprintf "songs_dlc_%s" dlcName
        let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
        yield GraphItem.normal name canonical "hsan" [ Tag.Database; Tag.HsanDB ]

        yield! [ sprintf "album_%s_256" dlcName; sprintf "album_%s_128" dlcName; sprintf "album_%s_64" dlcName  ]
        |> List.map (fun name -> GraphItem.dds name canonicalAlbumArt)

        yield! project.Arrangements
        |> List.tryPick (function Vocals v -> v.CustomFont | _ -> None)
        |> Option.map (fun _ ->
            let name = sprintf "lyrics_%s" dlcName
            let canonical = sprintf "/assets/ui/lyrics/%s" dlcName
            GraphItem.dds name canonical)
        |> Option.toList

        let name = sprintf "song_%s" dlcName
        yield GraphItem.bnk name platform

        let name = sprintf "song_%s_preview" dlcName
        yield GraphItem.bnk name platform ]

    { Items = items }

let serialize (output: Stream) (graph: Graph) =
    use writer = new StreamWriter(output, Encoding.UTF8, -1, true)
    graph.Items
    |> List.iteri (fun i item ->
        if i <> 0 then writer.WriteLine()
        GraphItem.write writer item)
