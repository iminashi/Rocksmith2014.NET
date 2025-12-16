module Rocksmith2014.Audio.Utils

open System.IO

/// Returns the total length of the audio file at the given path.
let getLength (path: string) =
    using (AudioReader.Create path) (fun audio -> audio.Stream.TotalTime)

/// Creates a path for the preview audio file from the given path.
///
/// Example: "some/path/file.ext" -> "some/path/file_preview.wav"
let createPreviewAudioPath (sourcePath: string) =
    let directory = Path.GetDirectoryName(sourcePath)

    let fileName =
        $"{Path.GetFileNameWithoutExtension(sourcePath)}_preview.wav"

    Path.Combine(directory, fileName)
