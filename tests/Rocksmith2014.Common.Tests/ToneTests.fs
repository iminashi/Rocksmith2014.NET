module ToneTests

open Expecto
open Rocksmith2014.Common.Manifest
open System.IO
open System

[<Tests>]
let toneTests =
    testList "Tone Tests" [
        test "Can be imported from XML" {
            let tone = Tone.fromXmlFile "test.tone2014.xml"

            Expect.equal tone.Volume "-12" "Volume is correct"
            Expect.equal tone.Key "Test" "Key is correct"
            Expect.equal tone.Name "Test" "Name is correct"
            Expect.equal tone.ToneDescriptors [| "$[35720]CLEAN" |] "Descriptors are correct"
            Expect.equal tone.GearList.Cabinet.Category "Dynamic_Cone" "Cabinet category is correct"
            Expect.equal tone.GearList.Cabinet.Skin null "Cabinet skin is null"
            Expect.equal tone.GearList.Amp.Key "Amp_OrangeAD50" "Amp key is correct"
            Expect.hasLength tone.GearList.Amp.KnobValues 4 "Amp knob values length is correct" }

        testAsync "Can be exported to XML" {
            let testFile = "testExport.tone2014.xml"
            if File.Exists testFile then File.Delete testFile
            let imported = Tone.fromXmlFile "test.tone2014.xml"

            do! Tone.exportXml testFile imported
            let tone = Tone.fromXmlFile testFile

            Expect.equal tone.Volume "-12" "Volume is correct"
            Expect.equal tone.Key "Test" "Key is correct"
            Expect.equal tone.Name "Test" "Name is correct"
            Expect.equal tone.ToneDescriptors [| "$[35720]CLEAN" |] "Descriptors are correct"
            Expect.equal tone.GearList.Cabinet.Type "Cabinets" "Cabinet type is correct"
            Expect.equal tone.GearList.Cabinet.SkinIndex (Nullable()) "Cabinet skin index is null"
            Expect.equal tone.GearList.Amp.Type "Amps" "Amp type is correct"
            Expect.hasLength tone.GearList.Amp.KnobValues 4 "Amp knob values length is correct" }

        testAsync "Can be exported to JSON and imported from JSON" {
            let testFile = "testExport.tone2014.json"
            if File.Exists testFile then File.Delete testFile
            let imported = Tone.fromXmlFile "test.tone2014.xml"

            do! Tone.exportJson testFile imported
            let! tone = Tone.fromJsonFile testFile

            Expect.equal tone.Volume "-12" "Volume is correct"
            Expect.equal tone.Key "Test" "Key is correct"
            Expect.equal tone.Name "Test" "Name is correct"
            Expect.equal tone.ToneDescriptors [| "$[35720]CLEAN" |] "Descriptors are correct"
            Expect.equal tone.GearList.Cabinet.Category "Dynamic_Cone" "Cabinet category is correct"
            Expect.equal tone.GearList.Cabinet.Skin null "Cabinet skin is null"
            Expect.equal tone.GearList.Amp.Key "Amp_OrangeAD50" "Amp key is correct"
            Expect.hasLength tone.GearList.Amp.KnobValues 4 "Amp knob values length is correct" }
    ]
