module Rocksmith2014.DLCProject.AggregateGraph

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System.IO
open System.Text

type Graph = { Items: GraphItem list }

let [<Literal>] private canonicalXBlock = "/gamexblocks/nsongs"
let [<Literal>] private canonicalXmlSong = "/songs/arr"
let [<Literal>] private canonicalAlbumArt = "/gfxassets/album_art"

/// Creates an aggregate graph object for the project.
let create (platform: Platform) (project: DLCProject) =
    let dlcName = project.DLCKey.ToLowerInvariant()
    let partition = Partitioner.create project

    { Items = [
        yield GraphItem.normal dlcName canonicalXBlock "xblock" [ Tag.EmergentWorld; Tag.XWorld ]

        for arrangement in project.Arrangements do
            let name = sprintf "%s_%s" dlcName (partition arrangement |> snd)

            match arrangement with 
            | Showlights _ ->
                yield GraphItem.llid name canonicalXmlSong "xml" [ Tag.Application; Tag.XML ]

            | _ ->
                let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
                yield GraphItem.normal name canonical "json" [ Tag.Database; Tag.JsonDB ]

                let canonical = sprintf "/songs/bin/%s" (Platform.getPath platform Platform.Path.SNG)
                yield GraphItem.llid name canonical "sng" [ Tag.Application; Tag.MusicgameSong ]

        let name = sprintf "songs_dlc_%s" dlcName
        let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
        yield GraphItem.normal name canonical "hsan" [ Tag.Database; Tag.HsanDB ]

        yield! [ 64; 128; 256 ]
        |> List.map (fun size -> GraphItem.dds (sprintf "album_%s_%i" dlcName size) canonicalAlbumArt)

        yield! project.Arrangements
        |> List.tryPick (function Vocals v -> v.CustomFont | _ -> None)
        |> Option.map (fun _ ->
            let name = sprintf "lyrics_%s" dlcName
            let canonical = sprintf "/assets/ui/lyrics/%s" dlcName
            GraphItem.dds name canonical)
        |> Option.toList

        yield GraphItem.bnk (sprintf "song_%s" dlcName) platform
        yield GraphItem.bnk (sprintf "song_%s_preview" dlcName) platform ] }

/// Serializes the aggregate graph into the output stream.
let serialize (output: Stream) (graph: Graph) =
    use writer = new StreamWriter(output, Encoding.UTF8, -1, true, NewLine = "\n")
    graph.Items
    |> List.iteri (fun i item ->
        if i <> 0 then writer.WriteLine()
        GraphItem.write writer item)
