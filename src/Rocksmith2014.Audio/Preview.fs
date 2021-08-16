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
    file.Skip(offset).Take(TimeSpan.FromMilliseconds(float PreviewLength))

/// Creates a preview audio file.
let create (sourcePath: string) (targetPath: string) (startOffset: TimeSpan) =
    use audio = AudioReader.Create sourcePath

    let previewSection = getPreviewSection startOffset audio.SampleProvider
    let preview = fade FadeIn FadeOut PreviewLength previewSection
    WaveFileWriter.CreateWaveFile16(targetPath, preview)
