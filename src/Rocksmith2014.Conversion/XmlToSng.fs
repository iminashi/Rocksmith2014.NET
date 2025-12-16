module Rocksmith2014.Conversion.XmlToSng

open System
open System.Globalization
open Rocksmith2014
open Rocksmith2014.Conversion.Utils
open Rocksmith2014.SNG
open Rocksmith2014.XML.Extension

type NoteFlagger = Note option -> Note -> uint32

/// Returns a function that keeps a track of the current measure and the current beat.
let convertBeat () =
    let mutable beatCounter = 0s
    let mutable currentMeasure = -1s

    fun (xml: XML.InstrumentalArrangement) (xmlBeat: XML.Ebeat) ->
        if xmlBeat.Measure >= 0s then
            beatCounter <- 0s
            currentMeasure <- xmlBeat.Measure
        else
            beatCounter <- beatCounter + 1s

        let mask =
            if beatCounter = 0s then
                if (currentMeasure % 2s) = 0s then
                    BeatMask.FirstBeatOfMeasure ||| BeatMask.EvenMeasure
                else
                    BeatMask.FirstBeatOfMeasure
            else
                BeatMask.None

        { Time = msToSec xmlBeat.Time
          Measure = currentMeasure
          Beat = beatCounter
          PhraseIteration = findBeatPhraseIterationId xmlBeat.Time xml.PhraseIterations
          Mask = mask }

/// Converts an XML Vocal into an SNG vocal.
let convertVocal (xmlVocal: XML.Vocal) =
    { Time = msToSec xmlVocal.Time
      Length = msToSec xmlVocal.Length
      Lyric = xmlVocal.Lyric
      Note = int xmlVocal.Note }

/// Converts an XML GlyphDefinition into an SNG SymbolDefinition.
let convertSymbolDefinition (xmlGlyphDef: XML.GlyphDefinition) =
    { Symbol = xmlGlyphDef.Symbol
      Outer =
          { XMin = xmlGlyphDef.OuterXMin
            XMax = xmlGlyphDef.OuterXMax
            YMin = xmlGlyphDef.OuterYMin
            YMax = xmlGlyphDef.OuterYMax }
      Inner =
          { XMin = xmlGlyphDef.InnerXMin
            XMax = xmlGlyphDef.InnerXMax
            YMin = xmlGlyphDef.InnerYMin
            YMax = xmlGlyphDef.InnerYMax } }

/// Converts an XML Phrase into an SNG Phrase.
let convertPhrase (xml: XML.InstrumentalArrangement) (phraseId: int) (xmlPhrase: XML.Phrase) =
    let piLinks =
        xml.PhraseIterations
        |> Seq.filter (fun pi -> pi.PhraseId = phraseId)
        |> Seq.length

    { Solo = Convert.ToSByte(xmlPhrase.IsSolo)
      Disparity = Convert.ToSByte(xmlPhrase.IsDisparity)
      Ignore = Convert.ToSByte(xmlPhrase.IsIgnore)
      MaxDifficulty = int xmlPhrase.MaxDifficulty
      IterationCount = piLinks
      Name = xmlPhrase.Name }

/// Converts an XML PhraseProperty into an SNG PhraseExtraInfo.
let convertPhraseExtraInfo (xml: XML.PhraseProperty) =
    { PhraseId = xml.PhraseId
      Difficulty = xml.Difficulty
      Empty = xml.Empty
      LevelJump = xml.LevelJump
      Redundant = xml.Redundant }

/// Converts an XML ChordTemplate into an SNG Chord.
let convertChord (xml: XML.InstrumentalArrangement) (xmlChord: XML.ChordTemplate) =
    let mask =
        if xmlChord.IsArpeggio then
            ChordMask.Arpeggio
        elif xmlChord.DisplayName.EndsWith("-nop", StringComparison.Ordinal) then
            ChordMask.Nop
        else
            ChordMask.None

    { Mask = mask
      Frets = Array.copy xmlChord.Frets
      Fingers = Array.copy xmlChord.Fingers
      Notes = Midi.mapToMidiNotes xml.MetaData xmlChord.Frets
      Name = xmlChord.Name }

