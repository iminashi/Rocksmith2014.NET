module Rocksmith2014.XML.Processing.HandShapeAdjuster

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open Utils

let private isChordSlideAt (level: Level) time  =
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
    for level in arrangement.Levels do
        for hs in level.HandShapes do
            // Try to find a chord that is within 5ms from the handshape end time
            let chord =
                level.Chords.Find(fun chord ->
                    hs.ChordId = chord.ChordId
                    && (chord.Time >= hs.StartTime && chord.Time <= hs.EndTime)
                    && hs.EndTime - chord.Time <= 10)

            if notNull chord then
                let time = chord.Time
                let beat1, beat2 = findBeats arrangement.Ebeats time
                let note16th = (beat2.Time - beat1.Time) / 4

                // Add 1ms to skip this chord
                let nextTimeOpt = tryFindNextContentTime level (time + 1)
                let newEndTime = hs.EndTime + note16th

                hs.EndTime <-
                    match nextTimeOpt with
                    | Some nextTime when newEndTime >= nextTime ->
                        // If the new end time overlaps with the next note, chord or handshape,
                        // extend the handshape by half the distance to that note/chord/hs
                        hs.EndTime + (nextTime - hs.EndTime) / 2
                    | _ ->
                        newEndTime

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
