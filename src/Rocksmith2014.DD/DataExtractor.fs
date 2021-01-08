module Rocksmith2014.DD.DataExtractor

open Rocksmith2014.XML
open System
open System.Text
open System.IO

type PhraseData = 
  { Name : string
    MaxDifficulty : int
    StartTime : int
    EndTime : int
    LengthMs : int
    LengthBeats : int
    MeasureCount : int
    Beats : Ebeat list
    Notes : Note list
    Chords : Chord list
    HandShapes : HandShape list
    Anchors : Anchor list
    NoteCount : int
    RepeatedNotes : int
    ChordCount : int
    RepeatedChords : int
    TechCount : int
    BendCount : int
    TapCount : int
    TremoloCount : int
    PinchHarmonicCount : int
    HarmonicCount : int
    VibratoCount : int
    PalmMuteCount : int
    MaxChordStrings : int
    SlideCount : int
    UnpitchedSlideCount : int
    IgnoreCount : int
    UniqueLevels : int 
    TempoEstimate : int
    AnchorCount : int
    SoloPhrase : bool }

let getPath (arr: InstrumentalArrangement) =
    if arr.MetaData.ArrangementProperties.PathLead then 0
    elif arr.MetaData.ArrangementProperties.PathRhythm then 1
    else 2

let private isRepeatedNote (n1: Note, n2: Note) = n1.String = n2.String && n1.Fret = n2.Fret && n1.Mask = n2.Mask
let private isRepeatedChord (c1: Chord, c2: Chord) = c1.ChordId = c2.ChordId
let private getRepeatCount f = Seq.pairwise >> (Seq.filter f) >> Seq.length

let private lengthBy f = Seq.filter f >> Seq.length

let private countNotesByPredicate pred (notes: Note seq) (chords: Chord seq) =
    (notes |> lengthBy pred)
    +
    (chords
    |> lengthBy (fun c -> not <| isNull c.ChordNotes && c.ChordNotes |> Seq.exists pred))

let getPhraseIterationData (arr: InstrumentalArrangement) (iteration: PhraseIteration) =
    let phrase = arr.Phrases.[iteration.PhraseId]
    let maxD = phrase.MaxDifficulty |> int
    let piIndex = arr.PhraseIterations.IndexOf(iteration)
    let startTime = iteration.Time
    let endTime =
        if piIndex + 1 = arr.PhraseIterations.Count then arr.MetaData.SongLength
        else arr.PhraseIterations.[piIndex + 1].Time
    let lengthMs = endTime - startTime

    let isSolo =
        phrase.IsSolo
        ||
        match arr.Sections.Find(fun s -> s.Time = startTime) with
        | null -> false
        | s -> s.Name = "solo" || s.Name = "tapping"

    let beats =
        arr.Ebeats
        |> getRange startTime endTime

    let measureCount =
        beats
        |> List.filter (fun b -> b.Measure >= 0s)
        |> List.length

    let notes =
        arr.Levels.[maxD].Notes
        |> getRange startTime endTime

    let chords =
        arr.Levels.[maxD].Chords
        |> getRange startTime endTime

    let anchors =
        arr.Levels.[maxD].Anchors
        |> getRange startTime endTime

    let handShapes =
        arr.Levels.[maxD].HandShapes
        |> getRange startTime endTime

    let repeatedNotes = notes |> getRepeatCount isRepeatedNote
    let repeatedChords = chords |> getRepeatCount isRepeatedChord

    let techCount =
        (notes
        |> lengthBy (fun n -> n.Mask <> NoteMask.None || n.IsSlide || n.IsUnpitchedSlide || n.IsVibrato || n.IsBend))
        +
        (chords |> lengthBy (fun c -> c.Mask <> ChordMask.None))

    let ignoreCount =
        (notes |> lengthBy (fun n -> n.IsIgnore)) + (chords |> lengthBy (fun c -> c.IsIgnore))
    
    let tapCount = notes |> lengthBy (fun n -> n.IsTap)
    let tremoloCount = countNotesByPredicate (fun n -> n.IsTremolo) notes chords
    let bendCount = countNotesByPredicate (fun n -> n.IsBend) notes chords
    let pHarmCount = countNotesByPredicate (fun n -> n.IsPinchHarmonic) notes chords
    let harmCount = countNotesByPredicate (fun n -> n.IsHarmonic) notes chords
    let vibratoCount = countNotesByPredicate (fun n -> n.IsVibrato) notes chords
    let slideCount = countNotesByPredicate (fun n -> n.IsSlide) notes chords
    let unpitchedSlideCount = countNotesByPredicate (fun n -> n.IsUnpitchedSlide) notes chords
    let palmMuteCount = countNotesByPredicate (fun n -> n.IsPalmMute) notes chords

    let mostChordNotes =
        if chords.Length = 0 then
            0
        else
            chords
            |> Seq.distinctBy (fun c -> c.ChordId)
            |> Seq.map (fun c ->
                let template = arr.ChordTemplates.[int c.ChordId]
                Array.FindAll(template.Frets, fun f -> f <> -1y).Length)
            |> Seq.max

    let uniqueLevels =
        let mutable unique = 1
        for l = 1 to maxD do
            let ns1 = arr.Levels.[l].Notes |> getRange startTime endTime
            let ns2 = arr.Levels.[l - 1].Notes |> getRange startTime endTime
            let cs1 = arr.Levels.[l].Chords |> getRange startTime endTime
            let cs2 = arr.Levels.[l - 1].Chords |> getRange startTime endTime

            if not (Comparers.sameNotes ns1 ns2) || not (Comparers.sameChords cs1 cs2) then
                unique <- unique + 1
        unique

    { Name = phrase.Name
      MaxDifficulty = maxD
      StartTime = startTime
      EndTime = endTime
      MeasureCount = measureCount
      Beats = beats
      Notes = notes
      Chords = chords
      HandShapes = handShapes
      Anchors = anchors
      NoteCount = notes.Length
      RepeatedNotes = repeatedNotes
      ChordCount = chords.Length
      RepeatedChords = repeatedChords
      TechCount = techCount
      BendCount = bendCount
      TapCount = tapCount
      TremoloCount = tremoloCount
      PinchHarmonicCount = pHarmCount
      HarmonicCount = harmCount
      VibratoCount = vibratoCount
      SlideCount = slideCount
      UnpitchedSlideCount = unpitchedSlideCount
      PalmMuteCount = palmMuteCount
      IgnoreCount = ignoreCount
      LengthMs = lengthMs
      LengthBeats = beats.Length
      UniqueLevels = uniqueLevels
      TempoEstimate = 1000 * 60 * beats.Length / lengthMs
      AnchorCount = anchors.Length
      MaxChordStrings = mostChordNotes
      SoloPhrase = isSolo }

