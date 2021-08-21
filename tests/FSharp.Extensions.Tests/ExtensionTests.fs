module ExtensionTests

open Expecto
open System

[<Tests>]
let extensionTests =
    testList "Extension Tests" [
        test "GeneralHelpers.notNull" {
            let a = "a"
            let b : obj = null
        
            Expect.isTrue (notNull a) "String is not null"
            Expect.isFalse (notNull b) "Null object is null" }

        test "File.tryMap" {
            let fileName = "FSharp.Extensions.Tests.dll"

            let result = File.tryMap (fun _ -> "success") fileName
        
            Expect.equal result (Some "success") "Function was called" }

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

        test "ResizeArray.init" {
            let list = ResizeArray.init 5 string
        
            Expect.hasLength list 5 "List has correct length"
            Expect.sequenceContainsOrder list ["0"; "1"; "2"; "3"; "4"] "List contains correct elements" }

        testAsync "Async.map" {
            let task = async { return 42 }

            let! result = Async.map string task
        
            Expect.equal result "42" "Result is correct" }

        test "Array.updateAt" {
            let arr = [| 0; 1; 2; 3 |]

            let result = Array.updateAt 2 50 arr
        
            Expect.isFalse (Object.ReferenceEquals(arr, result)) "Result is a new array"
            Expect.hasLength result 4 "Result has correct length"
            Expect.equal result.[2] 50 "Correct value was changed" }

        test "Array.allSame (different values)" {
            let arr = [| 6; 5; 5; 5 |]

            let result = Array.allSame arr
        
            Expect.isFalse result "All elements are not the same value" }

        test "Array.allSame (same values)" {
            let arr = [| 5; 5; 5; 5 |]

            let result = Array.allSame arr
        
            Expect.isTrue result "All elements are the same value" }

        test "Array.choosei" {
            let arr = [| 0; 1; 2; 3; 4; 5 |]

            let result =
                arr
                |> Array.choosei (fun i x ->
                    Expect.equal i x "Index is correct"
                    Some x)

            Expect.sequenceContainsOrder result arr "All elements were chosen" }

        test "Option.ofString" {
            let a = Option.ofString null
            let b = Option.ofString ""
            let c = Option.ofString "test"

            Expect.isNone a "Null is None"
            Expect.isNone b "Empty string is None"
            Expect.isSome c "Non-empty string is Some" }
    ]
