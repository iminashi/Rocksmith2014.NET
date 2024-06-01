module Rocksmith2014.DD.Tests.ComparerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.DD.Comparers


[<Tests>]
let maxSimilarityFastTests =
    testList "getMaxSimilarityFast Tests" [
        testCase "Correct similarity percent for two elements" <| fun _ ->
            let l1 = [ 1; 2 ]
            let l2 = [ 9; 1 ]

            let similarity = getMaxSimilarityFast id l1 l2

            Expect.equal similarity 50.0 "50% was returned"

        testCase "Correct similarity percent for three elements" <| fun _ ->
            let l1 = [ 1; 2; 3 ]
            let l2 = [ 9; 1; 5 ]

            let similarity = getMaxSimilarityFast id l1 l2

            Expect.floatClose Accuracy.high similarity 33.33333 "33.3% was returned"

        testCase "Correct similarity percent for four elements" <| fun _ ->
            let l1 = [ 1; 2; 3 ]
            let l2 = [ 9; 1; 5; 3 ]

            let similarity = getMaxSimilarityFast id l1 l2

            Expect.floatClose Accuracy.high similarity 66.66666 "66.6% was returned"
    ]

[<Tests>]
let comparerTests =
    testList "Same Item Count Tests" [
        testCase "Count is zero for different notes" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 1y)
                           Note(Time = 50, String = 0y, Fret = 5y)
                           Note(Time = 75, String = 1y, Fret = 9y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 2y)
                           Note(Time = 175, String = 0y, Fret = 4y)
                           Note(Time = 200, String = 1y, Fret = 8y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 0 "Count is correct"

        testCase "Correct for same notes" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount notes1.Length "Count is correct"

        testCase "Correct for same chords" <| fun _ ->
            let chords1 = [ Chord(Time = 125, ChordId = 50s)
                            Chord(Time = 225, ChordId = 25s, Mask = ChordMask.FretHandMute) ]
            let chords2 = [ Chord(Time = 500, ChordId = 50s)
                            Chord(Time = 625, ChordId = 25s, Mask = ChordMask.FretHandMute) ]

            let sameCount = getSameItemCount sameChord chords1 chords2

            Expect.equal sameCount chords1.Length "Count is correct"

        testCase "Correct when extra note at the beginning 1/2" <| fun _ ->
            let notes1 = [ Note(Time = 15, String = 0y, Fret = 0y)
                           Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when extra note at the beginning 2/2" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 115, String = 0y, Fret = 0y)
                           Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when extra note in between 1/2" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 55, String = 1y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when extra note in between 2/2" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 155, String = 1y, Fret = 13y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when two extra notes in between (one after the other)" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 155, String = 1y, Fret = 13y)
                           Note(Time = 165, String = 1y, Fret = 13y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when two extra notes in between (here and there)" <| fun _ ->
            let notes1 = [ Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 25, String = 5y, Fret = 22y)
                           Note(Time = 50, String = 1y, Fret = 13y)
                           Note(Time = 25, String = 5y, Fret = 22y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 155, String = 1y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when two extra notes at the beginning" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 0y, Fret = 0y)
                           Note(Time = 10, String = 0y, Fret = 0y)
                           Note(Time = 25, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when starting note is different 1/3" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 0y, Fret = 10y)
                           Note(Time = 5, String = 0y, Fret = 14y)
                           Note(Time = 50, String = 0y, Fret = 13y)
                           Note(Time = 75, String = 1y, Fret = 12y) ]
            let notes2 = [ Note(Time = 100, String = 1y, Fret = 12y)
                           Note(Time = 125, String = 0y, Fret = 14y)
                           Note(Time = 175, String = 0y, Fret = 13y)
                           Note(Time = 200, String = 1y, Fret = 12y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 3 "Count is correct"

        testCase "Correct when starting note is different 2/3" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 1y, Fret = 7y)
                           Note(Time = 1, String = 0y, Fret = 5y)
                           Note(Time = 2, String = 0y, Fret = 4y)
                           Note(Time = 50, String = 0y, Fret = 5y)
                           Note(Time = 75, String = 0y, Fret = 4y) ]
            let notes2 = [ Note(Time = 100, String = 0y, Fret = 5y)
                           Note(Time = 100, String = 0y, Fret = 5y)
                           Note(Time = 125, String = 0y, Fret = 4y)
                           Note(Time = 175, String = 0y, Fret = 5y)
                           Note(Time = 200, String = 0y, Fret = 4y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 4 "Count is correct"

        testCase "Correct when starting note is different 3/3" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 0y, Fret = 5y)
                           Note(Time = 1, String = 0y, Fret = 5y)
                           Note(Time = 2, String = 0y, Fret = 4y)
                           Note(Time = 50, String = 0y, Fret = 5y)
                           Note(Time = 75, String = 0y, Fret = 4y) ]
            let notes2 = [ Note(Time = 100, String = 1y, Fret = 7y)
                           Note(Time = 100, String = 0y, Fret = 5y)
                           Note(Time = 125, String = 0y, Fret = 4y)
                           Note(Time = 175, String = 0y, Fret = 5y)
                           Note(Time = 200, String = 0y, Fret = 4y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 4 "Count is correct"

        testCase "Calculation does not take forever" <| fun _ ->
            let notes1 =
                [ Note(Time = 67477, Fret = 5y, String = 5y); Note(Time = 67757, Fret = 4y, String = 5y)
                  Note(Time = 68037, Fret = 5y, String = 4y); Note(Time = 68318, Fret = 7y, String = 4y)
                  Note(Time = 68598, Fret = 7y, String = 4y); Note(Time = 68738, Fret = 7y, String = 4y)
                  Note(Time = 68878, Fret = 5y, String = 5y); Note(Time = 69159, Fret = 5y, String = 5y)
                  Note(Time = 69299, Fret = 5y, String = 5y); Note(Time = 69439, Fret = 7y, String = 5y)
                  Note(Time = 69720, Fret = 7y, String = 5y); Note(Time = 69860, Fret = 5y, String = 5y)
                  Note(Time = 70000, Fret = 7y, String = 5y); Note(Time = 70280, Fret = 9y, String = 5y)
                  Note(Time = 70420, Fret = 7y, String = 5y); Note(Time = 70841, Fret = 5y, String = 5y)
                  Note(Time = 71402, Fret = 9y, String = 5y); Note(Time = 71542, Fret = 7y, String = 5y)
                  Note(Time = 71963, Fret = 5y, String = 5y); Note(Time = 72523, Fret = 7y, String = 4y)
                  Note(Time = 72804, Fret = 4y, String = 5y); Note(Time = 73084, Fret = 4y, String = 5y)
                  Note(Time = 73224, Fret = 4y, String = 5y); Note(Time = 73364, Fret = 7y, String = 4y)
                  Note(Time = 73645, Fret = 7y, String = 4y); Note(Time = 73785, Fret = 7y, String = 4y)
                  Note(Time = 73925, Fret = 6y, String = 4y); Note(Time = 74206, Fret = 7y, String = 3y)
                  Note(Time = 74486, Fret = 6y, String = 3y); Note(Time = 74766, Fret = 4y, String = 3y)
                  Note(Time = 75047, Fret = 6y, String = 3y); Note(Time = 75607, Fret = 5y, String = 4y)
                  Note(Time = 76028, Fret = 6y, String = 4y); Note(Time = 76449, Fret = 5y, String = 5y)
                  Note(Time = 76729, Fret = 4y, String = 5y); Note(Time = 77009, Fret = 5y, String = 4y)
                  Note(Time = 77290, Fret = 7y, String = 4y); Note(Time = 77570, Fret = 7y, String = 4y)
                  Note(Time = 77710, Fret = 7y, String = 4y); Note(Time = 77850, Fret = 5y, String = 5y)
                  Note(Time = 78131, Fret = 5y, String = 5y); Note(Time = 78271, Fret = 7y, String = 5y)
                  Note(Time = 78411, Fret = 7y, String = 5y); Note(Time = 78692, Fret = 7y, String = 5y)
                  Note(Time = 78832, Fret = 5y, String = 5y); Note(Time = 78972, Fret = 7y, String = 5y)
                  Note(Time = 79252, Fret = 9y, String = 5y); Note(Time = 79392, Fret = 10y, String = 5y)
                  Note(Time = 79813, Fret = 9y, String = 5y); Note(Time = 80374, Fret = 7y, String = 5y)
                  Note(Time = 80514, Fret = 5y, String = 5y); Note(Time = 81495, Fret = 7y, String = 4y)
                  Note(Time = 81635, Fret = 7y, String = 4y); Note(Time = 81776, Fret = 4y, String = 5y)
                  Note(Time = 82056, Fret = 4y, String = 5y); Note(Time = 82336, Fret = 4y, String = 5y)
                  Note(Time = 82477, Fret = 7y, String = 4y); Note(Time = 82617, Fret = 6y, String = 4y)
                  Note(Time = 82897, Fret = 4y, String = 5y); Note(Time = 82967, Fret = 5y, String = 5y)
                  Note(Time = 83177, Fret = 5y, String = 5y); Note(Time = 83458, Fret = 4y, String = 5y)
                  Note(Time = 83738, Fret = 5y, String = 4y); Note(Time = 84019, Fret = 7y, String = 4y)
                  Note(Time = 85981, Fret = 5y, String = 4y); Note(Time = 86121, Fret = 5y, String = 4y)
                  Note(Time = 86262, Fret = 7y, String = 4y); Note(Time = 86542, Fret = 7y, String = 4y)
                  Note(Time = 86822, Fret = 7y, String = 4y); Note(Time = 86963, Fret = 5y, String = 4y)
                  Note(Time = 87103, Fret = 6y, String = 3y); Note(Time = 87383, Fret = 5y, String = 5y)
                  Note(Time = 87663, Fret = 5y, String = 5y); Note(Time = 87944, Fret = 4y, String = 5y)
                  Note(Time = 88084, Fret = 7y, String = 4y); Note(Time = 88224, Fret = 6y, String = 4y)
                  Note(Time = 88505, Fret = 7y, String = 4y); Note(Time = 89065, Fret = 6y, String = 3y)
                  Note(Time = 89486, Fret = 6y, String = 3y); Note(Time = 89626, Fret = 5y, String = 4y)
                  Note(Time = 89906, Fret = 6y, String = 4y); Note(Time = 90187, Fret = 7y, String = 4y)
                  Note(Time = 90748, Fret = 5y, String = 5y); Note(Time = 91028, Fret = 5y, String = 5y)
                  Note(Time = 91168, Fret = 4y, String = 5y); Note(Time = 91308, Fret = 5y, String = 5y)
                  Note(Time = 91449, Fret = 4y, String = 5y); Note(Time = 91589, Fret = 5y, String = 5y)
                  Note(Time = 91729, Fret = 4y, String = 5y); Note(Time = 91869, Fret = 7y, String = 4y) ]

            let notes2 =
                [ Note(Time = 213563, Fret = 7y, String = 4y); Note(Time = 213843, Fret = 7y, String = 4y)
                  Note(Time = 213983, Fret = 7y, String = 4y); Note(Time = 214123, Fret = 5y, String = 5y)
                  Note(Time = 214404, Fret = 5y, String = 5y); Note(Time = 214544, Fret = 5y, String = 5y)
                  Note(Time = 214684, Fret = 7y, String = 5y); Note(Time = 214964, Fret = 7y, String = 5y)
                  Note(Time = 215105, Fret = 5y, String = 5y); Note(Time = 215245, Fret = 7y, String = 5y)
                  Note(Time = 215525, Fret = 9y, String = 5y); Note(Time = 215665, Fret = 7y, String = 5y)
                  Note(Time = 216086, Fret = 5y, String = 5y); Note(Time = 216647, Fret = 9y, String = 5y)
                  Note(Time = 216787, Fret = 7y, String = 5y); Note(Time = 217207, Fret = 5y, String = 5y)
                  Note(Time = 217768, Fret = 7y, String = 4y); Note(Time = 218049, Fret = 4y, String = 5y)
                  Note(Time = 218329, Fret = 4y, String = 5y); Note(Time = 218469, Fret = 4y, String = 5y)
                  Note(Time = 218609, Fret = 7y, String = 4y); Note(Time = 218890, Fret = 7y, String = 4y)
                  Note(Time = 219030, Fret = 7y, String = 4y); Note(Time = 219170, Fret = 6y, String = 4y)
                  Note(Time = 219450, Fret = 7y, String = 3y); Note(Time = 219731, Fret = 6y, String = 3y)
                  Note(Time = 220011, Fret = 4y, String = 3y); Note(Time = 220292, Fret = 6y, String = 3y)
                  Note(Time = 220852, Fret = 5y, String = 4y); Note(Time = 221273, Fret = 6y, String = 4y)
                  Note(Time = 221693, Fret = 5y, String = 5y); Note(Time = 221974, Fret = 4y, String = 5y)
                  Note(Time = 222254, Fret = 5y, String = 4y); Note(Time = 222534, Fret = 7y, String = 4y)
                  Note(Time = 222815, Fret = 7y, String = 4y); Note(Time = 222955, Fret = 7y, String = 4y)
                  Note(Time = 223095, Fret = 5y, String = 5y); Note(Time = 223376, Fret = 5y, String = 5y)
                  Note(Time = 223516, Fret = 5y, String = 5y); Note(Time = 223656, Fret = 7y, String = 5y)
                  Note(Time = 223936, Fret = 7y, String = 5y); Note(Time = 224077, Fret = 5y, String = 5y)
                  Note(Time = 224217, Fret = 7y, String = 5y); Note(Time = 224497, Fret = 9y, String = 5y)
                  Note(Time = 224637, Fret = 10y, String = 5y); Note(Time = 225058, Fret = 9y, String = 5y)
                  Note(Time = 225619, Fret = 7y, String = 5y); Note(Time = 225759, Fret = 5y, String = 5y)
                  Note(Time = 226740, Fret = 5y, String = 4y); Note(Time = 226880, Fret = 5y, String = 4y)
                  Note(Time = 227020, Fret = 7y, String = 4y); Note(Time = 227301, Fret = 7y, String = 4y)
                  Note(Time = 227581, Fret = 7y, String = 4y); Note(Time = 227721, Fret = 5y, String = 4y)
                  Note(Time = 227862, Fret = 6y, String = 3y); Note(Time = 228142, Fret = 5y, String = 5y)
                  Note(Time = 228422, Fret = 5y, String = 5y); Note(Time = 228703, Fret = 5y, String = 5y)
                  Note(Time = 228843, Fret = 4y, String = 5y); Note(Time = 228983, Fret = 5y, String = 4y)
                  Note(Time = 229263, Fret = 7y, String = 4y); Note(Time = 231226, Fret = 5y, String = 4y)
                  Note(Time = 231366, Fret = 5y, String = 4y); Note(Time = 231506, Fret = 7y, String = 4y)
                  Note(Time = 231787, Fret = 7y, String = 4y); Note(Time = 232067, Fret = 7y, String = 4y)
                  Note(Time = 232207, Fret = 5y, String = 4y); Note(Time = 232348, Fret = 6y, String = 3y)
                  Note(Time = 232628, Fret = 5y, String = 5y); Note(Time = 232908, Fret = 5y, String = 5y)
                  Note(Time = 233189, Fret = 4y, String = 5y); Note(Time = 233329, Fret = 7y, String = 4y)
                  Note(Time = 233469, Fret = 6y, String = 4y); Note(Time = 233749, Fret = 7y, String = 4y)
                  Note(Time = 234310, Fret = 6y, String = 3y); Note(Time = 234731, Fret = 6y, String = 3y)
                  Note(Time = 234871, Fret = 5y, String = 4y); Note(Time = 235151, Fret = 6y, String = 4y)
                  Note(Time = 235432, Fret = 7y, String = 4y); Note(Time = 235992, Fret = 5y, String = 5y)
                  Note(Time = 236273, Fret = 5y, String = 5y); Note(Time = 236413, Fret = 4y, String = 5y)
                  Note(Time = 236553, Fret = 5y, String = 5y); Note(Time = 236693, Fret = 4y, String = 5y)
                  Note(Time = 236834, Fret = 5y, String = 5y); Note(Time = 236974, Fret = 4y, String = 5y)
                  Note(Time = 237114, Fret = 7y, String = 4y); Note(Time = 238235, Fret = 7y, String = 5y)
                  Note(Time = 238376, Fret = 5y, String = 5y); Note(Time = 238516, Fret = 7y, String = 5y)
                  Note(Time = 238656, Fret = 5y, String = 5y); Note(Time = 238796, Fret = 7y, String = 5y)
                  Note(Time = 238936, Fret = 5y, String = 5y); Note(Time = 239077, Fret = 7y, String = 5y)
                  Note(Time = 239217, Fret = 5y, String = 5y); Note(Time = 239357, Fret = 7y, String = 4y)]

            getSameItemCount sameNote notes1 notes2 |> ignore

            Expect.isTrue true "An eternity has not passed by"

        // [ 4   1 8 8 8 ]
        // [ 5 0 1 8 8 8 ]
        testCase "Correct when starting note is different and different note in between" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 0y, Fret = 4y)
                           Note(Time = 1, String = 0y, Fret = 1y)
                           Note(Time = 2, String = 0y, Fret = 8y)
                           Note(Time = 50, String = 0y, Fret = 8y)
                           Note(Time = 75, String = 0y, Fret = 8y) ]
            let notes2 = [ Note(Time = 100, String = 0y, Fret = 5y)
                           Note(Time = 100, String = 0y, Fret = 0y)
                           Note(Time = 125, String = 0y, Fret = 1y)
                           Note(Time = 175, String = 0y, Fret = 8y)
                           Note(Time = 200, String = 0y, Fret = 8y)
                           Note(Time = 225, String = 0y, Fret = 8y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 4 "Count is correct"

        // [ 5             1 2 3 ]
        // [ 5 0 0 0 0 0 0 1 2 3 ]
        testCase "Correct when starting 6 different notes in between" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 0y, Fret = 5y)
                           Note(Time = 1, String = 0y, Fret = 1y)
                           Note(Time = 2, String = 0y, Fret = 2y)
                           Note(Time = 50, String = 0y, Fret = 3y) ]
            let notes2 = [ Note(Time = 50, String = 0y, Fret = 5y)
                           Note(Time = 100, String = 0y, Fret = 0y)
                           Note(Time = 110, String = 0y, Fret = 0y)
                           Note(Time = 120, String = 0y, Fret = 0y)
                           Note(Time = 130, String = 0y, Fret = 0y)
                           Note(Time = 140, String = 0y, Fret = 0y)
                           Note(Time = 150, String = 0y, Fret = 0y)
                           Note(Time = 200, String = 0y, Fret = 1y)
                           Note(Time = 210, String = 0y, Fret = 2y)
                           Note(Time = 220, String = 0y, Fret = 3y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 4 "Count is correct"

        // [   1 2 3 1 2 3 0 ]
        // [ 0 1 2 3 1 2 3   ]
        testCase "Correct when different starting and ending note 1/2" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 0y, Fret = 1y)
                           Note(Time = 1, String = 0y, Fret = 2y)
                           Note(Time = 2, String = 0y, Fret = 3y)
                           Note(Time = 3, String = 0y, Fret = 1y)
                           Note(Time = 4, String = 0y, Fret = 2y)
                           Note(Time = 5, String = 0y, Fret = 3y)
                           Note(Time = 6, String = 0y, Fret = 0y) ]

            let notes2 = [ Note(Time = 0, String = 0y, Fret = 0y)
                           Note(Time = 1, String = 0y, Fret = 1y)
                           Note(Time = 2, String = 0y, Fret = 2y)
                           Note(Time = 3, String = 0y, Fret = 3y)
                           Note(Time = 4, String = 0y, Fret = 1y)
                           Note(Time = 5, String = 0y, Fret = 2y)
                           Note(Time = 6, String = 0y, Fret = 3y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 6 "Count is correct"

        // [ 0 1 2 3 1 2 3   ]
        // [   1 2 3 1 2 3 0 ]
        testCase "Correct when different starting and ending note 2/2" <| fun _ ->
            let notes1 = [ Note(Time = 0, String = 0y, Fret = 0y)
                           Note(Time = 1, String = 0y, Fret = 1y)
                           Note(Time = 2, String = 0y, Fret = 2y)
                           Note(Time = 3, String = 0y, Fret = 3y)
                           Note(Time = 4, String = 0y, Fret = 1y)
                           Note(Time = 5, String = 0y, Fret = 2y)
                           Note(Time = 6, String = 0y, Fret = 3y) ]

            let notes2 = [ Note(Time = 0, String = 0y, Fret = 1y)
                           Note(Time = 1, String = 0y, Fret = 2y)
                           Note(Time = 2, String = 0y, Fret = 3y)
                           Note(Time = 3, String = 0y, Fret = 1y)
                           Note(Time = 4, String = 0y, Fret = 2y)
                           Note(Time = 5, String = 0y, Fret = 3y)
                           Note(Time = 6, String = 0y, Fret = 0y) ]

            let sameCount = getSameItemCount sameNote notes1 notes2

            Expect.equal sameCount 6 "Count is correct"
    ]
