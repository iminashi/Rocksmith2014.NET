namespace Rocksmith2014.Common.Manifest

open System
open System.Collections.Generic
open System.IO
open System.Runtime.Serialization
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Xml

type Pedal =
    { Type : string 
      KnobValues : Map<string, float32> option
      Key : string
      Category : string option
      Skin : string option
      SkinIndex : float32 option }

type Gear =
    { Amp : Pedal
      Cabinet : Pedal
      Racks : Pedal option array
      PrePedals : Pedal option array
      PostPedals : Pedal option array }

type Tone =
    { GearList : Gear
      ToneDescriptors : string array 
      NameSeparator : string
      IsCustom : bool option
      Volume : string
      MacVolume : string option
      Key : string
      Name : string
      SortOrder : float32 option }

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

[<AllowNullLiteral>]
type PedalDto() =
    member val Type : string = null with get, set
    member val KnobValues : IDictionary<string, float32> = null with get, set
    member val Key : string = null with get, set
    member val Category : string = null with get, set
    member val Skin : string = null with get, set
    member val SkinIndex : Nullable<float32> = Nullable() with get, set

[<CLIMutable>]
type GearDto =
    { Rack1 : PedalDto
      Rack2 : PedalDto
      Rack3 : PedalDto
      Rack4 : PedalDto
      Amp : PedalDto
      Cabinet : PedalDto
      PrePedal1 : PedalDto
      PrePedal2 : PedalDto
      PrePedal3 : PedalDto
      PrePedal4 : PedalDto
      PostPedal1 : PedalDto
      PostPedal2 : PedalDto
      PostPedal3 : PedalDto
      PostPedal4 : PedalDto }

[<CLIMutable>]
type ToneDto =
    { GearList : GearDto
      ToneDescriptors : string array 
      NameSeparator : string
      IsCustom : Nullable<bool>
      Volume : string
      MacVolume : string
      Key : string
      Name : string
      SortOrder : Nullable<float32> }

