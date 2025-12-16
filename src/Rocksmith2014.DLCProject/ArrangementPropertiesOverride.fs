module Rocksmith2014.DLCProject.ArrangementPropertiesOverride

open System
open Rocksmith2014.XML
open FlagBuilder

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

open type ArrPropFlags

let fromArrangementProperties (prop: ArrangementProperties) =
    flags {
        if prop.BarreChords then BarreChords
        if prop.Bends then Bends
        if prop.DoubleStops then DoubleStops
        if prop.DropDPower then DropDPower
        if prop.FifthsAndOctaves then FifthsAndOctaves
        if prop.FingerPicking then FingerPicking
        if prop.Harmonics then Harmonics
        if prop.PinchHarmonics then PinchHarmonics
        if prop.SlapPop then SlapPop
        if prop.Sustain then Sustain
        if prop.Tapping then Tapping
        if prop.TwoFingerPicking then TwoFingerPicking
        if prop.PalmMutes then PalmMutes
        if prop.FretHandMutes then FretHandMutes
        if prop.Hopo then Hopo
        if prop.NonStandardChords then NonStandardChords
        if prop.OpenChords then OpenChords
        if prop.PowerChords then PowerChords
        if prop.Slides then Slides
        if prop.UnpitchedSlides then UnpitchedSlides
        if prop.Syncopation then Syncopation
        if prop.Tremolo then Tremolo
        if prop.Vibrato then Vibrato
    }

/// Applies the flags to the arrangement properties object.
let apply (props: ArrangementProperties) (flags: ArrPropFlags) =
    let (!!) flag = (flags &&& flag) = flag

    props.BarreChords <- !! BarreChords
    props.Bends <- !! Bends
    props.DoubleStops <- !! DoubleStops
    props.DropDPower <- !! DropDPower
    props.FifthsAndOctaves <- !! FifthsAndOctaves
    props.FingerPicking <- !! FingerPicking
    props.Harmonics <- !! Harmonics
    props.PinchHarmonics <- !! PinchHarmonics
    props.SlapPop <- !! SlapPop
    props.Sustain <- !! Sustain
    props.Tapping <- !! Tapping
    props.TwoFingerPicking <- !! TwoFingerPicking
    props.PalmMutes <- !! PalmMutes
    props.FretHandMutes <- !! FretHandMutes
    props.Hopo <- !! Hopo
    props.NonStandardChords <- !! NonStandardChords
    props.OpenChords <- !! OpenChords
    props.PowerChords <- !! PowerChords
    props.Slides <- !! Slides
    props.UnpitchedSlides <- !! UnpitchedSlides
    props.Syncopation <- !! Syncopation
    props.Tremolo <- !! Tremolo
    props.Vibrato <- !! Vibrato
