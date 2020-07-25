module Rocksmith2014.Conversion.XmlToSng

open System
open System.Globalization
open Rocksmith2014
open Rocksmith2014.Conversion.Utils
open Rocksmith2014.SNG

type HandShapeMap = Map<int16, Set<int>>

type NoteFlagger = ValueOption<Note> -> Note -> uint32

type XmlEntity =
    | XmlNote of XmlNote : XML.Note
    | XmlChord of XmlChord : XML.Chord

let getTimeCode = function
    | XmlNote xn -> xn.Time
    | XmlChord xc -> xc.Time

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
                BeatMask.FirstBeatOfMeasure
                ||| if (currentMeasure % 2s) = 0s then BeatMask.EvenMeasure else BeatMask.None
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
      Outer = { xMin = xmlGlyphDef.OuterXMin; xMax = xmlGlyphDef.OuterXMax; yMin = xmlGlyphDef.OuterYMin; yMax = xmlGlyphDef.OuterYMax }
      Inner = { xMin = xmlGlyphDef.InnerXMin; xMax = xmlGlyphDef.InnerXMax; yMin = xmlGlyphDef.InnerYMin; yMax = xmlGlyphDef.InnerYMax } }

/// Converts an XML Phrase into an SNG Phrase.
let convertPhrase (xml: XML.InstrumentalArrangement) phraseId (xmlPhrase: XML.Phrase) =
    let piLinks =
        xml.PhraseIterations
        |> Seq.filter (fun pi -> pi.PhraseId = phraseId)
        |> Seq.length

    { Solo = boolToByte xmlPhrase.IsSolo
      Disparity = boolToByte xmlPhrase.IsDisparity
      Ignore = boolToByte xmlPhrase.IsIgnore
      MaxDifficulty = int xmlPhrase.MaxDifficulty
      PhraseIterationLinks = piLinks
      Name = xmlPhrase.Name }

/// Converts an XML PhraseProperty into an SNG PhraseExtraInfo.
let convertPhraseExtraInfo (xml:XML.PhraseProperty) =
    { PhraseId = xml.PhraseId
      Difficulty = xml.Difficulty
      Empty = xml.Empty
      LevelJump = xml.LevelJump
      Redundant = xml.Redundant }

/// Converts an XML ChordTemplate into an SNG Chord.
let convertChord (xml:XML.InstrumentalArrangement) (xmlChord:XML.ChordTemplate) =
    let mask =
        if xmlChord.IsArpeggio then
            ChordMask.Arpeggio
        elif xmlChord.DisplayName.EndsWith("-nop") then
            ChordMask.Nop
        else
            ChordMask.None

    { Mask = mask
      Frets = Array.copy xmlChord.Frets
      Fingers = Array.copy xmlChord.Fingers
      Notes = Midi.mapToMidiNotes xml xmlChord.Frets
      Name = xmlChord.Name }

/// Converts an XML BendValue into an SNG BendValue.
let convertBendValue (xmlBv: XML.BendValue) =
    { Time = msToSec xmlBv.Time
      Step = xmlBv.Step }

/// Converts an XML PhraseIteration into an SNG PhraseIteration.
let convertPhraseIteration (xml: XML.InstrumentalArrangement) index (xmlPi: XML.PhraseIteration) =
    let endTime =
        if index = xml.PhraseIterations.Count - 1 then
            xml.SongLength
        else
            xml.PhraseIterations.[index + 1].Time

    { PhraseId = xmlPi.PhraseId
      StartTime = msToSec xmlPi.Time
      NextPhraseTime = msToSec endTime
      Difficulty = [| int xmlPi.HeroLevels.Easy; int xmlPi.HeroLevels.Medium; int xmlPi.HeroLevels.Hard |] }

/// Converts an XML NewLinkedDifficulty into an SNG NewLinkedDifficulty.
let convertNLD (xmlNLD: XML.NewLinkedDiff) =
    { LevelBreak = int xmlNLD.LevelBreak
      NLDPhrases = Array.ofSeq xmlNLD.PhraseIds }

/// Converts an XML Event into an SNG Event.
let convertEvent (xmlEvent: XML.Event) =
    { Time = msToSec xmlEvent.Time
      Name = xmlEvent.Code }

/// Converts an XML ToneChange into an SNG Tone.
let convertTone (xmlTone: XML.ToneChange) =
    { Time = msToSec xmlTone.Time
      ToneId = int xmlTone.Id }

