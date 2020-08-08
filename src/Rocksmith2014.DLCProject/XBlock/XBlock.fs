module Rocksmith2014.DLCProject.XBlock

open System
open System.IO
open System.Xml.Serialization
open System.Xml
open System.Text
open Rocksmith2014.DLCProject
open Rocksmith2014.Common

[<XmlRoot("set"); CLIMutable>]
type Set =
    { [<XmlAttribute("value")>]
      Value: string }

[<XmlRoot("property"); CLIMutable>]
type Property =
    { [<XmlAttribute("name")>]
      Name: string

      [<XmlElement("set")>]
      Set: Set }

[<XmlRoot("entity"); CLIMutable>]
type Entity =
    { [<XmlAttribute("id")>]
      Id: string

      [<XmlAttribute("modelName")>]
      ModelName: string

      [<XmlAttribute("name")>]
      Name: string

      [<XmlAttribute("iterations")>]
      Iterations: int

      [<XmlArray("properties"); XmlArrayItem(ElementName = "property")>] 
      Properties: Property array }

[<XmlRoot("game"); CLIMutable>]
type Game =
    { [<XmlArray("entitySet"); XmlArrayItem(ElementName = "entity")>]
      EntitySet: Entity array }

let create (platform: Platform) (project: DLCProject) =
    let dlcName = project.DLCKey.ToLowerInvariant()
    let partition = Partitioner.create project

    let entitySet =
        ([], project.Arrangements)
        ||> List.fold (fun state arr ->
            match arr with
            | Showlights -> state
            | arr ->
                let fileName = partition arr |> snd

                let properties = [|
                    match platform with
                    | PC | Mac -> { Name = "Header"; Set = { Value = sprintf "urn:database:hsan-db:songs_dlc_%s" dlcName } }
                    { Name = "Manifest"; Set = { Value = sprintf "urn:database:json-db:%s_%s" dlcName fileName } }
                    { Name = "SngAsset"; Set = { Value = sprintf "urn:application:musicgame-song:%s_%s" dlcName fileName } }
                    { Name = "AlbumArtSmall"; Set = { Value = sprintf "urn:image:dds:album_%s_64" dlcName } }
                    { Name = "AlbumArtMedium"; Set = { Value = sprintf "urn:image:dds:album_%s_128" dlcName } }
                    { Name = "AlbumArtLarge"; Set = { Value = sprintf "urn:image:dds:album_%s_256" dlcName } }
                    let lyricArt = 
                        match arr with
                        | Vocals { CustomFont = Some _ } -> sprintf "urn:image:dds:lyrics_%s" dlcName
                        | _ -> String.Empty
                    { Name = "LyricArt"; Set = { Value = lyricArt } }
                    { Name = "ShowLightsXMLAsset"; Set = { Value = sprintf "urn:application:xml:%s_showlights" dlcName } }
                    { Name = "SoundBank"; Set = { Value = sprintf "urn:audio:wwise-sound-bank:song_%s" dlcName } }
                    { Name = "PreviewSoundBank"; Set = { Value = sprintf "urn:audio:wwise-sound-bank:song_%s_preview" dlcName } } |]

                let entity =
                    { Id = (Arrangement.getPersistentId arr).ToString("N")
                      ModelName = "RSEnumerable_Song"
                      Name = sprintf "%s_%s" project.DLCKey fileName
                      Iterations = 0
                      Properties = properties }
                entity::state)

    { EntitySet = entitySet |> List.toArray }

let serialize (output: Stream) (game: Game) =
    let ns = XmlSerializerNamespaces()
    ns.Add("", "")
    let serializer = XmlSerializer(typeof<Game>)
    let settings = XmlWriterSettings(Indent = true, Encoding = UTF8Encoding(true), CloseOutput = false)
    use writer = XmlWriter.Create(output, settings)
    serializer.Serialize(writer, game, ns)

let deserialize (input: Stream) =
    let serializer = XmlSerializer(typeof<Game>, "")
    use reader = new StreamReader(input)
    serializer.Deserialize(reader) :?> Game
