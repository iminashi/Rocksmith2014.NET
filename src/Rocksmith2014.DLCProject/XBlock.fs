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
    { [<XmlAttribute("value")>] Value: string }

[<XmlRoot("property"); CLIMutable>]
type Property =
    { [<XmlAttribute("name")>] Name: string
      [<XmlElement("set")>] Set: Set }

    /// Creates a property with the given name and value.
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

/// Creates a Game XBlock object for the project.
let create (platform: Platform) (project: DLCProject) =
    let dlcName = project.DLCKey.ToLowerInvariant()
    let partition = Partitioner.create project
    let prop name value = Property.Create(name, value)

    { EntitySet =
        project.Arrangements
        |> List.choose (function
            | Showlights _ ->
                None
            | arr ->
                let fileName = partition arr |> snd

                let properties = [|
                    match platform with
                    | PC | Mac -> prop "Header" $"urn:database:hsan-db:songs_dlc_{dlcName}"
                    prop "Manifest" $"urn:database:json-db:{dlcName}_{fileName}"
                    prop "SngAsset" $"urn:application:musicgame-song:{dlcName}_{fileName}"
                    prop "AlbumArtSmall" $"urn:image:dds:album_{dlcName}_64"
                    prop "AlbumArtMedium" $"urn:image:dds:album_{dlcName}_128"
                    prop "AlbumArtLarge" $"urn:image:dds:album_{dlcName}_256"
                    let lyricArt = 
                        match arr with
                        | Vocals { CustomFont = Some _ } -> $"urn:image:dds:lyrics_{dlcName}"
                        | _ -> String.Empty
                    prop "LyricArt" lyricArt
                    prop "ShowLightsXMLAsset" $"urn:application:xml:{dlcName}_showlights"
                    let soundBank =
                        match arr with
                        | Instrumental { CustomAudio = Some _ } -> $"urn:audio:wwise-sound-bank:song_{dlcName}_{fileName}"
                        | _ -> $"urn:audio:wwise-sound-bank:song_{dlcName}"
                    prop "SoundBank" soundBank
                    prop "PreviewSoundBank" $"urn:audio:wwise-sound-bank:song_{dlcName}_preview" |]

                { Id = (Arrangement.getPersistentId arr).ToString("N")
                  ModelName = "RSEnumerable_Song"
                  Name = $"{project.DLCKey}_{Arrangement.getName arr false}"
                  Iterations = 0
                  Properties = properties }
                |> Some)
        |> List.toArray }

/// Serializes the Game object into the output stream.
let serialize (output: Stream) (game: Game) =
    let ns = XmlSerializerNamespaces()
    ns.Add("", "")
    let serializer = XmlSerializer(typeof<Game>)
    let settings = XmlWriterSettings(Indent = true, Encoding = UTF8Encoding(true), CloseOutput = false)
    use writer = XmlWriter.Create(output, settings)
    serializer.Serialize(writer, game, ns)

/// Deserializes a Game object from the input stream.
let deserialize (input: Stream) =
    let serializer = XmlSerializer(typeof<Game>, "")
    use reader = new StreamReader(input)
    serializer.Deserialize(reader) :?> Game
