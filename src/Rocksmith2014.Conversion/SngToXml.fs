module Rocksmith2014.Conversion.SngToXml

open Rocksmith2014
open Rocksmith2014.SNG
open Rocksmith2014.Conversion.Utils
open Rocksmith2014.Common

/// Converts an SNG Beat into an XML Ebeat.
let convertBeat (sngBeat: Beat) =
    let measure =
        if (sngBeat.Mask &&& BeatMask.FirstBeatOfMeasure) = BeatMask.None then
            -1s
        else
            sngBeat.Measure

    XML.Ebeat(secToMs sngBeat.Time, measure)

/// Converts an SNG Chord into an XML ChordTemplate.
let convertChordTemplate (sngChord: Chord) =
    let dispName =
        match sngChord.Mask with
        | ChordMask.Arpeggio -> sngChord.Name + "-arp"
        | ChordMask.Nop -> sngChord.Name + "-nop"
        | _ -> sngChord.Name

    XML.ChordTemplate(sngChord.Name, dispName, sngChord.Fingers, sngChord.Frets)

/// Converts an SNG Phrase into an XML Phrase.
let convertPhrase (sngPhrase: Phrase) =
    let mask =
        if sngPhrase.Disparity = 1y  then XML.PhraseMask.Disparity else XML.PhraseMask.None
        ||| if sngPhrase.Solo = 1y   then XML.PhraseMask.Solo      else XML.PhraseMask.None
        ||| if sngPhrase.Ignore = 1y then XML.PhraseMask.Ignore    else XML.PhraseMask.None

    XML.Phrase(sngPhrase.Name, byte sngPhrase.MaxDifficulty, mask)

/// Converts an SNG PhraseIteration into an XML PhraseIteration.
let convertPhraseIteration (sngPhraseIter: PhraseIteration) =
    XML.PhraseIteration(
        secToMs sngPhraseIter.StartTime,
        sngPhraseIter.PhraseId,
        sngPhraseIter.Difficulty
    )

/// Converts an SNG PhraseExtraInfo into an XML PhraseProperty.
let convertPhraseExtraInfo (sngInfo: PhraseExtraInfo) =
    XML.PhraseProperty(
        PhraseId = sngInfo.PhraseId,
        Difficulty = sngInfo.Difficulty,
        Empty = sngInfo.Empty,
        LevelJump = sngInfo.LevelJump,
        Redundant = sngInfo.Redundant
    )

/// Converts an SNG BendValue into an XML BendValue.
let convertBendValue (sngBend: BendValue) =
    XML.BendValue(secToMs sngBend.Time, sngBend.Step)

/// Converts an SNG Vocal into an XML Vocal.
let convertVocal (sngVocal: Vocal) =
    let time = secToMs sngVocal.Time
    let length = secToMs sngVocal.Length
    XML.Vocal(time, length, sngVocal.Lyric, byte sngVocal.Note)

/// Converts an SNG SymbolDefinition into an XML GlyphDefinition.
let convertSymbolDefinition (sngSymbol: SymbolDefinition) =
    XML.GlyphDefinition(
        Symbol = sngSymbol.Symbol,
        OuterYMin = sngSymbol.Outer.YMin,
        OuterYMax = sngSymbol.Outer.YMax,
        OuterXMin = sngSymbol.Outer.XMin,
        OuterXMax = sngSymbol.Outer.XMax,
        InnerYMin = sngSymbol.Inner.YMin,
        InnerYMax = sngSymbol.Inner.YMax,
        InnerXMin = sngSymbol.Inner.XMin,
        InnerXMax = sngSymbol.Inner.XMax
    )

/// Converts an SNG NewLinkedDifficulty into an XML NewLinkedDifficulty.
let convertNLD (sngNld: NewLinkedDifficulty) =
    XML.NewLinkedDiff(sbyte sngNld.LevelBreak, sngNld.NLDPhrases)

/// Converts an SNG Event into an XML Event.
let convertEvent (sngEvent: Event) =
    XML.Event(sngEvent.Name, secToMs sngEvent.Time)

/// Converts an SNG Tone into an XML ToneChange.
let convertTone (attributes: Manifest.Attributes option) (sngTone: Tone) =
    let name =
        match attributes, sngTone.ToneId with
        | Some attr, 0 -> attr.Tone_A
        | Some attr, 1 -> attr.Tone_B
        | Some attr, 2 -> attr.Tone_C
        | Some attr, 3 -> attr.Tone_D
        | _, _ -> "N/A"

    XML.ToneChange(name, secToMs sngTone.Time, byte sngTone.ToneId)

/// Converts an SNG Section into an XML Section.
let convertSection (sngSection: Section) =
    XML.Section(sngSection.Name, secToMs sngSection.StartTime, int16 sngSection.Number)

/// Converts an SNG Anchor into an XML Anchor.
let convertAnchor (sngAnchor: Anchor) =
    XML.Anchor(sngAnchor.FretId, secToMs sngAnchor.StartTime, sbyte sngAnchor.Width)

/// Converts an SNG FingerPrint into an XML HandShape.
let convertHandShape (fp: FingerPrint) =
    XML.HandShape(int16 fp.ChordId, secToMs fp.StartTime, secToMs fp.EndTime)

