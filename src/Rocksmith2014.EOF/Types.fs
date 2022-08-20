module Rocksmith2014.EOF.EOFTypes

open Rocksmith2014.XML
open System

type ProGuitarTrack =
    | ActualTrack of name: string * ImportedArrangement
    | EmptyTrack of name: string

type EOFTrack =
    | Track0
    | Legacy of name: string * behavior: byte * type': byte * lanes: byte
    | Vocals of vocals: Vocal seq
    | ProGuitar of guitarTrack: ProGuitarTrack

type EOFSection =
    { Name: string
      Type: byte
      StartTime: uint
      EndTime: uint
      Flags: uint }

    static member Create(t, s, e, f) =
        { Name = String.Empty; Type = t; StartTime = s; EndTime = e; Flags = f }

type EOFTimeSignature =
    | ``TS 2 | 4``
    | ``TS 3 | 4``
    | ``TS 4 | 4``
    | ``TS 5 | 4``
    | ``TS 6 | 4``
    | CustomTS of nominator: uint * denominator: uint

type IniStringType =
    | Custom = 0uy
    //| Album = 1uy
    | Artist = 2uy
    | Title = 3uy
    | Frettist = 4uy
    //| (unused)
    | Year = 6uy
    | LoadingText = 7uy
    | Album = 8uy
    | Genre = 9uy
    | TrackNumber = 10uy

type IniString =
    { StringType: IniStringType
      Value: string }

[<Flags>]
type EOFNoteFlag =
     | ZERO           = 0u
     | HOPO           = 1u
     | CRAZY          = 4u
     | F_HOPO         = 8u // Manually defines the note as being HOPO
     | ACCENT         = 32u
     | P_HARMONIC     = 64u
     | LINKNEXT       = 128u
     | UNPITCH_SLIDE  = 256u
     | HO             = 512u
     | PO             = 1024u
     | TAP            = 2048u
     | SLIDE_UP       = 4096u
     | SLIDE_DOWN     = 8192u
     | STRING_MUTE    = 16384u
     | PALM_MUTE      = 32768u
     | TREMOLO        = 131072u
   //| UP_STRUM       = 262144u
   //| DOWN_STRUM     = 524288u
   //| MID_STRUM      = 1048576u
     | BEND           = 2097152u
     | HARMONIC       = 4194304u
   //| SLIDE_REVERSE  = 8388608u
     | VIBRATO        = 16777216u
     | RS_NOTATION    = 33554432u
     | POP            = 67108864u
     | SLAP           = 134217728u
   //| HD             = 268435456u
     | SPLIT          = 536870912u
     | EXTENDED_FLAGS = 2147483648u

[<Flags>]
type EOFExtendedNoteFlag =
    | ZERO       = 0u
    | IGNORE     = 1u
    | SUSTAIN    = 2u
    | STOP       = 4u
    | GHOST_HS   = 8u
    | CHORDIFY   = 16u
    | FINGERLESS = 32u
    | PRE_BEND   = 64u

[<Flags>]
type EOFTrackFlag =
    | SIX_LANES       = 1u
    | ALT_NAME        = 2u
    | UNLIMITED_DIFFS = 4u
    | GHL_MODE        = 8u
    | RS_BONUS_ARR    = 16u
    | GHL_MODE_MS     = 32u
    | RS_ALT_ARR      = 64u
    | RS_PICKED_BASS  = 128u

[<Flags>]
type EOFEventFlag =
    | RS_PHRASE      = 1us
    | RS_SECTION     = 2us
    | RS_EVENT       = 4us
    | RS_SOLO_PHRASE = 8us
    | FLOATING_POS   = 16us

type EOFEvent =
    { Text: string
      BeatNumber: int
      TrackNumber: uint16
      Flag: EOFEventFlag }

type EOFNote =
    {
        ChordName: string
        ChordNumber: byte
        Difficulty: byte
        BitFlag: byte
        GhostBitFlag: byte
        Frets: byte array
        LegacyBitFlags: byte
        Position: uint
        Length: uint
        Flags: EOFNoteFlag

        SlideEndFret: byte voption
        BendStrength: byte voption
        UnpitchedSlideEndFret: byte voption
        ExtendedNoteFlags: EOFExtendedNoteFlag
    }

module EOFNote =
    let Empty =
        {
            ChordName = String.Empty
            ChordNumber = 0uy
            Difficulty = 0uy
            BitFlag = 0uy
            GhostBitFlag = 0uy
            Frets = Array.singleton 0uy
            LegacyBitFlags = 0uy
            Position = 0u
            Length = 1u
            Flags = EOFNoteFlag.ZERO
            SlideEndFret = ValueNone
            BendStrength = ValueNone
            UnpitchedSlideEndFret = ValueNone
            ExtendedNoteFlags = EOFExtendedNoteFlag.ZERO
        }

[<Struct>]
type SustainAdjustment =
    { Difficulty: byte
      Time: uint
      NewSustain: uint }

type HsResult =
    | AdjustSustains of SustainAdjustment array
    | SectionCreated of EOFSection

type ChordData =
    { Template: ChordTemplate
      ChordId: int16
      HandshapeId: int
      IsFullPanel: bool
      IsFirstInHandShape: bool
      IsLinkNext: bool
      Fingering: byte array }

type NoteGroup =
    { Chord: ChordData option
      Time: uint
      Difficulty: byte
      Notes: Note array }
