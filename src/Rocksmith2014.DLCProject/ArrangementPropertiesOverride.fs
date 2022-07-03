module Rocksmith2014.DLCProject.ArrangementPropertiesOverride

open Rocksmith2014.XML
open System

[<Flags>]
type ArrPropFlags =
    | Empty =             0b000000000000000000000000u
    | BarreChords =       0b000000000000000000000001u
    | Bends =             0b000000000000000000000010u
    | DoubleStops =       0b000000000000000000000100u
    | DropDPower =        0b000000000000000000001000u
    | FifthsAndOctaves =  0b000000000000000000010000u
    | FingerPicking =     0b000000000000000000100000u
    | Harmonics =         0b000000000000000001000000u
    | PinchHarmonics =    0b000000000000000010000000u
    | SlapPop =           0b000000000000000100000000u
    | Sustain =           0b000000000000001000000000u
    | Tapping =           0b000000000000010000000000u
    | TwoFingerPicking =  0b000000000000100000000000u
    | PalmMutes =         0b000000000001000000000000u
    | FretHandMutes =     0b000000000010000000000000u
    | Hopo =              0b000000000100000000000000u
    | NonStandardChords = 0b000000001000000000000000u
    | OpenChords =        0b000000010000000000000000u
    | PowerChords =       0b000000100000000000000000u
    | Slides =            0b000001000000000000000000u
    | UnpitchedSlides =   0b000010000000000000000000u
    | Syncopation =       0b000100000000000000000000u
    | Tremolo =           0b001000000000000000000000u
    | Vibrato =           0b010000000000000000000000u

let fromArrangementProperties (prop: ArrangementProperties) =
    if prop.BarreChords then ArrPropFlags.BarreChords else ArrPropFlags.Empty
    ||| if prop.Bends then ArrPropFlags.Bends else ArrPropFlags.Empty
    ||| if prop.DoubleStops then ArrPropFlags.DoubleStops else ArrPropFlags.Empty
    ||| if prop.DropDPower then ArrPropFlags.DropDPower else ArrPropFlags.Empty
    ||| if prop.FifthsAndOctaves then ArrPropFlags.FifthsAndOctaves else ArrPropFlags.Empty
    ||| if prop.FingerPicking then ArrPropFlags.FingerPicking else ArrPropFlags.Empty
    ||| if prop.Harmonics then ArrPropFlags.Harmonics else ArrPropFlags.Empty
    ||| if prop.PinchHarmonics then ArrPropFlags.PinchHarmonics else ArrPropFlags.Empty
    ||| if prop.SlapPop then ArrPropFlags.SlapPop else ArrPropFlags.Empty
    ||| if prop.Sustain then ArrPropFlags.Sustain else ArrPropFlags.Empty
    ||| if prop.Tapping then ArrPropFlags.Tapping else ArrPropFlags.Empty
    ||| if prop.TwoFingerPicking then ArrPropFlags.TwoFingerPicking else ArrPropFlags.Empty
    ||| if prop.PalmMutes then ArrPropFlags.PalmMutes else ArrPropFlags.Empty
    ||| if prop.FretHandMutes then ArrPropFlags.FretHandMutes else ArrPropFlags.Empty
    ||| if prop.Hopo then ArrPropFlags.Hopo else ArrPropFlags.Empty
    ||| if prop.NonStandardChords then ArrPropFlags.NonStandardChords else ArrPropFlags.Empty
    ||| if prop.OpenChords then ArrPropFlags.OpenChords else ArrPropFlags.Empty
    ||| if prop.PowerChords then ArrPropFlags.PowerChords else ArrPropFlags.Empty
    ||| if prop.Slides then ArrPropFlags.Slides else ArrPropFlags.Empty
    ||| if prop.UnpitchedSlides then ArrPropFlags.UnpitchedSlides else ArrPropFlags.Empty
    ||| if prop.Syncopation then ArrPropFlags.Syncopation else ArrPropFlags.Empty
    ||| if prop.Tremolo then ArrPropFlags.Tremolo else ArrPropFlags.Empty
    ||| if prop.Vibrato then ArrPropFlags.Vibrato else ArrPropFlags.Empty

/// Applies the flags to the arrangement properties object.
let apply (props: ArrangementProperties) (flags: ArrPropFlags) =
    let (!!) flag = (flags &&& flag) = flag
    
    props.BarreChords <- !! ArrPropFlags.BarreChords
    props.Bends <- !! ArrPropFlags.Bends
    props.DoubleStops <- !! ArrPropFlags.DoubleStops
    props.DropDPower <- !! ArrPropFlags.DropDPower
    props.FifthsAndOctaves <- !! ArrPropFlags.FifthsAndOctaves
    props.FingerPicking <- !! ArrPropFlags.FingerPicking
    props.Harmonics <- !! ArrPropFlags.Harmonics
    props.PinchHarmonics <- !! ArrPropFlags.PinchHarmonics
    props.SlapPop <- !! ArrPropFlags.SlapPop
    props.Sustain <- !! ArrPropFlags.Sustain
    props.Tapping <- !! ArrPropFlags.Tapping
    props.TwoFingerPicking <- !! ArrPropFlags.TwoFingerPicking
    props.PalmMutes <- !! ArrPropFlags.PalmMutes
    props.FretHandMutes <- !! ArrPropFlags.FretHandMutes
    props.Hopo <- !! ArrPropFlags.Hopo
    props.NonStandardChords <- !! ArrPropFlags.NonStandardChords
    props.OpenChords <- !! ArrPropFlags.OpenChords
    props.PowerChords <- !! ArrPropFlags.PowerChords
    props.Slides <- !! ArrPropFlags.Slides
    props.UnpitchedSlides <- !! ArrPropFlags.UnpitchedSlides
    props.Syncopation <- !! ArrPropFlags.Syncopation
    props.Tremolo <- !! ArrPropFlags.Tremolo
    props.Vibrato <- !! ArrPropFlags.Vibrato
