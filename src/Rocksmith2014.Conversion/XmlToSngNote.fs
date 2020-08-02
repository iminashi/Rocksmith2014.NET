module Rocksmith2014.Conversion.XmlToSngNote

open Rocksmith2014
open Rocksmith2014.SNG
open Rocksmith2014.Conversion.XmlToSng
open Rocksmith2014.Conversion.Utils
open System.Collections.Generic

/// Calculates a hash value for a note.
let private hashNote note = hash note |> uint32

/// Calculates a hash value for a chord note.
let private hashChordNotes cn = hash cn

/// Mask bits that need to be considered when converting an XML chord note into SNG.
let private xmlCnMask =
    XML.NoteMask.LinkNext ||| XML.NoteMask.Accent ||| XML.NoteMask.Tremolo
    ||| XML.NoteMask.FretHandMute ||| XML.NoteMask.HammerOn ||| XML.NoteMask.Harmonic
    ||| XML.NoteMask.PalmMute ||| XML.NoteMask.PinchHarmonic ||| XML.NoteMask.Pluck
    ||| XML.NoteMask.PullOff ||| XML.NoteMask.Slap

/// Creates an SNG note mask for a chord note.
let private createMaskForChordNote (note: XML.Note) =
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
let private createMaskForNote parentNote isArpeggio (note: XML.Note) =
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

/// Returns true if the double stop bit should be set.
let private isDoubleStop (template: XML.ChordTemplate) =
    template.Frets
    |> Seq.filter (fun f -> f <> -1y)
    |> Seq.length
    |> (=) 2

/// Returns true if the strum bit should be set.
let private isStrum (chord: XML.Chord) =
    match chord.ChordNotes with
    | null -> false
    | cn when cn.Count = 0 -> false
    | _ -> true

/// Creates an SNG note mask for a chord.
let private createMaskForChord (template: XML.ChordTemplate) sustain chordNoteId isArpeggio (chord:XML.Chord) =
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

/// Creates a BendData32 object for the XML chord note.
let private createBendData32 (chordNote: XML.Note) =
    let usedCount = chordNote.BendValues.Count
    let bv = Array.init 32 (fun i ->
        if i < usedCount then
            convertBendValue chordNote.BendValues.[i]
        else
            BendValue.Empty)

    { BendValues = bv
      UsedCount = usedCount }

/// Creates a note mask array from the XML chord notes.
let private createChordNotesMask (chordNotes: ResizeArray<XML.Note>) =
    let masks = Array.zeroCreate<NoteMask> 6
    for note in chordNotes do
        masks.[int note.String] <- createMaskForChordNote note
    masks

/// Creates chord notes for the chord and returns the ID number for them.
let private createChordNotes (pendingLinkNexts: Dictionary<int8, int16>) thisId (accuData: AccuData) (chord: XML.Chord) =
    // Convert the masks first to check if the chord notes need to be created at all
    let masks = createChordNotesMask chord.ChordNotes

    if Array.forall (fun m -> m = NoteMask.None) masks then
        -1
    else
        let slideTo = Array.replicate 6 -1y
        let slideUnpitchTo = Array.replicate 6 -1y
        let vibrato = Array.zeroCreate<int16> 6
        let bendDict = Dictionary<int, BendData32>()
    
        for note in chord.ChordNotes do
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
        lock accuData.ChordNotesMap (fun _ ->
            if accuData.ChordNotesMap.ContainsKey(hash) then
                accuData.ChordNotesMap.[hash]
            else
                let id = accuData.ChordNotes.Count
                accuData.ChordNotes.Add(chordNotes)
                accuData.ChordNotesMap.Add(hash, id)
                id)

