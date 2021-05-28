module ActivePatternTests

open Expecto
open Rocksmith2014.Common

let testString = "Some STRING"

[<Tests>]
let tests =
    testList "Active Pattern Tests" [
        test "Contains ignores case" {
            let result =
                match testString with
                | Contains "string" -> true
                | _ -> false

            Expect.isTrue result "Contains matched to true" }

        test "Contains can fail to match" {
            let result =
                match testString with
                | Contains "xyz" -> true
                | _ -> false

            Expect.isFalse result "Contains matched to false" }

        test "EndsWith ignores case" {
            let result =
                match testString with
                | EndsWith "string" -> true
                | _ -> false

            Expect.isTrue result "EndsWith matched to true" }

        test "EndsWith can fail to match" {
            let result =
                match testString with
                | EndsWith "xyz" -> true
                | _ -> false

            Expect.isFalse result "EndsWith matched to false" }
    ]
