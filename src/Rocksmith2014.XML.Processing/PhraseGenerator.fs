module Rocksmith2014.XML.Processing.PhraseGenerator

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open Rocksmith2014.XML.Processing.Utils

/// Minimum distance required between phrases in milliseconds.
let [<Literal>] private MinimumPhraseSeparation = 2000

let [<Literal>] private MinimumNoGuitarPhraseLength = 2500

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
        | _ -> ra |> ResizeArray.findMaxBy f |> Some

    let findContentEnd (level: Level) =
        let note =
            level.Notes
            |> tryMax (fun x -> x.Time + x.Sustain)

        let chord =
            level.Chords
            |> tryMax (fun x ->
                x.Time + if x.HasChordNotes then x.ChordNotes[0].Sustain else 0)

        let hs =
            level.HandShapes
            |> ResizeArray.tryLast
            |> Option.map (fun x -> x.EndTime)

        maxOfThree 0 note chord hs

    let getEndPhraseTime (level: Level) (arr: Inst) =
        let oldEndPhrase =
            arr.Phrases
            |> Seq.tryFindIndex (fun x -> String.equalsIgnoreCase EndPhraseName x.Name)
            |> Option.bind (fun index ->
                arr.PhraseIterations
                |> ResizeArray.tryFind (fun x -> x.PhraseId = index))

        let noMoreContentTime = findContentEnd level
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

    let getContentStartTime (level: Level) =
        tryFindNextContentTime level 0

    let getFirstPhraseTime (contentStartTime: int) (arr: Inst) =
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

    let addEndPhrase (endPhraseTime: int) (arr: Inst) =
        arr.PhraseIterations.Add(PhraseIteration(endPhraseTime, arr.Phrases.Count))
        arr.Phrases.Add(Phrase(EndPhraseName, 0uy, PhraseMask.None))
        addSection NoGuitar endPhraseTime arr

    let erasePhrasesAndSections (arr: Inst) =
        arr.Phrases.Clear()
        arr.PhraseIterations.Clear()
        arr.Sections.Clear()

    let addFirstPhrase (firstPhraseResult: FirstPhraseResult) (arr: Inst) =
        arr.Phrases.Add(Phrase(FirstPhraseName, 0uy, PhraseMask.None))

        match firstPhraseResult with
        | CreateAtTime time ->
            arr.PhraseIterations.Add(PhraseIteration(time, 0))
        | InsertNewBeat newBeat ->
            arr.Ebeats.Insert(0, newBeat)
            arr.PhraseIterations.Add(PhraseIteration(newBeat.Time, 0))

    let closestToInitial (initial: int) (startTime: int) (endTime: int) =
        if initial - startTime < endTime - initial then
            startTime
        else
            endTime

    let findGoodPhraseTime (chordTemplates: ResizeArray<ChordTemplate>) (level: Level) (initialTime: int) =
        let inline getNoteEndTime (note: Note) =
            if note.Sustain > 0 then
                Some (note.Time + note.Sustain)
            else
                // Add 100ms if the note has no sustain to prevent infinite loop
                Some (note.Time + 100)

        let rec finder loopCount earlier time =
            let loopCount = loopCount + 1
            if loopCount > 20 then
                // Safeguard for infinite loop
                time
            else
                let insideNoteSustain =
                    level.Notes
                    |> ResizeArray.tryFind (fun x -> x.Time < time && x.Time + x.Sustain > time)

                match insideNoteSustain with
                | Some note ->
                    // Time was inside a note's sustain, continue search from new position
                    let newTime = if earlier then note.Time else note.Time + note.Sustain
                    finder loopCount earlier newTime
                | None ->
                    let noteIndexAtCurrentTime = level.Notes.FindIndexByTime(time)

                    let breaksLinkNext =
                        if noteIndexAtCurrentTime < 0 then
                            None
                        else
                            let note = level.Notes[noteIndexAtCurrentTime]

                            let linkNextNoteTime =
                                match findPreviousNoteOnSameString level.Notes noteIndexAtCurrentTime with
                                | Some note, _ when note.IsLinkNext ->
                                    Some (if earlier then note.Time else note.Time + note.Sustain)
                                | _ ->
                                    None

                            match linkNextNoteTime with
                            | Some _ when not earlier ->
                                // Use the end time of the linknext target note when searching for a later time
                                getNoteEndTime note
                            | Some _ ->
                                linkNextNoteTime
                            | None ->
                                // Try to find a chord with linknext chord note on the same string as note at current time
                                let linkNextChordTime =
                                    match findPreviousChordUsingSameString chordTemplates level.Chords note.String note.Time with
                                    | Some (c, _) when c.HasChordNotes && c.ChordNotes.Exists(fun cn -> cn.String = note.String && cn.IsLinkNext) ->
                                        Some c.Time
                                    | _ ->
                                        None

                                match linkNextChordTime with
                                | Some _ when not earlier ->
                                    // Use the linknext target note end time when searching for a later time
                                    getNoteEndTime note
                                | _ ->
                                    linkNextChordTime

                    match breaksLinkNext with
                    | Some newTime ->
                        // Time was on a note head and the previous note on the same string had linknext, continue search
                        finder loopCount earlier newTime
                    | None ->
                        let breaksHandShape =
                            level.HandShapes
                            |> ResizeArray.tryFind (fun x -> x.StartTime < time && x.EndTime > time)

                        match breaksHandShape with
                        | Some hs ->
                            // Time was inside a handshape, continue search
                            let newTime = if earlier then hs.Time else hs.EndTime
                            finder loopCount earlier newTime
                        | None ->
                            time

        let earlierTime = finder 0 true initialTime
        let laterTime = finder 0 false initialTime

        earlierTime, laterTime

    let createPhrasesAndSections (level: Level) (contentStartTime: int) (endPhraseTime: int) (arr: Inst) =
        let mutable measureCounter = 0
        let mutable nextPhraseTime: int option = None

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
                let prevPhraseTime = arr.Sections[arr.Sections.Count - 1].Time

                let canCreatePhrase, time =
                    match nextPhraseTime with
                    | None ->
                        let earlierTime, laterTime = findGoodPhraseTime arr.ChordTemplates level beat.Time
                        let closestToInitialTime = closestToInitial beat.Time earlierTime laterTime

                        // Don't create duplicate or really small phrases
                        if closestToInitialTime - prevPhraseTime > MinimumPhraseSeparation then
                            true, closestToInitialTime
                        elif laterTime - prevPhraseTime > MinimumPhraseSeparation then
                            true, laterTime
                        else
                            false, laterTime
                    | Some t ->
                        // Next phrase time after noguitar phrase has been determined
                        nextPhraseTime <- None
                        true, t

                if canCreatePhrase then
                    let nextContentTime = tryFindNextContentTime level time

                    let sectionType =
                        match nextContentTime with
                        | None ->
                            NoGuitar
                        | Some nextContentTime ->
                            if nextContentTime - time >= MinimumNoGuitarPhraseLength then
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

/// Generates sections and phrases for the arrangement, replacing any existing phrases.
let generate (arr: Inst) =
    let level =
        if arr.Levels.Count = 1 then
            arr.Levels[0]
        else
            arr.GenerateTranscriptionTrack().Result

    match getContentStartTime level with
    | Some contentStartTime ->
        let endPhraseTime = getEndPhraseTime level arr
        let firstPhraseResult = getFirstPhraseTime contentStartTime arr

        erasePhrasesAndSections arr
        addFirstPhrase firstPhraseResult arr
        createPhrasesAndSections level contentStartTime endPhraseTime arr
        addEndPhrase endPhraseTime arr
    | None ->
        // Edge case: there are no notes, chords or handshapes in the arrangement
        ()