module Tone =
    let [<Literal>] private ArrayNs = "http://schemas.microsoft.com/2003/10/Serialization/Arrays"

    let private getPedal (ns: string option) (gearList: XmlElement) (name: string) =
        let node =
            match ns with
            | Some ns -> fun (xel: XmlElement) name -> xel.Item(name, ns)
            | None -> fun (xel: XmlElement) name -> xel.Item name

        let pedal = node gearList name
        if pedal.IsEmpty then
            None
        else
            let cat = node pedal "Category"
            let skin = node pedal "Skin"
            let skinIndex = node pedal "SkinIndex"

            let knobValues =
                // TODO: Handle missing
                (node pedal "KnobValues").ChildNodes
                |> Seq.cast<XmlNode>
                |> Seq.map (fun knob ->
                    let key = knob.Item("Key", ArrayNs).InnerText
                    let value = knob.Item("Value", ArrayNs).InnerText
                    key, float32 value)
                |> Map.ofSeq

            { Category = (if cat.IsEmpty then None else Some cat.InnerText)
              Type = (node pedal "Type").InnerText
              Key = (node pedal "PedalKey").InnerText
              KnobValues = Some knobValues
              Skin = (if not (isNull skin || skin.IsEmpty) then Some skin.InnerText else None)
              SkinIndex = if not (isNull skinIndex || skinIndex.IsEmpty) then Some (float32 skinIndex.InnerText) else None }
            |> Some

    let private getGearList (ns: string option) (gearList: XmlElement) =
        let getPedal = getPedal ns gearList
    
        { Racks = [| getPedal "Rack1"; getPedal "Rack2"; getPedal "Rack3"; getPedal "Rack4" |]
          Amp = getPedal "Amp" |> Option.get
          Cabinet = getPedal "Cabinet" |> Option.get
          PrePedals = [| getPedal "PrePedal1"; getPedal "PrePedal2"; getPedal "PrePedal3"; getPedal "PrePedal4" |]
          PostPedals = [| getPedal "PostPedal1"; getPedal "PostPedal2"; getPedal "PostPedal3"; getPedal "PostPedal4" |] }

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

        { Tone.GearList = getGearList ns (node "GearList")
          ToneDescriptors = getDescriptors (node "ToneDescriptors")
          NameSeparator = nodeText "NameSeparator"
          IsCustom = Boolean.Parse(nodeText "IsCustom") |> Some // TODO: Handle missing
          Volume = nodeText "Volume"
          MacVolume = if isNull macVol then None else Some macVol.InnerText
          Key = nodeText "Key"
          Name = nodeText "Name"
          // Sort order is not needed
          SortOrder = None }

    /// Imports a tone from a Tone2014 XML file.
    let fromXmlFile (fileName: string) =
        let doc = XmlDocument()
        doc.Load(fileName)
        let xel = doc.DocumentElement
        if xel.Name <> "Tone2014" then failwith "Not a valid tone XML file."
        importXml None xel

    /// Imports a tone from a JSON file.
    let fromJsonFile (fileName: string) = async {
        use file = File.OpenRead fileName

        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        options.Converters.Add(JsonFSharpConverter())
        return! JsonSerializer.DeserializeAsync<Tone>(file, options) }

    /// Exports a tone into a JSON file.
    let exportJson (path: string) (tone: Tone) = async {
        use file = File.Create path

        let options = JsonSerializerOptions(WriteIndented = true, IgnoreNullValues = true)
        options.Converters.Add(JsonFSharpConverter())
        do! JsonSerializer.SerializeAsync(file, tone, options) }

    let private pedalFromDto (dto: PedalDto) =
        { Category = Option.ofObj dto.Category
          Type = dto.Type
          Key = dto.Key
          KnobValues =
            if isNull dto.KnobValues then
                None
            else
                dto.KnobValues |> Seq.map (|KeyValue|) |> Map.ofSeq |> Some
          Skin = Option.ofObj dto.Skin
          SkinIndex = Option.ofNullable dto.SkinIndex }

    let fromDto dto : Tone =
        let gear =
            { Racks = [| dto.GearList.Rack1; dto.GearList.Rack2; dto.GearList.Rack3; dto.GearList.Rack4 |] |> Array.map (Option.ofObj >> Option.map pedalFromDto)
              Amp = pedalFromDto dto.GearList.Amp
              Cabinet = pedalFromDto dto.GearList.Cabinet
              PrePedals = [| dto.GearList.PrePedal1; dto.GearList.PrePedal2; dto.GearList.PrePedal3; dto.GearList.PrePedal4 |] |> Array.map (Option.ofObj >> Option.map pedalFromDto)
              PostPedals = [| dto.GearList.PostPedal1; dto.GearList.PostPedal2; dto.GearList.PostPedal3; dto.GearList.PostPedal4 |] |> Array.map (Option.ofObj >> Option.map pedalFromDto) }

        { GearList = gear
          ToneDescriptors = dto.ToneDescriptors
          NameSeparator = dto.NameSeparator
          IsCustom = Option.ofNullable dto.IsCustom
          Volume = dto.Volume
          MacVolume = Option.ofObj dto.MacVolume
          Key = dto.Key
          Name = dto.Name
          SortOrder = Option.ofNullable dto.SortOrder }

    let toPedalDto (pedal: Pedal) =
        let kv = pedal.KnobValues |> Option.map Dictionary |> Option.defaultWith Dictionary
        PedalDto(Key = pedal.Key,
                 Type = pedal.Type,
                 KnobValues = kv,
                 Category = Option.toObj pedal.Category,
                 Skin = Option.toObj pedal.Skin,
                 SkinIndex = Option.toNullable pedal.SkinIndex)

    let toDto (tone: Tone) =
        let tryGetPedal index pedalArray =
            pedalArray
            |> Option.ofObj
            |> Option.bind (Array.tryItem index >> Option.bind id >> Option.map toPedalDto)
            |> Option.defaultValue null

        let gear =
            { Rack1 = tone.GearList.Racks |> tryGetPedal 0
              Rack2 = tone.GearList.Racks |> tryGetPedal 1
              Rack3 = tone.GearList.Racks |> tryGetPedal 2
              Rack4 = tone.GearList.Racks |> tryGetPedal 3
              Amp = toPedalDto tone.GearList.Amp
              Cabinet = toPedalDto tone.GearList.Cabinet
              PrePedal1 = tone.GearList.PrePedals |> tryGetPedal 0
              PrePedal2 = tone.GearList.PrePedals |> tryGetPedal 1
              PrePedal3 = tone.GearList.PrePedals |> tryGetPedal 2
              PrePedal4 = tone.GearList.PrePedals |> tryGetPedal 3
              PostPedal1 = tone.GearList.PostPedals |> tryGetPedal 0
              PostPedal2 = tone.GearList.PostPedals |> tryGetPedal 1
              PostPedal3 = tone.GearList.PostPedals |> tryGetPedal 2
              PostPedal4 = tone.GearList.PostPedals |> tryGetPedal 3 }
        
        { GearList = gear
          ToneDescriptors = tone.ToneDescriptors
          NameSeparator = tone.NameSeparator
          IsCustom = Option.toNullable tone.IsCustom
          Volume = tone.Volume
          MacVolume = Option.toObj tone.MacVolume
          Key = tone.Key
          Name = tone.Name
          SortOrder = Option.toNullable tone.SortOrder }

    /// Exports a tone into an XML file in a format that is compatible with the Toolkit.
    let exportXml (path: string) (tone: Tone) = async {
        let serializer = DataContractSerializer(typeof<ToneDto>)
        using (XmlWriter.Create(path, XmlWriterSettings(Indent = true))) (fun writer -> serializer.WriteObject(writer, toDto tone))

        // Read the file back and fix it up to be importable in the Toolkit
        let! text = File.ReadAllTextAsync path
        let sb = StringBuilder(text)

        // The class name is Tone2014
        sb.Replace("<ToneDto", "<Tone2014")
          .Replace("</ToneDto>", "</Tone2014>")
          // F# incompatibility stuff
          .Replace("_x0040_", "")
          // The key for pedals is PedalKey
          .Replace("<Key>", "<PedalKey>")
          .Replace("</Key>", "</PedalKey>")
          // Fix the key tag of the tone itself that was replaced
          .Replace($"<PedalKey>{tone.Key}</PedalKey>", $"<Key>{tone.Key}</Key>")
          // Sort order is not nullable
          .Replace("""<SortOrder i:nil="true" />""", "<SortOrder>0.0</SortOrder>")
          // Tone key/name import does not seem to work otherwise for some reason
          .Replace($"<NameSeparator>{tone.NameSeparator}</NameSeparator>\r\n  <Name>{tone.Name}</Name>", $"<Name>{tone.Name}</Name>\r\n  <NameSeparator>{tone.NameSeparator}</NameSeparator>")
          // Change the namespace
          .Replace("http://schemas.datacontract.org/2004/07/Rocksmith2014.Common.Manifest", "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.DLCPackage.Manifest.Tone")
          |> ignore
        do! File.WriteAllTextAsync(path, sb.ToString()) }
