module EventConverter

open Rocksmith2014.XML
open Rocksmith2014.EOF.EOFTypes
open Rocksmith2014.EOF.Helpers

let createEOFEvents
        (getTrackNumber: InstrumentalArrangement -> int)
        (beats: Ebeat array)
        (inst: InstrumentalArrangement) =
    let create text time flags =
        { Text = text
          BeatNumber = getClosestBeat beats time
          TrackNumber = getTrackNumber inst |> uint16
          Flag = flags }

    // Do not import time signature events
    let otherEvents =
        inst.Events
        |> Seq.filter (fun e -> e.Code.StartsWith("TS") |> not)
        |> Seq.map (fun e -> create e.Code e.Time EOFEventFlag.RS_EVENT)

    let sectionEvents =
        inst.Sections
        |> Seq.map (fun s -> create s.Name s.Time EOFEventFlag.RS_SECTION)

    let phraseEvents =
        inst.PhraseIterations
        |> Seq.map (fun p ->
            let phrase = inst.Phrases[p.PhraseId]
            create phrase.Name p.Time EOFEventFlag.RS_PHRASE)
        // Apply solo phrase flags
        |> Seq.map (fun e ->
            let isSolo =
                e.Text |> String.startsWith "solo"
                || sectionEvents |> Seq.exists (fun s -> s.BeatNumber = e.BeatNumber && s.Text |> String.startsWith "solo")

            if isSolo then
                { e with Flag = e.Flag ||| EOFEventFlag.RS_SOLO_PHRASE }
            else
                e)

    //unifiedPhrasesAndSections
    phraseEvents
    |> Seq.append sectionEvents
    |> Seq.append otherEvents

let unifyEvents (trackCount: int) (events: EOFEvent array) =
    events
    |> Array.groupBy (fun e -> e.Text, e.BeatNumber, e.Flag)
    |> Array.collect (fun (_, group) ->
        if group.Length = trackCount then
            { group[0] with TrackNumber = 0us } |> Array.singleton
        else
            group)
