module Rocksmith2014.DD.PhraseCombiner

open System
open System.Collections.Generic
open Rocksmith2014.XML
open DataExtractor
open Comparers

type private CombinationData = { MainId: int; Ids: int array; SameDifficulty: bool }

let private calculateSimilarity fn fc (phrase1: PhraseData) (phrase2: PhraseData) =
    let noteSimilarity = fn phrase1.Notes phrase2.Notes
    let chordSimilarity = fc phrase1.Chords phrase2.Chords
    
    (noteSimilarity + chordSimilarity) / 2.
    |> (round >> int)

let private getSimilarity threshold (phrase1: PhraseData) (phrase2: PhraseData) =
    let simFastest = calculateSimilarity getMaxSimilarityFastest getMaxSimilarityFastest phrase1 phrase2

    let simFast =
        if simFastest < threshold then 0
        else calculateSimilarity (getMaxSimilarityFast noteProjection) (getMaxSimilarityFast chordProjection) phrase1 phrase2

    if simFast < threshold then 0
    else calculateSimilarity (getSimilarityPercent sameNote) (getSimilarityPercent sameChord) phrase1 phrase2

let private findSamePhrases threshold (levelCounts: int array) (iterationData: PhraseData array) =
    // Ignore the first and last phrases (COUNT, END)
    let lastId = iterationData.Length - 1
    let matchedPhrases = HashSet<int>([| 0; lastId |])

    iterationData
    |> Array.mapi (fun i iter ->
        if matchedPhrases.Contains i then
            None
        else
            iterationData
            |> Array.skip (i + 1)
            |> Array.Parallel.choose (fun data ->
                let id = Array.IndexOf(iterationData, data)
                if not <| matchedPhrases.Contains id && getSimilarity threshold iter data >= threshold then
                    Some id
                else
                    None)
            |> function
            | [||] -> None
            | ids ->
                // Save the IDs that were matched
                matchedPhrases.UnionWith ids

                let sameDifficulty =
                    ids
                    |> Array.map (fun id -> levelCounts.[id])
                    |> Utils.allSame

                Some { MainId = i; Ids = ids; SameDifficulty = sameDifficulty })
    |> Array.choose id

let combineSamePhrases (config: GeneratorConfig)
                       (iterationData: PhraseData array)
                       (iterations: PhraseIteration array)
                       (levelCounts: int array) =
    let sameDifficulties, differentDifficulties =
        match config.PhraseSearch with
        | SearchDisabled ->
            [||], [||]
        | WithThreshold threshold ->
            findSamePhrases threshold levelCounts iterationData
            |> Array.partition (fun x -> x.SameDifficulty)

    // Create a mapping to the new phrase IDs after phrases with the same content have been combined
    let newPhraseIds =
        let mutable phraseId = 0
        let idMap = Dictionary<int, int>()

        Array.init iterations.Length (fun id ->
            match Array.tryFind (fun x -> id = x.MainId || Array.contains id x.Ids) sameDifficulties with
            | Some data ->
                match idMap.TryGetValue data.MainId with
                | true, v -> v
                | false, _ ->
                    idMap.Add(data.MainId, phraseId)
                    phraseId <- phraseId + 1
                    phraseId - 1
            | None ->
                phraseId <- phraseId + 1
                phraseId - 1)

    // Create phrase iterations
    let newPhraseIterations =
        (iterations, levelCounts)
        ||> Array.mapi2 (fun i oldPi levelCount ->
            let pi = PhraseIteration(oldPi.Time, newPhraseIds.[i])

            let maxDiff = byte (levelCount - 1)
            if maxDiff > 0uy then
                // Create hero levels
                let easy = byte <| round (float maxDiff / 4.)
                let medium = byte <| round (float maxDiff / 2.)
                pi.HeroLevels <- HeroLevels(easy, medium, maxDiff)
            pi)

    // Create linked difficulties
    let linkedDiffs =
        differentDifficulties
        |> Array.map (fun data ->
            let ids = seq {
                yield newPhraseIds.[data.MainId]
                yield! data.Ids |> Seq.map (fun x -> newPhraseIds.[x]) }
            NewLinkedDiff(-1y, ids))

    // Create phrases
    let phrases =
        let firstId = newPhraseIterations.[0].PhraseId
        let lastId = (Array.last newPhraseIterations).PhraseId
        let createdPhraseIds = HashSet<int>()
        let mutable counter = 0

        newPhraseIterations
        |> Array.choose (fun pi ->
            if createdPhraseIds.Contains pi.PhraseId then
                None
            else
                let maxDiff = pi.HeroLevels.Hard

                let name =
                    if pi.PhraseId = firstId then "COUNT"
                    elif pi.PhraseId = lastId then "END"
                    elif maxDiff = 0uy then "NOGUITAR"
                    else
                        counter <- counter + 1
                        $"p{counter - 1}"

                createdPhraseIds.Add pi.PhraseId |> ignore
                Some <| Phrase(name, maxDiff, PhraseMask.None))

    phrases, newPhraseIterations, linkedDiffs