let getPhraseData (arr: InstrumentalArrangement) (phrase: Phrase) =
    // Pick the first phrase iteration for analysis
    let id = arr.Phrases.IndexOf(phrase)
    let pi = arr.PhraseIterations.Find(fun pi -> pi.PhraseId = id)
    getPhraseIterationData arr pi

let toCSVLines (filePath: string) =
    let sb = StringBuilder()
    let arr = InstrumentalArrangement.Load filePath
    let fn = Path.GetFileNameWithoutExtension filePath
    let key, arrName = let s = fn.Split('_') in s.[0], s.[1]
    let meta = sprintf "%s,%s,%i" key arrName (getPath arr)

    arr.Phrases
    |> Seq.filter (fun p -> p.MaxDifficulty > 0uy)
    |> Seq.map (fun p ->
        let data = getPhraseData arr p
        sb.Clear()
          .Append(meta).Append(',')
          .Append(data.Name).Append(',')
          .Append(data.MaxDifficulty + 1).Append(',')
          .Append(data.UniqueLevels).Append(',')
          .Append(data.LengthMs).Append(',')
          .Append(data.LengthBeats).Append(',')
          .Append(data.TempoEstimate).Append(',')
          .Append(data.NoteCount).Append(',')
          .Append(data.RepeatedNotes).Append(',')
          .Append(data.ChordCount).Append(',')
          .Append(data.RepeatedChords).Append(',')
          .Append(data.TechCount).Append(',')
          .Append(data.PalmMuteCount).Append(',')
          .Append(data.BendCount).Append(',')
          .Append(data.HarmonicCount).Append(',')
          .Append(data.PinchHarmonicCount).Append(',')
          .Append(data.TapCount).Append(',')
          .Append(data.TremoloCount).Append(',')
          .Append(data.VibratoCount).Append(',')
          .Append(data.SlideCount).Append(',')
          .Append(data.UnpitchedSlideCount).Append(',')
          .Append(data.IgnoreCount).Append(',')
          .Append(data.AnchorCount).Append(',')
          .Append(data.MaxChordStrings).Append(',')
          .Append(if data.SoloPhrase then 1 else 0)
          .ToString()
        )
