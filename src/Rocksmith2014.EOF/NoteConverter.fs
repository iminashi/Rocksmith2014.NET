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

        if note.IsSlide then
            EOFNoteFlag.RS_NOTATION
            if note.SlideTo > note.Fret then
                EOFNoteFlag.SLIDE_UP
            else
                EOFNoteFlag.SLIDE_DOWN
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
                let f =
                    if note.IsFretHandMute then 128uy ||| byte note.Fret else byte note.Fret

                Array.singleton f

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
                BendStrength = ValueNone
                UnpitchedSlideEndFret = if note.IsUnpitchedSlide then ValueSome (byte note.SlideUnpitchTo) else ValueNone
                ExtendedNoteFlags = 0u
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
                ExtendedNoteFlags = 0u
            }
    )
