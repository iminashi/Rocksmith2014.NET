module Rocksmith2014.XML.Processing.Utils

open Rocksmith2014.XML

let timeToString time =
    let minutes = time / 1000 / 60
    let seconds = (time / 1000) - (minutes * 60)
    let milliSeconds = time - (minutes * 60 * 1000) - (seconds * 1000)
    $"{minutes:D2}:{seconds:D2}.{milliSeconds:D3}"

let findTimeOfNthNoteFrom (level: Level) (startTime: int) (nthNote: int) =
    let noteTimes =
        level.Notes
        |> Seq.filter (fun n -> n.Time >= startTime)
        |> Seq.map (fun n -> n.Time)
        // Notes on the same timecode (i.e. split chords) count as one
        |> Seq.distinct
        |> Seq.truncate nthNote

    let chordTimes =
        level.Chords
        |> Seq.filter (fun c -> c.Time >= startTime)
        |> Seq.map (fun c -> c.Time)
        |> Seq.distinct
        |> Seq.truncate nthNote

    noteTimes
    |> Seq.append chordTimes
    |> Seq.sort
    |> Seq.skip (nthNote - 1)
    |> Seq.head
