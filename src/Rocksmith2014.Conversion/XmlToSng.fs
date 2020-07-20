module Rocksmith2014.Conversion.XmlToSng

open System
open Rocksmith2014
open Rocksmith2014.Conversion.Utils
open Rocksmith2014.SNG.Types
open System.Collections.Generic

/// Represents data that is being accumulated when mapping XML notes/chords into SNG notes.
type AccuData =
    { StringMasks : int8[,]
      ChordNotes : ResizeArray<ChordNote>
      AnchorExtensions : ResizeArray<AnchorExtension>
      NotesInPhraseIterationsExclIgnore : uint16[]
      NotesInPhraseIterationsAll : uint16[] }

    with
        member this.AddNote(pi:int, ignored:bool) =
             this.NotesInPhraseIterationsAll.[pi] <- this.NotesInPhraseIterationsAll.[pi] + 1us

             if not ignored then
                this.NotesInPhraseIterationsExclIgnore.[pi] <- this.NotesInPhraseIterationsExclIgnore.[pi] + 1us

        static member Init(arr:XML.InstrumentalArrangement) =
              { StringMasks = Array2D.zeroCreate (arr.Sections.Count) 36
                ChordNotes = ResizeArray()
                AnchorExtensions = ResizeArray()
                NotesInPhraseIterationsExclIgnore = Array.zeroCreate (arr.PhraseIterations.Count)
                NotesInPhraseIterationsAll = Array.zeroCreate (arr.PhraseIterations.Count) }

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

/// Returns a function that keeps a track of the current measure and the current beat.
let convertBeat () =
    let mutable beatCounter = 0s
    let mutable currentMeasure = -1s

    fun (xml:XML.InstrumentalArrangement) (xmlBeat:XML.Ebeat) ->
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
          PhraseIteration = findPhraseIterationId xmlBeat.Time xml.PhraseIterations
          Mask = mask }

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
      StringMask = Array.zeroCreate 36 } // TODO: Implement

let convertAnchor index (level:XML.Level) (xml:XML.InstrumentalArrangement) (xmlAnchor:XML.Anchor) =
    let uninitFirstNote = 3.4028234663852886e+38f
    let uninitLastNote = 1.1754943508222875e-38f
    
    let endTime =
        if index = level.Anchors.Count - 1 then
            // Use time of last phrase iteration (should be END)
            xml.PhraseIterations.[xml.PhraseIterations.Count - 1].Time
        else
            level.Anchors.[index + 1].Time

    { StartTime = msToSec xmlAnchor.Time
      EndTime = msToSec endTime
      FirstNoteTime = uninitFirstNote // TODO: Implement
      LastNoteTime = uninitLastNote // TODO: Implement
      FretId = xmlAnchor.Fret
      Width = int xmlAnchor.Width
      PhraseIterationId = findPhraseIterationId xmlAnchor.Time xml.PhraseIterations }

let convertHandshape (xmlHs:XML.HandShape) =
    { ChordId = int xmlHs.ChordId
      StartTime = msToSec xmlHs.StartTime
      EndTime = msToSec xmlHs.EndTime
      FirstNoteTime = -1.f // TODO: Implement
      LastNoteTime = -1.f } // TODO: Implement

let createNoteTimes (level:XML.Level) =
    let chords =
        level.Chords
        |> Seq.map (fun c -> c.Time)
    level.Notes
    |> Seq.map (fun n -> n.Time)
    |> Seq.append chords
    |> Seq.sort
    |> Seq.toArray

let divideNoteTimesPerPhraseIteration (noteTimes:int[]) (arr:XML.InstrumentalArrangement) =
    arr.PhraseIterations
    |> Seq.mapi (fun i pi ->
        let endTime = if i = arr.PhraseIterations.Count - 1 then arr.SongLength else arr.PhraseIterations.[i + 1].Time
        noteTimes
        |> Seq.skipWhile (fun t -> t < pi.Time)
        |> Seq.takeWhile (fun t -> t >= pi.Time && t < endTime)
        |> Seq.toArray)
    |> Seq.toArray

let createFingerprintMap (noteTimes:int[]) (level:XML.Level) =
    let toSet (hs:XML.HandShape) =
        let times =
            noteTimes
            |> Seq.skipWhile (fun t -> t < hs.StartTime)
            |> Seq.takeWhile (fun t -> t >= hs.StartTime && t < hs.EndTime)
            |> Set.ofSeq
        hs.ChordId, times
    
    level.HandShapes
    |> Seq.map toSet
    |> Map.ofSeq

