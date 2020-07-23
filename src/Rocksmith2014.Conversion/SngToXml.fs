module Rocksmith2014.Conversion.SngToXml

open Rocksmith2014
open Rocksmith2014.SNG
open Rocksmith2014.Conversion.Utils
open Nessos.Streams

/// Converts an SNG Beat into an XML Ebeat.
let convertBeat (sngBeat:Beat) =
    let measure =
        if (sngBeat.Mask &&& BeatMask.FirstBeatOfMeasure) = BeatMask.None then
            -1s
        else
            sngBeat.Measure

    XML.Ebeat(secToMs sngBeat.Time, measure)

/// Converts an SNG Chord into an XML ChordTemplate.
let convertChordTemplate (sngChord:Chord) =
    let dispName =
        match sngChord.Mask with
        | ChordMask.Arpeggio -> sngChord.Name + "-arp"
        | ChordMask.Nop -> sngChord.Name + "-nop"
        | _ -> sngChord.Name

    XML.ChordTemplate(sngChord.Name, dispName, sngChord.Fingers, sngChord.Frets)

/// Converts an SNG Phrase into an XML Phrase.
let convertPhrase (sngPhrase:Phrase) =
    let mask =
        XML.PhraseMask.None
        ||| if sngPhrase.Disparity = 1y then XML.PhraseMask.Disparity else XML.PhraseMask.None
        ||| if sngPhrase.Solo = 1y then XML.PhraseMask.Solo else XML.PhraseMask.None
        ||| if sngPhrase.Ignore = 1y then XML.PhraseMask.Ignore else XML.PhraseMask.None

    XML.Phrase(sngPhrase.Name, byte sngPhrase.MaxDifficulty, mask)

/// Converts an SNG PhraseIteration into an XML PhraseIteration.
let convertPhraseIteration (sngPhraseIter:PhraseIteration) =
    XML.PhraseIteration(
        secToMs sngPhraseIter.StartTime,
        sngPhraseIter.PhraseId,
        sngPhraseIter.Difficulty)

/// Converts an SNG PhraseExtraInfo into an XML PhraseProperty.
let convertPhraseExtraInfo (sngInfo:PhraseExtraInfo) =
    XML.PhraseProperty(
        PhraseId = sngInfo.PhraseId,
        Difficulty = sngInfo.Difficulty,
        Empty = sngInfo.Empty,
        LevelJump = sngInfo.LevelJump,
        Redundant = sngInfo.Redundant)

/// Converts an SNG BendValue into an XML BendValue.
let convertBendValue (sngBend:BendValue) =
    XML.BendValue(secToMs sngBend.Time, sngBend.Step)

let convertVocal (sngVocal:Vocal) =
    let time = secToMs sngVocal.Time
    let length = secToMs sngVocal.Length
    XML.Vocal(time, length, sngVocal.Lyric, byte sngVocal.Note)

let convertSymbolDefinition (sngSymbol:SymbolDefinition) =
    XML.GlyphDefinition(
        Symbol = sngSymbol.Symbol,
        OuterYMin = sngSymbol.Outer.yMin,
        OuterYMax = sngSymbol.Outer.yMax,
        OuterXMin = sngSymbol.Outer.xMin,
        OuterXMax = sngSymbol.Outer.xMax,
        InnerYMin = sngSymbol.Inner.yMin,
        InnerYMax = sngSymbol.Inner.yMax,
        InnerXMin = sngSymbol.Inner.xMin,
        InnerXMax = sngSymbol.Inner.xMax)

let convertNLD (sngNld:NewLinkedDifficulty) =
    XML.NewLinkedDiff(sbyte sngNld.LevelBreak, sngNld.NLDPhrases)

let convertEvent (sngEvent:Event) =
    XML.Event(sngEvent.Name, secToMs sngEvent.Time)

let convertTone (sngTone:Tone) =
    XML.ToneChange("N/A", secToMs sngTone.Time, byte sngTone.ToneId)

let convertSection (sngSection:Section) =
    XML.Section(sngSection.Name, secToMs sngSection.StartTime, int16 sngSection.Number)

let convertAnchor (sngAnchor:Anchor) =
    XML.Anchor(sngAnchor.FretId, secToMs sngAnchor.StartTime, sbyte sngAnchor.Width)

let convertHandShape (fp:FingerPrint) =
    XML.HandShape(int16 fp.ChordId, secToMs fp.StartTime, secToMs fp.EndTime)

