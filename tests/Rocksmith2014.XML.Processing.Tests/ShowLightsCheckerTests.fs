module ShowLightsCheckerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let showLightsTests =
    testList "Arrangement Checker (Show Lights)" [
        testCase "Detects missing fog note" <| fun _ ->
            let sl = ResizeArray(seq { ShowLight(100, ShowLight.BeamMin) })

            let result = ShowLightsChecker.check sl

            Expect.isSome result "Checker returned an issue"

        testCase "Detects missing beam note" <| fun _ ->
            let sl = ResizeArray(seq { ShowLight(100, ShowLight.FogMin) })

            let result = ShowLightsChecker.check sl

            Expect.isSome result "Checker returned an issue"

        testCase "Returns None for valid show lights" <| fun _ ->
            let sl = ResizeArray(seq { ShowLight(100, ShowLight.FogMin); ShowLight(100, ShowLight.BeamOff) })

            let result = ShowLightsChecker.check sl

            Expect.isNone result "Checker returned None"
    ]
