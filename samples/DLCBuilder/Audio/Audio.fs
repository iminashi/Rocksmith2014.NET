module DLCBuilder.Audio.Tools

open System
open System.IO
open DLCBuilder.Audio
open NAudio.Wave

let private getAudioReader (fileName : string)  =
    if fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) then
        new AudioFileReader(fileName)
    else
        invalidOp "The audio file must be a wave file."

/// Adds a fade-in and fade-out to the sample provider.
let private fade fadeIn fadeOut audioLength (sampleProvider : ISampleProvider) =
    AudioFader(sampleProvider, fadeIn, fadeOut, audioLength) :> ISampleProvider

/// Gets a 28 second section from the sample provider starting at the given offset.
let private getPreviewSection (offset : TimeSpan) (file : ISampleProvider) =
    file.Skip(offset).Take(TimeSpan.FromSeconds 28.)

/// Returns the total length of the given wave file.
let getLength (fileName : string) =
    using (getAudioReader fileName) (fun reader -> reader.TotalTime)

/// Creates a preview audio file .
let createPreview (sourceFile: string) (startOffset: TimeSpan) =
    let targetFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileNameWithoutExtension(sourceFile) + "_preview.wav")
    use audio = getAudioReader sourceFile
    let fadeIn = 2500<ms>
    let fadeOut = 3000<ms>

    let previewSection = getPreviewSection startOffset audio
    let preview = fade fadeIn fadeOut (28L * 1000L<ms>) previewSection
    WaveFileWriter.CreateWaveFile16(targetFile, preview)
    targetFile
