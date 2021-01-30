[<AutoOpen>]
module Rocksmith2014.Audio.Types

open NAudio.Wave
open NAudio.Vorbis
open Rocksmith2014.Common
open System

[<Measure>] type ms

type AudioReader(reader: WaveStream, provider: ISampleProvider) =
    new(input: AudioFileReader) =
        new AudioReader(input :> WaveStream, input :> ISampleProvider)

    new(input: VorbisWaveReader) =
        new AudioReader(input :> WaveStream, input :> ISampleProvider)

    member _.Reader = reader
    member _.SampleProvider = provider

    member _.Position with get() = reader.Position
    member _.Length with get() = reader.Length

    /// Returns an audio reader for the given filename.
    static member Create(fileName) =
        match fileName with
        | EndsWith ".wav" -> new AudioReader(new AudioFileReader(fileName))
        | EndsWith ".ogg" -> new AudioReader(new VorbisWaveReader(fileName))
        | _ -> raise <| NotSupportedException "Only vorbis and wave files are supported."

    interface IDisposable with
        member _.Dispose() = reader.Dispose()
