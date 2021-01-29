module DLCBuilder.Audio.Conversion

open System.IO
open System.Diagnostics
open System
open NAudio.Wave
open NAudio.Vorbis

let private toolsDir = Path.Combine(AppContext.BaseDirectory, "Tools")
let private ww2ogg = Path.Combine(toolsDir, "ww2ogg")
let private revorb = Path.Combine(toolsDir, "revorb")

let oggToWav sourcePath targetPath =
    use ogg = new VorbisWaveReader(sourcePath)
    WaveFileWriter.CreateWaveFile16(targetPath, ogg)

let private processFiles path pattern cmd args =
    Directory.EnumerateFiles(path, pattern)
    |> Seq.iter (fun file ->
        let startInfo = ProcessStartInfo(FileName = cmd,
                                         Arguments = String.Format(args, file),
                                         WorkingDirectory = toolsDir,
                                         CreateNoWindow = true)
        use proc = new Process(StartInfo = startInfo)
        proc.Start() |> ignore
        proc.WaitForExit())

let allWemToOgg folder toWave =
    processFiles folder "*.wem" ww2ogg "\"{0}\" --pcb packed_codebooks_aoTuV_603.bin"
    processFiles folder "*.ogg" revorb "\"{0}\""

    if toWave then
        Directory.EnumerateFiles(folder, "*.ogg")
        |> Seq.iter(fun sourcePath ->
            let targetPath = Path.ChangeExtension(sourcePath, "wav")
            oggToWav sourcePath targetPath
            File.Delete sourcePath)
