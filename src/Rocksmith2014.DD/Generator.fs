module Rocksmith2014.DD.Generator

open Rocksmith2014.XML
open Rocksmith2014.XML.Extension
open System.Collections.Generic

let private lockObj = obj ()

/// Returns new finger and fret arrays for the chord template with the given number of notes removed.
let private removeNotes notesToRemove fromHighest (template: ChordTemplate) =
    let fingers = Array.copy template.Fingers
    let frets = Array.copy template.Frets
    let startIndex, change = if fromHighest then 0, 1 else frets.Length - 1, -1

    let rec loop i leftToRemove =
        if leftToRemove = 0 || i < 0 || i > 5 then
            assert (leftToRemove = 0)
            struct {| Fingers = fingers; Frets = frets |}
        elif frets[i] <> -1y then
            fingers[i] <- -1y
            frets[i] <- -1y
            loop (i + change) (leftToRemove - 1)
        else
            loop (i + change) leftToRemove

    loop startIndex notesToRemove

let private applyChordId (templates: ResizeArray<ChordTemplate>) =
    let templateMap = Dictionary<int16 * byte, int16>()

    fun (request: TemplateRequest) ->
        let chordId =
            match templateMap.TryGetValue((request.OriginalId, request.NoteCount)) with
            | true, chordId ->
                chordId
            | false, _ ->
                let template = templates[int request.OriginalId]
                let noteCount = getNoteCount template
                let notesToRemove = noteCount - int request.NoteCount
                let newTemp = removeNotes notesToRemove request.FromHighestNote template

                let chordId =
                    lock lockObj (fun () ->
                        let existing = templates.FindIndex(fun x ->
                            x.DisplayName = template.Name
                            && x.Name = template.DisplayName
                            && x.Frets = newTemp.Frets
                            && x.Fingers = newTemp.Fingers)

                        match existing with
                        | -1 ->
                            ChordTemplate(template.Name, template.DisplayName, newTemp.Fingers, newTemp.Frets)
                            |> templates.Add

                            int16 (templates.Count - 1)
                        | index ->
                            int16 index)

                templateMap.Add((request.OriginalId, request.NoteCount), chordId)
                chordId

        match request.Target with
        | ChordTarget chord -> chord.ChordId <- chordId
        | HandShapeTarget hs -> hs.ChordId <- chordId

let private generateLevels (config: GeneratorConfig) (arr: InstrumentalArrangement) (phraseData: DataExtractor.PhraseData) =
    // Generate one level for empty phrases with only anchors copied
    if phraseData.NoteCount + phraseData.ChordCount = 0 then
        let level = Level(0y)
        level.Anchors.AddRange(phraseData.Anchors)
        [| level |]
    else
        let entities =
            createXmlEntityArrayFromLists phraseData.Notes phraseData.Chords

        let divisions =
            entities
            |> Array.map (fun e ->
                let time = getTimeCode e
                time, BeatDivider.getDivision phraseData time e)

        let notesInDivision =
            divisions
            |> Array.groupBy snd
            |> Array.map (fun (group, elems) -> group, elems.Length)
            |> readOnlyDict

        let noteTimeToDivision = readOnlyDict divisions
        let divisionMap = BeatDivider.createDivisionMap divisions entities.Length

        // Determine the number of levels to generate for this phrase
        let levelCount =
            match config.LevelCountGeneration with
            | LevelCountGeneration.Simple ->
                LevelCounter.getSimpleLevelCount phraseData divisionMap
            | LevelCountGeneration.MLModel ->
                LevelCounter.predictLevelCount (DataExtractor.getPath arr) phraseData
            | LevelCountGeneration.Constant count ->
                count

        let applyChordId' = applyChordId arr.ChordTemplates

        Array.init levelCount (fun diff ->
            // Copy everything for the hardest level
            if diff = levelCount - 1 then
                Level(
                    sbyte diff,
                    ResizeArray(phraseData.Notes),
                    ResizeArray(phraseData.Chords),
                    ResizeArray(phraseData.Anchors),
                    ResizeArray(phraseData.HandShapes)
                )
            else
                let diffPercent = float (diff + 1) / float levelCount

                let levelEntities, templateRequests1 =
                    EntityChooser.choose diffPercent divisionMap noteTimeToDivision notesInDivision arr.ChordTemplates phraseData.HandShapes phraseData.MaxChordStrings entities
                    |> Array.unzip
                let notes = levelEntities |> Array.choose (function XmlNote n -> Some n | _ -> None)
                let chords = levelEntities |> Array.choose (function XmlChord n -> Some n | _ -> None)

                // Ensure that the link next attributes for chords are correct
                // A chord that has a link next attribute, but does not have any notes with link next will crash the game
                chords
                |> Array.iter (fun chord ->
                    if chord.IsLinkNext && chord.ChordNotes.TrueForAll(fun x -> not x.IsLinkNext) then
                        chord.IsLinkNext <- false)

                let handShapes, templateRequests2 =
                    HandShapeChooser.choose diffPercent levelEntities entities phraseData.MaxChordStrings arr.ChordTemplates phraseData.HandShapes
                    |> List.unzip

                let anchors =
                    AnchorChooser.choose levelEntities phraseData.Anchors phraseData.StartTime phraseData.EndTime

                templateRequests1
                |> Seq.append templateRequests2
                |> Seq.choose id
                |> Seq.iter applyChordId'

                Level(
                    sbyte diff,
                    ResizeArray(notes),
                    ResizeArray(chords),
                    ResizeArray(anchors),
                    ResizeArray(handShapes)
                ))

/// Generates DD levels for an arrangement.
let generateForArrangement (config: GeneratorConfig) (arr: InstrumentalArrangement) =
    let phraseIterations = arr.PhraseIterations.ToArray()

    let phraseIterationData =
        phraseIterations
        |> Array.Parallel.map (DataExtractor.getPhraseIterationData arr)

    // Create the difficulty levels
    let levels =
        phraseIterationData
        |> Array.Parallel.map (generateLevels config arr)

    let generatedLevelCount = levels |> Array.map Array.length
    let maxDiff = generatedLevelCount |> Array.max

    // Combine the data in the levels
    let combinedLevels =
        Seq.init maxDiff (fun diff -> 
            let level = Level(sbyte diff)

            levels
            |> Array.iter (fun lvl ->
                if diff < lvl.Length then
                    level.Anchors.AddRange(lvl[diff].Anchors)
                    level.Notes.AddRange(lvl[diff].Notes)
                    level.HandShapes.AddRange(lvl[diff].HandShapes)
                    level.Chords.AddRange(lvl[diff].Chords))
            level)

    let phrases, newPhraseIterations, newLinkedDiffs =
        PhraseCombiner.combineSamePhrases config phraseIterationData phraseIterations generatedLevelCount

    arr.Levels <- ResizeArray(combinedLevels)
    arr.Phrases <- ResizeArray(phrases)
    arr.PhraseIterations <- ResizeArray(newPhraseIterations)
    arr.NewLinkedDiffs <- ResizeArray(newLinkedDiffs)

    arr

/// Generates DD levels for an arrangement loaded from a file and saves it into the target file.
let generateForFile config fileName targetFile =
    let arr =
        InstrumentalArrangement.Load(fileName)
        |> generateForArrangement config

    arr.Save(targetFile)
