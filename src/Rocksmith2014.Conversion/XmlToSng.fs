module Rocksmith2014.Conversion.XmlToSng

open System
open System.Collections.Generic
open System.Globalization
open Rocksmith2014
open Rocksmith2014.Conversion.Utils
open Rocksmith2014.SNG.Types
open Nessos.Streams

type HandShapeMap = Map<int16, Set<int>>

type XmlEntity =
    | XmlNote of XmlNote : XML.Note
    | XmlChord of XmlChord : XML.Chord

type NoteCounts =
    { mutable Easy : int
      mutable Medium : int
      mutable Hard : int
      mutable Ignored : int }

/// Represents data that is being accumulated when mapping XML notes/chords into SNG notes.
type AccuData =
    { StringMasks : int8[,]
      ChordNotes : ResizeArray<ChordNotes>
      ChordNotesMap : Dictionary<int, int>
      AnchorExtensions : ResizeArray<AnchorExtension>
      NotesInPhraseIterationsExclIgnored : int[]
      NotesInPhraseIterationsAll : int[]
      NoteCounts : NoteCounts
      mutable FirstNoteTime : int }

    member this.AddNote(pi:int, difficulty:byte, heroLeves:XML.HeroLevels, ignored:bool) =
        this.NotesInPhraseIterationsAll.[pi] <- this.NotesInPhraseIterationsAll.[pi] + 1
    
        if not ignored then
            this.NotesInPhraseIterationsExclIgnored.[pi] <- this.NotesInPhraseIterationsExclIgnored.[pi] + 1
    
        if heroLeves.Easy = difficulty then
            this.NoteCounts.Easy <- this.NoteCounts.Easy + 1
        elif heroLeves.Medium = difficulty then
            this.NoteCounts.Medium <- this.NoteCounts.Medium + 1
        elif heroLeves.Hard = difficulty then
            this.NoteCounts.Hard <- this.NoteCounts.Hard + 1
            if ignored then
                this.NoteCounts.Ignored <- this.NoteCounts.Ignored + 1
    
    member this.Reset() =
        this.AnchorExtensions.Clear()
        Array.Clear(this.NotesInPhraseIterationsAll, 0, this.NotesInPhraseIterationsAll.Length)
        Array.Clear(this.NotesInPhraseIterationsExclIgnored, 0, this.NotesInPhraseIterationsExclIgnored.Length)
    
    static member Init(arr:XML.InstrumentalArrangement) =
        { StringMasks = Array2D.zeroCreate (arr.Sections.Count) 36
          ChordNotes = ResizeArray()
          AnchorExtensions = ResizeArray()
          ChordNotesMap = Dictionary()
          NotesInPhraseIterationsExclIgnored = Array.zeroCreate (arr.PhraseIterations.Count)
          NotesInPhraseIterationsAll = Array.zeroCreate (arr.PhraseIterations.Count)
          NoteCounts = { Easy = 0; Medium = 0; Hard = 0; Ignored = 0 }
          FirstNoteTime = Int32.MaxValue }

/// Finds the index of the phrase iteration that contains the given time code.
let findPhraseIterationId (time:int) (iterations:ResizeArray<XML.PhraseIteration>) =
    let rec find index =
        if index < 0 then
            0
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
          // TODO: fix this
          PhraseIteration = findPhraseIterationId xmlBeat.Time xml.PhraseIterations
          Mask = mask }

let convertVocal (xmlVocal:XML.Vocal) =
    { Time = msToSec xmlVocal.Time
      Length = msToSec xmlVocal.Length
      Lyric = xmlVocal.Lyric
      Note = int xmlVocal.Note }

let convertSymbolDefinition (xmlGlyphDef:XML.GlyphDefinition) =
    { Symbol = xmlGlyphDef.Symbol
      Outer = { xMin = xmlGlyphDef.OuterXMin; xMax = xmlGlyphDef.OuterXMax; yMin = xmlGlyphDef.OuterYMin; yMax = xmlGlyphDef.OuterYMax }
      Inner = { xMin = xmlGlyphDef.InnerXMin; xMax = xmlGlyphDef.InnerXMax; yMin = xmlGlyphDef.InnerYMin; yMax = xmlGlyphDef.InnerYMax } }

let convertPhrase (xml:XML.InstrumentalArrangement) phraseId (xmlPhrase:XML.Phrase) =
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

let standardTuningMidiNotes = [| 40; 45; 50; 55; 59; 64 |];

