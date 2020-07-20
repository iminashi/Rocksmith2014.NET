module Rocksmith2014.Conversion.XmlToSng

open System
open Rocksmith2014
open Rocksmith2014.SNG.Types
open System.Collections.Generic

let msToSec (time:int) = float32 time / 1000.0f

let convertVocal (xmlVocal:XML.Vocal) =
    { Time = msToSec xmlVocal.Time
      Length = msToSec xmlVocal.Length
      Lyric = xmlVocal.Lyric
      Note = int xmlVocal.Note }

let convertPhrase phraseId (xml:XML.InstrumentalArrangement) (xmlPhrase:XML.Phrase) =
    let piLinks =
        xml.PhraseIterations
        |> Seq.filter (fun pi -> pi.PhraseId = phraseId)
        |> Seq.length

    { Solo = if (xmlPhrase.Mask &&& XML.PhraseMask.Solo) <> XML.PhraseMask.None then 1y else 0y
      Disparity = if (xmlPhrase.Mask &&& XML.PhraseMask.Disparity) <> XML.PhraseMask.None then 1y else 0y
      Ignore = if (xmlPhrase.Mask &&& XML.PhraseMask.Ignore) <> XML.PhraseMask.None then 1y else 0y
      MaxDifficulty = int xmlPhrase.MaxDifficulty
      PhraseIterationLinks = piLinks
      Name = xmlPhrase.Name }

let convertChord (xmlChord:XML.ChordTemplate) =
    let mask =
        if xmlChord.DisplayName.EndsWith("-arp") then
            ChordMask.Arpeggio
        elif xmlChord.DisplayName.EndsWith("-nop") then
            ChordMask.Nop
        else
            ChordMask.None

    { Mask = mask
      Frets = Array.copy xmlChord.Frets
      Fingers = Array.copy xmlChord.Fingers
      Notes = Array.zeroCreate 6 // TODO: Implement
      Name = xmlChord.Name }

let convertBendValue (xmlBv:XML.BendValue) =
    { Time = msToSec xmlBv.Time
      Step = xmlBv.Step
      Unk3 = 0s
      Unk4 = 0y
      Unk5 = 0y }

let convertPhraseIteration index (xml:XML.InstrumentalArrangement) (xmlPi:XML.PhraseIteration) =
    let endTime =
        if index = xml.PhraseIterations.Count - 1 then
            xml.SongLength
        else
            xml.PhraseIterations.[index + 1].Time

    { PhraseId = xmlPi.PhraseId
      StartTime = msToSec xmlPi.Time
      NextPhraseTime = msToSec endTime
      Difficulty = [| int xmlPi.HeroLevels.Easy; int xmlPi.HeroLevels.Medium; int xmlPi.HeroLevels.Hard |] }

let convertNLD (xmlNLD:XML.NewLinkedDiff) =
    { LevelBreak = int xmlNLD.LevelBreak
      NLDPhrases = Array.ofSeq xmlNLD.PhraseIds }

let convertEvent (xmlEvent:XML.Event) =
    { Time = msToSec xmlEvent.Time
      Name = xmlEvent.Code }

let convertTone (xmlTone:XML.ToneChange) =
    { Time = msToSec xmlTone.Time
      ToneId = int xmlTone.Id }

let convertSection index (xml:XML.InstrumentalArrangement) (xmlSection:XML.Section) =
    let endTime =
        if index = xml.Sections.Count - 1 then
            xml.SongLength
        else
            xml.Sections.[index + 1].Time

    let startPi =
        xml.PhraseIterations
        |> Seq.tryFindIndex (fun pi -> pi.Time >= xmlSection.Time)
        |> Option.defaultValue 0

    let endPi =
        xml.PhraseIterations
        |> Seq.tryFindIndexBack (fun pi -> pi.Time < endTime)
        |> Option.defaultValue 0

    { Name = xmlSection.Name
      Number = int xmlSection.Number
      StartTime = msToSec xmlSection.Time
      EndTime = msToSec endTime
      StartPhraseIterationId = startPi
      EndPhraseIterationId = endPi
      StringMask = Array.zeroCreate 36 } // TODO: Implement

let convertAnchor index lvl (xml:XML.InstrumentalArrangement) (xmlAnchor:XML.Anchor) =
    let uninitFirstNote = 3.4028234663852886e+38f
    let uninitLastNote = 1.1754943508222875e-38f
    
    let endTime =
        if index = xml.Levels.[lvl].Anchors.Count - 1 then
            // Use time of last phrase iteration (should be END)
            xml.PhraseIterations.[xml.PhraseIterations.Count - 1].Time
        else
            xml.Levels.[lvl].Anchors.[index + 1].Time

    let piIndex =
        xml.PhraseIterations
        |> Seq.tryFindIndexBack (fun pi -> xmlAnchor.Time >= pi.Time)
        |> Option.defaultValue 0

    { StartTime = msToSec xmlAnchor.Time
      EndTime = msToSec endTime
      FirstNoteTime = uninitFirstNote // TODO: Implement
      LastNoteTime = uninitLastNote // TODO: Implement
      FretId = xmlAnchor.Fret
      Width = int xmlAnchor.Width
      PhraseIterationId = piIndex }

let convertHandshape (xmlHs:XML.HandShape) =
    { ChordId = int xmlHs.ChordId
      StartTime = msToSec xmlHs.StartTime
      EndTime = msToSec xmlHs.EndTime
      FirstNoteTime = -1.f // TODO: Implement
      LastNoteTime = -1.f } // TODO: Implement

