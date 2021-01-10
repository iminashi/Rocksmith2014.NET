module Rocksmith2014.DD.PhraseCombiner

open Rocksmith2014.XML
open Rocksmith2014.DD.DataExtractor
open System
open System.Collections.Generic

type private CombinationData = { MainId: int; Ids: int array; SameDifficulty: bool }

let private hasSameContent (phrase1: PhraseData) (phrase2: PhraseData) =
    Comparers.sameNotes phrase1.Notes phrase2.Notes
    &&
    Comparers.sameChords phrase1.Chords phrase2.Chords

let private findSamePhrases (levelCounts: int array) (iterationData: PhraseData array) =
    // TODO: Improve search for similar phrases

    let matchedPhrases = HashSet<int>()
    let lastId = iterationData.Length - 1

    iterationData
    |> Array.mapi (fun i iter ->
        // Ignore the first and last phrases (COUNT, END)
        if (not <| matchedPhrases.Contains i) && i <> 0 && i <> lastId then
            let rest = iterationData.[(i + 1)..]
            let ids =
                Array.FindAll(rest, fun x -> hasSameContent iter x)
                |> Array.map (fun x ->
                    let id =
                        iterationData
                        |> Array.findIndex (fun y -> Object.ReferenceEquals(x, y))
                    matchedPhrases.Add id |> ignore
                    id)
                |> Array.filter (fun x -> x <> 0 && x <> lastId)

            if ids.Length = 0 then
                None
            else
                let sameDifficulty =
                    let difficulties =
                        ids
                        |> Array.map (fun i -> levelCounts.[i])
                    let first = difficulties.[0]
                    
                    difficulties |> Array.forall ((=) first)

                Some { MainId = i; Ids = ids; SameDifficulty = sameDifficulty }
        else
            None)
    |> Array.choose id

let combineSamePhrases (iterationData: PhraseData array) (iterations: PhraseIteration array) (levelCounts: int array) =
    let samePhraseIds = findSamePhrases levelCounts iterationData

    let sameDifficulties, differentDifficulties =
        samePhraseIds
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
                pi.HeroLevels <- HeroLevels(maxDiff / 3uy, maxDiff / 2uy, maxDiff)
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
                Some(Phrase(name, maxDiff, PhraseMask.None))
        )

    phrases, newPhraseIterations, linkedDiffs
