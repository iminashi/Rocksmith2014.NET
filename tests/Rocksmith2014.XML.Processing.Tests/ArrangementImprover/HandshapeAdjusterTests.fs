module Rocksmith2014.XML.Processing.Tests.HandshapeAdjusterTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let handShapeAdjusterTests =
    testList "Arrangement Improver (Hand Shape Adjuster)" [
        testCase "Shortens handshape length" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(ChordId = 1s, Time = 2000) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let hs2 = HandShape(1s, 2000, 3000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.shortenHandshapes arr

            Expect.isTrue (hs1.EndTime < 2000) "Handshape was shortened"
            Expect.isTrue (hs1.StartTime < hs1.EndTime) "Handshape end comes after the start"

        testCase "Shortens length of a really short handshape" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1950); Chord(ChordId = 1s, Time = 2000) ]
            let hs1 = HandShape(0s, 1950, 2000)
            let hs2 = HandShape(1s, 2000, 3000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.shortenHandshapes arr

            Expect.isTrue (hs1.EndTime < 2000) "Handshape was shortened"
            Expect.isTrue (hs1.StartTime < hs1.EndTime) "Handshape end comes after the start"

        testCase "Does not fail on handshapes that exceed the last beat" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let hs1 = HandShape(0s, 2500, 2600)
            let hs2 = HandShape(0s, 2600, 2800)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.shortenHandshapes arr

            Expect.isTrue (hs1.EndTime < 2600) "Handshape was shortened"

        testCase "Lengthens handshape when chord is at end of handshape" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2000) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2250 "Handshape was lengthened by time of 16th note"

        testCase "Lengthens handshape when chord is at end of handshape and next note is very close" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2000) ]
            let notes = ![ Note(Time = 2050) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, Notes = notes, HandShapes = ![ hs1 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2025 "Handshape was lengthened"

        testCase "Lengthens handshape when chord is at end of handshape and next anchor is very close" <| fun _ ->
            let beats = ![ Ebeat(500, -1s); Ebeat(1000, -1s); Ebeat(1500, -1s); Ebeat(2500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2000) ]
            let anchors = ![ Anchor(4y, 2100) ]
            let hs1 = HandShape(0s, 1000, 2000)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, Anchors = anchors, HandShapes = ![ hs1 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2050 "Handshape was lengthened"

        testCase "Test handshape handshape lengthening with two handshapes (lengthen both)" <| fun _ ->
            let beats = ![ for i in 1..20 -> Ebeat(i * 500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2090); Chord(Time = 3000); Chord(Time = 3500) ]
            let hs1 = HandShape(0s, 1000, 2100)
            let hs2 = HandShape(0s, 3000, 3500)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2225 "1st handshape was lengthened"
            Expect.equal hs2.EndTime 3625 "2nd handshape was lengthened"

        testCase "Test handshape handshape lengthening with two handshapes (lengthen second)" <| fun _ ->
            let beats = ![ for i in 1..20 -> Ebeat(i * 500, -1s) ]
            let chords = ![ Chord(Time = 1000); Chord(Time = 2000); Chord(Time = 2150); Chord(Time = 2500) ]
            let hs1 = HandShape(0s, 1000, 2100)
            let hs2 = HandShape(0s, 2150, 2500)
            let arr =
                InstrumentalArrangement(
                    Ebeats = beats,
                    Levels = ![ Level(Chords = chords, HandShapes = ![ hs1; hs2 ]) ]
                )

            HandShapeAdjuster.lengthenHandshapes arr

            Expect.equal hs1.EndTime 2100 "1st handshape was not lengthened"
            Expect.equal hs2.EndTime 2625 "2nd handshape was lengthened"
    ]
