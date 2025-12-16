module Rocksmith2014.DLCProject.AggregateGraph

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open Rocksmith2014.Common
open Rocksmith2014.DLCProject

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
            let name = $"{dlcName}_{partition arrangement |> snd}"

            match arrangement with
            | Showlights _ ->
                yield GraphItem.llid name CanonicalXmlSong "xml" [ Tag.Application; Tag.XML ]

            | _ ->
                let canonical = $"/manifests/songs_dlc_{dlcName}"
                yield GraphItem.normal name canonical "json" [ Tag.Database; Tag.JsonDB ]
                yield GraphItem.sng name platform

                // Custom audio file
                match arrangement with
                | Instrumental { CustomAudio = Some _ } ->
                    yield GraphItem.bnk $"song_{name}" platform
                | _ -> ()

        let name = $"songs_dlc_{dlcName}"
        let canonical = $"/manifests/songs_dlc_{dlcName}"
        yield GraphItem.normal name canonical "hsan" [ Tag.Database; Tag.HsanDB ]

        yield! [ 64; 128; 256 ]
        |> List.map (fun size -> GraphItem.dds $"album_{dlcName}_{size}" CanonicalAlbumArt)

        yield! project.Arrangements
        |> List.choose (function
            | Vocals ({ CustomFont = Some _ } as v) ->
                Some v
            | _ ->
                None)
        |> List.map (fun v ->
            let name = Utils.getCustomFontName v.Japanese dlcName
            let canonical = $"/assets/ui/lyrics/{dlcName}"
            GraphItem.dds name canonical)

        yield GraphItem.bnk $"song_{dlcName}" platform
        yield GraphItem.bnk $"song_{dlcName}_preview" platform ] }

/// Serializes the aggregate graph into the output stream.
let serialize (output: Stream) (graph: Graph) =
    use writer =
        new StreamWriter(output, Encoding.UTF8, -1, true, NewLine = "\n")

    graph.Items
    |> List.iteri (fun i item ->
        if i <> 0 then writer.WriteLine()
        GraphItem.write writer item)

type private ParsedLine =
    { UUID: string
      TagType: string
      Value: string }

/// Parses an aggregate graph from a string.
let parse (text: string) =
    let findTagType tt x =  if x.TagType = tt then Some x.Value else None

    { Items =
        text.Split('\n')
        // Aggregate graph files created by the Toolkit contain an empty line at the end.
        |> Array.filter String.notEmpty
        |> Array.map (fun line ->
            let m = Regex.Match(line, """<urn:uuid:([^>]+)> <http://emergent.net/aweb/1.0/([^>]+)> "([^"]+)"\.""")
            { UUID = m.Groups[1].Value; TagType = m.Groups[2].Value; Value = m.Groups[3].Value })
        |> Array.groupBy (fun x -> x.UUID)
        |> Array.map (fun (uuid, values) ->
            { UUID = Guid.Parse uuid
              LLID = values |> Array.tryPick (findTagType TagType.LLID) |> Option.map Guid.Parse
              Tags = values |> Array.choose (findTagType TagType.Tag) |> List.ofArray
              Canonical = Array.pick (findTagType TagType.Canonical) values
              Name = Array.pick (findTagType TagType.Name) values
              RelPath = Array.pick (findTagType TagType.RelPath) values
              LogPath = Array.tryPick (findTagType TagType.LogPath) values })
        |> List.ofArray }