let private mapToMidiNotes (xml:XML.InstrumentalArrangement) (frets: sbyte array) =
    Array.init 6 (fun str ->
        if frets.[str] = -1y then
            -1
        else
            let tuning = xml.Tuning.Strings
            let fret =
                if xml.Capo > 0y && frets.[str] = 0y then
                    int xml.Capo
                else
                    int frets.[str]
            let offset = if xml.ArrangementProperties.PathBass then -12 else 0
            standardTuningMidiNotes.[str] + int tuning.[str] + fret + offset
    )

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
      Notes = mapToMidiNotes xml xmlChord.Frets
      Name = xmlChord.Name }

let convertBendValue (xmlBv:XML.BendValue) =
    { Time = msToSec xmlBv.Time
      Step = xmlBv.Step }

let convertPhraseIteration (xml:XML.InstrumentalArrangement) index (xmlPi:XML.PhraseIteration) =
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

let convertSection (xml:XML.InstrumentalArrangement) index (xmlSection:XML.Section) =
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

let convertAnchor (noteTimes:int array) (level:XML.Level) (xml:XML.InstrumentalArrangement) index (xmlAnchor:XML.Anchor) =
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

let convertHandshape (handShapeMap : HandShapeMap) (xmlHs:XML.HandShape) =
    let firstNoteTime, lastNoteTime =
        match handShapeMap |> Map.tryFind xmlHs.ChordId with
        | Some set when not set.IsEmpty -> msToSec set.MinimumElement, msToSec set.MaximumElement
        | _ -> -1.f, -1.f

    { ChordId = int xmlHs.ChordId
      StartTime = msToSec xmlHs.StartTime
      EndTime = msToSec xmlHs.EndTime
      FirstNoteTime = firstNoteTime
      LastNoteTime = lastNoteTime }

//let divideNoteTimesPerPhraseIteration (noteTimes:int[]) (arr:XML.InstrumentalArrangement) =
//    arr.PhraseIterations
//    |> Seq.mapi (fun i pi ->
//        let endTime = if i = arr.PhraseIterations.Count - 1 then arr.SongLength else arr.PhraseIterations.[i + 1].Time
//        noteTimes
//        |> Seq.skipWhile (fun t -> t < pi.Time)
//        |> Seq.takeWhile (fun t -> t < endTime)
//        |> Seq.toArray)
//    |> Seq.toArray

let createHandShapeMap (noteTimes:int[]) (level:XML.Level) : HandShapeMap =
    let toSet (hs:XML.HandShape) =
        let times =
            Array.FindAll(noteTimes, (fun t -> t >= hs.StartTime && t < hs.EndTime))
            |> Set.ofArray
        hs.ChordId, times
    
    level.HandShapes
    |> Seq.map toSet
    |> Map.ofSeq

let xmlCnMask = XML.NoteMask.LinkNext ||| XML.NoteMask.Accent ||| XML.NoteMask.Tremolo 
                ||| XML.NoteMask.FretHandMute ||| XML.NoteMask.HammerOn ||| XML.NoteMask.Harmonic
                ||| XML.NoteMask.PalmMute ||| XML.NoteMask.PinchHarmonic ||| XML.NoteMask.Pluck
                ||| XML.NoteMask.PullOff ||| XML.NoteMask.Slap

let createMaskForChordNote (note:XML.Note) =
    // Not used for chord notes: Single, Ignore, Child, Right Hand, Left Hand, Arpeggio
    // Supported by the game, although not possible in official files: Tap, Pluck, Slap

    // Apply flags from properties not in the XML note mask
    let baseMask =
        NoteMask.None
        ||| if note.Fret = 0y        then NoteMask.Open           else NoteMask.None
        ||| if note.Sustain > 0      then NoteMask.Sustain        else NoteMask.None
        ||| if note.IsSlide          then NoteMask.Slide          else NoteMask.None
        ||| if note.IsUnpitchedSlide then NoteMask.UnpitchedSlide else NoteMask.None
        ||| if note.IsVibrato        then NoteMask.Vibrato        else NoteMask.None
        ||| if note.IsBend           then NoteMask.Bend           else NoteMask.None
        ||| if note.IsTap            then NoteMask.Tap            else NoteMask.None

    // Apply flags from the XML note mask if needed
    if (note.Mask &&& xmlCnMask) = XML.NoteMask.None then
        baseMask
    else
        baseMask
        ||| if note.IsLinkNext      then NoteMask.Parent        else NoteMask.None
        ||| if note.IsAccent        then NoteMask.Accent        else NoteMask.None
        ||| if note.IsTremolo       then NoteMask.Tremolo       else NoteMask.None
        ||| if note.IsFretHandMute  then NoteMask.Mute          else NoteMask.None
        ||| if note.IsHammerOn      then NoteMask.HammerOn      else NoteMask.None
        ||| if note.IsHarmonic      then NoteMask.Harmonic      else NoteMask.None
        ||| if note.IsPalmMute      then NoteMask.PalmMute      else NoteMask.None
        ||| if note.IsPinchHarmonic then NoteMask.PinchHarmonic else NoteMask.None
        ||| if note.IsPluck         then NoteMask.Pluck         else NoteMask.None
        ||| if note.IsPullOff       then NoteMask.PullOff       else NoteMask.None
        ||| if note.IsSlap          then NoteMask.Slap          else NoteMask.None
    
