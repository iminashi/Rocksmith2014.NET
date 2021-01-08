module Rocksmith2014.DD.PhraseCombiner

open Rocksmith2014.XML
open Rocksmith2014.DD.DataExtractor
open System
open System.Collections.Generic

let combineNGPhrases (iterations: PhraseIteration array) (phrases: ResizeArray<Phrase>) =
    let isNGPhrase = Predicate<Phrase>(fun p -> p.Name = "NG")

    let firstIndex = phrases.FindIndex isNGPhrase
    let mutable lastIndex = phrases.FindLastIndex isNGPhrase

    while firstIndex <> -1 && lastIndex <> firstIndex do
        phrases.RemoveAt lastIndex

        // Update the phrase ids of the phrase iterations
        for pi in iterations do
            if pi.PhraseId = lastIndex then pi.PhraseId <- firstIndex
            elif pi.PhraseId > lastIndex then pi.PhraseId <- pi.PhraseId - 1

        lastIndex <- phrases.FindLastIndex isNGPhrase

let private hasSameContent (phrase1: PhraseData) (phrase2: PhraseData) =
    Comparers.sameNotes phrase1.Notes phrase2.Notes
    &&
    Comparers.sameChords phrase1.Chords phrase2.Chords

let private findSamePhrases (iterationData: PhraseData array) =
    let matchedPhrases = HashSet<int>()

    iterationData
    |> Array.mapi (fun i iter ->
        if iter.ChordCount + iter.NoteCount > 0 && not <| matchedPhrases.Contains i then
            let rest = iterationData.[(i + 1)..]
            Array.FindAll(rest, fun x -> hasSameContent iter x)
            |> Array.map (fun x ->
                let id =
                    iterationData
                    |> Array.findIndex (fun y -> Object.ReferenceEquals(x, y))
                matchedPhrases.Add id |> ignore
                id)
        else
            [||]
    )

let combineSamePhrases (iterationData: PhraseData array) (iterations: PhraseIteration array) (phrases: ResizeArray<Phrase>) =
    let samePhraseIds = findSamePhrases iterationData

    // Update the phrase ids of the phrase iterations
    samePhraseIds
    |> Array.iteri (fun index others ->
        if others.Length > 0 then
            for pi in iterations do
                if Array.contains pi.PhraseId others then
                    pi.PhraseId <- index)

    // Remove the redundant phrases
    samePhraseIds
    |> Array.collect (Array.map (fun index -> phrases.[index].Name))
    |> Array.iter (fun name ->
        let phraseId = phrases.FindIndex(fun x -> x.Name = name)
        phrases.RemoveAt phraseId

        // Update the phrase ids of the phrase iterations
        for pi in iterations do
            if pi.PhraseId > phraseId then pi.PhraseId <- pi.PhraseId - 1)