/// Creates an SNG note mask for a single note.
let createMaskForNote (note:XML.Note) =
    // TODO: Is the left hand bit ever used?

    // Apply flags from properties not in the XML note mask
    let baseMask =
        NoteMask.Single
        ||| if note.Fret = 0y        then NoteMask.Open           else NoteMask.None
        ||| if note.Sustain > 0      then NoteMask.Sustain        else NoteMask.None
        ||| if note.IsSlide          then NoteMask.Slide          else NoteMask.None
        ||| if note.IsUnpitchedSlide then NoteMask.UnpitchedSlide else NoteMask.None
        ||| if note.IsTap            then NoteMask.Tap            else NoteMask.None
        ||| if note.IsVibrato        then NoteMask.Vibrato        else NoteMask.None
        ||| if note.IsBend           then NoteMask.Bend           else NoteMask.None

    // Apply flags from the XML note mask if needed
    if note.Mask = XML.NoteMask.None then
        baseMask
    else
        baseMask
        ||| if note.IsLinkNext      then NoteMask.Parent        else NoteMask.None
        ||| if note.IsAccent        then NoteMask.Accent        else NoteMask.None
        ||| if note.IsTremolo       then NoteMask.Tremolo       else NoteMask.None
        ||| if note.IsFretHandMute  then NoteMask.Mute          else NoteMask.None
        ||| if note.IsHammerOn      then NoteMask.HammerOn      else NoteMask.None
        ||| if note.IsHarmonic      then NoteMask.Harmonic      else NoteMask.None
        ||| if note.IsIgnore        then NoteMask.Ignore        else NoteMask.None
        ||| if note.IsPalmMute      then NoteMask.PalmMute      else NoteMask.None
        ||| if note.IsPinchHarmonic then NoteMask.PinchHarmonic else NoteMask.None
        ||| if note.IsPluck         then NoteMask.Pluck         else NoteMask.None
        ||| if note.IsPullOff       then NoteMask.PullOff       else NoteMask.None
        ||| if note.IsRightHand     then NoteMask.RightHand     else NoteMask.None
        ||| if note.IsSlap          then NoteMask.Slap          else NoteMask.None
    
let isDoubleStop (template:XML.ChordTemplate) =
    let frets =
        template.Frets
        |> Seq.filter (fun f -> f <> -1y)
        |> Seq.length
    frets = 2

let isStrum (chord:XML.Chord) =
    match chord.ChordNotes with
    | null -> false
    | cn when cn.Count = 0 -> false
    | _ -> true

/// Creates an SNG note mask for a chord.
let createMaskForChord (template:XML.ChordTemplate) (chord:XML.Chord) =
    // Apply flags from properties not in the XML chord mask
    let baseMask =
        NoteMask.Chord
        ||| if isDoubleStop template then NoteMask.DoubleStop else NoteMask.None
        ||| if isStrum chord         then NoteMask.Strum      else NoteMask.None
        ||| if template.IsArpeggio   then NoteMask.Arpeggio   else NoteMask.None
        // TODO: ChordNotes, Sustain

    // Apply flags from the XML chord mask if needed
    if chord.Mask = XML.ChordMask.None then
        baseMask
    else
        baseMask
        ||| if chord.IsAccent       then NoteMask.Accent       else NoteMask.None
        ||| if chord.IsFretHandMute then NoteMask.FretHandMute else NoteMask.None
        ||| if chord.IsHighDensity  then NoteMask.HighDensity  else NoteMask.None
        ||| if chord.IsIgnore       then NoteMask.Ignore       else NoteMask.None
        ||| if chord.IsLinkNext     then NoteMask.Parent       else NoteMask.None
        ||| if chord.IsPalmMute     then NoteMask.PalmMute     else NoteMask.None

let createFlag (lastNote:ValueOption<Note>) anchorFret noteFret =
    match lastNote with
    | ValueNone ->
        if noteFret <> 0y then 1u else 0u
    | ValueSome note ->
        if note.AnchorFretId <> anchorFret && noteFret <> 0y then 1u else 0u

let hashNote note = hash note |> uint32

type XmlEntity =
    | XmlNote of XML.Note
    | XmlChord of XML.Chord

let getTimeCode = function
    | XmlNote xn -> xn.Time
    | XmlChord xc -> xc.Time

