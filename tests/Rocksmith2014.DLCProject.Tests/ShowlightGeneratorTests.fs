module ShowlightGeneratorTests

open Expecto
open System
open Rocksmith2014.Common
open Rocksmith2014.SNG
open Rocksmith2014.DLCProject
open Rocksmith2014.XML
open Rocksmith2014.Conversion

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
      PersistentID = Guid.NewGuid()
      CustomAudio = None }

let showlights =
    let sng = SNG.readPackedFile "Tech_Test.sng" PC |> Async.RunSynchronously
    let sngs = [ Instrumental testLead, sng ]
    ShowLightGenerator.generate sngs

[<Tests>]
let showlightGeneratorTests =
    testList "Showlight Generator Tests" [
        test "Fog and beam notes are generated" {
            Expect.exists showlights (fun x -> (x.Note >= ShowLight.BeamMin && x.Note <= ShowLight.BeamMax) || x.Note = ShowLight.BeamOff) "Beam note exists."
            Expect.exists showlights (fun x -> x.Note >= ShowLight.FogMin && x.Note <= ShowLight.FogMax) "Fog note exists." }
        
        test "Laser light notes are generated" {
            Expect.exists showlights (fun x -> x.Note = ShowLight.LasersOn) "Lasers on note exists."
            Expect.exists showlights (fun x -> x.Note = ShowLight.LasersOff) "Lasers off note exists." }

        test "Duplicate fog notes are not generated" {
            let sections = ResizeArray(seq {
                Section("noguitar", 10_000, 0s)
                Section("riff", 11_000, 0s)
                Section("noguitar", 12_000, 1s)
                Section("riff", 13_000, 1s)
                Section("riff", 14_000, 2s)
                Section("solo", 15_000, 0s)
                Section("noguitar", 16_000, 2s) })
            let ebeats = ResizeArray(seq { Ebeat(10_000, 0s); Ebeat(10_500, -1s) })
            let sng =
                InstrumentalArrangement(Ebeats = ebeats, Sections = sections)
                |> ConvertInstrumental.xmlToSng

            let showlights = ShowLightGenerator.generateFogNotes sng
            let distinct = showlights |> List.distinctBy (fun s -> s.Time, s.Note)

            Expect.equal showlights.Length distinct.Length "No duplicates were created"
            
         }
    ]
