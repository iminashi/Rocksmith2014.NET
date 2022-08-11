module NoteConverter

open Rocksmith2014.XML
open System
open EOFTypes

let inline getBitFlag (string: sbyte) = 1uy <<< (int string)

type FlagBuilder () =
    member inline _.Yield(f) = f
    member inline _.Zero() = LanguagePrimitives.EnumOfValue(LanguagePrimitives.GenericZero)
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
        if note.IsHopo then
            EOFNoteFlag.HOPO
            EOFNoteFlag.F_HOPO
        if note.IsTap then EOFNoteFlag.TAP
        if note.IsVibrato then EOFNoteFlag.VIBRATO
        if note.IsSlap then EOFNoteFlag.SLAP
        if note.IsPluck then EOFNoteFlag.POP
        if note.IsLinkNext then EOFNoteFlag.LINKNEXT
        if note.IsAccent then EOFNoteFlag.ACCENT

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

let getExtendedNoteFlags wasChord (notes: Note array) =
    flags {
        if notes |> Array.forall (fun n -> n.IsIgnore) then
            EOFExtendedNoteFlag.IGNORE

        if wasChord && notes[0].Sustain > 0 then
            EOFExtendedNoteFlag.SUSTAIN
    }

type ChordData = { Template: ChordTemplate; Fingering: byte array }

let createNoteArray (inst: InstrumentalArrangement) (level: Level) =
    let notes =
        level.Notes.ToArray()
        |> Array.groupBy (fun x -> x.Time)
        |> Array.map (fun (_, group) -> None, group)

    let chords =
        level.Chords.ToArray()
        |> Array.map (fun c ->
            let template = inst.ChordTemplates[int c.ChordId]
            let notes =
                if c.HasChordNotes then
                    c.ChordNotes.ToArray()
                else
                    template.Frets
                    |> Array.choosei (fun stringIndex fret ->
                        if fret < 0y then
                            None
                        else
                            let mask =
                                flags {
                                    if c.IsAccent then NoteMask.Accent
                                    if c.IsFretHandMute then NoteMask.FretHandMute
                                    if c.IsPalmMute then NoteMask.PalmMute
                                    if c.IsIgnore then NoteMask.Ignore
                                }

                            Note(
                                Time = c.Time,
                                String = sbyte stringIndex,
                                Fret = fret,
                                Mask = mask
                            )
                            |> Some
                    )

            let chordData =
                let fingering =
                    notes
                    |> Array.map (fun n ->
                        match template.Fingers[int n.String] with
                        | 0y -> 5uy // Thumb
                        | f when f < 0y -> 0uy
                        | f -> byte f)

                { Template = template; Fingering = fingering }

            Some chordData, notes)

    let combined = Array.append chords notes

    combined
    |> Array.sortInPlaceBy (fun (_, ns) -> ns[0].Time)

    combined

let convertNotes (inst: InstrumentalArrangement) (level: Level) =
    let noteGroups = createNoteArray inst level

    noteGroups
    |> Array.map (fun (chordOpt, notes) ->
        let bitFlag =
            notes
            |> Array.fold (fun acc n -> acc ||| getBitFlag (sbyte n.String)) 0uy

        let extendedFlags = getExtendedNoteFlags chordOpt.IsSome notes

        let exFlag =
            if extendedFlags <> EOFExtendedNoteFlag.ZERO then
                EOFNoteFlag.EXTENDED_FLAGS
             else
                EOFNoteFlag.ZERO

        let splitFlag =
            if chordOpt.IsNone && notes.Length > 1 then
                EOFNoteFlag.SPLIT
            else
                EOFNoteFlag.ZERO

        let commonFlags =
            (LanguagePrimitives.EnumOfValue(~~~ 0u), notes)
            ||> Seq.fold (fun acc note -> getNoteFlags note &&& acc)

        // TODO: tech notes

        let frets =
            notes
            |> Array.map (fun note -> if note.IsFretHandMute then 128uy ||| byte note.Fret else byte note.Fret)

        let slide =
            let s1 = notes[0].SlideTo
            if s1 > 0y && notes |> Array.forall (fun n -> n.SlideTo = s1) then
                ValueSome (byte s1)
            else
                ValueNone

        let unpitchedSlide =
            let s1 = notes[0].SlideUnpitchTo
            if s1 > 0y && notes |> Array.forall (fun n -> n.SlideUnpitchTo = s1) then
                ValueSome (byte s1)
            else
                ValueNone

        {
            ChordName = chordOpt |> Option.map (fun x -> x.Template.Name) |> Option.defaultValue String.Empty
            ChordNumber = 0uy
            NoteType = 0uy
            BitFlag = bitFlag
            GhostBitFlag = 0uy
            Frets = frets
            LegacyBitFlags = 0uy
            Position = notes[0].Time |> uint
            Length = max (uint notes[0].Sustain) 1u
            Flags = commonFlags ||| exFlag ||| splitFlag

            SlideEndFret = slide
            BendStrength = ValueNone
            UnpitchedSlideEndFret = unpitchedSlide
            ExtendedNoteFlags = extendedFlags
        },
        chordOpt
        |> Option.map (fun x -> x.Fingering)
        // TODO: fingering for split chord with handshape defined?
        |> Option.defaultWith (fun () -> Array.replicate notes.Length 0uy)
    )
