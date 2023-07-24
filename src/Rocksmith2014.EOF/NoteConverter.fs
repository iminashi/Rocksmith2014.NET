module Rocksmith2014.EOF.NoteConverter

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open System
open EOFTypes
open FlagBuilder

let inline getBitFlag (string: sbyte) = 1uy <<< (int string)

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

let getExtendedNoteFlags (chordData: ChordData option) (note: Note) =
    let sustainFlagNeeded =
        note.Sustain > 0 &&
        chordData
        |> Option.exists (fun c ->
            not (c.IsLinkNext || note.IsVibrato || note.IsBend || note.IsTremolo || note.IsSlide || note.IsUnpitchedSlide)
        )

    flags {
        if note.IsIgnore then
            EOFExtendedNoteFlag.IGNORE

        if sustainFlagNeeded then
            EOFExtendedNoteFlag.SUSTAIN
    }

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

let convertTemplateFingering (template: ChordTemplate) (stringIndex: int) =
    match template.Fingers[stringIndex] with
    | 0y -> 5uy // Thumb
    | f when f < 0y -> 0uy
    | f -> byte f

let fretsFromTemplate (notes: Note array) (ct: ChordTemplate) =
    ct.Frets
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
            |> Some)

let fingeringFromTemplate (ct: ChordTemplate) =
    ct.Frets
    |> Array.mapi (fun i x -> if x >= 0y then i else 0xDEADBEEF)
    |> Array.filter ((<>) 0xDEADBEEF)
    |> Array.map (convertTemplateFingering ct)

let bitFlagFromTemplate (ct: ChordTemplate) =
    ct.Frets
    |> Array.mapi (fun i f -> if f >= 0y then getBitFlag (sbyte i) else 0uy)
    |> Array.reduce (|||)

let createNoteGroups (inst: InstrumentalArrangement) =
    inst.Levels
    |> Seq.mapi (fun diff level ->
        let ghostChords =
            level.HandShapes
            |> Seq.filter (fun hs ->
                level.Notes.Exists(fun n -> n.Time = hs.StartTime) |> not
                && level.Chords.Exists(fun c -> c.Time = hs.StartTime) |> not)
            |> Seq.map (fun hs ->
                let ct = inst.ChordTemplates[int hs.ChordId]
                let fingering = fingeringFromTemplate ct

                let chordData =
                    { Template = ct
                      ChordId = hs.ChordId
                      HandshapeId = -1
                      IsFullPanel = false
                      IsFirstInHandShape = true
                      IsLinkNext = false
                      Fingering = fingering }

                { Chord = Some chordData
                  Time = uint hs.StartTime
                  Difficulty = byte diff
                  Notes = Array.empty })
            |> Seq.toArray

        let notes =
            level.Notes.ToArray()
            |> Array.groupBy (fun n -> n.Time)
            |> Array.map (fun (time, group) ->
                { Chord = None
                  // Ensure that notes are sorted from lowest string to highest
                  Notes = group |> Array.sortBy (fun n -> n.String)
                  Time = uint time
                  Difficulty = byte diff })

        let chords =
            level.Chords.ToArray()
            |> Array.map (fun c ->
                let template = inst.ChordTemplates[int c.ChordId]
                let notes =
                    if c.HasChordNotes then
                        c.ChordNotes.ToArray() |> Array.sortBy (fun n -> n.String)
                    else
                        notesFromTemplate c template

                let chordData =
                    let fingering =
                        notes
                        |> Array.map (fun n -> convertTemplateFingering template (int n.String))

                    let handshapeId =
                        level.HandShapes.FindIndex(fun hs -> c.Time >= hs.StartTime && c.Time < hs.EndTime)
                    let handshapeStartTime =
                        if handshapeId < 0 then -1 else level.HandShapes[handshapeId].StartTime

                    { Template = template
                      ChordId = c.ChordId
                      HandshapeId = handshapeId
                      IsFullPanel = c.HasChordNotes && not c.IsHighDensity
                      IsFirstInHandShape = c.Time = handshapeStartTime
                      IsLinkNext = c.IsLinkNext
                      Fingering = fingering }

                { Chord = Some chordData
                  Notes = notes
                  Difficulty = byte diff
                  Time = uint c.Time })

        Seq.concat [ chords; notes; ghostChords ]
    )
    |> Seq.concat
    |> Seq.sortBy (fun x -> x.Time, x.Difficulty)
    |> Seq.toArray

let convertBendValue (step: float32) =
    let isQuarter = ceil step <> step
    if isQuarter then
        byte (step * 2.f) ||| 128uy
    else
        byte step

