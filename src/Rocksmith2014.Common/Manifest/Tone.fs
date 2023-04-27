namespace Rocksmith2014.Common.Manifest

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Runtime.Serialization
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.RegularExpressions
open System.Xml
open Newtonsoft.Json
open Rocksmith2014.Common

type Pedal =
    { Type: string
      KnobValues: Map<string, float32>
      Key: string
      Category: string option
      Skin: string option
      SkinIndex: float32 option }

type Gear =
    { Amp: Pedal
      Cabinet: Pedal
      Racks: Pedal option array
      PrePedals: Pedal option array
      PostPedals: Pedal option array }

type Tone =
    { GearList: Gear
      ToneDescriptors: string array
      NameSeparator: string
      Volume: float
      MacVolume: float option
      Key: string
      Name: string
      SortOrder: float32 option }

    override this.ToString() =
        let description =
            if isNull this.ToneDescriptors || this.ToneDescriptors.Length = 0 then
                String.Empty
            else
                $" ({ToneDescriptor.combineUINames this.ToneDescriptors})"

        let key =
            if String.IsNullOrEmpty(this.Key) || this.Key = this.Name then
                String.Empty
            else
                $" [{this.Key}]"

        sprintf "%s%s%s" this.Name key description

[<AllowNullLiteral; Sealed>]
type PedalDto() =
    member val Type: string = null with get, set
    member val KnobValues: IDictionary<string, float32> = null with get, set
    [<JsonPropertyName("Key"); JsonProperty("Key")>]
    member val PedalKey: string = null with get, set
    member val Category: string = null with get, set
    member val Skin: string = null with get, set
    member val SkinIndex: Nullable<float32> = Nullable() with get, set

[<CLIMutable>]
type GearDto =
    { Rack1: PedalDto
      Rack2: PedalDto
      Rack3: PedalDto
      Rack4: PedalDto
      Amp: PedalDto
      Cabinet: PedalDto
      PrePedal1: PedalDto
      PrePedal2: PedalDto
      PrePedal3: PedalDto
      PrePedal4: PedalDto
      PostPedal1: PedalDto
      PostPedal2: PedalDto
      PostPedal3: PedalDto
      PostPedal4: PedalDto }

[<CLIMutable>]
type ToneDto =
    { GearList: GearDto
      ToneDescriptors: string array
      NameSeparator: string
      IsCustom: Nullable<bool>
      Volume: string
      MacVolume: string
      Key: string
      Name: string
      SortOrder: Nullable<float32> }