/// Converts an XML BendValue into an SNG BendValue.
let convertBendValue (xmlBv: XML.BendValue) =
    { Time = msToSec xmlBv.Time
      Step = xmlBv.Step }

/// Converts an XML PhraseIteration into an SNG PhraseIteration.
let convertPhraseIteration (piTimes: int array) (index: int) (xmlPi: XML.PhraseIteration) =
    { PhraseId = xmlPi.PhraseId
      StartTime = msToSec xmlPi.Time
      EndTime = msToSec piTimes[index + 1]
      Difficulty =
        [| int xmlPi.HeroLevels.Easy
           int xmlPi.HeroLevels.Medium
           int xmlPi.HeroLevels.Hard |] }

/// Converts an XML NewLinkedDifficulty into an SNG NewLinkedDifficulty.
let convertNLD (xmlNLD: XML.NewLinkedDiff) =
    { LevelBreak = int xmlNLD.LevelBreak
      NLDPhrases = xmlNLD.PhraseIds.ToArray() }

/// Converts an XML Event into an SNG Event.
let convertEvent (xmlEvent: XML.Event) =
    { Time = msToSec xmlEvent.Time
      Name = xmlEvent.Code }

/// Converts an XML ToneChange into an SNG Tone.
let convertTone (xmlTone: XML.ToneChange) =
    { Time = msToSec xmlTone.Time
      ToneId = int xmlTone.Id }

/// Converts an XML Section into an SNG Section.
let convertSection (stringMasks: int8[][]) (xml: XML.InstrumentalArrangement) (index: int) (xmlSection: XML.Section) =
    let endTime =
        if index = xml.Sections.Count - 1 then
            xml.MetaData.SongLength
        else
            xml.Sections[index + 1].Time

    let startPi = findPhraseIterationId xmlSection.Time xml.PhraseIterations

    let endPi =
        let rec find index =
            if index >= xml.PhraseIterations.Count || xml.PhraseIterations[index].Time >= endTime then
                index - 1
            else
                find (index + 1)
        find (startPi + 1)

    { Name = xmlSection.Name
      Number = int xmlSection.Number
      StartTime = msToSec xmlSection.Time
      EndTime = msToSec endTime
      StartPhraseIterationId = startPi
      EndPhraseIterationId = endPi
      StringMask = stringMasks[index] }

// Uninitialized values found in anchors that have no notes
let [<Literal>] private UninitFirstNote = 3.4028234663852886e+38f
let [<Literal>] private UninitLastNote = 1.1754943508222875e-38f

