namespace Rocksmith2014.Common.Manifest

open System.Collections.Generic
open System

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
        sprintf "%s%s" this.Key description