/// Converts an XML Section into an SNG Section.
let convertSection (stringMasks: int8[][]) (xml: XML.InstrumentalArrangement) index (xmlSection: XML.Section) =
    let endTime =
        if index = xml.Sections.Count - 1 then
            xml.SongLength
        else
            xml.Sections.[index + 1].Time

    let startPi = findPhraseIterationId xmlSection.Time xml.PhraseIterations
    let endPi =
        let rec find index =
            if index >= xml.PhraseIterations.Count || xml.PhraseIterations.[index].Time >= endTime then
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
      StringMask = stringMasks.[index] }

/// Converts an XML Anchor into an SNG Anchor.
let convertAnchor (noteTimes: int array) (level: XML.Level) (xml: XML.InstrumentalArrangement) index (xmlAnchor: XML.Anchor) =
    // Uninitialized values found in anchors that have no notes
    let uninitFirstNote = 3.4028234663852886e+38f
    let uninitLastNote = 1.1754943508222875e-38f

    let endTime =
        if index = level.Anchors.Count - 1 then
            // Use time of last phrase iteration (should be END)
            xml.PhraseIterations.[xml.PhraseIterations.Count - 1].Time
        else
            level.Anchors.[index + 1].Time

    let notesInAnchor = Array.FindAll(noteTimes, (fun t -> t >= xmlAnchor.Time && t < endTime))

    let struct (firstNoteTime, lastNoteTime) =
        match notesInAnchor with
        | [||] -> struct (uninitFirstNote, uninitLastNote)
        | arr -> struct (msToSec (Array.head arr), msToSec (Array.last arr))

    { StartTime = msToSec xmlAnchor.Time
      EndTime = msToSec endTime
      FirstNoteTime = firstNoteTime
      LastNoteTime = lastNoteTime
      FretId = xmlAnchor.Fret
      Width = int xmlAnchor.Width
      PhraseIterationId = findPhraseIterationId xmlAnchor.Time xml.PhraseIterations }

/// Converts an XML HandShape into an SNG FingerPrint.
let convertHandshape (handShapeMap: HandShapeMap) (xmlHs: XML.HandShape) =
    let firstNoteTime, lastNoteTime =
        match handShapeMap |> Map.tryFind xmlHs.ChordId with
        | Some set when not set.IsEmpty -> msToSec set.MinimumElement, msToSec set.MaximumElement
        | _ -> -1.f, -1.f

    { ChordId = int xmlHs.ChordId
      StartTime = msToSec xmlHs.StartTime
      EndTime = msToSec xmlHs.EndTime
      FirstNoteTime = firstNoteTime
      LastNoteTime = lastNoteTime }

/// Creates a map of hand shapes that contains the times of all the notes inside the hand shape.
let createHandShapeMap (noteTimes: int array) (level: XML.Level) : HandShapeMap =
    let toSet (hs:XML.HandShape) =
        let times =
            Array.FindAll(noteTimes, (fun t -> t >= hs.StartTime && t < hs.EndTime))
            |> Set.ofArray
        hs.ChordId, times

    level.HandShapes
    |> Seq.map toSet
    |> Map.ofSeq

/// Creates a DNA from an XML event.
let private eventToDNA (event: XML.Event) =
    match event.Code with
    | "dna_none"  -> Some { DnaId = 0; Time = msToSec event.Time }
    | "dna_solo"  -> Some { DnaId = 1; Time = msToSec event.Time }
    | "dna_riff"  -> Some { DnaId = 2; Time = msToSec event.Time }
    | "dna_chord" -> Some { DnaId = 3; Time = msToSec event.Time }
    | _ -> None

/// Creates DNAs for the XML arrangement.
let createDNAs (xml: XML.InstrumentalArrangement) =
    xml.Events
    |> Seq.choose eventToDNA
    |> Seq.toArray

/// Creates an SNG MetaData from the XML arrangement.
let createMetaData (accuData:AccuData) (xml:XML.InstrumentalArrangement) =
    let firstNoteTime = msToSec accuData.FirstNoteTime
    let conversionDate = DateTime.Now.ToString("MM-d-yy HH:mm", CultureInfo.InvariantCulture)
    let maxScore = 100_000.

    { MaxScore = maxScore
      MaxNotesAndChords = float accuData.NoteCounts.Hard
      MaxNotesAndChordsReal = float (accuData.NoteCounts.Hard - accuData.NoteCounts.Ignored)
      PointsPerNote = maxScore / float accuData.NoteCounts.Hard
      FirstBeatLength = msToSec (xml.Ebeats.[1].Time - xml.Ebeats.[0].Time)
      StartTime = msToSec xml.StartBeat
      CapoFretId = if xml.Capo <= 0y then -1y else xml.Capo
      LastConversionDateTime = conversionDate
      Part = xml.Part
      SongLength = msToSec xml.SongLength
      Tuning = Array.copy xml.Tuning.Strings
      FirstNoteTime = firstNoteTime
      MaxDifficulty = xml.Levels.Count - 1 }
