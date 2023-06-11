module Rocksmith2014.DD.Generator

open Rocksmith2014.XML
open Rocksmith2014.XML.Extension

let private generateLevels (config: GeneratorConfig) (arr: InstrumentalArrangement) (phraseData: DataExtractor.PhraseData) =
    // Generate one level for empty phrases with only anchors copied
    if phraseData.NoteCount + phraseData.ChordCount = 0 then
        let level = Level(0y)
        level.Anchors.AddRange(phraseData.Anchors)
        [| level |]
    else
        let entities =
            createXmlEntityArrayFromLists phraseData.Notes phraseData.Chords

        let scores =
            entities
            |> Array.map (fun e ->
                let time = getTimeCode e
                time, NoteScorer.getScore phraseData time e)

        let notesWithScore =
            scores
            |> Array.groupBy snd
            |> Array.map (fun (group, elems) -> group, elems.Length)
            |> readOnlyDict

        let noteTimeToScore = readOnlyDict scores
        let scoreMap = NoteScorer.createScoreMap scores entities.Length

        // Determine the number of levels to generate for this phrase
        let levelCount =
            match config.LevelCountGeneration with
            | LevelCountGeneration.Simple ->
                LevelCounter.getSimpleLevelCount phraseData scoreMap
            | LevelCountGeneration.MLModel ->
                LevelCounter.predictLevelCount (DataExtractor.getPath arr) phraseData
            | LevelCountGeneration.Constant count ->
                count

        let applyChordId = TemplateRequestApplier.applyChordId arr.ChordTemplates

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
                    EntityChooser.choose diffPercent scoreMap noteTimeToScore notesWithScore arr.ChordTemplates phraseData.HandShapes phraseData.MaxChordStrings entities
                    |> Array.unzip
                let notes = levelEntities |> Array.choose (function XmlNote n -> Some n | _ -> None)
                let chords = levelEntities |> Array.choose (function XmlChord c -> Some c | _ -> None)

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
                |> Seq.iter applyChordId

                Level(
                    sbyte diff,
                    ResizeArray(notes),
                    ResizeArray(chords),
                    ResizeArray(anchors),
                    ResizeArray(handShapes)
                ))

/// Generates DD levels for an arrangement. Returns the mutated arrangement.
let generateForArrangement (config: GeneratorConfig) (arr: InstrumentalArrangement) =
    let phraseIterations = arr.PhraseIterations.ToArray()

    let phraseIterationData =
        phraseIterations
        |> Array.Parallel.map (DataExtractor.getPhraseIterationData arr)

    // Create the difficulty levels
    let levelDataForPhrases =
        phraseIterationData
        |> Array.Parallel.map (generateLevels config arr)

    let generatedLevelCounts = levelDataForPhrases |> Array.map Array.length
    let maxDiff = generatedLevelCounts |> Array.max

    // Combine the data in the levels
    let combinedLevels =
        Seq.init maxDiff (fun diff ->
            let level = Level(sbyte diff)

            levelDataForPhrases
            |> Array.iter (fun phraseData ->
                if diff < phraseData.Length then
                    level.Anchors.AddRange(phraseData[diff].Anchors)
                    level.Notes.AddRange(phraseData[diff].Notes)
                    level.HandShapes.AddRange(phraseData[diff].HandShapes)
                    level.Chords.AddRange(phraseData[diff].Chords))
            level)

    let phrases, newPhraseIterations, newLinkedDiffs =
        PhraseCombiner.combineSamePhrases config phraseIterationData phraseIterations generatedLevelCounts

    arr.Levels <- ResizeArray(combinedLevels)
    arr.Phrases <- ResizeArray(phrases)
    arr.PhraseIterations <- ResizeArray(newPhraseIterations)
    arr.NewLinkedDiffs <- ResizeArray(newLinkedDiffs)

    arr

/// Generates DD levels for an arrangement loaded from a file and saves it into the target file.
let generateForFile config sourcePath targetPath =
    let arr =
        InstrumentalArrangement.Load(sourcePath)
        |> generateForArrangement config

    arr.Save(targetPath)
