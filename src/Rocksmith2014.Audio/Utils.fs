module Rocksmith2014.Audio.Utils

open Rocksmith2014.Common
open NAudio.Wave

/// Returns an audio reader for the given filename.
let getAudioReader (fileName: string)  =
    if String.endsWith ".wav" fileName then
        new AudioFileReader(fileName)
    else
        invalidOp "The audio file must be a wave file."

/// Returns the total length of the audio file with the given name.
let getLength (fileName: string) =
    using (getAudioReader fileName) (fun reader -> reader.TotalTime)
