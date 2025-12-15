module Rocksmith2014.XML.Processing.HandShapeAdjuster

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open Rocksmith2014.XML.Processing.Utils

let private isChordSlideAt (level: Level) (time: int)  =
    match level.Chords.FindByTime(time) with
    | null ->
        false
    | chord when chord.IsLinkNext ->
        chord.HasChordNotes
        && chord.ChordNotes.Exists(fun cn -> cn.IsSlide)
    | _ ->
        false

let private findBeats (beats: ResizeArray<Ebeat>) (time: int) =
    let nextBeat =
        let i = beats.FindIndex(fun b -> b.Time > time)
        // For handshapes that come after the last beat (bad XML) use the last beat
        if i = -1 then beats.Count - 1 else i

    let beat1 = beats[nextBeat - 1]
    let beat2 = beats[nextBeat]

    beat1, beat2

/// Lengthens handshapes that end with a chord.
let lengthenHandshapes (arrangement: InstrumentalArrangement) =
    let rec findIndexOfLastChordMaybeInsideHandshape (index: int) (chords: ResizeArray<Chord>) (hs: HandShape) =
        if index >= chords.Count then
            // Reached the last chord in the arrangement (assume that it is within a handshape)
            index - 1
        else
            if chords[index].Time > hs.EndTime then
                // Handshape end passed, return the index of the previous chord
                index - 1
            else
                // Not inside the handshape yet or within the handshape, keep searching
                findIndexOfLastChordMaybeInsideHandshape (index + 1) chords hs

    let rec adjustHandShapes (level: Level) (chordIndex: int) (hsIndex: int) =
        if hsIndex < level.HandShapes.Count && chordIndex < level.Chords.Count then
            let hs = level.HandShapes[hsIndex]
            let lastChordIndex = findIndexOfLastChordMaybeInsideHandshape chordIndex level.Chords hs

            if lastChordIndex >= 0 then
                let chord = level.Chords[lastChordIndex]
                let time = chord.Time

                // Lengthen the handshape if the chord is within 10ms from the handshape end time
                // Check that chord actually is within the handshape,
                // since the search logic currently does not consider handshapes with notes only
                if time >= hs.StartTime && time <= hs.EndTime && hs.EndTime - time <= 10 then
                    let beat1, beat2 = findBeats arrangement.Ebeats time
                    let note16th = (beat2.Time - beat1.Time) / 4

                    let nextAnchorTimeOpt =
                        level.Anchors.Find(fun a -> a.Time > time)
                        |> Option.ofObj
                        |> Option.map (fun a -> a.Time)

                    let nextPhraseTimeOpt =
                        arrangement.PhraseIterations.Find(fun p -> p.Time > time)
                        |> Option.ofObj
                        |> Option.map (fun p -> p.Time)

                    // Add 1ms to skip this chord
                    let nextContentTimeOpt = tryFindNextContentTime level (time + 1)
                    let newEndTime = hs.EndTime + note16th

                    let nextTimeOpt =
                        Option.minOfMany [ nextAnchorTimeOpt; nextPhraseTimeOpt; nextContentTimeOpt ]

                    hs.EndTime <-
                        match nextTimeOpt with
                        | Some nextTime when newEndTime >= nextTime ->
                            // If the new end time overlaps with the next note, chord or handshape,
                            // extend the handshape by half the distance to that note/chord/hs
                            hs.EndTime + (nextTime - hs.EndTime) / 2
                        | _ ->
                            newEndTime

                adjustHandShapes level lastChordIndex (hsIndex + 1)

    for level in arrangement.Levels do
        adjustHandShapes level 0 0

/// Shortens the lengths of handshapes that are too close to the next one.
let shortenHandshapes (arrangement: InstrumentalArrangement) =
    for level in arrangement.Levels do
        let handShapes = level.HandShapes

        for i = 1 to handShapes.Count - 1 do
            let followingStartTime = handShapes[i].StartTime
            let followingEndTime = handShapes[i].EndTime

            let precedingHandshape = handShapes[i - 1]
            let precedingStartTime = precedingHandshape.StartTime
            let precedingEndTime = precedingHandshape.EndTime

            // Ignore nested handshapes
            if precedingEndTime < followingEndTime then
                let beat1, beat2 = findBeats arrangement.Ebeats precedingEndTime

                let note32nd = (beat2.Time - beat1.Time) / 8
                let shortenBy16thNote =
                    // If it is a chord slide and the handshape length is an 8th note or longer
                    isChordSlideAt level precedingStartTime
                    && precedingEndTime - precedingStartTime > note32nd * 4

                let minDistance =
                    // Shorten the min. distance required for 32nd notes or smaller
                    if precedingEndTime - precedingStartTime <= note32nd then
                        (beat2.Time - beat1.Time) / 12
                    elif shortenBy16thNote then
                        note32nd * 2
                    else
                        note32nd

                let currentDistance = followingStartTime - precedingEndTime

                if currentDistance < minDistance then
                    let newEndTime =
                        let time = followingStartTime - minDistance
                        // For very small note values
                        if time <= precedingStartTime then
                            precedingStartTime + (precedingEndTime - precedingStartTime) / 2
                        else
                            time

                    precedingHandshape.EndTime <- newEndTime