/// Converts an XML Anchor into an SNG Anchor.
let convertAnchor (notes: Note array)
                  (noteTimes: int array)
                  (level: XML.Level)
                  (xml: XML.InstrumentalArrangement)
                  (index: int)
                  (xmlAnchor: XML.Anchor) =
    let startTime = msToSec xmlAnchor.Time
    let endTime =
        if index = level.Anchors.Count - 1 then
            // Use time of last phrase iteration (should be END)
            xml.PhraseIterations[xml.PhraseIterations.Count - 1].Time
        else
            level.Anchors[index + 1].Time

    (* It is likely impossible to get identical values when testing an official file SNG -> XML -> SNG
       Cases like these can be found:

       Note without any sustain: lastNoteTime = note.Time + 9ms
       Note with unpitched slide: lastNoteTime = note.Time + note.Sustain + 100ms *)

    let firstNoteTime, lastNoteTime =
        match findFirstAndLastTime noteTimes xmlAnchor.Time endTime with
        | ValueNone ->
            UninitFirstNote, UninitLastNote
        | ValueSome (firstIndex, lastIndex) ->
            let firstNote = notes[firstIndex]
            let lastNote = notes[lastIndex]
            let firstNoteTime =
                if firstIndex = 0 then
                    firstNote.Time
                else
                    let prevNote = notes[firstIndex - 1]
                    // If this anchor is at the end of a slide note, use the time where the target note would be
                    if prevNote.Mask ?= NoteMask.Slide
                       && prevNote.Time + prevNote.Sustain - startTime < 0.001f then
                        startTime
                    else
                        firstNote.Time

            let lastNoteTime =
                // The sustain of the last note is included, unless it is a slide
                if lastNote.Mask ?= NoteMask.Slide || lastNote.Mask ?= NoteMask.Parent then
                    lastNote.Time
                else
                    lastNote.Time + lastNote.Sustain

            firstNoteTime, lastNoteTime

    { StartTime = startTime
      EndTime = msToSec endTime
      FirstNoteTime = firstNoteTime
      LastNoteTime = lastNoteTime
      FretId = xmlAnchor.Fret
      Width = int xmlAnchor.Width
      PhraseIterationId = findPhraseIterationId xmlAnchor.Time xml.PhraseIterations }

/// Converts an XML HandShape into an SNG FingerPrint.
let convertHandshape (noteTimes: int array) (entities: XmlEntity array) (xmlHs: XML.HandShape) =
    let firstNoteTime, lastNoteTime =
        match findFirstAndLastTime noteTimes xmlHs.StartTime xmlHs.EndTime with
        | ValueNone ->
            -1.f, -1.f
        | ValueSome (first, last) ->
            let endTime =
                let t = noteTimes[last] + getSustain entities[last]

                if t >= xmlHs.EndTime then
                    -1.f
                else
                    msToSec t

            msToSec noteTimes[first], endTime

    { ChordId = int xmlHs.ChordId
      StartTime = msToSec xmlHs.StartTime
      EndTime = msToSec xmlHs.EndTime
      FirstNoteTime = firstNoteTime
      LastNoteTime = lastNoteTime }

/// Creates a DNA from an XML event.
let private eventToDNA (event: XML.Event) =
    match event.Code with
    | "dna_none"  -> Some DNA.None
    | "dna_solo"  -> Some DNA.Solo
    | "dna_riff"  -> Some DNA.Riff
    | "dna_chord" -> Some DNA.Chord
    | _ -> None
    |> Option.map (fun id -> { DnaId = id; Time = msToSec event.Time })

/// Creates DNAs for the XML arrangement.
let createDNAs (xml: XML.InstrumentalArrangement) =
    xml.Events
    |> Seq.choose eventToDNA
    |> Seq.toArray

/// Creates an SNG MetaData from the XML arrangement.
let createMetaData (accuData: AccuData) (firstNoteTime: float32) (xml: XML.InstrumentalArrangement) =
    let conversionDate = DateTime.Now.ToString("MM-d-yy HH:mm", CultureInfo.InvariantCulture)
    let maxScore = 100_000.

    { MaxScore = maxScore
      MaxNotesAndChords = float accuData.NoteCounts.Hard
      MaxNotesAndChordsReal = float (accuData.NoteCounts.Hard - accuData.NoteCounts.Ignored)
      PointsPerNote = maxScore / Math.Max(1.0, float accuData.NoteCounts.Hard)
      FirstBeatLength = msToSec (xml.Ebeats[1].Time - xml.Ebeats[0].Time)
      StartTime = msToSec xml.StartBeat
      CapoFretId = if xml.MetaData.Capo <= 0y then -1y else xml.MetaData.Capo
      LastConversionDateTime = conversionDate
      Part = xml.MetaData.Part
      SongLength = msToSec xml.MetaData.SongLength
      Tuning = Array.copy xml.MetaData.Tuning.Strings
      FirstNoteTime = firstNoteTime
      MaxDifficulty = xml.Levels.Count - 1 }
