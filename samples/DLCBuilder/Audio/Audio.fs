module DLCBuilder.Audio.Tools

open Rocksmith2014.Common
open System
open System.IO
open System.Buffers
open NAudio.Wave

let [<Literal>] FadeIn = 2500<ms>
let [<Literal>] FadeOut = 3000<ms>
let [<Literal>] PreviewLength = 28_000L<ms>

let private getAudioReader (fileName: string)  =
    if String.endsWith ".wav" fileName then
        new AudioFileReader(fileName)
    else
        invalidOp "The audio file must be a wave file."

/// Adds a fade-in and fade-out to the sample provider.
let private fade fadeIn fadeOut audioLength (sampleProvider : ISampleProvider) =
    AudioFader(sampleProvider, fadeIn, fadeOut, audioLength) :> ISampleProvider

/// Gets a 28 second section from the sample provider starting at the given offset.
let private getPreviewSection (offset: TimeSpan) (file: ISampleProvider) =
    file.Skip(offset).Take(TimeSpan.FromSeconds 28.)

/// Returns the total length of the given wave file.
let getLength (fileName: string) =
    using (getAudioReader fileName) (fun reader -> reader.TotalTime)

/// Creates a preview audio file .
let createPreview (sourceFile: string) (startOffset: TimeSpan) =
    let targetFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileNameWithoutExtension(sourceFile) + "_preview.wav")
    use audio = getAudioReader sourceFile

    let previewSection = getPreviewSection startOffset audio
    let preview = fade FadeIn FadeOut PreviewLength previewSection
    WaveFileWriter.CreateWaveFile16(targetFile, preview)
    targetFile

let [<Literal>] private BufferSize = 50_000

/// Calculates a volume value using BS.1770 integrated loudness with -16 as reference value.
let calculateVolume (fileName: string) =
    use audio = getAudioReader fileName
    let lufsMeter = LufsMeter(float audio.WaveFormat.SampleRate, audio.WaveFormat.Channels)
    let buffer = ArrayPool<float32>.Shared.Rent(BufferSize)
    let channels = audio.WaveFormat.Channels

    while audio.Position < audio.Length do
        let samplesRead = audio.Read(buffer, 0, BufferSize)
        if samplesRead > 0 then
            let perChannel = samplesRead / channels
            Array.init channels (fun ch ->
                Array.init perChannel (fun pos -> float buffer.[pos * channels + ch])
            )
            |> lufsMeter.ProcessBuffer

    ArrayPool.Shared.Return buffer
    Math.Round(-1. * (16. + lufsMeter.GetIntegratedLoudness()), 1)
