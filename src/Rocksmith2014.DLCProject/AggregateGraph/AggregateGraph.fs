module Rocksmith2014.DLCProject.AggregateGraph

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System.IO
open System.Text

type Graph = { Items: ResizeArray<GraphItem> }

[<Literal>]
let private canonicalXBlock = "/gamexblocks/nsongs"

[<Literal>]
let private canonicalXmlSong = "/songs/arr"

let create (platform: Platform) (project: DLCProject) =
    let dlcName = project.DLCKey.ToLower()

    let items = ResizeArray<GraphItem>()

    let xbl = GraphItem.normal dlcName canonicalXBlock "xblock" [ Tag.EmergentWorld; Tag.XWorld ]
    items.Add(xbl)

    for arrangement in project.Arrangements do
        match arrangement with 
        | Showlights ->
            let name = sprintf "%s_showlights" dlcName
            let sl = GraphItem.llid name canonicalXmlSong "xml" [ Tag.Application; Tag.XML ]
            items.Add(sl)

        | _ ->
            let name = "" // TODO

            let json =
                let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
                GraphItem.normal name canonical "json" [ Tag.Database; Tag.JsonDB ]
            items.Add(json)

            let sng =
                let canonical = sprintf "/songs/bin/%s" (Platform.getPath platform 1)
                GraphItem.llid name canonical "sng" [ Tag.Application; Tag.MusicgameSong ]
            items.Add(sng)

    let hsan =
        let name = sprintf "songs_dlc_%s" dlcName
        let canonical = sprintf "/manifests/songs_dlc_%s" dlcName
        GraphItem.normal name canonical "hsan" [ Tag.Database; Tag.HsanDB ]
    items.Add(hsan)

    [ sprintf "album_%s_256" dlcName; sprintf "album_%s_128" dlcName; sprintf "album_%s_64" dlcName  ]
    |> List.iter (fun art ->
        let dds =
            let canonical = "/gfxassets/album_art"
            GraphItem.llid art canonical "dds" [ Tag.DDS; Tag.Image ]
        items.Add(dds))

    project.Arrangements
    |> List.tryPick (function Vocals v -> v.CustomFont | _ -> None)
    |> Option.iter (fun _ ->
        let dds =
            let name = sprintf "lyrics_%s" dlcName
            let canonical = sprintf "/assets/ui/lyrics/%s" dlcName
            GraphItem.llid name canonical "dds" [ Tag.DDS; Tag.Image ]
        items.Add(dds))

    let bnkMain =
        let name = sprintf "song_%s" dlcName
        GraphItem.bnk name platform
    items.Add(bnkMain)

    let bnkPreview =
        let name = sprintf "song_%s_preview" dlcName
        GraphItem.bnk name platform
    items.Add(bnkPreview)

    { Items = items }

let serialize (output: Stream) (graph: Graph) =
    use writer = new StreamWriter(output, Encoding.UTF8, -1, true)
    graph.Items
    |> Seq.iteri (fun i item ->
        if i <> 0 then writer.WriteLine()
        GraphItem.write writer item)
