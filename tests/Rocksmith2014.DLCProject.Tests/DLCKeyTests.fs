module DLCKeyTests

open Expecto
open Rocksmith2014.DLCProject

[<Tests>]
let dlcKeyTests =
    testList "DLC Key Tests" [
        test "DLC key can be created" {
            let key = DLCKey.create "creator" "Artist" "Title"

            Expect.isNotEmpty key "A key was created" }

        test "Created DLC key has good length" {
            let key = DLCKey.create "" "" ""

            Expect.isTrue (key.Length >= 5) "The key has five or more characters" }
    ]
