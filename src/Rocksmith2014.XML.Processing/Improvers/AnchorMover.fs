module Rocksmith2014.XML.Processing.AnchorMover

open System
open Rocksmith2014.XML

let private pickTimeAndDistance (anchor: Anchor) (noteTime: int) =
    let distance = anchor.Time - noteTime
    if distance <> 0 && abs distance <= 5 then
        Some(anchor, noteTime)
    else
        None

/// Adjusts the times for anchors that are very close to a note but not exactly on a note.
let improve (arrangement: InstrumentalArrangement) =
    for level in arrangement.Levels do
        let noteAndChordTimes =
            level.Notes
            |> Seq.map _.Time
            |> Seq.append (level.Chords |> Seq.map _.Time)
            |> Seq.distinct
            |> Seq.sort
            |> Array.ofSeq

        let anchorsNearNoteOrChord =
            level.Anchors
            |> Seq.choose (fun anchor ->
                match Array.BinarySearch(noteAndChordTimes, anchor.Time) with
                | index when index < 0 ->
                    noteAndChordTimes
                    |> Array.tryPick (pickTimeAndDistance anchor)
                | _ ->
                    None)

        let slideEndTimes =
            let chordSlides =
                level.Chords
                |> Seq.choose (fun chord ->
                    if chord.HasChordNotes then
                        chord.ChordNotes
                        |> ResizeArray.tryFind _.IsSlide
                        |> Option.map (fun n -> n.Time + n.Sustain)
                    else
                        None)

            level.Notes
            |> Seq.choose (fun note -> if note.IsSlide then Some (note.Time + note.Sustain) else None)
            |> Seq.append chordSlides
            |> Set.ofSeq

        anchorsNearNoteOrChord
        |> Seq.filter (fun (anchor, _) -> not <| slideEndTimes.Contains(anchor.Time))
        |> Seq.iter (fun (anchor, noteTime) -> anchor.Time <- noteTime)