let divideNoteTimesPerPhraseIteration (level:XML.Level) (arr:XML.InstrumentalArrangement) =
    let noteTimes =
        let chords =
            level.Chords
            |> Seq.map (fun c -> c.Time)
        level.Notes
        |> Seq.map (fun n -> n.Time)
        |> Seq.append chords
        |> Seq.sort
        |> Seq.toArray

    arr.PhraseIterations
    |> Seq.mapi (fun i pi ->
        let endTime = if i = arr.PhraseIterations.Count - 1 then arr.SongLength else arr.PhraseIterations.[i + 1].Time
        noteTimes
        |> Seq.skipWhile (fun t -> t < pi.Time)
        |> Seq.takeWhile (fun t -> t >= pi.Time && t < endTime)
        |> Seq.toArray)
    |> Seq.toArray

/// Creates an SNG note mask for a single note.
let createMaskForNote (note:XML.Note) =
    // TODO: implement
    NoteMask.Single
    ||| if note.IsLinkNext then NoteMask.Parent else NoteMask.None
    ||| if note.Fret = 0y then NoteMask.Open else NoteMask.None
    ||| if note.IsAccent then NoteMask.Accent else NoteMask.None

/// Finds the index of the phrase iteration that contains the given time code.
let findPhraseIterationId (time:int) (iterations:ResizeArray<XML.PhraseIteration>) =
    let rec find index =
        if index < 0 then
            -1
        elif iterations.[index].Time <= time then
            index
        else
            find (index - 1)
    find (iterations.Count - 1)
        
/// Returns a function valid for converting notes in a single difficulty level.
let convertNote () =
    // Dictionary of link-next parent notes in need of a child note.
    // Mapping: string number -> index of note in phrase iteration
    let pendingLinkNexts = Dictionary<int8, int16>()

    fun (noteTimes:int[][]) (level:XML.Level) (xml:XML.InstrumentalArrangement) (xmlNote:XML.Note) ->
        let parentNote =
            let mutable id = -1s
            if pendingLinkNexts.Remove(xmlNote.String, &id) then id else -1s

        let bendValues =
            if xmlNote.BendValues |> isNull |> not then
                xmlNote.BendValues
                |> Seq.map convertBendValue
                |> Seq.toArray
            else
                [||]

        let anchor = 
            level.Anchors
            |> Seq.findBack (fun a -> a.Time <= xmlNote.Time)

        let phraseIterationId = xml.PhraseIterations |> findPhraseIterationId xmlNote.Time
        let phraseId = xml.PhraseIterations.[phraseIterationId].PhraseId

        let this = int16 (Array.BinarySearch(noteTimes.[phraseIterationId], xmlNote.Time))
        let prev = this - 1s
        let next = if this = int16 noteTimes.[phraseIterationId].Length - 1s then -1s else this + 1s

        if xmlNote.IsLinkNext then pendingLinkNexts.Add(xmlNote.String, this)

        let mask =
            createMaskForNote xmlNote
            ||| if parentNote <> -1s then NoteMask.Child else NoteMask.None

        { Mask = mask
          Flags = 0u // TODO: implement
          Hash = 0u // TODO: implement
          Time = msToSec xmlNote.Time
          StringIndex = xmlNote.String
          FretId = xmlNote.Fret
          AnchorFretId = anchor.Fret
          AnchorWidth = anchor.Width
          ChordId = -1
          ChordNotesId = -1
          PhraseId = phraseId
          PhraseIterationId = phraseIterationId
          FingerPrintId = [| 99s; 99s|] // TODO: implement
          NextIterNote = next
          PrevIterNote = prev
          ParentPrevNote = parentNote
          SlideTo = xmlNote.SlideTo
          SlideUnpitchTo = xmlNote.SlideUnpitchTo
          LeftHand = xmlNote.LeftHand
          Tap = xmlNote.Tap
          PickDirection = if (xmlNote.Mask &&& XML.NoteMask.PickDirection) <> XML.NoteMask.None then 1y else 0y
          Slap = if xmlNote.IsSlap then 1y else -1y
          Pluck = if xmlNote.IsPluck then 1y else -1y
          Vibrato = int16 xmlNote.Vibrato
          Sustain = msToSec xmlNote.Sustain
          MaxBend = xmlNote.MaxBend
          BendData = bendValues }

let private eventToDNA (event:XML.Event) =
    match event.Code with
    | "dna_none" -> Some({ DnaId = 0; Time = msToSec event.Time })
    | "dna_solo" -> Some({ DnaId = 1; Time = msToSec event.Time })
    | "dna_riff" -> Some({ DnaId = 2; Time = msToSec event.Time })
    | "dna_chord" -> Some({ DnaId = 3; Time = msToSec event.Time })
    | _ -> None

let createDNAs (xml:XML.InstrumentalArrangement) =
    xml.Events
    |> Seq.choose eventToDNA
    |> Seq.toArray

let convertMetaData (xml:XML.InstrumentalArrangement) =
    { MaxScore = 10_000.
      MaxNotesAndChords = 0. // TODO: Implement
      MaxNotesAndChordsReal = 0. // TODO: Implement
      PointsPerNote = 0. // TODO: Implement
      FirstBeatLength = 0.f // TODO: Implement
      StartTime = msToSec xml.StartBeat
      CapoFretId = if xml.Capo <= 0y then -1y else xml.Capo
      LastConversionDateTime = "N/A" // TODO: Implement
      Part = xml.Part
      SongLength = msToSec xml.SongLength
      Tuning = Array.copy xml.Tuning.Strings
      Unk11FirstNoteTime = 0.f // TODO: Implement
      Unk12FirstNoteTime = 0.f // TODO: Implement
      MaxDifficulty = xml.Levels.Count - 1 }