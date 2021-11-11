module Rocksmith2014.Audio.Volume

open System
open System.Buffers
open NAudio.Wave

let [<Literal>] private BufferSize = 50_000

let private calculateLoudness (sampleProvider: ISampleProvider) =
    let buffer = ArrayPool<float32>.Shared.Rent(BufferSize)
    let channels = sampleProvider.WaveFormat.Channels
    let lufsMeter = LufsMeter(float sampleProvider.WaveFormat.SampleRate, channels)

    let rec loop () =
        match sampleProvider.Read(buffer, 0, BufferSize) with
        | 0 ->
            ()
        | samplesRead ->
            let perChannel = samplesRead / channels
            Array.init channels (fun ch ->
                Array.init perChannel (fun pos -> float buffer[pos * channels + ch]))
            |> lufsMeter.ProcessBuffer
            loop ()

    try
        loop ()
        lufsMeter.GetIntegratedLoudness()
    finally
        ArrayPool.Shared.Return(buffer)

/// Calculates a volume value using BS.1770 integrated loudness with -16 as reference value.
let calculate (fileName: string) =
    use audio = AudioReader.Create(fileName)
    let loudness = calculateLoudness audio.SampleProvider

    if Double.IsInfinity(loudness) then
        0.
    else
        Math.Round(-16. - loudness, 1, MidpointRounding.AwayFromZero)
