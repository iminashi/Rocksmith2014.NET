module Rocksmith2014.XML.Processing.PhraseGenerator

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open Utils

/// Minimum distance required between phrases in milliseconds.
let [<Literal>] private MinimumPhraseSeparation = 2000

let [<Literal>] private FirstPhraseName = "COUNT"
let [<Literal>] private EndPhraseName = "END"

[<AutoOpen>]
module private Helpers =
    type Inst = InstrumentalArrangement

    type SectionName = Riff | NoGuitar
    type FirstPhraseResult =
        | CreateAtTime of int
        | InsertNewBeat of Ebeat

    let maxOfThree defaultValue o1 o2 o3 =
        [ o1; o2; o3 ]
        |> List.choose id
        |> List.tryMax
        |> Option.defaultValue defaultValue

    let tryMax f (ra: ResizeArray<_>) =
        match ra.Count with
        | 0 -> None
        | _ -> ra |> Seq.map f |> Seq.max |> Some

    let findContentEnd (arr: Inst) =
        let note =
            arr.Levels[0].Notes
            |> tryMax (fun x -> x.Time + x.Sustain)

        let chord =
            arr.Levels[0].Chords
            |> tryMax (fun x ->
                x.Time + if x.HasChordNotes then x.ChordNotes[0].Sustain else 0)

        let hs =
            arr.Levels[0].HandShapes
            |> ResizeArray.tryLast
            |> Option.map (fun x -> x.EndTime)

        maxOfThree 0 note chord hs

    let getEndPhraseTime (arr: Inst) =
        let oldEndPhrase =
            arr.Phrases
            |> Seq.tryFindIndex (fun x -> String.equalsIgnoreCase EndPhraseName x.Name)
            |> Option.bind (fun index ->
                arr.PhraseIterations
                |> ResizeArray.tryFind (fun x -> x.PhraseId = index))

        let noMoreContentTime = findContentEnd arr
        let endPhraseTime =
            match oldEndPhrase with
            | Some oldEnd ->
                // EOF may place the END phrase poorly in some rare cases
                if oldEnd.Time < noMoreContentTime then noMoreContentTime else oldEnd.Time
            | None ->
                noMoreContentTime

        if endPhraseTime <> noMoreContentTime then
            // The old END phrase time is usable
            endPhraseTime
        else
            // Use the next beat after the content has ended
            arr.Ebeats
            |> Seq.tryFindIndexBack (fun b -> b.Time <= endPhraseTime)
            |> Option.bind (fun i -> arr.Ebeats |> ResizeArray.tryItem (i + 1))
            |> function
                | Some b ->
                    b.Time
                | None ->
                    // If a beat is not found, create the phrase 100ms after content end time
                    min (endPhraseTime + 100) (arr.MetaData.SongLength - 100)

    let findNextContent (level: Level) time =
        let tryFindNext (ra: ResizeArray<#IHasTimeCode>) =
            ra
            |> ResizeArray.tryFind (fun x -> x.Time >= time)
            |> Option.map (fun x -> x.Time)

        let noteTime = tryFindNext level.Notes
        let chordTime = tryFindNext level.Chords
        let handShapeTime = tryFindNext level.HandShapes

        Option.minOfMany [ noteTime; chordTime; handShapeTime ]

    let getContentStartTime (arr: Inst) =
        findFirstLevelWithContent arr
        |> Option.bind (fun level -> findNextContent level 0)

    let getFirstPhraseTime contentStartTime (arr: Inst) =
        let firstBeatTime = arr.Ebeats[0].Time

        if firstBeatTime = contentStartTime then
            if contentStartTime = 0 then
                failwith "Phrase generation failed:\nThere is no room for an empty phrase before the arrangement content starts."
            else
                let beatLength = arr.Ebeats[1].Time - firstBeatTime
                let newBeatTime = max 0 (firstBeatTime - beatLength)
                InsertNewBeat(Ebeat(newBeatTime, 0s))
        else
            CreateAtTime firstBeatTime

    let addPhrase =
        let mutable number = 0s

        fun time (arr: Inst) ->
            arr.PhraseIterations.Add(PhraseIteration(time, arr.Phrases.Count))
            arr.Phrases.Add(Phrase($"p%i{number}", 0uy, PhraseMask.None))
            number <- number + 1s

    let addSection =
        let mutable riffNumber = 1s
        let mutable ngSectionNumber = 1s

        fun name time (arr: Inst) ->
            match name with
            | Riff ->
                arr.Sections.Add(Section("riff", time, riffNumber))
                riffNumber <- riffNumber + 1s
            | NoGuitar ->
                arr.Sections.Add(Section("noguitar", time, ngSectionNumber))
                ngSectionNumber <- ngSectionNumber + 1s

    let addEndPhrase endPhraseTime (arr: Inst) =
        arr.PhraseIterations.Add(PhraseIteration(endPhraseTime, arr.Phrases.Count))
        arr.Phrases.Add(Phrase(EndPhraseName, 0uy, PhraseMask.None))
        addSection NoGuitar endPhraseTime arr

    let erasePhrasesAndSections (arr: Inst) =
        arr.Phrases.Clear()
        arr.PhraseIterations.Clear()
        arr.Sections.Clear()

    let addFirstPhrase firstPhraseResult (arr: Inst) =
        arr.Phrases.Add(Phrase(FirstPhraseName, 0uy, PhraseMask.None))

        match firstPhraseResult with
        | CreateAtTime time ->
            arr.PhraseIterations.Add(PhraseIteration(time, 0))
        | InsertNewBeat newBeat ->
            arr.Ebeats.Insert(0, newBeat)
            arr.PhraseIterations.Add(PhraseIteration(newBeat.Time, 0))

    let closestToInitial initial startTime endTime =
        if initial - startTime < endTime - initial then
            startTime
        else
            endTime

    let findGoodPhraseTime (level: Level) initialTime =
        let outsideNoteSustainTime =
            level.Notes
            |> ResizeArray.tryFind (fun x -> x.Time < initialTime && x.Time + x.Sustain > initialTime)
            |> Option.map (fun x -> closestToInitial initialTime x.Time (x.Time + x.Sustain))

        let outsideNoteLinkNextTime =
            let time =
                outsideNoteSustainTime
                |> Option.defaultValue initialTime

            level.Notes
            |> ResizeArray.tryFind (fun x -> x.IsLinkNext && x.Time + x.Sustain = time)
            |> Option.map (fun x -> x.Time)

        let outsideHandShapeTime =
            level.HandShapes
            |> ResizeArray.tryFind (fun x -> x.StartTime < initialTime && x.EndTime > initialTime)
            |> Option.map (fun x -> closestToInitial initialTime x.StartTime x.EndTime)

        let outsideChordLinkNextTime =
            let time =
                outsideHandShapeTime
                |> Option.defaultValue initialTime

            level.Chords
            |> ResizeArray.tryFind (fun x ->
                x.IsLinkNext
                && x.HasChordNotes
                && x.ChordNotes.Exists(fun n -> n.Time + n.Sustain = time))
            |> Option.map (fun x -> x.Time)

        outsideNoteLinkNextTime
        |> Option.orElse outsideNoteSustainTime
        |> Option.orElse outsideChordLinkNextTime
        |> Option.orElse outsideHandShapeTime
        |> Option.defaultValue initialTime

    let createPhrasesAndSections contentStartTime endPhraseTime (arr: Inst) =
        let mutable measureCounter = 0
        let mutable nextPhraseTime: int option = None
        let level = arr.Levels[0]

        // Add first phrase/section at content start time
        addPhrase contentStartTime arr
        addSection Riff contentStartTime arr

        arr.Ebeats
        |> Seq.skipWhile (fun x -> x.Time < contentStartTime)
        |> Seq.takeWhile (fun x -> x.Time < endPhraseTime)
        |> Seq.iter (fun beat ->
            if beat.Measure <> -1s && nextPhraseTime.IsNone then
                measureCounter <- measureCounter + 1

            if measureCounter >= 9 || nextPhraseTime |> Option.exists (fun x -> beat.Time >= x) then
                measureCounter <- 1
                let time =
                    match nextPhraseTime with
                    | None ->
                        findGoodPhraseTime level beat.Time
                    | Some t ->
                        nextPhraseTime <- None
                        t

                let prevPhraseTime = arr.Sections[arr.Sections.Count - 1].Time
                let canCreatePhrase =
                    // Don't create duplicate or really small phrases
                    time - prevPhraseTime > MinimumPhraseSeparation

                if canCreatePhrase then
                    let nextContentTime = findNextContent level time

                    let sectionType =
                        match nextContentTime with
                        | None ->
                            NoGuitar
                        | Some nextContentTime ->
                            if nextContentTime - time >= 2500 then
                                nextPhraseTime <- Some nextContentTime
                                NoGuitar
                            else
                                Riff

                    // Check if a new anchor needs to be created
                    match tryFindActiveAnchor level time with
                    | Some activeAnchor when activeAnchor.Time <> time ->
                        level.Anchors.InsertByTime(Anchor(activeAnchor.Fret, time, activeAnchor.Width))
                    | _ ->
                        ()

                    addPhrase time arr
                    addSection sectionType time arr
        )

let generate (arr: Inst) =
    match getContentStartTime arr with
    | Some contentStartTime ->
        let endPhraseTime = getEndPhraseTime arr
        let firstPhraseResult = getFirstPhraseTime contentStartTime arr

        erasePhrasesAndSections arr
        addFirstPhrase firstPhraseResult arr
        createPhrasesAndSections contentStartTime endPhraseTime arr
        addEndPhrase endPhraseTime arr
    | None ->
        // Edge case: there are no notes, chords or handshapes in the arrangement
        ()
