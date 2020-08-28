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
        let getPedal (pedal: XmlElement) =
            if pedal.IsEmpty then Unchecked.defaultof<Pedal>
            else
                let cat = pedal.Item "Category"
                let typ = pedal.Item "Type"
                let key = pedal.Item "PedalKey"
                let skin = pedal.Item "Skin"
                let skinIndex = pedal.Item "SkinIndex"
                let knobValues = pedal.Item "KnobValues"
                let knobs =
                    knobValues.ChildNodes
                    |> Seq.cast<XmlNode>
                    |> Seq.map (fun node ->
                        let key = (node.Item ("Key", "http://schemas.microsoft.com/2003/10/Serialization/Arrays")).InnerText
                        let value = (node.Item ("Value", "http://schemas.microsoft.com/2003/10/Serialization/Arrays")).InnerText
                        key, float32 value)
                    |> dict
                    |> Dictionary
                printfn "%A" knobs
        
                Pedal(Category = (if cat.IsEmpty then null else cat.InnerText),
                      Type = typ.InnerText,
                      Key = key.InnerText,
                      KnobValues = knobs,
                      Skin = (if not <| isNull skin then skin.InnerText else null),
                      SkinIndex = (if not <| isNull skinIndex then Nullable(float32 skin.InnerText) else Nullable()))
        
        let getGearList (gearList: XmlElement) =
            let amp = gearList.Item "Amp"
            let cab = gearList.Item "Cabinet"
            let r1 = gearList.Item "Rack1"
            let r2 = gearList.Item "Rack2"
            let r3 = gearList.Item "Rack3"
            let r4 = gearList.Item "Rack4"
            let pre1 = gearList.Item "PrePedal1"
            let pre2 = gearList.Item "PrePedal2"
            let pre3 = gearList.Item "PrePedal3"
            let pre4 = gearList.Item "PrePedal4"
            let post1 = gearList.Item "PostPedal1"
            let post2 = gearList.Item "PostPedal2"
            let post3 = gearList.Item "PostPedal3"
            let post4 = gearList.Item "PostPedal4"
        
            { Rack1 = getPedal r1
              Rack2 = getPedal r2
              Rack3 = getPedal r3
              Rack4 = getPedal r4
              Amp = getPedal amp
              Cabinet = getPedal cab
              PrePedal1 = getPedal pre1
              PrePedal2 = getPedal pre2
              PrePedal3 = getPedal pre3
              PrePedal4 = getPedal pre4
              PostPedal1 = getPedal post1
              PostPedal2 = getPedal post2
              PostPedal3 = getPedal post3
              PostPedal4 = getPedal post4 }

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
            let gearList = getGearList (docEl.Item "GearList")
            let isCustom = (docEl.Item "IsCustom").InnerText
            let macVol = docEl.Item "MacVolume"
            let sortOrder = docEl.Item "SortOrder"
            let toneDesc = getDescriptors (docEl.Item "ToneDescriptors")

            { GearList = gearList
              ToneDescriptors = toneDesc
              NameSeparator = (docEl.Item "NameSeparator").InnerText
              IsCustom = Boolean.Parse(isCustom)
              Volume = (docEl.Item "Volume").InnerText
              MacVolume = if isNull macVol then null else macVol.InnerText
              Key = (docEl.Item "Key").InnerText
              Name = (docEl.Item "Name").InnerText
              SortOrder = if isNull sortOrder then Nullable() else Nullable(float32 sortOrder.InnerText) }
