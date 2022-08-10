module NoteConverter

open Rocksmith2014.XML
open Rocksmith2014.XML.Extension
open System
open EOFTypes

let inline getBitFlag (string: sbyte) = 1uy <<< (int string)

type FlagBuilder () =
    member inline _.Yield(f) = f
    member inline _.Zero() = LanguagePrimitives.EnumOfValue(0u)
    member inline _.Combine(f1, f2) = f1 ||| f2
    member inline _.Delay(f) = f()

let flags = FlagBuilder()

let getNoteFlags (note: Note) =
    flags {
        if note.IsUnpitchedSlide then EOFNoteFlag.UNPITCH_SLIDE
        if note.IsFretHandMute then EOFNoteFlag.STRING_MUTE
        if note.IsPalmMute then EOFNoteFlag.PALM_MUTE
        if note.IsHarmonic then EOFNoteFlag.HARMONIC
        if note.IsPinchHarmonic then EOFNoteFlag.P_HARMONIC
        if note.IsHammerOn then EOFNoteFlag.HO
        if note.IsPullOff then EOFNoteFlag.PO
        if note.IsTap then EOFNoteFlag.TAP
        if note.IsVibrato then EOFNoteFlag.VIBRATO
        if note.IsSlap then EOFNoteFlag.SLAP
        if note.IsPluck then EOFNoteFlag.POP
        if note.IsLinkNext then EOFNoteFlag.LINKNEXT
        if note.IsAccent then EOFNoteFlag.ACCENT

        if note.IsIgnore then EOFNoteFlag.EXTENDED_FLAGS

        if note.IsBend then
            EOFNoteFlag.RS_NOTATION
            EOFNoteFlag.BEND

        if note.IsSlide then
            EOFNoteFlag.RS_NOTATION
            if note.SlideTo > note.Fret then
                EOFNoteFlag.SLIDE_UP
            else
                EOFNoteFlag.SLIDE_DOWN
    }

let getExtendedNoteFlags (note: Note) =
    flags {
        if note.IsIgnore then EOFExtendedNoteFlag.IGNORE
    }

let convertNotes (inst: InstrumentalArrangement) (level: Level) =
    let entities = createXmlEntityArrayFromLevel level
    let chordTemplates = inst.ChordTemplates

    entities
    |> Array.map (fun noteOrChord ->
        match noteOrChord with
        | XmlNote note ->
            // TODO: Combine 'split' chords
            let bitFlag = getBitFlag note.String
            let frets =
                if note.IsFretHandMute then 128uy ||| byte note.Fret else byte note.Fret
                |> Array.singleton

            {
                ChordName = String.Empty
                ChordNumber = 0uy
                NoteType = 0uy
                BitFlag = bitFlag
                GhostBitFlag = 0uy
                Frets = frets
                LegacyBitFlags = 0uy
                Position = note.Time |> uint
                Length = max (uint note.Sustain) 1u
                Flags = getNoteFlags note

                SlideEndFret = if note.IsSlide then ValueSome (byte note.SlideTo) else ValueNone
                BendStrength = if note.IsBend then ValueSome (byte note.MaxBend) else ValueNone // TODO correct conversion
                UnpitchedSlideEndFret = if note.IsUnpitchedSlide then ValueSome (byte note.SlideUnpitchTo) else ValueNone
                ExtendedNoteFlags = getExtendedNoteFlags note
            }
        | XmlChord chord ->
            {
                ChordName = chordTemplates[int chord.ChordId].Name
                ChordNumber = 0uy
                NoteType = 0uy
                BitFlag = 0uy
                GhostBitFlag = 0uy
                Frets = Array.empty
                LegacyBitFlags = 0uy
                Position = chord.Time |> uint
                Length = 0u
                Flags = EOFNoteFlag.ZERO

                SlideEndFret = ValueNone
                BendStrength = ValueNone
                UnpitchedSlideEndFret = ValueNone
                ExtendedNoteFlags = EOFExtendedNoteFlag.ZERO
            }
    )
