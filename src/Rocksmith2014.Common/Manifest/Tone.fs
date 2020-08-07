namespace Rocksmith2014.Common.Manifest

open System.Collections.Generic

type Pedal =
    { Type : string
      KnobValues : Dictionary<string, float32>
      Key : string
      Category : string
      Skin : string option
      SkinIndex : float32 option }

type Gear =
    { Rack1 : Pedal option
      Rack2 : Pedal option
      Rack3 : Pedal option
      Rack4 : Pedal option
      Amp : Pedal
      Cabinet : Pedal
      PrePedal1 : Pedal option
      PrePedal2 : Pedal option
      PrePedal3 : Pedal option
      PrePedal4 : Pedal option
      PostPedal1 : Pedal option
      PostPedal2 : Pedal option
      PostPedal3 : Pedal option
      PostPedal4 : Pedal option }

type Tone =
    { GearList : Gear
      ToneDescriptors : string array 
      NameSeparator : string
      IsCustom : bool
      Volume : string
      MacVolume : string
      Key : string
      Name : string
      SortOrder : float32 }
