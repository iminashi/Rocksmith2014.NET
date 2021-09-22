module Rocksmith2014.Audio.Preview

open NAudio.Wave
open System

let [<Literal>] private FadeIn = 2500<ms>
let [<Literal>] private FadeOut = 3000<ms>
let [<Literal>] private PreviewLength = 28_000L<ms>

/// Adds a fade-in and fade-out to the sample provider.
let private fade fadeIn fadeOut audioLength sampleProvider =
    AudioFader(sampleProvider, fadeIn, fadeOut, audioLength) :> ISampleProvider

/// Gets a 28 second section from the sample provider starting at the given offset.
let private getPreviewSection (offset: TimeSpan) (file: ISampleProvider) =
    file
        .Skip(offset)
        .Take(TimeSpan.FromMilliseconds(float PreviewLength))

/// Creates a preview audio file.
let create (sourcePath: string) (targetPath: string) (startOffset: TimeSpan) =
    use audio = AudioReader.Create(sourcePath)
    let audioLength = audio.Stream.TotalTime.TotalMilliseconds

    let fadeIn, fadeOut =
        // Edge case: the audio length is shorter than the total fade time
        if audioLength < float (FadeIn + FadeOut) then
            let l = int (audioLength / 2.) * 1<ms>
            l, l
        else
            FadeIn, FadeOut

    let previewLength = min (int64 audioLength * 1L<ms>) PreviewLength
    let previewSection = getPreviewSection startOffset audio.SampleProvider
    let preview = fade fadeIn fadeOut previewLength previewSection

    WaveFileWriter.CreateWaveFile16(targetPath, preview)
