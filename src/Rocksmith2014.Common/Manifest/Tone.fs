namespace Rocksmith2014.Common.Manifest

open System
open System.Collections.Generic
open System.Xml

type Pedal() =
    member val Type : string = null with get, set
    member val KnobValues : Dictionary<string, float32> = null with get, set
    member val Key : string = null with get, set
    member val Category : string = null with get, set
    member val Skin : string = null with get, set
    member val SkinIndex : Nullable<float32> = Nullable() with get, set

type Gear =
    { Rack1 : Pedal
      Rack2 : Pedal
      Rack3 : Pedal
      Rack4 : Pedal
      Amp : Pedal
      Cabinet : Pedal
      PrePedal1 : Pedal
      PrePedal2 : Pedal
      PrePedal3 : Pedal
      PrePedal4 : Pedal
      PostPedal1 : Pedal
      PostPedal2 : Pedal
      PostPedal3 : Pedal
      PostPedal4 : Pedal }

type Tone =
    { GearList : Gear
      ToneDescriptors : string array 
      NameSeparator : string
      IsCustom : Nullable<bool>
      Volume : string
      MacVolume : string
      Key : string
      Name : string
      SortOrder : Nullable<float32> }

    override this.ToString() =
        let description =
            if isNull this.ToneDescriptors || this.ToneDescriptors.Length = 0 then
                String.Empty
            else
                " (" + ToneDescriptor.combineUINames this.ToneDescriptors + ")"
        let key =
            if isNull this.Key || this.Key = this.Name then
                String.Empty
            else
                " [" + this.Key + "]"
        sprintf "%s%s%s" this.Name key description

module Tone =
    let [<Literal>] private ArrayNs = "http://schemas.microsoft.com/2003/10/Serialization/Arrays"

    let private getPedal (ns: string option) (gearList: XmlElement) (name: string) =
        let node =
            match ns with
            | Some ns -> fun (xel: XmlElement) name -> xel.Item(name, ns)
            | None -> fun (xel: XmlElement) name -> xel.Item name

        let pedal = node gearList name
        if pedal.IsEmpty then
            Unchecked.defaultof<Pedal>
        else
            let cat = node pedal "Category"
            let skin = node pedal "Skin"
            let skinIndex = node pedal "SkinIndex"
            let knobValues = Dictionary<string, float32>()

            (node pedal "KnobValues").ChildNodes
            |> Seq.cast<XmlNode>
            |> Seq.iter (fun knob ->
                let key = knob.Item("Key", ArrayNs).InnerText
                let value = knob.Item("Value", ArrayNs).InnerText
                knobValues.Add(key, float32 value))

            Pedal(Category = (if cat.IsEmpty then null else cat.InnerText),
                  Type = (node pedal "Type").InnerText,
                  Key = (node pedal "PedalKey").InnerText,
                  KnobValues = knobValues,
                  Skin = (if not (isNull skin || skin.IsEmpty) then skin.InnerText else null),
                  SkinIndex = if not (isNull skinIndex || skinIndex.IsEmpty) then Nullable(float32 skinIndex.InnerText) else Nullable())

    let private getGearList (ns: string option) (gearList: XmlElement) =
        let getPedal = getPedal ns gearList
    
        { Rack1 = getPedal "Rack1"
          Rack2 = getPedal "Rack2"
          Rack3 = getPedal "Rack3"
          Rack4 = getPedal "Rack4"
          Amp = getPedal "Amp"
          Cabinet = getPedal "Cabinet"
          PrePedal1 = getPedal "PrePedal1"
          PrePedal2 = getPedal "PrePedal2"
          PrePedal3 = getPedal "PrePedal3"
          PrePedal4 = getPedal "PrePedal4"
          PostPedal1 = getPedal "PostPedal1"
          PostPedal2 = getPedal "PostPedal2"
          PostPedal3 = getPedal "PostPedal3"
          PostPedal4 = getPedal "PostPedal4" }

    let private getDescriptors (descs: XmlElement) =
        descs.ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map (fun x -> x.InnerText)
        |> Seq.toArray

    /// Imports a tone from a Tone2014 XML structure using the optional XML namespace.
    let importXml (ns: string option) (xmlNode: XmlNode) =
        let node =
            match ns with
            | Some ns -> fun name -> xmlNode.Item(name, ns)
            | None -> fun name -> xmlNode.Item name
        let nodeText name = (node name).InnerText

        let macVol = node "MacVolume"

        { GearList = getGearList ns (node "GearList")
          ToneDescriptors = getDescriptors (node "ToneDescriptors")
          NameSeparator = nodeText "NameSeparator"
          IsCustom = Boolean.Parse(nodeText "IsCustom") |> Nullable
          Volume = nodeText "Volume"
          MacVolume = if isNull macVol then null else macVol.InnerText
          Key = nodeText "Key"
          Name = nodeText "Name"
          // Sort order is not needed
          SortOrder = Nullable() }

    /// Imports a tone from a Tone2014 XML file.
    let fromXmlFile (fileName: string) =
        let doc = XmlDocument()
        doc.Load(fileName)
        let xel = doc.DocumentElement
        if xel.Name <> "Tone2014" then failwith "Not a valid tone XML file."
        importXml None xel