/// Returns a function that is valid for converting notes in a single difficulty level.
let convertNote (noteTimes: int[])
                (piTimes: int[])
                (fingerPrints: FingerPrint[][])
                (accuData: AccuData)
                (flag: NoteFlagger)
                (xml: XML.InstrumentalArrangement)
                (difficulty: int) =
    // Dictionary of link-next parent notes in need of a child note.
    // Mapping: string number => index of note in phrase iteration
    let pendingLinkNexts = Dictionary<int8, int16>()
    // The previous converted note
    let mutable previousNote : Note option = None

    fun (index: int) (xmlEnt: XmlEntity) ->

        let level = xml.Levels.[difficulty]
        let timeCode = getTimeCode xmlEnt
        let timeSeconds = msToSec timeCode

        let piId = findPhraseIterationId timeCode xml.PhraseIterations
        let phraseIteration = xml.PhraseIterations.[piId]
        let phraseId = phraseIteration.PhraseId
        let anchor = findAnchor timeCode level.Anchors
        let sectionId = findSectionId timeCode xml.Sections

        // The index of this note
        let this = int16 index

        // The index of the previous note in the same phrase iteration
        let previous =
            if index = 0 || noteTimes.[index - 1] < phraseIteration.Time then
                -1s
            else
                this - 1s

        // The index of the next note in the same phrase iteration
        let next =
            if index = noteTimes.Length - 1 || noteTimes.[index + 1] >= piTimes.[piId + 1] then
                -1s
            else
                this + 1s

        let fingerPrintIds =
            [| int16 (findFingerPrintId timeSeconds fingerPrints.[0])
               int16 (findFingerPrintId timeSeconds fingerPrints.[1]) |]
        let isArpeggio = fingerPrintIds.[1] <> -1s

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
                    | bendValues -> bendValues |> mapToArray convertBendValue

                // Using TryAdd because of possible link next errors in CDLC
                if note.IsLinkNext then pendingLinkNexts.TryAdd(note.String, this) |> ignore
                let mask = createMaskForNote parentNote isArpeggio note

                // Create anchor extension if needed
                if note.IsSlide then
                    let ax = 
                        { BeatTime = msToSec (timeCode + note.Sustain)
                          FretId = note.SlideTo }
                    accuData.AnchorExtensions.[difficulty].Add(ax)

                // Update the string mask for this note's section/difficulty
                let sMask = accuData.StringMasks.[sectionId].[difficulty]
                accuData.StringMasks.[sectionId].[difficulty] <- sMask ||| (1y <<< int note.String)

                {| String = note.String; Fret = note.Fret; Mask = mask; Parent = parentNote;
                   BendValues = bendValues; SlideTo = note.SlideTo; UnpSlide = note.SlideUnpitchTo; LeftHand = note.LeftHand
                   Vibrato = int16 note.Vibrato; Sustain = msToSec note.Sustain; MaxBend = note.MaxBend
                   PickDirection = if (note.Mask &&& XML.NoteMask.PickDirection) <> XML.NoteMask.None then 1y else 0y
                   Tap = if note.Tap > 0y then note.Tap else -1y
                   Slap = if note.IsSlap then 1y else -1y
                   Pluck = if note.IsPluck then 1y else -1y
                   // Values note applicable to notes
                   ChordId = -1; ChordNoteId = -1; |}
        
            // XML Chords
            | XmlChord chord ->
                let template = xml.ChordTemplates.[int chord.ChordId]
                let sustain =
                    match chord.ChordNotes with
                    | null -> 0.f
                    | cn when cn.Count = 0 -> 0.f
                    | cn -> msToSec cn.[0].Sustain

                let chordNoteId =
                    match chord.ChordNotes with
                    | null -> -1
                    | chordNotes ->
                        for cn in chordNotes do
                            // Update the string mask for this chord's section/difficulty
                            let sMask = accuData.StringMasks.[sectionId].[difficulty]
                            accuData.StringMasks.[sectionId].[difficulty] <- sMask ||| (1y <<< int cn.String)

                        createChordNotes pendingLinkNexts this accuData chord
            
                let mask = createMaskForChord template sustain chordNoteId isArpeggio chord

                {| Mask = mask; ChordId = int chord.ChordId; ChordNoteId = chordNoteId; Sustain = sustain;
                   // Other values are not applicable to chords
                   String = -1y; Fret = -1y; Parent = -1s; BendValues = [||]; SlideTo = -1y; UnpSlide = -1y;
                   LeftHand = -1y; Tap = -1y; PickDirection = -1y; Slap = -1y; Pluck = -1y
                   Vibrato = 0s; MaxBend = 0.f |}

        // The initial note which will be used for calculating the hash
        let initialNote =
            { Mask = data.Mask
              Flags = 0u
              Hash = 0u
              Time = timeSeconds
              StringIndex = data.String
              FretId = data.Fret
              AnchorFretId = anchor.Fret
              AnchorWidth = anchor.Width
              ChordId = data.ChordId
              ChordNotesId = data.ChordNoteId
              PhraseId = phraseId
              PhraseIterationId = piId
              FingerPrintId = fingerPrintIds
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
        let flags = flag previousNote initialNote

        accuData.AddNote(piId, byte difficulty, heroLevels, isIgnore)
        previousNote <- Some initialNote

        { initialNote with
            Hash = hashNote initialNote
            Flags = flags
            NextIterNote = next
            PrevIterNote = previous
            ParentPrevNote = data.Parent }
