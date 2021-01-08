module Rocksmith2014.DD.Generator

open Rocksmith2014.XML
open System
open LevelCounter
open System.Collections.Generic

let private lockObj = obj()

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
                    let sustain = getSustain e
                    time + errorMargin >= a.Time && time < endTime
                    ||
                    time + sustain + errorMargin >= a.Time && time + sustain < endTime)
                then
                    a::result
                else
                    result
            add result tail

    add [] anchors
    |> List.rev

let private applyChordId (templates: ResizeArray<ChordTemplate>) =
    let templateMap = Dictionary<int16 * byte, int16>()

    fun (request: TemplateRequest) ->
        let id = 
            match templateMap.TryGetValue((request.OriginalId, request.NoteCount)) with
            | true, id ->
                id
            | false, _ ->
                let template = templates.[int request.OriginalId]
                let noteCount = getNoteCount template

                let mutable removeNotes = noteCount - int request.NoteCount
                let newFingers, newFrets =
                    let fingers = Array.copy template.Fingers
                    let frets = Array.copy template.Frets
                    for i = frets.Length - 1 downto 0 do
                        if frets.[i] <> -1y && removeNotes > 0 then
                            removeNotes <- removeNotes - 1
                            fingers.[i] <- -1y
                            frets.[i] <- -1y
                    fingers, frets

                let id =
                    lock lockObj (fun _ ->
                        let existing = templates.FindIndex(fun x ->
                            x.DisplayName = template.Name
                            && x.Name = template.DisplayName
                            && x.Frets = newFrets
                            && x.Fingers = newFingers)

                        match existing with
                        | -1 ->
                            let id = int16 templates.Count
                            let newTemplate = ChordTemplate(template.Name, template.DisplayName, newFingers, newFrets)
                            templates.Add newTemplate
                            id
                        | index ->
                            int16 index)
                templateMap.Add((request.OriginalId, request.NoteCount), id)
                id

        match request.Target with
        | ChordTarget chord -> chord.ChordId <- id
        | HandShapeTarget hs -> hs.ChordId <- id

let private generateLevels (arr: InstrumentalArrangement) (phraseData: DataExtractor.PhraseData) =
    // Determine the number of levels to generate for this phrase
    let levelCount = predictLevelCount (DataExtractor.getPath arr) phraseData

    if phraseData.NoteCount + phraseData.ChordCount = 0 then
        // Copy anchors only
        let level = Level 0y
        level.Anchors.AddRange phraseData.Anchors
        [| level |]
    else
        let entities = createXmlEntityArray phraseData.Notes phraseData.Chords
        let divisions =
            entities
            |> Array.map (fun e ->
                let time = getTimeCode e
                time, BeatDivider.getDivision phraseData.Beats time)

        let notesInDivision =
            divisions
            |> Array.groupBy snd
            |> Array.map (fun (group, elems) -> group, elems.Length)
            |> Map.ofArray

        let applyChordId' = applyChordId arr.ChordTemplates

        Array.init levelCount (fun diff ->
            // Copy everything for the hardest level
            if diff = levelCount - 1 then
                Level(sbyte diff,
                      ResizeArray(phraseData.Notes),
                      ResizeArray(phraseData.Chords),
                      ResizeArray(phraseData.Anchors),
                      ResizeArray(phraseData.HandShapes))
            else
                let diffPercent = byte <| 100 * (diff + 1) / levelCount

                let levelEntities, templateRequests1 =
                    EntityChooser.choose diffPercent divisions notesInDivision arr.ChordTemplates entities
                    |> Array.unzip
                let notes = levelEntities |> Array.choose (function XmlNote n -> Some n | _ -> None)
                let chords = levelEntities |> Array.choose (function XmlChord n -> Some n | _ -> None)

                let handShapes, templateRequests2 =
                    HandShapeChooser.choose diffPercent levelEntities entities arr.ChordTemplates phraseData.HandShapes
                    |> List.unzip

                let anchors = chooseAnchors levelEntities phraseData.Anchors phraseData.EndTime

                templateRequests1
                |> Seq.append templateRequests2
                |> Seq.choose id
                |> Seq.iter applyChordId'

                Level(sbyte diff,
                      ResizeArray(notes),
                      ResizeArray(chords),
                      ResizeArray(anchors),
                      ResizeArray(handShapes))     
        )

let generateForArrangement (arr: InstrumentalArrangement) =
    let phraseIterations = arr.PhraseIterations.ToArray()
    let phraseIterationData =
        phraseIterations
        |> Array.Parallel.map (DataExtractor.getPhraseIterationData arr)

    // Create the difficulty levels
    let levels =
        phraseIterationData
        |> Array.Parallel.map (generateLevels arr)

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

    // TODO: Improve search for similar phrases
    // Use same name or create linked difficulty

    let phrases = ResizeArray(phrases)
    PhraseCombiner.combineSamePhrases phraseIterationData newPhraseIterations phrases
    PhraseCombiner.combineNGPhrases newPhraseIterations phrases

    arr.Levels <- ResizeArray(combinedLevels)
    arr.Phrases <- phrases
    arr.PhraseIterations <- ResizeArray(newPhraseIterations)

    arr

let generateForFile fileName targetFile =
    let arr =
        InstrumentalArrangement.Load fileName
        |> generateForArrangement

    arr.Save targetFile
