module Rocksmith2014.DLCProject.XBlock

open System.IO
open System.Xml.Serialization
open System.Xml
open System.Text

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
