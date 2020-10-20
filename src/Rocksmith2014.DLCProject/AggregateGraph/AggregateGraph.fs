module Rocksmith2014.DLCProject.AggregateGraph

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System.IO
open System.Text
open System.Text.RegularExpressions
open System

type Graph = { Items: GraphItem list }

let [<Literal>] private CanonicalXBlock = "/gamexblocks/nsongs"
let [<Literal>] private CanonicalXmlSong = "/songs/arr"
let [<Literal>] private CanonicalAlbumArt = "/gfxassets/album_art"

/// Creates an aggregate graph object for the project.
let create (platform: Platform) (project: DLCProject) =
    let dlcName = project.DLCKey.ToLowerInvariant()
    let partition = Partitioner.create project

    { Items = [
        yield GraphItem.normal dlcName CanonicalXBlock "xblock" [ Tag.EmergentWorld; Tag.XWorld ]

        for arrangement in project.Arrangements do
            let name = sprintf "%s_%s" dlcName (partition arrangement |> snd)

            match arrangement with 
            | Showlights _ ->
                yield GraphItem.llid name CanonicalXmlSong "xml" [ Tag.Application; Tag.XML ]

            | _ ->
                let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
                yield GraphItem.normal name canonical "json" [ Tag.Database; Tag.JsonDB ]
                yield GraphItem.sng name platform

        let name = sprintf "songs_dlc_%s" dlcName
        let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
        yield GraphItem.normal name canonical "hsan" [ Tag.Database; Tag.HsanDB ]

        yield! [ 64; 128; 256 ]
        |> List.map (fun size -> GraphItem.dds (sprintf "album_%s_%i" dlcName size) CanonicalAlbumArt)

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

type private ParsedLine = { UUID : string; TagType : string; Value : string }

/// Parses an aggregate graph from a string.
let parse (text: string) =
    let findTagType tt x =  if x.TagType = tt then Some x.Value else None

    { Items =
        text.Split('\n')
        // Aggregate graph files created by the Toolkit contain an empty line at the end.
        |> Array.filter (String.IsNullOrWhiteSpace >> not)
        |> Array.map (fun line ->
            let m = Regex.Match(line, """<urn:uuid:([^>]+)> <http://emergent.net/aweb/1.0/([^>]+)> "([^"]+)"\.""")
            { UUID = m.Groups.[1].Value; TagType = m.Groups.[2].Value; Value = m.Groups.[3].Value })
        |> Array.groupBy (fun x -> x.UUID)
        |> Array.map (fun (uuid, values) ->
            { UUID = Guid.Parse uuid
              LLID = values |> Array.tryPick (findTagType "llid") |> Option.map Guid.Parse
              Tags = values |> Array.choose (findTagType "tag") |> List.ofArray
              Canonical = Array.pick (findTagType "canonical") values
              Name = Array.pick (findTagType "name") values
              RelPath = Array.pick (findTagType "relpath") values
              LogPath= Array.tryPick (findTagType "logpath") values })
        |> List.ofArray }
