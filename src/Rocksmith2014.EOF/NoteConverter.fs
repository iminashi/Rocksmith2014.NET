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

let getNoteFlags (extFlag: EOFExtendedNoteFlag) (note: Note) =
    flags {
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
        if note.IsTremolo then EOFNoteFlag.TREMOLO

        if note.IsUnpitchedSlide then EOFNoteFlag.UNPITCH_SLIDE
        if note.IsSlide then
            EOFNoteFlag.RS_NOTATION
            if note.SlideTo > note.Fret then
                EOFNoteFlag.SLIDE_UP
            else
                EOFNoteFlag.SLIDE_DOWN

        if extFlag <> EOFExtendedNoteFlag.ZERO then
            EOFNoteFlag.EXTENDED_FLAGS
    }

let getExtendedNoteFlags wasChord (note: Note) =
    flags {
        if note.IsIgnore then EOFExtendedNoteFlag.IGNORE
        if wasChord && note.Sustain > 0 then EOFExtendedNoteFlag.SUSTAIN
    }

type ChordData =
    { Template: ChordTemplate
      ChordId: int16
      HandshapeId: int
      IsFullPanel: bool
      IsFirstInHandShape: bool
      Fingering: byte array }

let notesFromTemplate (c: Chord) (template: ChordTemplate) =
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
            |> Some)

let getTemplateFingering (template: ChordTemplate) (stringIndex: int) =
    match template.Fingers[stringIndex] with
    | 0y -> 5uy // Thumb
    | f when f < 0y -> 0uy
    | f -> byte f

// TODO: handshapes where no note/chord is found at the start time

type NoteGroup =
    { Chord: ChordData option
      Time: uint
      Notes: Note array }

let createNoteGroups (inst: InstrumentalArrangement) (level: Level) =
    let notes =
        level.Notes.ToArray()
        |> Array.groupBy (fun x -> x.Time)
        |> Array.map (fun (t, group) -> { Chord = None; Notes = group; Time = uint t })

    let chords =
        level.Chords.ToArray()
        |> Array.map (fun c ->
            let template = inst.ChordTemplates[int c.ChordId]
            let notes =
                if c.HasChordNotes then
                    c.ChordNotes.ToArray()
                else
                    notesFromTemplate c template

            let chordData =
                let fingering =
                    notes
                    |> Array.map (fun n -> getTemplateFingering template (int n.String))

                let handshapeId =
                    level.HandShapes.FindIndex(fun hs -> c.Time >= hs.StartTime && c.Time < hs.EndTime)
                let handshapeStartTime =
                    if handshapeId < 0 then -1 else level.HandShapes[handshapeId].StartTime

                { Template = template
                  ChordId = c.ChordId
                  HandshapeId = handshapeId
                  IsFullPanel = c.HasChordNotes && not c.IsHighDensity
                  IsFirstInHandShape = c.Time = handshapeStartTime
                  Fingering = fingering }

            { Chord = Some chordData; Notes = notes; Time = uint c.Time })

    let combined = Array.append chords notes

    combined
    |> Array.sortInPlaceBy (fun x -> x.Time)

    combined

let convertBendValue (step: float32) =
    let isQuarter = ceil step <> step
    if isQuarter then
        byte (step * 2.f) ||| 128uy
    else
        byte step