module Tone =
    [<Literal>]
    let private ArrayNs =
        "http://schemas.microsoft.com/2003/10/Serialization/Arrays"

    let private getPedal (ns: string option) (gearList: XmlElement) (name: string) =
        let node =
            match ns with
            | Some ns ->
                fun (xel: XmlElement) name -> xel.Item(name, ns)
            | None ->
                fun (xel: XmlElement) name -> xel.Item(name)

        let pedal = node gearList name

        if pedal.IsEmpty then
            None
        else
            let cat = node pedal "Category"
            let skin = node pedal "Skin"
            let skinIndex = node pedal "SkinIndex"

            let knobValues =
                match node pedal "KnobValues" with
                | null ->
                    Map.empty
                | knobs ->
                    knobs.ChildNodes
                    |> Seq.cast<XmlNode>
                    |> Seq.map (fun knob ->
                        let key = knob.Item("Key", ArrayNs).InnerText
                        let value = knob.Item("Value", ArrayNs).InnerText
                        key, float32 value)
                    |> Map.ofSeq

            { Category = if cat.IsEmpty then None else Some cat.InnerText
              Type = (node pedal "Type").InnerText
              Key = (node pedal "PedalKey").InnerText
              KnobValues = knobValues
              Skin =
                if not (isNull skin || skin.IsEmpty) then
                    Some skin.InnerText
                else
                    None
              SkinIndex =
                if not (isNull skinIndex || skinIndex.IsEmpty) then
                    Some(float32 skinIndex.InnerText)
                else
                    None }
            |> Some

    let private getGearList (ns: string option) (gearList: XmlElement) =
        let getPedal = getPedal ns gearList

        match getPedal "Amp", getPedal "Cabinet" with
        | Some amp, Some cabinet ->
            { Amp = amp
              Cabinet = cabinet
              PrePedals = [| 1 .. 4 |] |> Array.map (fun i -> getPedal $"PrePedal{i}")
              PostPedals = [| 1 .. 4 |] |> Array.map (fun i -> getPedal $"PostPedal{i}")
              Racks = [| 1 .. 4 |] |> Array.map (fun i -> getPedal $"Rack{i}") }
        | None, _ ->
            failwith "The tone is missing an amp."
        | _, None ->
            failwith "The tone is missing a cabinet."

    let private getDescriptors (descs: XmlElement) =
        descs.ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map (fun x -> x.InnerText)
        |> Seq.toArray

    let private volumeFromString (vol: string) =
        (* Some DLC have strange values:
           -Love in an Elevator: "-26.250S"
           -Valleri: "-19 .250"
           Some CDLC use ',' as decimal separator *)
        let volFiltered = Regex.Replace(vol.Replace(',', '.'), "[^0-9.-]", "")
        Math.Round(float volFiltered, 1, MidpointRounding.AwayFromZero)

    let private volumeToString (vol: float) = vol.ToString("0.000", NumberFormatInfo.InvariantInfo)

    /// Imports a tone from a Tone2014 XML structure using the optional XML namespace.
    let importXml (ns: string option) (xmlNode: XmlNode) =
        let node =
            match ns with
            | Some ns ->
                fun name -> xmlNode.Item(name, ns)
            | None ->
                // fsharplint:disable-next-line ReimplementsFunction
                fun name -> xmlNode.Item(name)

        let nodeText name = (node name).InnerText

        let macVol = node "MacVolume"

        { Tone.GearList = getGearList ns (node "GearList")
          ToneDescriptors = getDescriptors (node "ToneDescriptors")
          NameSeparator = nodeText "NameSeparator"
          Volume = nodeText "Volume" |> volumeFromString
          MacVolume =
            macVol
            |> Option.ofObj
            |> Option.map (fun x -> volumeFromString x.InnerText)
          Key = nodeText "Key"
          Name = nodeText "Name"
          // Sort order is not needed
          SortOrder = None }

    /// Imports a tone from a Tone2014 XML file.
    let fromXmlFile (fileName: string) =
        let doc = XmlDocument()
        doc.Load(fileName)
        let xel = doc.DocumentElement

        if xel.Name <> "Tone2014" then
            failwith "Not a valid tone XML file."

        importXml None xel

    let private pedalFromDto (dto: PedalDto) =
        { Category = Option.ofObj dto.Category
          Type = dto.Type
          Key = dto.PedalKey
          KnobValues =
            match dto.KnobValues with
            | null ->
                Map.empty
            | values ->
                values
                |> Seq.map (|KeyValue|)
                |> Map.ofSeq
          Skin = Option.ofObj dto.Skin
          SkinIndex = Option.ofNullable dto.SkinIndex }

    // Default cabinet used for CDLC tones that are missing a cabinet
    let private defaultCabinet =
        { Category = None
          Type = "Cabinets"
          Key = "Cab_Marshall1960TV_Ribbon_Cone"
          KnobValues = Map.empty
          Skin = None
          SkinIndex = None }

    let fromDto dto : Tone =
        let gear =
            let fromDtoArray = Array.map (Option.ofObj >> Option.map pedalFromDto)

            let cabinet =
                dto.GearList.Cabinet
                |> Option.ofObj
                |> Option.map pedalFromDto
                |> Option.defaultValue defaultCabinet

            { Amp = pedalFromDto dto.GearList.Amp
              Cabinet = cabinet
              PrePedals = fromDtoArray [| dto.GearList.PrePedal1; dto.GearList.PrePedal2; dto.GearList.PrePedal3; dto.GearList.PrePedal4 |]
              PostPedals = fromDtoArray [| dto.GearList.PostPedal1; dto.GearList.PostPedal2; dto.GearList.PostPedal3; dto.GearList.PostPedal4 |]
              Racks = fromDtoArray [| dto.GearList.Rack1; dto.GearList.Rack2; dto.GearList.Rack3; dto.GearList.Rack4 |] }

        { GearList = gear
          ToneDescriptors = dto.ToneDescriptors
          NameSeparator = dto.NameSeparator
          Volume = volumeFromString dto.Volume
          MacVolume = dto.MacVolume |> Option.ofObj |> Option.map volumeFromString
          Key = dto.Key
          Name = dto.Name
          SortOrder = Option.ofNullable dto.SortOrder }

    let toPedalDto (pedal: Pedal) =
        PedalDto(
            PedalKey = pedal.Key,
            Type = pedal.Type,
            KnobValues = Dictionary(pedal.KnobValues),
            Category = Option.toObj pedal.Category,
            Skin = Option.toObj pedal.Skin,
            SkinIndex = Option.toNullable pedal.SkinIndex
        )

    let toDto (tone: Tone) =
        let tryGetPedal index pedalArray =
            pedalArray
            |> Array.tryItem index
            |> Option.bind id
            |> Option.map toPedalDto
            |> Option.toObj

        let gear =
            { Amp = toPedalDto tone.GearList.Amp
              Cabinet = toPedalDto tone.GearList.Cabinet
              PrePedal1 = tone.GearList.PrePedals |> tryGetPedal 0
              PrePedal2 = tone.GearList.PrePedals |> tryGetPedal 1
              PrePedal3 = tone.GearList.PrePedals |> tryGetPedal 2
              PrePedal4 = tone.GearList.PrePedals |> tryGetPedal 3
              PostPedal1 = tone.GearList.PostPedals |> tryGetPedal 0
              PostPedal2 = tone.GearList.PostPedals |> tryGetPedal 1
              PostPedal3 = tone.GearList.PostPedals |> tryGetPedal 2
              PostPedal4 = tone.GearList.PostPedals |> tryGetPedal 3
              Rack1 = tone.GearList.Racks |> tryGetPedal 0
              Rack2 = tone.GearList.Racks |> tryGetPedal 1
              Rack3 = tone.GearList.Racks |> tryGetPedal 2
              Rack4 = tone.GearList.Racks |> tryGetPedal 3 }

        { GearList = gear
          ToneDescriptors = tone.ToneDescriptors
          NameSeparator = tone.NameSeparator
          IsCustom = Nullable(true)
          Volume = volumeToString tone.Volume
          MacVolume = tone.MacVolume |> Option.map volumeToString |> Option.toObj
          Key = tone.Key
          Name = tone.Name
          SortOrder = Option.toNullable tone.SortOrder }

    /// Returns the number of effects used in the gear list.
    let getEffectCount (gearList: Gear) =
        seq {
            yield! gearList.PrePedals
            yield! gearList.PostPedals
            yield! gearList.Racks
        }
        |> Seq.sumBy Option.count

    /// Imports a tone from a JSON stream.
    let fromJsonStream (stream: Stream) =
        backgroundTask {
            let options = FSharpJsonOptions.Create(ignoreNull = true)
            let! dto = JsonSerializer.DeserializeAsync<ToneDto>(stream, options)
            return fromDto dto
        }

    /// Imports a tone from a JSON file.
    let fromJsonFile (fileName: string) =
        backgroundTask {
            use file = File.OpenRead(fileName)
            return! fromJsonStream file
        }

    /// Exports a tone into a JSON file.
    let exportJson (path: string) (tone: Tone) =
        backgroundTask {
            use file = File.Create(path)
            let options = FSharpJsonOptions.Create(indent = true, ignoreNull = true)
            do! JsonSerializer.SerializeAsync(file, toDto tone, options)
        }

    /// Exports a tone into an XML file in a format that is compatible with the Toolkit.
    let exportXml (path: string) (tone: Tone) =
        backgroundTask {
            let nl = Environment.NewLine
            let serializer = DataContractSerializer(typeof<ToneDto>)
            use mem = MemoryStreamPool.Default.GetStream()
            use writer = XmlWriter.Create(mem, XmlWriterSettings(Indent = true, Encoding = Encoding.UTF8))

            let sb =
                serializer.WriteObject(writer, toDto tone)
                writer.Flush()
                mem.Position <- 0L
                use reader = new StreamReader(mem)
                StringBuilder(reader.ReadToEnd())

            use file = File.Create(path)
            use writer = new StreamWriter(file, Encoding.UTF8)

            // Fix up the XML for it to be importable in the Toolkit
            do!
                sb
                  // The class name is Tone2014
                  .Replace("ToneDto", "Tone2014")
                  // F# incompatibility stuff
                  .Replace("_x0040_", "")
                  // Sort order is not nullable
                  .Replace("""<SortOrder i:nil="true" />""", "<SortOrder>0.0</SortOrder>")
                  // Toolkit does not have MacVolume
                  .Replace($"  <MacVolume i:nil=\"true\" />{nl}", "")
                  // Tone key/name import does not seem to work otherwise
                  .Replace($"<NameSeparator>{tone.NameSeparator}</NameSeparator>{nl}  <Name>{tone.Name}</Name>", $"<Name>{tone.Name}</Name>{nl}  <NameSeparator>{tone.NameSeparator}</NameSeparator>")
                  // Change the namespace
                  .Replace("http://schemas.datacontract.org/2004/07/Rocksmith2014.Common.Manifest", "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.DLCPackage.Manifest.Tone")
               |> writer.WriteAsync
        }
