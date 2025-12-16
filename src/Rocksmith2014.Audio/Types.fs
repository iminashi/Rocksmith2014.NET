[<AutoOpen>]
module Rocksmith2014.Audio.Types

open System
open NAudio.Wave
open NAudio.Vorbis
open NAudio.Flac

[<Measure>]
type ms

type AudioReader(stream: WaveStream, provider: ISampleProvider) =
    new(input: WaveFileReader) =
        new AudioReader(input :> WaveStream, input.ToSampleProvider())

    new(input: VorbisWaveReader) =
        new AudioReader(input :> WaveStream, input :> ISampleProvider)

    new(input: FlacReader) =
        new AudioReader(input :> WaveStream, input.ToSampleProvider())

    member _.Stream = stream
    member _.SampleProvider = provider

    member _.Position = stream.Position
    member _.Length = stream.Length

    /// Returns an audio reader for the given filename.
    static member Create(path: string) =
        match path with
        | HasExtension ".wav" ->
            new AudioReader(new WaveFileReader(path))
        | HasExtension ".ogg" ->
            new AudioReader(new VorbisWaveReader(path))
        | HasExtension ".flac" ->
            new AudioReader(new FlacReader(path))
        | _ ->
            raise <| NotSupportedException "Only vorbis, wave and FLAC files are supported."

    interface IDisposable with
        member _.Dispose() = stream.Dispose()
