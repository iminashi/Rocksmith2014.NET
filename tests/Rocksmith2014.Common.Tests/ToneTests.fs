module ToneTests

open Expecto
open Rocksmith2014.Common.Manifest

[<Tests>]
let toneTests =
    testList "Tone Tests" [
        test "Can be imported from XML" {
            let tone = Tone.fromXmlFile "test.tone2014.xml"

            Expect.equal tone.Volume "-12" "Volume is correct"
            Expect.equal tone.ToneDescriptors [| "$[35720]CLEAN" |] "Descriptors are correct"
            Expect.equal tone.GearList.Cabinet.Category "Dynamic_Cone" "Cabinet category is correct"
            Expect.equal tone.GearList.Cabinet.Skin null "Cabinet skin is null"
            Expect.equal tone.GearList.Amp.Key "Amp_OrangeAD50" "Amp key is correct"
            Expect.hasLength tone.GearList.Amp.KnobValues 4 "Amp knob values length is correct" }
    ]
