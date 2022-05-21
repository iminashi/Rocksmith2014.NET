module Rocksmith2014.DD.Tests.AnchorChooserTests

open Expecto
open Rocksmith2014.DD
open Rocksmith2014.XML
open Rocksmith2014.XML.Extension

[<Tests>]
let anchorChooserTests =
    testList "Anchor Chooser Tests" [
        testCase "Chooses anchor at the start of the phrase" <| fun _ ->
            let anchors = [ Anchor(0y, 1050) ]
            let entities = [| XmlNote(Note(Time = 1200)) |]

            let result = AnchorChooser.choose entities anchors 1050 2000

            Expect.hasLength result 1 "One anchor was returned"

        testCase "Chooses anchor at the end of sustain" <| fun _ ->
            let anchors = [ Anchor(0y, 1350) ]
            let entities = [| XmlNote(Note(Time = 1200, Fret = 1y, Sustain = 150, SlideTo = 2y)) |]

            let result = AnchorChooser.choose entities anchors 1050 2000

            Expect.hasLength result 1 "One anchor was returned"
    ] 
