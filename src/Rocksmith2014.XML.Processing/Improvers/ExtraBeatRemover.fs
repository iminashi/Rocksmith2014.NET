module Rocksmith2014.XML.Processing.ExtraBeatRemover

open Rocksmith2014.XML

/// Removes beats that come after the audio has ended.
let improve (arrangement: InstrumentalArrangement) =
    let mutable lastBeat = arrangement.Ebeats.[arrangement.Ebeats.Count - 1]
    let mutable penultimateBeat = arrangement.Ebeats.[arrangement.Ebeats.Count - 2]
    let audioEnd = arrangement.MetaData.SongLength

    while penultimateBeat.Time > audioEnd do
        arrangement.Ebeats.Remove lastBeat |> ignore

        lastBeat <- penultimateBeat
        penultimateBeat <- arrangement.Ebeats.[arrangement.Ebeats.Count - 2]

    // Remove the last beat unless it is closer to the audio end than the penultimate beat
    if audioEnd - penultimateBeat.Time <= lastBeat.Time - audioEnd then
        arrangement.Ebeats.Remove lastBeat |> ignore
        lastBeat <- penultimateBeat
    
    // Move the last beat to the time audio ends
    lastBeat.Time <- audioEnd
