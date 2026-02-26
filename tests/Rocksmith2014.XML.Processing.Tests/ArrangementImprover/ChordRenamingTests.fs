module Rocksmith2014.XML.Processing.Tests.ChordRenamingTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

let private empty = Array.create 6 -1y

let private fs e1 a d g b e2 = [| e1; a; d; g; b; e2 |] |> Array.map sbyte

let private standardTuning = [| 0s; 0s; 0s; 0s; 0s; 0s |]
let private dropDTuning = [| -2s; 0s; 0s; 0s; 0s; 0s |]

let private template name e1 a d g b e2 =
    ChordTemplate(name, name, empty, fs e1 a d g b e2)

let private templateNameOnly name =
    ChordTemplate(name, name, empty, empty)

[<Tests>]
let chordNameTests =
    testList "Arrangement Improver (Chord Names)" [
        testCase "Fixes minor chord names" <| fun _ ->
            let c1 = templateNameOnly "Emin"
            let c2 = templateNameOnly "Amin7"
            let chords = ![ c1; c2 ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.equal c1.Name "Em" "Name was fixed"
            Expect.equal c1.DisplayName "Em" "DisplayName was fixed"
            Expect.isFalse (chords |> Seq.exists(fun c -> c.Name.Contains("min") || c.DisplayName.Contains("min"))) "All chords were fixed"

        testCase "Fixes -arp chord names" <| fun _ ->
            let c = templateNameOnly "E-arp"
            let chords = ![ c ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.equal c.Name "E" "Name was fixed"
            Expect.equal c.DisplayName "E-arp" "DisplayName was not changed"

        testCase "Fixes -nop chord names" <| fun _ ->
            let c = templateNameOnly "CMaj7-nop"
            let chords = ![ c ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.equal c.Name "CMaj7" "Name was fixed"
            Expect.equal c.DisplayName "CMaj7-nop" "DisplayName was not changed"

        testCase "Can convert chords to arpeggios" <| fun _ ->
            let c = templateNameOnly "CminCONV"
            let chords = ![ c ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.equal c.Name "Cm" "Name was fixed"
            Expect.equal c.DisplayName "Cm-arp" "DisplayName was fixed"

        testCase "Fixes empty chord names" <| fun _ ->
            let c = templateNameOnly " "
            let chords = ![ c ]
            let arr = InstrumentalArrangement(ChordTemplates = chords)

            ArrangementImprover.processChordNames arr

            Expect.stringHasLength c.Name 0 "Name was fixed"
            Expect.stringHasLength c.DisplayName 0 "DisplayName was fixed"
    ]

[<Tests>]
let doubleStopNameTests =
    testList "Arrangement Improver (Double Stop Names)" [
        testCase "Removes name from double stops" <| fun _ ->
            let c1 = template "E-5" 0 1 -1 -1 -1 -1
            let c2 = template "E3" -1 7 6 -1 -1 -1
            let arr = InstrumentalArrangement(ChordTemplates = ResizeArray [ c1; c2 ])

            DoubleStopNameRemover.improve standardTuning arr

            Expect.equal c1.Name "" "Name was removed"
            Expect.equal c1.DisplayName "" "DisplayName was removed"
            Expect.equal c2.Name "" "Name was removed"
            Expect.equal c2.DisplayName "" "DisplayName was removed"

        testCase "Does not remove name from full chord" <| fun _ ->
            let c1 = template "Em" 0 2 2 0 0 0
            let arr = InstrumentalArrangement(ChordTemplates = ResizeArray [ c1 ])

            DoubleStopNameRemover.improve standardTuning arr

            Expect.equal c1.Name "Em" "Name was not removed"
            Expect.equal c1.DisplayName "Em" "DisplayName was not removed"

        testCase "Does not remove name from common power chord" <| fun _ ->
            let c1 = template "E5" 0 2 -1 -1 -1 -1
            let arr = InstrumentalArrangement(ChordTemplates = ResizeArray [ c1 ])

            DoubleStopNameRemover.improve standardTuning arr

            Expect.equal c1.Name "E5" "Name was not removed"
            Expect.equal c1.DisplayName "E5" "DisplayName was not removed"

        testCase "Does not remove name from common power chord (drop D tuning)" <| fun _ ->
            let c1 = template "D5" 0 0 -1 -1 -1 -1
            let arr = InstrumentalArrangement(ChordTemplates = ResizeArray [ c1 ])

            DoubleStopNameRemover.improve dropDTuning arr

            Expect.equal c1.Name "D5" "Name was not removed"
            Expect.equal c1.DisplayName "D5" "DisplayName was not removed"
    ]
