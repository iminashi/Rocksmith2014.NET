module Rocksmith2014.Audio.Conversion

open System.IO
open System.Diagnostics
open System
open NAudio.Wave
open NAudio.Vorbis

let private toolsDir = Path.Combine(AppContext.BaseDirectory, "Tools")
let private ww2ogg = Path.Combine(toolsDir, "ww2ogg")
let private revorb = Path.Combine(toolsDir, "revorb")

/// Converts a vorbis file into a wave file.
let oggToWav sourcePath targetPath =
    use ogg = new VorbisWaveReader(sourcePath)
    WaveFileWriter.CreateWaveFile16(targetPath, ogg)

let private processFiles cmd args (files: string list) =
    files
    |> List.iter (fun file ->
        let startInfo = ProcessStartInfo(FileName = cmd,
                                         Arguments = String.Format(args, file),
                                         WorkingDirectory = toolsDir,
                                         CreateNoWindow = true)
        use proc = new Process(StartInfo = startInfo)
        proc.Start() |> ignore
        proc.WaitForExit())

/// Converts wem files to ogg or wav files.
let wemToOgg toWave wemfiles =
    let oggFiles =
        wemfiles
        |> List.map (fun path -> Path.ChangeExtension(path, "ogg"))

    processFiles ww2ogg "\"{0}\" --pcb packed_codebooks_aoTuV_603.bin" wemfiles
    processFiles revorb "\"{0}\"" oggFiles

    if toWave then
        oggFiles
        |> List.iter(fun sourcePath ->
            let targetPath = Path.ChangeExtension(sourcePath, "wav")
            oggToWav sourcePath targetPath
            File.Delete sourcePath)
