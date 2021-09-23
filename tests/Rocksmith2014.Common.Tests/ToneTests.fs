module ToneTests

open Expecto
open Rocksmith2014.Common.Manifest
open System.IO

[<Tests>]
let toneTests =
    testList "Tone Tests" [
        test "Can be imported from XML" {
            let tone = Tone.fromXmlFile "test.tone2014.xml"

            Expect.equal tone.Volume -12. "Volume is correct"
            Expect.equal tone.Key "Test" "Key is correct"
            Expect.equal tone.Name "Test" "Name is correct"
            Expect.equal tone.ToneDescriptors [| "$[35720]CLEAN" |] "Descriptors are correct"
            Expect.isNone tone.GearList.Cabinet.Category "Cabinet category is none"
            Expect.isNone tone.GearList.Cabinet.Skin "Cabinet skin is none"
            Expect.equal tone.GearList.Amp.Key "Amp_OrangeAD50" "Amp key is correct"
            Expect.hasLength tone.GearList.Amp.KnobValues 4 "There are 4 amp knob values"
            Expect.isSome tone.GearList.PrePedals.[0] "Pre-pedal 1 was imported"
        }

        testAsync "Can be exported to XML" {
            let testFile = "testExport.tone2014.xml"
            if File.Exists(testFile) then File.Delete(testFile)
            let imported = Tone.fromXmlFile "test.tone2014.xml"

            do! Tone.exportXml testFile imported
            let tone = Tone.fromXmlFile testFile

            Expect.equal tone.Volume -12. "Volume is correct"
            Expect.equal tone.Key "Test" "Key is correct"
            Expect.equal tone.Name "Test" "Name is correct"
            Expect.equal tone.ToneDescriptors [| "$[35720]CLEAN" |] "Descriptors are correct"
            Expect.equal tone.GearList.Cabinet.Type "Cabinets" "Cabinet type is correct"
            Expect.isNone tone.GearList.Cabinet.SkinIndex "Cabinet skin index is none"
            Expect.equal tone.GearList.Amp.Type "Amps" "Amp type is correct"
            Expect.hasLength tone.GearList.Amp.KnobValues 4 "There are 4 amp knob values"
            Expect.isSome tone.GearList.PrePedals.[0] "Pre-pedal 1 was imported"
        }

        testAsync "Can be exported to JSON and imported from JSON" {
            let testFile = "testExport.tone2014.json"
            if File.Exists(testFile) then File.Delete(testFile)
            let imported = Tone.fromXmlFile "test.tone2014.xml"

            do! Tone.exportJson testFile imported
            let! tone = Tone.fromJsonFile testFile

            Expect.equal tone.Volume -12. "Volume is correct"
            Expect.equal tone.Key "Test" "Key is correct"
            Expect.equal tone.Name "Test" "Name is correct"
            Expect.equal tone.ToneDescriptors [| "$[35720]CLEAN" |] "Descriptors are correct"
            Expect.isNone tone.GearList.Cabinet.Skin "Cabinet skin is none"
            Expect.equal tone.GearList.Amp.Key "Amp_OrangeAD50" "Amp key is correct"
            Expect.hasLength tone.GearList.Amp.KnobValues 4 "There are 4 amp knob values"
        }

        test "Number of effects can be counted" {
            let pedal =
                { Type = ""
                  KnobValues = Map.empty
                  Key = ""
                  Category = None
                  Skin = None
                  SkinIndex = None }
            let gearList =
                { Amp = pedal
                  Cabinet = pedal
                  Racks = [| Some pedal; None; None; Some pedal |]
                  PrePedals = [| Some pedal; None; Some pedal; None |]
                  PostPedals = [| Some pedal; Some pedal; None; None |] }

            let count = Tone.getEffectCount gearList

            Expect.equal count 6 "Gear list has 6 effects"
        }
    ]
