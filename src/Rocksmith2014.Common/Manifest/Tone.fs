namespace Rocksmith2014.Common.Manifest

open System.Collections.Generic
open System
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
      IsCustom : bool
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
        sprintf "%s%s" this.Name description

    static member ImportFromXml(fileName: string) =
        let getPedal (gearList: XmlElement) (name: string) =
            let pedal = gearList.Item name
            if pedal.IsEmpty then Unchecked.defaultof<Pedal>
            else
                let cat = pedal.Item "Category"
                let skin = pedal.Item "Skin"
                let skinIndex = pedal.Item "SkinIndex"
                let knobValues =
                    (pedal.Item "KnobValues").ChildNodes
                    |> Seq.cast<XmlNode>
                    |> Seq.map (fun node ->
                        let key = (node.Item ("Key", "http://schemas.microsoft.com/2003/10/Serialization/Arrays")).InnerText
                        let value = (node.Item ("Value", "http://schemas.microsoft.com/2003/10/Serialization/Arrays")).InnerText
                        key, float32 value)
                    |> dict
                    |> Dictionary
        
                Pedal(Category = (if cat.IsEmpty then null else cat.InnerText),
                      Type = (pedal.Item "Type").InnerText,
                      Key = (pedal.Item "PedalKey").InnerText,
                      KnobValues = knobValues,
                      Skin = (if not <| isNull skin then skin.InnerText else null),
                      SkinIndex = (if not <| isNull skinIndex then Nullable(float32 skin.InnerText) else Nullable()))
        
        let getGearList (gearList: XmlElement) =
            let getPedal = getPedal gearList
        
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

        let getDescriptors (descs: XmlElement) =
            descs.ChildNodes
            |> Seq.cast<XmlNode>
            |> Seq.map (fun x -> x.InnerText)
            |> Seq.toArray

        let doc = XmlDocument()
        doc.Load(fileName)
        let docEl = doc.DocumentElement

        if docEl.Name <> "Tone2014" then
            failwith "Not a valid tone XML file."
        else
            let macVol = docEl.Item "MacVolume"
            let sortOrder = docEl.Item "SortOrder"

            { GearList = getGearList (docEl.Item "GearList")
              ToneDescriptors = getDescriptors (docEl.Item "ToneDescriptors")
              NameSeparator = (docEl.Item "NameSeparator").InnerText
              IsCustom = Boolean.Parse((docEl.Item "IsCustom").InnerText)
              Volume = (docEl.Item "Volume").InnerText
              MacVolume = if isNull macVol then null else macVol.InnerText
              Key = (docEl.Item "Key").InnerText
              Name = (docEl.Item "Name").InnerText
              SortOrder = if isNull sortOrder then Nullable() else Nullable(float32 sortOrder.InnerText) }
