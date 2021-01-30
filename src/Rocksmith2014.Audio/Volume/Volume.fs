module Rocksmith2014.Audio.Volume

open System
open System.Buffers

let [<Literal>] private BufferSize = 50_000

/// Calculates a volume value using BS.1770 integrated loudness with -16 as reference value.
let calculate (fileName: string) =
    let buffer = ArrayPool<float32>.Shared.Rent(BufferSize)

    try
        use audio = AudioReader.Create fileName
        let sampleProvider = audio.SampleProvider
        let lufsMeter = LufsMeter(float sampleProvider.WaveFormat.SampleRate, sampleProvider.WaveFormat.Channels)
        let channels = sampleProvider.WaveFormat.Channels

        while audio.Position < audio.Length do
            let samplesRead = sampleProvider.Read(buffer, 0, BufferSize)
            if samplesRead > 0 then
                let perChannel = samplesRead / channels
                Array.init channels (fun ch ->
                    Array.init perChannel (fun pos -> float buffer.[pos * channels + ch])
                )
                |> lufsMeter.ProcessBuffer

        Math.Round(-1. * (16. + lufsMeter.GetIntegratedLoudness()), 1)
    finally ArrayPool.Shared.Return buffer