/// Creates an SNG note mask for a single note.
let createMaskForNote parentNote isArpeggio (note:XML.Note) =
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
        ||| if note.LeftHand <> -1y  then NoteMask.LeftHand       else NoteMask.None
        ||| if parentNote <> -1s     then NoteMask.Child          else NoteMask.None
        ||| if isArpeggio            then NoteMask.Arpeggio       else NoteMask.None

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
let createMaskForChord (template:XML.ChordTemplate) sustain chordNoteId isArpeggio (chord:XML.Chord) =
    // Apply flags from properties not in the XML chord mask
    let baseMask =
        NoteMask.Chord
        ||| if isDoubleStop template then NoteMask.DoubleStop else NoteMask.None
        ||| if isStrum chord         then NoteMask.Strum      else NoteMask.None
        ||| if template.IsArpeggio   then NoteMask.Arpeggio   else NoteMask.None
        ||| if sustain > 0.f         then NoteMask.Sustain    else NoteMask.None
        ||| if chordNoteId <> -1     then NoteMask.ChordNotes else NoteMask.None
        ||| if isArpeggio            then NoteMask.Arpeggio   else NoteMask.None

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

let private hashNote note = hash note |> uint32
let private hashChordNotes cn = hash cn

let getTimeCode = function
    | XmlNote xn -> xn.Time
    | XmlChord xc -> xc.Time

let createBendData32 (note:XML.Note) =
    let usedCount = note.BendValues.Count
    let bv = Array.init 32 (fun i -> 
        if i < usedCount then
            convertBendValue note.BendValues.[i]
        else
            BendValue.Empty)

    { BendValues = bv
      UsedCount = usedCount }

let createChordNotesMask (chordNotes : ResizeArray<XML.Note>) =
    let masks = Array.zeroCreate<NoteMask> 6
    for note in chordNotes do
        let strIndex = int note.String

        masks.[strIndex] <- createMaskForChordNote note
    masks

let createChordNotes (pendingLinkNexts:Dictionary<int8, int16>) thisId (accuData:AccuData) (chord:XML.Chord) =
    match chord.ChordNotes with
    | null -> -1
    | xmlChordNotes ->
        // Convert the masks first to check if the chord notes need to be created at all
        let masks = createChordNotesMask xmlChordNotes
        if Array.forall (fun m -> m = NoteMask.None) masks then
            -1
        else
            let slideTo = Array.replicate 6 -1y
            let slideUnpitchTo = Array.replicate 6 -1y
            let vibrato = Array.zeroCreate<int16> 6
            let bendDict = Dictionary<int, BendData32>()

            for note in xmlChordNotes do
                let strIndex = int note.String

                slideTo.[strIndex] <- note.SlideTo
                slideUnpitchTo.[strIndex] <- note.SlideUnpitchTo
                vibrato.[strIndex] <- int16 note.Vibrato

                if note.IsBend then
                    bendDict.Add(strIndex, createBendData32 note)

                if note.IsLinkNext then
                    pendingLinkNexts.TryAdd(note.String, thisId) |> ignore

            let bendData = Array.init<BendData32> 6 (fun i ->
                if bendDict.ContainsKey(i) then
                    bendDict.[i]
                else
                    BendData32.Empty)

            let chordNotes =
                { Mask = masks; BendData = bendData; SlideTo = slideTo; SlideUnpitchTo = slideUnpitchTo; Vibrato = vibrato }

            let hash = hashChordNotes chordNotes
            if accuData.ChordNotesMap.ContainsKey(hash) then
                accuData.ChordNotesMap.[hash]
            else
                let id = accuData.ChordNotes.Count
                accuData.ChordNotes.Add(chordNotes)
                accuData.ChordNotesMap.Add(hash, id)
                id

