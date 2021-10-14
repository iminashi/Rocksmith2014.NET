module Rocksmith2014.Audio.Conversion

open NAudio.Vorbis
open NAudio.Wave
open System
open System.Diagnostics
open System.IO

let private toolsDir = Path.Combine(AppContext.BaseDirectory, "Tools")
let private ww2ogg = Path.Combine(toolsDir, "ww2ogg")
let private revorb = Path.Combine(toolsDir, "revorb")

/// Converts a vorbis file into a wave file.
let oggToWav sourcePath targetPath =
    use ogg = new VorbisWaveReader(sourcePath)
    WaveFileWriter.CreateWaveFile16(targetPath, ogg)

let private createArgs path extraArgs =
    let pathArg = $"\"%s{path}\""
    if String.notEmpty extraArgs then
        String.Join(' ', pathArg, extraArgs)
    else
        pathArg

let private processFile cmd path extraArgs =
    let startInfo =
        ProcessStartInfo(
            FileName = cmd,
            Arguments = createArgs path extraArgs,
            WorkingDirectory = toolsDir,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        )

    use proc = new Process(StartInfo = startInfo)
    proc.Start() |> ignore
    proc.WaitForExit()
    proc.ExitCode, proc.StandardOutput.ReadToEnd()

let private validateWw2oggOutput (exitCode, output) =
    if exitCode <> 0 then
        failwith $"ww2ogg process failed with output:\n%s{output}"

let private validateRevorbOutput (exitCode, output) =
    if exitCode <> 0 then
        let message =
            if String.notEmpty output then
                $"output:\n{output}"
            else
                $"exit code: {exitCode}"

        failwith $"revorb process failed with {message}"

/// Converts a wem file into a vorbis file.
let wemToOgg wemfile =
    let oggFile = Path.ChangeExtension(wemfile, "ogg")

    processFile ww2ogg wemfile "--pcb packed_codebooks_aoTuV_603.bin"
    |> validateWw2oggOutput

    processFile revorb oggFile ""
    |> validateRevorbOutput

/// Converts a wem file into a wave file.
let wemToWav wemFile =
    wemToOgg wemFile

    let oggFile = Path.ChangeExtension(wemFile, "ogg")
    let targetPath = Path.ChangeExtension(wemFile, "wav")
    oggToWav oggFile targetPath
    File.Delete(oggFile)
