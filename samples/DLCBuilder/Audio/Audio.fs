module DLCBuilder.Audio.Tools

open Rocksmith2014.Common
open System
open System.IO
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
