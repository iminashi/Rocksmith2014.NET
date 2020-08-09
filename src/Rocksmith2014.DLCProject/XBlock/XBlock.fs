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

    static member Create(name, value) = { Name = name; Set = { Value = value } }

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

    let entitySet = [|
        for arr in project.Arrangements do
            match arr with
            | Showlights -> ()
            | arr ->
                let fileName = partition arr |> snd

                let properties = [|
                    match platform with
                    | PC | Mac -> Property.Create("Header", sprintf "urn:database:hsan-db:songs_dlc_%s" dlcName)
                    Property.Create("Manifest", sprintf "urn:database:json-db:%s_%s" dlcName fileName)
                    Property.Create("SngAsset", sprintf "urn:application:musicgame-song:%s_%s" dlcName fileName)
                    Property.Create("AlbumArtSmall", sprintf "urn:image:dds:album_%s_64" dlcName)
                    Property.Create("AlbumArtMedium", sprintf "urn:image:dds:album_%s_128" dlcName)
                    Property.Create("AlbumArtLarge", sprintf "urn:image:dds:album_%s_256" dlcName)
                    let lyricArt = 
                        match arr with
                        | Vocals { CustomFont = Some _ } -> sprintf "urn:image:dds:lyrics_%s" dlcName
                        | _ -> String.Empty
                    Property.Create("LyricArt", lyricArt)
                    Property.Create("ShowLightsXMLAsset", sprintf "urn:application:xml:%s_showlights" dlcName)
                    Property.Create("SoundBank", sprintf "urn:audio:wwise-sound-bank:song_%s" dlcName)
                    Property.Create("PreviewSoundBank", sprintf "urn:audio:wwise-sound-bank:song_%s_preview" dlcName) |]

                { Id = (Arrangement.getPersistentId arr).ToString("N")
                  ModelName = "RSEnumerable_Song"
                  Name = sprintf "%s_%s" project.DLCKey (Arrangement.getName arr false)
                  Iterations = 0
                  Properties = properties } |]

    { EntitySet = entitySet }

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
