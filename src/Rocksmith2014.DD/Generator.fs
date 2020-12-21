module Rocksmith2014.DD.Generator

open Rocksmith2014.XML
open System
open LevelCounter

type XmlEntity =
    | XmlNote of Note
    | XmlChord of Chord

let private getTimeCode = function
    | XmlNote xn -> xn.Time
    | XmlChord xc -> xc.Time

/// Creates an XML entity array from the notes and chords.
let private createXmlEntityArray (xmlNotes: Note list) (xmlChords: Chord list) =
    if xmlChords.Length = 0 then
        xmlNotes
        |> List.map XmlNote
        |> Array.ofList
    elif xmlNotes.Length = 0 then
        xmlChords
        |> List.map XmlChord
        |> Array.ofList
    else
        let entityArray =
            [| yield! List.map XmlNote xmlNotes
               yield! List.map XmlChord xmlChords |]

        Array.sortInPlaceBy getTimeCode entityArray
        entityArray

let private chooseEntities diffPercent (entities: XmlEntity array) =
    entities
    |> Array.choose (fun e ->
        // TODO: Actual implementation
        match e with
        | XmlNote _ ->
            Some e
        | XmlChord _ ->
            Some e
    )

let private chooseHandShapes diffPercent (handShapes: HandShape list) =
    // TODO: Actual implementation
    handShapes
    |> List.choose (fun hs ->
        if diffPercent <= 15 then
            None
        else
            Some hs
    )

/// Copies the necessary anchors into the difficulty level.
let private chooseAnchors (entities: XmlEntity array) (anchors: Anchor list) phraseEndTime =
    // Assume that anchors up to 3ms after a note were meant to be on the note
    let errorMargin = 3

    // TODO: Anchors that contain no notes even at the hardest level

    let rec add result (anchors: Anchor list) =
        match anchors with
        | [] -> result
        | a::tail ->
            let endTime =
                match tail with
                | a2::_ -> a2.Time
                | [] -> phraseEndTime
            let result =
                if entities |> Array.exists (fun e ->
                    let time = getTimeCode e
                    time + errorMargin >= a.Time && time < endTime)
                then
                    a::result
                else
                    result
            add result tail

    add [] anchors

let private generateLevels (arr: InstrumentalArrangement) (phraseData: DataExtractor.PhraseData) =
    // Determine the number of levels to generate for this phrase
    let levelCount = predictLevelCount (DataExtractor.getPath arr) phraseData

    if phraseData.NoteCount + phraseData.ChordCount = 0 then
        // Copy anchors only
        let level = Level 0y
        level.Anchors.AddRange(phraseData.Anchors)
        [| level |]
    else
        Array.init levelCount (fun diff ->
            // Copy everything for the hardest level
            if diff = levelCount - 1 then
                Level(sbyte diff,
                      ResizeArray(phraseData.Notes),
                      ResizeArray(phraseData.Chords),
                      ResizeArray(phraseData.Anchors),
                      ResizeArray(phraseData.HandShapes))
            else
                let diffPercent = 100 * diff / levelCount
                let entities = createXmlEntityArray phraseData.Notes phraseData.Chords
                let levelEntities = chooseEntities diffPercent entities
                let notes = levelEntities |> Array.choose (function XmlNote n -> Some n | _ -> None)
                let chords = levelEntities |> Array.choose (function XmlChord n -> Some n | _ -> None)
                let handShapes = chooseHandShapes diffPercent phraseData.HandShapes

                Level(sbyte diff,
                      ResizeArray(notes),
                      ResizeArray(chords),
                      ResizeArray(chooseAnchors levelEntities phraseData.Anchors phraseData.EndTime),
                      ResizeArray(handShapes))     
        )

let generateForArrangement (arr: InstrumentalArrangement) =
    let phraseIterations = arr.PhraseIterations.ToArray()

    // Create the difficulty levels
    let levels =
        phraseIterations
        |> Array.Parallel.map (DataExtractor.getPhraseIterationData arr >> generateLevels arr)

    let maxDiff =
        levels
        |> Array.maxBy (fun l -> l.Length)
        |> fun l -> l.Length

    // Create phrases
    let phrases =
        levels
        |> Array.mapi (fun i lvls ->
            let name =
                if i = 0 then
                    // Usually "COUNT"
                    arr.Phrases.[0].Name
                elif i = levels.Length - 1 then
                    "END"
                elif lvls.Length = 1 then
                    "NG"
                else
                    $"p{i}"

            Phrase(name, byte (lvls.Length - 1), PhraseMask.None)
        )

    // Create phrase iterations
    let newPhraseIterations =
        phraseIterations
        |> Array.mapi (fun i pi ->
            let phrase = phrases.[i]
            let pi = PhraseIteration(pi.Time, i)
            if phrase.MaxDifficulty > 0uy then
                // Create hero levels
                let heroLevels = 
                    HeroLevels(
                        phrase.MaxDifficulty / 3uy,
                        phrase.MaxDifficulty / 2uy,
                        phrase.MaxDifficulty)
                pi.HeroLevels <- heroLevels
            pi
        )

    // Combine the data in the levels
    let combinedLevels =
        Seq.init maxDiff (fun diff -> 
            let level = Level(sbyte diff)
            levels
            |> Array.iter
                (fun lvl ->
                    if diff < lvl.Length then
                        level.Anchors.AddRange(lvl.[diff].Anchors)
                        level.Notes.AddRange(lvl.[diff].Notes)
                        level.HandShapes.AddRange(lvl.[diff].HandShapes)
                        level.Chords.AddRange(lvl.[diff].Chords)
                )
            level
        )

    // TODO: Search for similar phrases
    // Use same name or create linked difficulty

    // TODO: Combine noguitar phrases

    arr.Levels <- ResizeArray(combinedLevels)
    arr.Phrases <- ResizeArray(phrases)
    arr.PhraseIterations <- ResizeArray(newPhraseIterations)

    arr

let generateForFile fileName targetFile =
    let arr =
        InstrumentalArrangement.Load fileName
        |> generateForArrangement

    arr.Save targetFile
