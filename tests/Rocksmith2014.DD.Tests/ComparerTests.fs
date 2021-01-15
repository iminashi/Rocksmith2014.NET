module Rocksmith2014.DD.Tests.ComparerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.DD.Comparers

[<Tests>]
let comparerTests =
    testList "Same Element Count Tests" [
        testCase "Correct for same notes" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameElementCount sameNote notes1 notes2

            Expect.equal sameCount notes1.Length "Count is correct"

        testCase "Correct for same chords" <| fun _ ->
            let chords1 = [ Chord(Time = 125, ChordId = 50s)
                            Chord(Time = 225, ChordId = 25s, Mask = ChordMask.FretHandMute) ]
            let chords2 = [ Chord(Time = 500, ChordId = 50s)
                            Chord(Time = 625, ChordId = 25s, Mask = ChordMask.FretHandMute) ]

            let sameCount = getSameElementCount sameChord chords1 chords2

            Expect.equal sameCount chords1.Length "Count is correct"

        testCase "Correct when extra note at beginning 1/2" <| fun _ ->
            let notes1 = [ Note(Time = 15, String = 0y, Fret = 0y)
                           Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameElementCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when extra note at beginning 2/2" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 115, String = 0y, Fret = 0y)
                           Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameElementCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when extra note in between 1/2" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 55, String = 1y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameElementCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when extra note in between 2/2" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 155, String = 1y, Fret = 13y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameElementCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when two extra notes in between" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 155, String = 1y, Fret = 13y)
                           Note(Time = 165, String = 1y, Fret = 13y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameElementCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when two extra notes in beginning" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 0y, Fret = 0y)
                           Note(Time = 10, String = 0y, Fret = 0y)
                           Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameElementCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"
    ]