let convertNoteMask (sngMask:NoteMask) =
    // Optimization for notes without techniques
    if (sngMask &&& Masks.NoteTechniques) = NoteMask.None then
        XML.NoteMask.None
    else
        XML.NoteMask.None
        ||| if sngMask ?= NoteMask.Accent        then XML.NoteMask.Accent        else XML.NoteMask.None
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

let convertNote (sngNote:Note) =
    if sngNote.ChordId <> -1 then invalidOp "Cannot convert a chord into a note."
    
    let mask =
        convertNoteMask sngNote.Mask
        ||| if sngNote.PickDirection > 0y then XML.NoteMask.PickDirection else XML.NoteMask.None

    let bendValues =
        sngNote.BendData
        |> Stream.ofArray
        |> Stream.map convertBendValue 
        |> Stream.toResizeArray

    XML.Note(Mask = mask,
             Time = secToMs sngNote.Time,
             String = sngNote.StringIndex,
             Fret = sngNote.FretId,
             Sustain = secToMs sngNote.Sustain,
             Vibrato = byte sngNote.Vibrato,
             SlideTo = sngNote.SlideTo,
             SlideUnpitchTo = sngNote.SlideUnpitchTo,
             LeftHand = sngNote.LeftHand,
             BendValues = bendValues,
             // Default value used for tap in XML is 0, in SNG it is -1
             Tap = if sngNote.Tap < 0y then 0y else sngNote.Tap)

let convertChordMask (sngMask:NoteMask) =
    // Optimization for chords without techniques
    if (sngMask &&& Masks.ChordTechniques) = NoteMask.None then
        XML.ChordMask.None
    else
        XML.ChordMask.None
        ||| if sngMask ?= NoteMask.Accent       then XML.ChordMask.Accent       else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.FretHandMute then XML.ChordMask.FretHandMute else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.HighDensity  then XML.ChordMask.HighDensity  else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.Ignore       then XML.ChordMask.Ignore       else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.PalmMute     then XML.ChordMask.PalmMute     else XML.ChordMask.None
        ||| if sngMask ?= NoteMask.Parent       then XML.ChordMask.LinkNext     else XML.ChordMask.None    

let convertBendData32 (bd:BendData32) =
    bd.BendValues
    |> Stream.ofArray
    |> Stream.take bd.UsedCount
    |> Stream.map convertBendValue
    |> Stream.toResizeArray

let private convertChordNotes (sng:SNG) (chord:Note) =
    let template = sng.Chords.[chord.ChordId]
    let xmlNotes = ResizeArray()

    for i = 0 to 5 do
        if template.Frets.[i] <> -1y then
            let cn = XML.Note(
                        Time = secToMs chord.Time,
                        Fret = template.Frets.[i],
                        LeftHand = template.Fingers.[i],
                        String = sbyte i,
                        Sustain = secToMs chord.Sustain)

            match chord.ChordNotesId with
            | id when id = -1 || id >= sng.ChordNotes.Length -> ()
            | id ->
                let chordNotes = sng.ChordNotes.[id]

                cn.Mask <- convertNoteMask chordNotes.Mask.[i]
                cn.SlideTo <- chordNotes.SlideTo.[i]
                cn.SlideUnpitchTo <- chordNotes.SlideUnpitchTo.[i]
                cn.Vibrato <- byte chordNotes.Vibrato.[i]
                cn.BendValues <- convertBendData32 chordNotes.BendData.[i]

            xmlNotes.Add(cn)
            
    xmlNotes

let convertChord (sng:SNG) (sngNote:Note) =
    if sngNote.ChordId = -1 then invalidOp "Cannot convert a note into a chord."    
    
    XML.Chord(Mask = convertChordMask sngNote.Mask,
              Time = secToMs sngNote.Time,
              ChordId = int16 sngNote.ChordId,
              ChordNotes = if sngNote.Mask ?= NoteMask.Strum then convertChordNotes sng sngNote else null)

let convertLevel (sng:SNG) (sngLevel:Level) =
    let anchors = sngLevel.Anchors |> mapToResizeArray convertAnchor

    let handShapes =
        seq { Stream.ofArray sngLevel.HandShapes
              Stream.ofArray sngLevel.Arpeggios }
        |> Stream.concat
        |> Stream.map convertHandShape
        |> Stream.sortBy (fun hs -> hs.StartTime)
        |> Stream.toResizeArray

    let sngNotes, sngChords = Array.partition (fun (n : Note) -> n.ChordId = -1) sngLevel.Notes
    let notes = sngNotes |> mapToResizeArray convertNote
    let chords = sngChords |> mapToResizeArray (convertChord sng)

    XML.Level(sbyte sngLevel.Difficulty, notes, chords, anchors, handShapes)
