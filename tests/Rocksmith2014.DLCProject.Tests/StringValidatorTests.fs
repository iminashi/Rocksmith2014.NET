module StringValidatorTests

open Expecto
open Rocksmith2014.DLCProject

[<Tests>]
let stringValidator =
    testList "String Validator Tests" [
        test "Non-alphanumeric characters are removed from DLC keys" {
            let str = StringValidator.dlcKey "@ab1RÅ!魔?"

            Expect.equal str "ab1R" "Non-alphanumeric characters were removed" }

        test "Leading non-alphanumeric characters are removed from sort fields" {
            let str = StringValidator.sortField "@!%ab1RÅ!魔?"

            Expect.equal str "ab1RÅ!魔?" "Non-alphanumeric characters were removed from the beginning" }

        test "Characters not included in the game's font are removed from fields" {
            let str = StringValidator.field "Artist AΏԘ₯糨"

            Expect.equal str "Artist A" "Characters not in the game's font were removed" }

        test "Diacritics are removed from filenames " {
            let str = StringValidator.fileName "Motörhead feat. Mötley Crüe"

            Expect.equal str "Motorhead-feat-Motley-Crue" "Diacritics were removed" }

        test "Invalid characters are removed from filenames" {
            let str = StringValidator.fileName @"A/B*C\D:E"

            Expect.equal str "ABCDE" "Invalid characters were removed" }
    ]