let convertNotes (inst: InstrumentalArrangement) (level: Level) =
    let noteGroups = createNoteGroups inst level

    noteGroups
    |> Array.mapi (fun index { Chord = chordOpt; Notes = notes; Time = time } ->
        let crazyFlag =
            chordOpt
            |> Option.bind (fun c ->
                noteGroups
                |> Array.tryItem (index - 1)
                |> Option.bind (fun x -> x.Chord)
                |> Option.map (fun prevData ->
                    // Apply "crazy" if the previous chord is in a different handshape
                    prevData.ChordId = c.ChordId && prevData.HandshapeId <> c.HandshapeId)
                |> Option.orElse (Some (c.IsFullPanel && not c.IsFirstInHandShape))
            )
            |> function
            | Some true -> EOFNoteFlag.CRAZY
            | _ -> EOFNoteFlag.ZERO

        // Find possible chord template for handshape at note time
        let handshapeTemplate =
            level.HandShapes.Find(fun hs -> hs.StartTime = int time)
            |> Option.ofObj
            |> Option.map (fun hs -> inst.ChordTemplates.[int hs.ChordId])

        let bitFlagsFromHandshape =
            handshapeTemplate
            |> Option.map (fun hst ->
                hst.Frets
                |> Array.mapi (fun i f -> if f >= 0y then getBitFlag (sbyte i) else 0uy)
                |> Array.reduce (|||))

        let bitFlags =
            notes
            |> Array.map (fun n -> getBitFlag (sbyte n.String))
        let trueBitFlag = bitFlags |> Array.reduce (|||)
        let commonBitFlag =
            bitFlagsFromHandshape
            |> Option.defaultValue trueBitFlag

        let ghostBitFlag =
            bitFlagsFromHandshape
            |> Option.map (fun tbf -> trueBitFlag ^^^ tbf)
            |> Option.defaultValue 0uy

        let extendedNoteFlags = notes |> Array.map (getExtendedNoteFlags chordOpt.IsSome)
        let commonExtendedNoteFlags = extendedNoteFlags |> Array.reduce (&&&)

        let splitFlag =
            if chordOpt.IsNone && notes.Length > 1 then
                EOFNoteFlag.SPLIT
            else
                EOFNoteFlag.ZERO

        let slide =
            let slideTo = notes[0].SlideTo
            let distance = notes[0].Fret - slideTo
            if slideTo > 0y && notes |> Array.forall (fun n -> n.Fret - n.SlideTo = distance) then
                ValueSome (byte slideTo)
            else
                ValueNone

        let unpitchedSlide =
            let uSlideTo = notes[0].SlideUnpitchTo
            let distance = notes[0].Fret - uSlideTo
            if uSlideTo > 0y && notes |> Array.forall (fun n -> n.Fret - n.SlideUnpitchTo = distance) then
                ValueSome (byte uSlideTo)
            else
                ValueNone

        let noteFlags =
            notes
            |> Array.mapi (fun i n -> getNoteFlags extendedNoteFlags[i] n)

        let commonFlags =
            let c = noteFlags |> Array.reduce (&&&)
            let c2 =
                if slide.IsNone then
                    c &&& (~~~ (EOFNoteFlag.SLIDE_DOWN ||| EOFNoteFlag.SLIDE_UP ||| EOFNoteFlag.RS_NOTATION))
                else
                    c
            if unpitchedSlide.IsNone then c2 &&& (~~~ EOFNoteFlag.UNPITCH_SLIDE) else c2

        let frets =
            handshapeTemplate
            |> Option.map (fun hst ->
                hst.Frets
                |> Array.choosei (fun i f ->
                    let isMuted =
                        notes
                        |> Array.exists (fun n -> n.String = sbyte i && n.IsFretHandMute)
                    if f < 0y then
                        None
                    else
                        if isMuted then
                            128uy ||| byte f
                        else
                            byte f
                        |> Some))
            |> Option.defaultWith (fun () ->
                notes
                |> Array.map (fun note ->
                    if note.IsFretHandMute then 128uy ||| byte note.Fret else byte note.Fret))

        let maxSus = (notes |> Array.maxBy (fun n -> n.Sustain)).Sustain
        let stopTechNotes =
            if splitFlag = EOFNoteFlag.ZERO || notes.Length = 1 then
                Array.empty
            else
                notes
                |> Array.choose (fun n ->
                    if maxSus - n.Sustain > 3 then
                        { EOFNote.Empty with
                              BitFlag = getBitFlag (sbyte n.String)
                              Position = uint (n.Time + n.Sustain)
                              Flags = EOFNoteFlag.EXTENDED_FLAGS
                              ExtendedNoteFlags = EOFExtendedNoteFlag.STOP }
                        |> Some
                    else
                        None)

        let techNotes =
            noteFlags
            |> Array.choosei (fun i flag ->
                let extFlag = extendedNoteFlags[i]
                if (flag &&& commonFlags = flag) && (extFlag &&& commonExtendedNoteFlags = extFlag) then
                    None
                else
                    let n = notes[i]
                    { EOFNote.Empty with
                        BitFlag = bitFlags[i]
                        Position = time
                        Flags = flag &&& (~~~ commonFlags)
                        SlideEndFret =
                            if slide.IsNone && n.IsSlide then
                                ValueSome (byte n.SlideTo)
                            else
                                ValueNone
                        UnpitchedSlideEndFret =
                            if unpitchedSlide.IsNone && n.IsUnpitchedSlide then
                                ValueSome (byte n.SlideUnpitchTo)
                            else
                                ValueNone
                        ExtendedNoteFlags = extFlag &&& (~~~ commonExtendedNoteFlags)
                    }
                    |> Some)

        let bendTechNotes =
            notes
            |> Array.collect (fun n ->
                if not n.IsBend then
                    Array.empty
                else
                    n.BendValues.ToArray()
                    |> Array.map (fun bv ->
                        { EOFNote.Empty with
                            BitFlag = getBitFlag (sbyte n.String)
                            Position = uint bv.Time
                            Flags = EOFNoteFlag.RS_NOTATION ||| EOFNoteFlag.BEND
                            BendStrength = ValueSome (convertBendValue bv.Step) }
                    )
            )

        let chordName =
            chordOpt
            |> Option.map (fun x -> x.Template.Name)
            |> Option.defaultValue String.Empty

        let eofNote =
            { EOFNote.Empty with
                ChordName = chordName
                BitFlag = commonBitFlag
                GhostBitFlag = ghostBitFlag
                Frets = frets
                Position = time
                Length = max (uint maxSus) 1u
                Flags = commonFlags ||| splitFlag ||| crazyFlag
                SlideEndFret = slide
                UnpitchedSlideEndFret = unpitchedSlide
                ExtendedNoteFlags = commonExtendedNoteFlags
            }

        let fingering =
            handshapeTemplate
            // Prefer fingering from template
            |> Option.map (fun x ->
                x.Frets
                |> Array.mapi (fun i x -> if x >= 0y then i else -1)
                |> Array.filter (fun i -> i > 0)
                |> Array.map (getTemplateFingering x))
            |> Option.orElseWith (fun () ->
                chordOpt
                |> Option.map (fun x -> x.Fingering))
            // No fingering defined
            |> Option.defaultWith (fun () -> Array.replicate notes.Length 0uy)

        let techNotes = Array.concat [ techNotes; bendTechNotes; stopTechNotes ]

        eofNote, fingering, techNotes
    )
