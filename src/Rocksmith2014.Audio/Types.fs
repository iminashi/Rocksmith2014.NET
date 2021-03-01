[<AutoOpen>]
module Rocksmith2014.Audio.Types

open NAudio.Wave
open NAudio.Vorbis
open Rocksmith2014.Common
open System

[<Measure>] type ms

type AudioReader(stream: WaveStream, provider: ISampleProvider) =
    new(input: WaveFileReader) =
        new AudioReader(input :> WaveStream, input.ToSampleProvider())

    new(input: VorbisWaveReader) =
        new AudioReader(input :> WaveStream, input :> ISampleProvider)

    member _.Stream = stream
    member _.SampleProvider = provider

    member _.Position with get() = stream.Position
    member _.Length with get() = stream.Length

    /// Returns an audio reader for the given filename.
    static member Create(fileName) =
        match fileName with
        | EndsWith ".wav" -> new AudioReader(new WaveFileReader(fileName))
        | EndsWith ".ogg" -> new AudioReader(new VorbisWaveReader(fileName))
        | _ -> raise <| NotSupportedException "Only vorbis and wave files are supported."

    interface IDisposable with
        member _.Dispose() = stream.Dispose()
