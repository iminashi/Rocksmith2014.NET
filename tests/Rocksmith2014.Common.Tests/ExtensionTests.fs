module ExtensionTests

open Expecto
open Rocksmith2014.Common

[<Tests>]
let extensionTests =
  testList "Extension Tests" [
    test "List.remove" {
        let b = "b"
        let list = ["a"; b; "c"]

        let res = List.remove b list

        Expect.hasLength res 2 "Has correct length"
        Expect.equal res.[1] "c" "Second element is c" }

    test "List.removeAt" {
        let list = ["a"; "b"; "c"]

        let res = List.removeAt 1 list

        Expect.sequenceContainsOrder res ["a"; "c"] "Result has correct elements" }

    test "List.insertAt" {
        let list = ["a"; "b"; "c"]

        let res = List.insertAt 1 "d" list

        Expect.sequenceContainsOrder res ["a"; "d"; "b"; "c"] "Result has correct elements" }

    test "List.update" {
        let b = "b"
        let list = ["a"; b; "c"]

        let res = List.update b "d" list

        Expect.sequenceContainsOrder res ["a"; "d"; "c"] "Result has correct elements" }
  ]