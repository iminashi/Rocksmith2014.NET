module Rocksmith2014.DD.Tests.HandShapeChooserTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.DD

[<Tests>]
let handShapeChooserTests =
    testList "Hand Shape Chooser Tests" [
        testCase "Returns none when no notes inside handshape" <| fun _ ->
            let templates = ResizeArray(seq { ChordTemplate("", "", [|1y;3y;4y;-1y;-1y;-1y|], [|3y;5y;5y;-1y;-1y;-1y|])})
            let handShapes = [ HandShape(0s, 2000, 3500) ]

            let hsResult, reqResult =
                HandShapeChooser.choose 100uy [||] [||] 3 templates handShapes
                |> List.unzip
            let reqResult = List.choose id reqResult

            Expect.hasLength reqResult 0 "No template request was returned"
            Expect.hasLength hsResult 0 "No hand shape was returned"

        testCase "Returns full hand shape for arpeggio" <| fun _ ->
            let n1 = Note(String = 0y, Time = 2000, Fret = 3y)
            let n2 = Note(String = 1y, Time = 2500, Fret = 5y)
            let n3 = Note(String = 2y, Time = 3000, Fret = 5y)
            let entities = [| XmlNote n1; XmlNote n2; XmlNote n3 |]
            let templates = ResizeArray(seq { ChordTemplate("", "", [|1y;3y;4y;-1y;-1y;-1y|], [|3y;5y;5y;-1y;-1y;-1y|])})
            let handShapes = [ HandShape(0s, 2000, 3500) ]

            let hsResult, reqResult =
                HandShapeChooser.choose 0uy entities entities 3 templates handShapes
                |> List.unzip
            let reqResult = List.choose id reqResult

            Expect.hasLength reqResult 0 "No template request was returned"
            Expect.hasLength hsResult 1 "One hand shape was returned"
            Expect.equal hsResult.[0].ChordId handShapes.[0].ChordId "Chord ID is same"
    ] 
