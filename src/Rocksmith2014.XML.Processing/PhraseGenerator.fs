module Rocksmith2014.XML.Processing.PhraseGenerator

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open Utils

[<AutoOpen>]
module private Helpers =
    type Inst = InstrumentalArrangement

    type SectionName = Riff | NoGuitar

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
            |> Seq.tryFindIndex (fun x -> String.equalsIgnoreCase "END" x.Name)
            |> Option.bind (fun index ->
                arr.PhraseIterations
                |> ResizeArray.tryFind (fun x -> x.PhraseId = index))

        let noMoreContentTime = findContentEnd arr
        let endPhraseTime =
            match oldEndPhrase with
            | Some oldEnd ->
                // EOF may place the END phrase poorly in some cases
                if oldEnd.Time < noMoreContentTime then noMoreContentTime else oldEnd.Time
            | None ->
                noMoreContentTime

        // Use the next beat after the content has ended
        arr.Ebeats
        |> ResizeArray.tryFind (fun x -> x.Time >= endPhraseTime)
        |> Option.map (fun x -> x.Time)
        |> Option.defaultValue endPhraseTime

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
                let newBeat = Ebeat(newBeatTime, 0s)
                Error newBeat
        else
            Ok firstBeatTime

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
        arr.Phrases.Add(Phrase("END", 0uy, PhraseMask.None))
        addSection NoGuitar endPhraseTime arr

    let erasePhrasesAndSections (arr: Inst) =
        arr.Phrases.Clear()
        arr.PhraseIterations.Clear()
        arr.Sections.Clear()

    let addFirstPhrase firstPhraseTime (arr: Inst) =
        arr.Phrases.Add(Phrase("COUNT", 0uy, PhraseMask.None))

        match firstPhraseTime with
        | Ok firstTime ->
            arr.PhraseIterations.Add(PhraseIteration(firstTime, 0))
        | Error newBeat ->
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

    let findActiveAnchor (level: Level) time =
        match level.Anchors.FindLast(fun x -> x.Time <= time) with
        | null -> level.Anchors[0]
        | anchor -> anchor

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

                // Don't create duplicate phrases
                if time <> arr.Sections[arr.Sections.Count - 1].Time then
                    let nextContentTime = findNextContent level time

                    let isNoGuitar =
                        match nextContentTime with
                        | None ->
                            true
                        | Some nextContentTime ->
                            if nextContentTime - time >= 2500 then
                                nextPhraseTime <- Some nextContentTime
                                true
                            else
                                false

                    // Check if a new anchor needs to be created
                    let activeAnchor = findActiveAnchor level time
                    if activeAnchor.Time <> time then
                        level.Anchors.InsertByTime(Anchor(activeAnchor.Fret, time, activeAnchor.Width))

                    addPhrase time arr
                    addSection (if isNoGuitar then NoGuitar else Riff) time arr
        )

let generate (arr: Inst) =
    match getContentStartTime arr with
    | Some contentStartTime ->
        let endPhraseTime = getEndPhraseTime arr
        let firstPhraseTime = getFirstPhraseTime contentStartTime arr

        erasePhrasesAndSections arr
        addFirstPhrase firstPhraseTime arr
        createPhrasesAndSections contentStartTime endPhraseTime arr
        addEndPhrase endPhraseTime arr
    | None ->
        // Edge case: there are no notes, chords or handshapes in the arrangement
        ()