/// Returns a function that is valid for converting notes in a single difficulty level.
let convertNote () =
    // Dictionary of link-next parent notes in need of a child note.
    // Mapping: string number => index of note in phrase iteration
    let pendingLinkNexts = Dictionary<int8, int16>()
    let mutable previousNote : ValueOption<Note> = ValueNone

    fun (noteTimes : int[])
        (handShapeMap : HandShapeMap)
        (accuData : AccuData)
        (xml : XML.InstrumentalArrangement)
        (difficulty : int)
        (index:int)
        (xmlEnt : XmlEntity) ->

        let level = xml.Levels.[difficulty]
        let timeCode = getTimeCode xmlEnt

        let piId = xml.PhraseIterations |> findPhraseIterationId timeCode
        let phraseIteration = xml.PhraseIterations.[piId]
        let phraseId = phraseIteration.PhraseId

        let this = int16 index
        let previous =
            if index = 0 || noteTimes.[index - 1] < phraseIteration.Time then
                -1s
            else
                this - 1s

        let next =
            if index < noteTimes.Length - 1 then
                let endTime =
                    if piId = xml.PhraseIterations.Count - 1 then
                        xml.SongLength
                    else
                        xml.PhraseIterations.[piId + 1].Time
                if noteTimes.[index + 1] < endTime then
                    this + 1s
                else
                    -1s
            else
                -1s

        let anchor = level.Anchors.FindLast(fun a -> a.Time <= timeCode)

        let struct (fingerPrintId, isArpeggio) =
            let hsOption =
                handShapeMap
                |> Map.tryPick (fun key set -> if set.Contains(timeCode) then Some key else None)

            match hsOption with
            // Arpeggio
            | Some id when xml.ChordTemplates.[int id].IsArpeggio -> struct ([| -1s; id |], true)
            // Normal handshape
            | Some id -> struct ([| id; -1s |], false)
            | None -> struct ([| -1s; -1s |], false)

        // TODO: String masks for sections

        let data =
            match xmlEnt with
            // XML Notes
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

                if note.IsLinkNext then pendingLinkNexts.TryAdd(note.String, this) |> ignore
                let mask = createMaskForNote parentNote isArpeggio note

                // Create anchor extension if needed
                if note.IsSlide then
                    let ax = 
                        { BeatTime = msToSec (timeCode + note.Sustain)
                          FretId = note.SlideTo }
                    accuData.AnchorExtensions.Add(ax)

                {| String = note.String; Fret = note.Fret; Mask = mask; ChordId = -1; ChordNoteId = -1; Parent = parentNote;
                   BendValues = bendValues; SlideTo = note.SlideTo; UnpSlide = note.SlideUnpitchTo; LeftHand = note.LeftHand
                   PickDirection = if (note.Mask &&& XML.NoteMask.PickDirection) <> XML.NoteMask.None then 1y else 0y
                   Tap = if note.Tap > 0y then note.Tap else -1y
                   Slap = if note.IsSlap then 1y else -1y
                   Pluck = if note.IsPluck then 1y else -1y
                   Vibrato = int16 note.Vibrato; Sustain = msToSec note.Sustain; MaxBend = note.MaxBend |}
            
            // XML Chords
            | XmlChord chord ->
                let chordNoteId = createChordNotes pendingLinkNexts this accuData chord
                let template = xml.ChordTemplates.[int chord.ChordId]
                let sustain =
                    match chord.ChordNotes with
                    | null -> 0.f
                    | cn when cn.Count = 0 -> 0.f
                    | cn -> msToSec cn.[0].Sustain
                
                let mask = createMaskForChord template sustain chordNoteId isArpeggio chord

                {| Mask = mask; ChordId = int chord.ChordId; ChordNoteId = chordNoteId; Sustain = sustain;
                   // Other values are not applicable to chords
                   String = -1y; Fret = -1y; Parent = -1s; BendValues = [||]; SlideTo = -1y; UnpSlide = -1y;
                   LeftHand = -1y; Tap = -1y; PickDirection = -1y; Slap = -1y; Pluck = -1y
                   Vibrato = 0s; MaxBend = 0.f |}

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

        let isIgnore = (data.Mask &&& NoteMask.Ignore) <> NoteMask.None
        let heroLevels = phraseIteration.HeroLevels

        accuData.AddNote(piId, byte difficulty, heroLevels, isIgnore)
        previousNote <- ValueSome initialNote

        { initialNote with
            Hash = hashNote initialNote
            Flags = createFlag previousNote anchor.Fret data.Fret
            NextIterNote = next
            PrevIterNote = previous
            ParentPrevNote = data.Parent }

