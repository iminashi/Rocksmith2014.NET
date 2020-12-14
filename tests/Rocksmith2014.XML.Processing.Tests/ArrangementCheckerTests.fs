module Rocksmith2014.XML.Processing.Tests.ArrangementCheckerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let arrangementCheckerTests =
    testList "Arrangement Checker" [

        testCase "Detects missing applause end event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One message created"
            Expect.stringContains results.[0] "without an end event" "Contains correct message"

        testCase "Can detect unexpected crowd speed event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("e2", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One message created"
            Expect.stringContains results.[0] "Unexpected" "Contains correct message"

        testCase "Can detect unexpected intro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("E3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One message created"
            Expect.stringContains results.[0] "Unexpected" "Contains correct message"

        testCase "Can detect unexpected outro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One message created"
            Expect.stringContains results.[0] "Unexpected" "Contains correct message"

        testCase "Can detect multiple unexpected events" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("e0", 3000))
            xml.Events.Add(Event("e1", 3000))
            xml.Events.Add(Event("E13", 5000))

            let results = ArrangementChecker.checkCrowdEventPlacement xml

            Expect.hasLength results 3 "Three messages created"
    ]
