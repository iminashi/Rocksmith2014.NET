module Rocksmith2014.XML.Processing.HandShapeAdjuster

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions

let private isChordSlideAt (level: Level) time  =
    match level.Chords.FindByTime(time) with
    | null ->
        false
    | chord when chord.IsLinkNext ->
        chord.HasChordNotes
        && chord.ChordNotes.Exists(fun cn -> cn.IsSlide)
    | _ ->
        false

/// Shortens the lengths of handshapes that are too close to the next one.
let improve (arrangement: InstrumentalArrangement) =
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
                let beat2Index =
                    let i = arrangement.Ebeats.FindIndex(fun b -> b.Time > precedingEndTime)
                    // For handshapes that come after the last beat (bad XML) use the last beat
                    if i = -1 then arrangement.Ebeats.Count - 1 else i

                let beat1 = arrangement.Ebeats[beat2Index - 1]
                let beat2 = arrangement.Ebeats[beat2Index]

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