let private eventToDNA (event:XML.Event) =
    match event.Code with
    | "dna_none"  -> Some { DnaId = 0; Time = msToSec event.Time }
    | "dna_solo"  -> Some { DnaId = 1; Time = msToSec event.Time }
    | "dna_riff"  -> Some { DnaId = 2; Time = msToSec event.Time }
    | "dna_chord" -> Some { DnaId = 3; Time = msToSec event.Time }
    | _ -> None

let createDNAs (xml:XML.InstrumentalArrangement) =
    xml.Events
    |> Seq.choose eventToDNA
    |> Seq.toArray

let convertMetaData (accuData:AccuData) (xml:XML.InstrumentalArrangement) =
    let firstNoteTime = msToSec accuData.FirstNoteTime
    let conversionDate = DateTime.Now.ToString("MM-d-yy HH:mm", CultureInfo.InvariantCulture)
    let maxScore = 10_000.

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
      Unk11FirstNoteTime = firstNoteTime
      Unk12FirstNoteTime = firstNoteTime
      MaxDifficulty = xml.Levels.Count - 1 }

let private createXmlEntityArray (xmlNotes:ResizeArray<XML.Note>) (xmlChords:ResizeArray<XML.Chord>) = 
    let entityArray = Array.zeroCreate<XmlEntity> (xmlNotes.Count + xmlChords.Count)

    for i = 0 to xmlNotes.Count - 1 do
        entityArray.[i] <- XmlNote (xmlNotes.[i])
    for i = 0 to xmlChords.Count - 1 do
        entityArray.[xmlNotes.Count + i] <- XmlChord (xmlChords.[i])

    Array.sortInPlaceBy getTimeCode entityArray
    entityArray

let convertLevel (accuData:AccuData) (xmlArr:XML.InstrumentalArrangement) (xmlLevel:XML.Level) =
    accuData.Reset()

    let difficulty = int xmlLevel.Difficulty
    let xmlEntities = createXmlEntityArray xmlLevel.Notes xmlLevel.Chords
    let noteTimes = xmlEntities |> Array.map getTimeCode
    let hsMap = createHandShapeMap noteTimes xmlLevel
    let convertNote' = convertNote() noteTimes hsMap accuData xmlArr difficulty

    if noteTimes.[0] < accuData.FirstNoteTime then
        accuData.FirstNoteTime <- noteTimes.[0]

    let notes = xmlEntities |> Array.mapi convertNote'

    let anchors =
        xmlLevel.Anchors
        |> Stream.ofResizeArray
        |> Stream.mapi (convertAnchor noteTimes xmlLevel xmlArr)
        |> Stream.toArray

    let isArpeggio (hs:XML.HandShape) = xmlArr.ChordTemplates.[int hs.ChordId].IsArpeggio
    let convertHandshape' = convertHandshape hsMap

    let arpeggios, handShapes =
        xmlLevel.HandShapes.ToArray()
        |> Array.partition isArpeggio
    let arpeggios = arpeggios |> Array.map convertHandshape'
    let handShapes = handShapes |> Array.map convertHandshape'

    let averageNotes =
        let piNotes = 
            accuData.NotesInPhraseIterationsAll 
            |> Array.indexed
        Array.init xmlArr.Phrases.Count (fun i ->
            piNotes
            |> Array.filter (fun v -> xmlArr.PhraseIterations.[fst v].PhraseId = i)
            |> Array.map (snd >> float32)
            |> tryAverage)

    { Difficulty = difficulty
      Anchors = anchors
      AnchorExtensions = accuData.AnchorExtensions.ToArray()
      HandShapes = handShapes
      Arpeggios = arpeggios
      Notes = notes
      AverageNotesPerIteration = averageNotes
      NotesInPhraseIterationsExclIgnored = Array.copy accuData.NotesInPhraseIterationsExclIgnored
      NotesInPhraseIterationsAll = Array.copy accuData.NotesInPhraseIterationsAll }
