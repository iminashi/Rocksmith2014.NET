module Rocksmith2014.Conversion.XmlToSngLevel

open Rocksmith2014
open Rocksmith2014.SNG
open Rocksmith2014.Conversion.XmlToSng
open Rocksmith2014.Conversion.Utils
open XmlToSngNote

/// Creates an XML entity array from the notes and chords.
let private createXmlEntityArray (xmlNotes: ResizeArray<XML.Note>) (xmlChords: ResizeArray<XML.Chord>) =
    if xmlChords.Count = 0 then
        Array.init xmlNotes.Count (fun i -> XmlNote xmlNotes.[i])
    elif xmlNotes.Count = 0 then
        Array.init xmlChords.Count (fun i -> XmlChord xmlChords.[i])
    else
        let entityArray = Array.zeroCreate<XmlEntity> (xmlNotes.Count + xmlChords.Count)

        for i = 0 to xmlNotes.Count - 1 do
            entityArray.[i] <- XmlNote (xmlNotes.[i])
        for i = 0 to xmlChords.Count - 1 do
            entityArray.[xmlNotes.Count + i] <- XmlChord (xmlChords.[i])

        Array.sortInPlaceBy getTimeCode entityArray
        entityArray

/// Converts am XML level into an SNG level.
let convertLevel (accuData: AccuData) (piTimes: int[]) (xmlArr: XML.InstrumentalArrangement) (xmlLevel: XML.Level) =
    let difficulty = int xmlLevel.Difficulty
    let xmlEntities = createXmlEntityArray xmlLevel.Notes xmlLevel.Chords
    let noteTimes = xmlEntities |> Array.map getTimeCode
    let isArpeggio (hs: XML.HandShape) = xmlArr.ChordTemplates.[int hs.ChordId].IsArpeggio
    let convertHandshape' = convertHandshape noteTimes

    let arpeggios, handShapes =
        xmlLevel.HandShapes.ToArray()
        |> Array.partition isArpeggio
    let arpeggios = arpeggios |> Array.map convertHandshape'
    let handShapes = handShapes |> Array.map convertHandshape'
    let fingerPrints = [| handShapes; arpeggios |]

    let convertNote' = convertNote noteTimes piTimes fingerPrints accuData NoteFlagFunctions.onAnchorChange xmlArr difficulty

    let notes = xmlEntities |> Array.mapi convertNote'

    let anchors =
        xmlLevel.Anchors
        |> mapiToArray (convertAnchor notes noteTimes xmlLevel xmlArr)

    let averageNotes =
        let piNotes =
            accuData.NotesInPhraseIterationsAll.[difficulty]
            |> Array.indexed
        Array.init xmlArr.Phrases.Count (fun phraseId ->
            piNotes
            |> Array.filter (fun v -> xmlArr.PhraseIterations.[fst v].PhraseId = phraseId)
            |> Array.map (snd >> float32)
            |> tryAverage)

    { Difficulty = difficulty
      Anchors = anchors
      AnchorExtensions = accuData.AnchorExtensions.[difficulty].ToArray()
      HandShapes = handShapes
      Arpeggios = arpeggios
      Notes = notes
      AverageNotesPerIteration = averageNotes
      NotesInPhraseIterationsExclIgnored = accuData.NotesInPhraseIterationsExclIgnored.[difficulty]
      NotesInPhraseIterationsAll = accuData.NotesInPhraseIterationsAll.[difficulty] }
