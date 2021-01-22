module ShowlightGeneratorTests

open Expecto
open System
open Rocksmith2014.Common
open Rocksmith2014.SNG
open Rocksmith2014.DLCProject
open Rocksmith2014.XML

let testLead =
    { XML = "instrumental.xml"
      Name = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      Priority = ArrangementPriority.Main
      TuningPitch = 440.
      Tuning = [|0s;0s;0s;0s;0s;0s|]
      BaseTone = String.Empty
      Tones = []
      ScrollSpeed = 1.3
      BassPicked = false
      MasterID = 12345
      PersistentID = Guid.NewGuid() }

let showlights =
    let sng = SNG.readPackedFile "Tech_Test.sng" PC |> Async.RunSynchronously
    let sngs = [ Instrumental testLead, sng ]
    ShowLightGenerator.generate "sl_test.xml" sngs
    ShowLights.Load "sl_test.xml"

[<Tests>]
let showlightGeneratorTests =
    testList "Showlight Generator Tests" [
        test "Fog and beam notes are generated" {
            Expect.exists showlights (fun x -> (x.Note >= ShowLight.BeamMin && x.Note <= ShowLight.BeamMax) || x.Note = ShowLight.BeamOff) "Beam note exists."
            Expect.exists showlights (fun x -> x.Note >= ShowLight.FogMin && x.Note <= ShowLight.FogMax) "Fog note exists." }
        
        test "Laser light notes are generated" {
            Expect.exists showlights (fun x -> x.Note = ShowLight.LasersOn) "Lasers on note exists."
            Expect.exists showlights (fun x -> x.Note = ShowLight.LasersOff) "Lasers off note exists." }
        ]
