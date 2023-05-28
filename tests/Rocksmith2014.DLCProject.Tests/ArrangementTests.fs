module ArrangementTests

open Expecto
open Rocksmith2014.DLCProject
open System

let private createBaseToneName _ = "base_tone"

[<Tests>]
let tests =
    testList "Arrangement Tests" [
        test "Instrumental arrangement can be loaded from file" {
            let result = Arrangement.fromFile createBaseToneName "instrumental.xml"

            let arr, metadata = Expect.wantOk result "Arrangement was loaded"

            Expect.isSome metadata "Metadata has a value"

            match arr with
            | Instrumental inst ->
                Expect.equal inst.Name ArrangementName.Lead "Arrangement name is correct"
                Expect.equal inst.Priority ArrangementPriority.Main "Priority is correct"
                Expect.equal inst.BaseTone "Tone_1" "Base tone key is correct"
                Expect.equal inst.TuningPitch 440. "Tuning pitch is correct"
                Expect.equal inst.XML "instrumental.xml" "XML filename is correct"
                Expect.notEqual inst.PersistentID Guid.Empty "Persistent ID is not an empty GUID"
            | _ ->
                failwith "Wrong arrangement type"
        }

        test "Vocals arrangement can be loaded from file" {
            let result = Arrangement.fromFile createBaseToneName "jvocals.xml"

            let arr, metadata = Expect.wantOk result "Arrangement was loaded"

            Expect.isNone metadata "Metadata has no value"

            match arr with
            | Vocals v ->
                Expect.isTrue v.Japanese "Japanese vocals were detected from filename"
                Expect.equal v.XML "jvocals.xml" "XML filename is correct"
                Expect.notEqual v.PersistentID Guid.Empty "Persistent ID is not an empty GUID"
            | _ ->
                failwith "Wrong arrangement type"
        }

        test "Trying to load EXT vocals arrangements returns correct error" {
            let result = Arrangement.fromFile createBaseToneName "vocals_RS2_EXT.xml"

            let error = Expect.wantError result "Expected arrangement load to fail with error"

            match error with
            | EofExtVocalsFile path ->
                Expect.equal path "vocals_RS2_EXT.xml" "XML path is correct"
            | _ ->
                failwith "Wrong error type"
        }

        test "Showlights arrangement can be loaded from file" {
            let result = Arrangement.fromFile createBaseToneName "showlights.xml"

            let arr, metadata = Expect.wantOk result "Arrangement was loaded"

            Expect.isNone metadata "Metadata has no value"

            match arr with
            | Showlights sl ->
                Expect.equal sl.XML "showlights.xml" "XML filename is correct"
            | _ ->
                failwith "Wrong arrangement type"
        }

        test "Invalid file returns an error" {
            let result = Arrangement.fromFile createBaseToneName "test.bnk"

            let error = Expect.wantError result "Loading arrangement failed"

            match error with
            | FailedWithException (fileName, ex) ->
                Expect.equal fileName "test.bnk" "Failed filename is correct"
                Expect.isNotEmpty ex.Message "Exception has a message"
            | _ ->
                failwith "Wrong error type"
        }
    ]
