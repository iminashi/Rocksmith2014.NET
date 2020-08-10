namespace Rocksmith2014.SNG

open System

[<Flags>]
type BeatMask =
    | None               = 0b00
    | FirstBeatOfMeasure = 0b01
    | EvenMeasure        = 0b10

[<Flags>]
type ChordMask =
    | None     = 0b00u
    | Arpeggio = 0b01u
    | Nop      = 0b10u

[<Flags>]
type NoteMask =
    | None           = 0b00000000_00000000_00000000_00000000u
  //| Unused         = 0b00000000_00000000_00000000_00000001u
    | Chord          = 0b00000000_00000000_00000000_00000010u
    | Open           = 0b00000000_00000000_00000000_00000100u
    // Fret-hand mute for chords.
    | FretHandMute   = 0b00000000_00000000_00000000_00001000u
    | Tremolo        = 0b00000000_00000000_00000000_00010000u
    | Harmonic       = 0b00000000_00000000_00000000_00100000u
    | PalmMute       = 0b00000000_00000000_00000000_01000000u
    | Slap           = 0b00000000_00000000_00000000_10000000u
    | Pluck          = 0b00000000_00000000_00000001_00000000u
    | HammerOn       = 0b00000000_00000000_00000010_00000000u
    | PullOff        = 0b00000000_00000000_00000100_00000000u
    | Slide          = 0b00000000_00000000_00001000_00000000u
    | Bend           = 0b00000000_00000000_00010000_00000000u
    | Sustain        = 0b00000000_00000000_00100000_00000000u
    | Tap            = 0b00000000_00000000_01000000_00000000u
    | PinchHarmonic  = 0b00000000_00000000_10000000_00000000u
    | Vibrato        = 0b00000000_00000001_00000000_00000000u
    // Fret-hand mute for notes.
    | Mute           = 0b00000000_00000010_00000000_00000000u
    | Ignore         = 0b00000000_00000100_00000000_00000000u
    | LeftHand       = 0b00000000_00001000_00000000_00000000u
    | RightHand      = 0b00000000_00010000_00000000_00000000u
    | HighDensity    = 0b00000000_00100000_00000000_00000000u
    | UnpitchedSlide = 0b00000000_01000000_00000000_00000000u
    | Single         = 0b00000000_10000000_00000000_00000000u
    | ChordNotes     = 0b00000001_00000000_00000000_00000000u
    | DoubleStop     = 0b00000010_00000000_00000000_00000000u
    | Accent         = 0b00000100_00000000_00000000_00000000u
    | Parent         = 0b00001000_00000000_00000000_00000000u
    | Child          = 0b00010000_00000000_00000000_00000000u
    | Arpeggio       = 0b00100000_00000000_00000000_00000000u
  //| Unused         = 0b01000000_00000000_00000000_00000000u
    | ChordPanel     = 0b10000000_00000000_00000000_00000000u

module Masks =
    /// Mask bits that need to be considered when converting a note to XML.
    let NoteTechniques = 
        NoteMask.Accent ||| NoteMask.HammerOn ||| NoteMask.Harmonic ||| NoteMask.Ignore ||| NoteMask.Mute
        ||| NoteMask.PalmMute ||| NoteMask.Parent ||| NoteMask.PinchHarmonic ||| NoteMask.Pluck
        ||| NoteMask.PullOff ||| NoteMask.RightHand ||| NoteMask.Slap ||| NoteMask.Tremolo

    /// Mask bits that need to be considered when converting a chord to XML.
    let ChordTechniques =
            NoteMask.Accent ||| NoteMask.FretHandMute ||| NoteMask.HighDensity
            ||| NoteMask.Ignore ||| NoteMask.PalmMute ||| NoteMask.Parent
