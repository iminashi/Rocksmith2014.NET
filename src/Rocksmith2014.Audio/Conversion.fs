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

let private processFile cmd args (file: string) =
    let startInfo = ProcessStartInfo(FileName = cmd,
                                     Arguments = String.Format(args, file),
                                     WorkingDirectory = toolsDir,
                                     RedirectStandardOutput = true,
                                     CreateNoWindow = true)
    use proc = new Process(StartInfo = startInfo)
    proc.Start() |> ignore
    proc.WaitForExit()
    let output = proc.StandardOutput.ReadToEnd()
    if output.Contains("error", StringComparison.OrdinalIgnoreCase) then
        failwith $"Process failed with output:\n{output}"

/// Converts a wem file into an vorbis file.
let wemToOgg wemfile =
    let oggFile = Path.ChangeExtension(wemfile, "ogg")

    processFile ww2ogg "\"{0}\" --pcb packed_codebooks_aoTuV_603.bin" wemfile
    processFile revorb "\"{0}\"" oggFile

/// Converts a wem file into a wave file.
let wemToWav wemFile =
    wemToOgg wemFile

    let oggFile = Path.ChangeExtension(wemFile, "ogg")
    let targetPath = Path.ChangeExtension(wemFile, "wav")
    oggToWav oggFile targetPath
    File.Delete oggFile
