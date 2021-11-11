module Rocksmith2014.XML.Processing.ExtraBeatRemover

open Rocksmith2014.XML

let private removeLast (beats: ResizeArray<_>) = beats.RemoveAt(beats.Count - 1)

/// Removes beats that come after the audio has ended.
let improve (arrangement: InstrumentalArrangement) =
    let mutable lastBeat = arrangement.Ebeats[arrangement.Ebeats.Count - 1]
    let mutable penultimateBeat = arrangement.Ebeats[arrangement.Ebeats.Count - 2]
    let audioEnd = arrangement.MetaData.SongLength

    while penultimateBeat.Time > audioEnd do
        removeLast arrangement.Ebeats

        lastBeat <- penultimateBeat
        penultimateBeat <- arrangement.Ebeats[arrangement.Ebeats.Count - 2]

    // Remove the last beat unless it is closer to the audio end than the penultimate beat
    if audioEnd - penultimateBeat.Time <= lastBeat.Time - audioEnd then
        removeLast arrangement.Ebeats
        lastBeat <- penultimateBeat

    // Move the last beat to the time audio ends
    lastBeat.Time <- audioEnd