/// Converts an SNG NoteMask into an XML NoteMask.
let convertNoteMask (sngMask: NoteMask) =
    // Optimization for notes without techniques
    if (sngMask &&& Masks.NoteTechniques) = NoteMask.None then
        XML.NoteMask.None
    else
        if sngMask ?= NoteMask.Accent            then XML.NoteMask.Accent        else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.HammerOn      then XML.NoteMask.HammerOn      else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.Harmonic      then XML.NoteMask.Harmonic      else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.Ignore        then XML.NoteMask.Ignore        else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.Mute          then XML.NoteMask.FretHandMute  else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.PalmMute      then XML.NoteMask.PalmMute      else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.Parent        then XML.NoteMask.LinkNext      else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.PinchHarmonic then XML.NoteMask.PinchHarmonic else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.Pluck         then XML.NoteMask.Pluck         else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.PullOff       then XML.NoteMask.PullOff       else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.RightHand     then XML.NoteMask.RightHand     else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.Slap          then XML.NoteMask.Slap          else XML.NoteMask.None
        ||| if sngMask ?= NoteMask.Tremolo       then XML.NoteMask.Tremolo       else XML.NoteMask.None

/// Converts an SNG Note into an XML Note.
let convertNote (sngNote: Note) =
    if sngNote.ChordId <> -1 then invalidOp "Cannot convert a chord into a note."
    
    let mask =
        convertNoteMask sngNote.Mask
        ||| if sngNote.PickDirection > 0y then XML.NoteMask.PickDirection else XML.NoteMask.None

    let bendValues =
        sngNote.BendData
        |> mapToResizeArray convertBendValue

    XML.Note(
        Mask = mask,
        Time = secToMs sngNote.Time,
        String = sngNote.StringIndex,
        Fret = sngNote.Fret,
        Sustain = secToMs sngNote.Sustain,
        Vibrato = byte sngNote.Vibrato,
        SlideTo = sngNote.SlideTo,
        SlideUnpitchTo = sngNote.SlideUnpitchTo,
        LeftHand = sngNote.LeftHand,
        MaxBend = sngNote.MaxBend,
        BendValues = bendValues,
        // Default value used for tap in XML is 0, in SNG it is -1
        Tap = max 0y sngNote.Tap
    )

/// Converts an SNG NoteMask into an XML ChordMask.
let convertChordMask (sngMask: NoteMask) =
    // Optimization for chords without techniques
    if (sngMask &&& Masks.ChordTechniques) = NoteMask.None then
        XML.ChordMask.None
    else
        if sngMask ?= NoteMask.Accent           then XML.ChordMask.Accent       else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.FretHandMute then XML.ChordMask.FretHandMute else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.HighDensity  then XML.ChordMask.HighDensity  else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.Ignore       then XML.ChordMask.Ignore       else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.PalmMute     then XML.ChordMask.PalmMute     else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.Parent       then XML.ChordMask.LinkNext     else XML.ChordMask.None

/// Converts an SNG BendData32 into a list of XML BendValues.
let convertBendData32 (bd: BendData32) =
    if bd.UsedCount = 0 then
        null
    else
        bd.BendValues
        |> Array.take bd.UsedCount
        |> mapToResizeArray convertBendValue

/// Creates a list of XML chord notes for an SNG Note.
let private createChordNotes (sng: SNG) (chord: Note) =
    let template = sng.Chords[chord.ChordId]
    let xmlNotes = ResizeArray()

    let chordNotes =
        match chord.ChordNotesId with
        | id when id = -1 || id >= sng.ChordNotes.Length ->
            ValueNone
        | id ->
            ValueSome sng.ChordNotes[id]

    for i = 0 to 5 do
        if template.Frets[i] <> -1y then
            let cn =
                XML.Note(
                    Time = secToMs chord.Time,
                    Fret = template.Frets[i],
                    LeftHand = template.Fingers[i],
                    String = sbyte i,
                    Sustain = secToMs chord.Sustain
                )

            chordNotes
            |> ValueOption.iter (fun chordNotes ->
                cn.Mask <- convertNoteMask chordNotes.Mask[i]
                cn.SlideTo <- chordNotes.SlideTo[i]
                cn.SlideUnpitchTo <- chordNotes.SlideUnpitchTo[i]
                cn.Vibrato <- byte chordNotes.Vibrato[i]
                cn.BendValues <- convertBendData32 chordNotes.BendData[i])

            xmlNotes.Add(cn)
            
    xmlNotes

/// Converts an SNG Note into an XML Chord.
let convertChord (sng: SNG) (sngNote: Note) =
    if sngNote.ChordId = -1 then invalidOp "Cannot convert a note into a chord."
    
    XML.Chord(
        Mask = convertChordMask sngNote.Mask,
        Time = secToMs sngNote.Time,
        ChordId = int16 sngNote.ChordId,
        ChordNotes =
            if sngNote.Mask ?= NoteMask.ChordPanel then
                createChordNotes sng sngNote
            else
                null
    )

let private mapFingerPrints (handShapes: FingerPrint array) (arpeggios: FingerPrint array) =
    let result = ResizeArray<XML.HandShape>(handShapes.Length + arpeggios.Length)

    for i = 0 to handShapes.Length - 1 do
        result.Add(convertHandShape handShapes[i])
    for i = 0 to arpeggios.Length - 1 do
        result.Add(convertHandShape arpeggios[i])

    result.Sort(fun hs1 hs2 -> hs1.Time.CompareTo(hs2.Time))
    result

/// Converts an SNG Level into an XML Level.
let convertLevel (sng: SNG) (sngLevel: Level) =
    let anchors = sngLevel.Anchors |> mapToResizeArray convertAnchor

    let handShapes = mapFingerPrints sngLevel.HandShapes sngLevel.Arpeggios

    let sngNotes, sngChords = Array.partition isNote sngLevel.Notes
    let notes = sngNotes |> mapToResizeArray convertNote
    let chords = sngChords |> mapToResizeArray (convertChord sng)

    XML.Level(sbyte sngLevel.Difficulty, notes, chords, anchors, handShapes)
