module Rocksmith2014.XML.Processing.CrowdEventAdder

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open System.Text.RegularExpressions

let [<Literal>] private IntroCrowdReactionDelay = 600 // 0.6 s
let [<Literal>] private IntroApplauseLength = 2_500 // 2.5 s
let [<Literal>] private OutroApplauseLength = 4_000 // 4 s
let [<Literal>] private VenueFadeOutLength = 5_000 // 5 s

module private Events =
    let [<Literal>] IntroApplauseStart = "E3"
    let [<Literal>] OutroApplauseStart = "D3"
    let [<Literal>] ApplauseEnd = "E13"
    let [<Literal>] CrowdSpeedSlow = "e0"
    let [<Literal>] CrowdSpeedMedium = "e1"
    let [<Literal>] CrowdSpeedFast = "e2"

let private addIntroApplauseEvent (arrangement: InstrumentalArrangement) =
    let startTime = Utils.getFirstNoteTime arrangement + IntroCrowdReactionDelay
    let endTime = startTime + IntroApplauseLength

    arrangement.Events.InsertByTime(Event(Events.IntroApplauseStart, startTime))
    arrangement.Events.InsertByTime(Event(Events.ApplauseEnd, endTime))

let private addOutroApplauseEvent (arrangement: InstrumentalArrangement) =
    let audioEnd = arrangement.MetaData.SongLength
    let startTime = audioEnd - VenueFadeOutLength - OutroApplauseLength

    arrangement.Events.InsertByTime(Event(Events.OutroApplauseStart, startTime))
    arrangement.Events.InsertByTime(Event(Events.ApplauseEnd, audioEnd))

/// Adds crowd events to the arrangement if it does not have them.
let improve (arrangement: InstrumentalArrangement) =
    let events = arrangement.Events

    // Add initial crowd tempo event only if there are no other tempo events present
    if not <| events.Exists(fun e -> Regex.IsMatch(e.Code, "e[0-2]$")) then
        let averageTempo = arrangement.MetaData.AverageTempo
        let startBeat = arrangement.StartBeat

        let crowdSpeed =
            if averageTempo < 90f then Events.CrowdSpeedSlow
            elif averageTempo < 170f then Events.CrowdSpeedMedium
            else Events.CrowdSpeedFast

        events.InsertByTime(Event(crowdSpeed, startBeat))

    if not <| events.Exists(fun e -> e.Code = Events.IntroApplauseStart) then
        addIntroApplauseEvent arrangement

    if not <| events.Exists(fun e -> e.Code = Events.OutroApplauseStart) then
        addOutroApplauseEvent arrangement
