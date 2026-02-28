module Rocksmith2014.DLCProject.XBlock

open System
open System.IO
open System.Text
open System.Xml
open System.Xml.Serialization
open Rocksmith2014.Common
open Rocksmith2014.DLCProject

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
                let lyricArt =
                    match arr with
                    | Vocals { CustomFont = Some _; Japanese = j } ->
                        $"urn:image:dds:%s{Utils.getCustomFontName j dlcName}"
                    | _ ->
                        String.Empty

                let soundBank =
                    match arr with
                    | Instrumental { CustomAudio = Some _ } ->
                        $"urn:audio:wwise-sound-bank:song_%s{dlcName}_%s{fileName}"
                    | _ ->
                        $"urn:audio:wwise-sound-bank:song_%s{dlcName}"

                let properties = [|
                    match platform with
                    | PC | Mac -> prop "Header" $"urn:database:hsan-db:songs_dlc_%s{dlcName}"
                    prop "Manifest" $"urn:database:json-db:%s{dlcName}_%s{fileName}"
                    prop "SngAsset" $"urn:application:musicgame-song:%s{dlcName}_%s{fileName}"
                    prop "AlbumArtSmall" $"urn:image:dds:album_%s{dlcName}_64"
                    prop "AlbumArtMedium" $"urn:image:dds:album_%s{dlcName}_128"
                    prop "AlbumArtLarge" $"urn:image:dds:album_%s{dlcName}_256"
                    prop "LyricArt" lyricArt
                    prop "ShowLightsXMLAsset" $"urn:application:xml:%s{dlcName}_showlights"
                    prop "SoundBank" soundBank
                    prop "PreviewSoundBank" $"urn:audio:wwise-sound-bank:song_%s{dlcName}_preview" |]

                { Id = (Arrangement.getPersistentId arr).ToString("N")
                  ModelName = "RSEnumerable_Song"
                  Name = $"%s{project.DLCKey}_%s{Arrangement.getName arr false}"
                  Iterations = 0
                  Properties = properties }
                |> Some)
        |> List.toArray }

/// Serializes the Game object into the output stream.
let serialize (output: Stream) (game: Game) =
    let ns = XmlSerializerNamespaces()
    ns.Add("", "")
    let serializer = XmlSerializer(typeof<Game>)

    let settings =
        XmlWriterSettings(Indent = true, Encoding = UTF8Encoding(true), CloseOutput = false)

    use writer = XmlWriter.Create(output, settings)
    serializer.Serialize(writer, game, ns)

/// Deserializes a Game object from the input stream.
let deserialize (input: Stream) =
    let serializer = XmlSerializer(typeof<Game>, "")
    use reader = new StreamReader(input)
    serializer.Deserialize(reader) :?> Game
