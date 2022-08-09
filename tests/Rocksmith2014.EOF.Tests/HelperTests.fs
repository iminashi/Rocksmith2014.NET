module HelperTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.EOF.Helpers
open EOFTypes

[<Tests>]
let tests =
    testList "EOF Helper tests" [
        test "Time signatures are calculated correctly" {
            let beats = seq {
                Ebeat(100, 0s)
                Ebeat(200, -1s)
                Ebeat(300, -1s)
                Ebeat(400, 1s)
                Ebeat(500, -1s)
                Ebeat(600, 2s)
                Ebeat(700, -1s)
                Ebeat(800, 3s)
                Ebeat(900, -1s)
                Ebeat(1000, -1s)
                Ebeat(1100, -1s)
                Ebeat(1100, 4s)
            }

            let timeSignatures = inferTimesignatures beats

            let expected = seq {
                100, ``TS 3 | 4``
                400, ``TS 2 | 4``
                800, ``TS 4 | 4``
                1100, CustomTS(1u, 4u)
            }
            Expect.sequenceEqual timeSignatures expected "Beat counts and times are correct"
        }

        test "Time signatures are parsed from event string correctly" {
            let tsStrings = [
                "TS:4/4"
                "TS:5/4"
                "TS:6-8"
                "TS:-1/4"
                "TS:0/2"
                "TS:2/0"
            ]

            let timeSignatures = tsStrings |> List.map tryParseTimeSignature

            let expected = [
                Some (4u, 4u)
                Some (5u, 4u)
                None
                None
                None
                None
            ]
            Expect.sequenceEqual timeSignatures expected "Parse results were correct"
        }
    ]