let convertNotes (inst: InstrumentalArrangement) =
    let noteGroups = createNoteGroups inst

    noteGroups
    |> Array.Parallel.mapi (fun index { Chord = chordOpt; Notes = notes; Time = time; Difficulty = diff } ->
        if notes.Length = 0 then
            // Create ghost chord
            let chord = chordOpt |> Option.get
            let bitFlag = bitFlagFromTemplate chord.Template
            let frets = fretsFromTemplate notes chord.Template

            let eofNote =
                { EOFNote.Empty with
                    Difficulty = diff
                    ChordName =  chord.Template.Name
                    BitFlag = bitFlag
                    GhostBitFlag = bitFlag
                    Frets = frets
                    Position = time }

            eofNote, chord.Fingering, Array.empty
        else
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
                inst.Levels[int diff].HandShapes.FindByTime(int time)
                |> Option.ofObj
                |> Option.map (fun hs -> inst.ChordTemplates.[int hs.ChordId])

            let bitFlagFromHandshape =
                handshapeTemplate
                |> Option.map bitFlagFromTemplate

            let bitFlags =
                notes
                |> Array.map (fun n -> getBitFlag (sbyte n.String))
            let trueBitFlag = bitFlags |> Array.reduce (|||)
            let commonBitFlag =
                bitFlagFromHandshape
                |> Option.defaultValue trueBitFlag

            let ghostBitFlag =
                bitFlagFromHandshape
                |> Option.map (fun tbf -> trueBitFlag ^^^ tbf)
                |> Option.defaultValue 0uy

            let extendedNoteFlags = notes |> Array.map (getExtendedNoteFlags chordOpt)
            let commonExtendedNoteFlags = extendedNoteFlags |> Array.reduce (&&&)

            let splitFlag =
                if chordOpt.IsNone && notes.Length > 1 then
                    EOFNoteFlag.SPLIT
                else
                    EOFNoteFlag.ZERO

            let slideCheck slideTo fret distance =
                slideTo > 0y && fret - slideTo = distance

            let slide =
                let slideTo = notes[0].SlideTo
                let distance = notes[0].Fret - slideTo
                if slideTo > 0y && notes |> Array.forall (fun n -> slideCheck n.SlideTo n.Fret distance) then
                    ValueSome (byte slideTo)
                else
                    ValueNone

            let unpitchedSlide =
                let uSlideTo = notes[0].SlideUnpitchTo
                let distance = notes[0].Fret - uSlideTo
                if uSlideTo > 0y && notes |> Array.forall (fun n -> slideCheck n.SlideUnpitchTo n.Fret distance) then
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
                |> Option.map (fretsFromTemplate notes)
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
                                Difficulty = diff
                                BitFlag = getBitFlag (sbyte n.String)
                                Position = uint (n.Time + n.Sustain)
                                Flags = EOFNoteFlag.EXTENDED_FLAGS
                                ExtendedNoteFlags = EOFExtendedNoteFlag.STOP
                                ActualNotePosition = time
                                EndPosition = time + uint maxSus
                            }
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
                            Difficulty = diff
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
                            ActualNotePosition = time
                            EndPosition = time + uint maxSus
                        }
                        |> Some)

            let bendTechNotes =
                notes
                |> Array.collect (fun n ->
                    if not n.IsBend then
                        Array.empty
                    else
                        let endPosition = time + uint maxSus

                        n.BendValues.ToArray()
                        // Don't import 0 strength bends at note start time
                        |> Array.filter (fun bv -> not (bv.Time = n.Time && bv.Step = 0.0f))
                        |> Array.map (fun bv ->
                            // In some very old buggy CDLC there can be bend values in chords
                            // whose time stamp is way before the chord itself
                            let position = max (uint bv.Time) time

                            { EOFNote.Empty with
                                Difficulty = diff
                                BitFlag = getBitFlag (sbyte n.String)
                                Position = position
                                Flags = EOFNoteFlag.RS_NOTATION ||| EOFNoteFlag.BEND
                                BendStrength = ValueSome (convertBendValue bv.Step)
                                ActualNotePosition = time
                                EndPosition = endPosition
                            }
                        )
                )

            let chordName =
                chordOpt
                |> Option.map (fun x -> x.Template.Name)
                |> Option.defaultValue String.Empty

            let eofNote =
                { EOFNote.Empty with
                    Difficulty = diff
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
                |> Option.map fingeringFromTemplate
                |> Option.orElseWith (fun () ->
                    chordOpt
                    |> Option.map (fun x -> x.Fingering))
                // No fingering defined
                |> Option.defaultWith (fun () -> Array.replicate eofNote.Frets.Length 0uy)

            let techNotes = Array.concat [ techNotes; bendTechNotes; stopTechNotes ]

            assert (fingering.Length = eofNote.Frets.Length)

            eofNote, fingering, techNotes)
    |> Array.unzip3
