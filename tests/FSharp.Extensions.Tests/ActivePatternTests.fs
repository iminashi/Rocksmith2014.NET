module ActivePatternTests

open Expecto

let testString = "Some STRING"

[<Tests>]
let tests =
    testList "Active Pattern Tests" [
        test "Contains ignores case" {
            let result =
                match testString with
                | Contains "string" -> true
                | _ -> false

            Expect.isTrue result "Contains matched to true"
        }

        test "Contains can fail to match" {
            let result =
                match testString with
                | Contains "xyz" -> true
                | _ -> false

            Expect.isFalse result "Contains matched to false"
        }

        test "EndsWith ignores case" {
            let result =
                match testString with
                | EndsWith "string" -> true
                | _ -> false

            Expect.isTrue result "EndsWith matched to true"
        }

        test "EndsWith can fail to match" {
            let result =
                match testString with
                | EndsWith "xyz" -> true
                | _ -> false

            Expect.isFalse result "EndsWith matched to false"
        }

        test "StartsWith ignores case" {
            let result =
                match testString with
                | StartsWith "some" -> true
                | _ -> false

            Expect.isTrue result "StartsWith matched to true"
        }

        test "StartsWith can fail to match" {
            let result =
                match testString with
                | StartsWith "xyz" -> true
                | _ -> false

            Expect.isFalse result "StartsWith matched to false"
        }
    ]