/// Returns a function that is valid for converting notes in a single difficulty level.
let convertNote () =
    // Dictionary of link-next parent notes in need of a child note.
    // Mapping: string number => index of note in phrase iteration
    let pendingLinkNexts = Dictionary<int8, int16>()
    let mutable lastNote : ValueOption<Note> = ValueNone

    fun (noteTimes:int[][])
        (fingerPrintMap:Map<int16,Set<int>>)
        (accuData:AccuData)
        (xml:XML.InstrumentalArrangement)
        (difficulty:int)
        (xmlEnt:XmlEntity) ->

        let level = xml.Levels.[difficulty]
        let timeCode = getTimeCode xmlEnt

        let piId = xml.PhraseIterations |> findPhraseIterationId timeCode
        let phraseId = xml.PhraseIterations.[piId].PhraseId
        
        let this = int16 (Array.BinarySearch(noteTimes.[piId], timeCode))
        let prev = this - 1s
        let next = if this = int16 noteTimes.[piId].Length - 1s then -1s else this + 1s

        let anchor = 
            level.Anchors
            |> Seq.findBack (fun a -> a.Time <= timeCode)

        let fingerPrintId =
            let fpOption =
                fingerPrintMap
                |> Map.tryPick (fun key set -> if set.Contains(timeCode) then Some key else None)

            match fpOption with
            // Arpeggio
            | Some id when xml.ChordTemplates.[int id].IsArpeggio -> [| -1s; id |]
            // Normal handshape
            | Some id -> [| id; -1s |]
            | None -> [| -1s; -1s |]

        let data =
            match xmlEnt with
            | XmlNote note ->
                let parentNote =
                    let mutable id = -1s
                    if pendingLinkNexts.Remove(note.String, &id) then id else -1s

                let bendValues =
                    match note.BendValues with
                    | null -> [||]
                    | bendValues ->
                        bendValues
                        |> Seq.map convertBendValue
                        |> Seq.toArray

                if note.IsLinkNext then pendingLinkNexts.Add(note.String, this)

                let mask =
                    createMaskForNote note
                    ||| if parentNote <> -1s        then NoteMask.Child    else NoteMask.None
                    ||| if fingerPrintId.[1] <> -1s then NoteMask.Arpeggio else NoteMask.None

                let pickDir =
                    if (note.Mask &&& XML.NoteMask.PickDirection) <> XML.NoteMask.None then 1y else -1y

                {| String = note.String; Fret = note.Fret; Mask = mask; ChordId = -1; ChordNoteId = -1; Parent = parentNote;
                   BendValues = bendValues; SlideTo = note.SlideTo; UnpSlide = note.SlideUnpitchTo;
                   LeftHand = note.LeftHand; Tap = note.Tap; PickDirection = pickDir;
                   Slap = if note.IsSlap then 1y else -1y
                   Pluck = if note.IsPluck then 1y else -1y
                   Vibrato = int16 note.Vibrato; Sustain = msToSec note.Sustain; MaxBend = note.MaxBend |}
            
            | XmlChord chord ->
                let template = xml.ChordTemplates.[int chord.ChordId]
                let mask = createMaskForChord template chord
                let sustain =
                    match chord.ChordNotes with
                    | null -> 0.f
                    | cn when cn.Count = 0 -> 0.f
                    | cn -> msToSec cn.[0].Sustain

                {| String = -1y; Fret = -1y; Mask = mask; ChordId = int chord.ChordId; ChordNoteId = 99999; Parent = -1s;
                   BendValues = [||]; SlideTo = -1y; UnpSlide = -1y;
                   LeftHand = -1y; Tap = -1y; PickDirection = -1y; Slap = -1y; Pluck = -1y
                   Vibrato = 0s; Sustain = sustain; MaxBend = 0.f |}

        let initialNote =
            { Mask = data.Mask
              Flags = 0u
              Hash = 0u
              Time = msToSec timeCode
              StringIndex = data.String
              FretId = data.Fret
              AnchorFretId = anchor.Fret
              AnchorWidth = anchor.Width
              ChordId = data.ChordId
              ChordNotesId = data.ChordNoteId
              PhraseId = phraseId
              PhraseIterationId = piId
              FingerPrintId = fingerPrintId
              NextIterNote = 0s
              PrevIterNote = 0s
              ParentPrevNote = 0s
              SlideTo = data.SlideTo
              SlideUnpitchTo = data.UnpSlide
              LeftHand = data.LeftHand
              Tap = data.Tap
              PickDirection = data.PickDirection
              Slap = data.Slap
              Pluck = data.Pluck
              Vibrato = data.Vibrato
              Sustain = data.Sustain
              MaxBend = data.MaxBend
              BendData = data.BendValues }

        accuData.AddNote(piId, (data.Mask &&& NoteMask.Ignore) <> NoteMask.None)
        lastNote <- ValueSome initialNote

        { initialNote with
            Hash = hashNote initialNote
            Flags = createFlag lastNote anchor.Fret data.Fret
            NextIterNote = next
            PrevIterNote = prev
            ParentPrevNote = data.Parent }

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