module StringValidatorTests

open Expecto
open Rocksmith2014.DLCProject

[<Tests>]
let stringValidator =
    testList "String Validator Tests" [
        test "DLC key" {
            let str = StringValidator.dlcKey "@ab1RÅ!魔?"

            Expect.equal str "ab1R" "Non-alphanumeric characters were removed" }

        test "Sort field" {
            let str = StringValidator.sortField "@!%ab1RÅ!魔?"

            Expect.equal str "ab1RÅ!魔?" "Non-alphanumeric characters were removed from the beginning" }

        test "Field" {
            let str = StringValidator.field "Artist AΏԘ₯糨"

            Expect.equal str "Artist A" "Characters not in the game's font were removed" }
    ]
