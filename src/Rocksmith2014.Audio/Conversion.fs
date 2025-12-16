module Rocksmith2014.Audio.Conversion

open System
open System.Diagnostics
open System.IO
open NAudio.Flac
open NAudio.Vorbis
open NAudio.Wave

let private toolsDir = Path.Combine(AppContext.BaseDirectory, "Tools")
let private ww2ogg = Path.Combine(toolsDir, "ww2ogg")
let private revorb = Path.Combine(toolsDir, "revorb")

/// Converts a vorbis file into a wave file.
let oggToWav (sourcePath: string) (targetPath: string) =
    use ogg = new VorbisWaveReader(sourcePath)
    WaveFileWriter.CreateWaveFile16(targetPath, ogg)

/// Converts a FLAC file into a wave file.
let flacToWav (sourcePath: string) (targetPath: string) =
    use flac = new FlacReader(sourcePath)
    WaveFileWriter.CreateWaveFile16(targetPath, flac.ToSampleProvider())

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
    if exitCode = 134 && OperatingSystem.IsMacOS() then
        // 134 = SIGABRT signal
        // Workaround for https://github.com/iminashi/Rocksmith2014.NET/issues/34
        ()
    elif exitCode <> 0 then
        let message =
            if String.notEmpty output then
                $"output:\n{output}"
            else
                $"exit code: {exitCode}"

        failwith $"revorb process failed with {message}"

let private wemToOggImpl sourcePath targetPath =
    processFile ww2ogg sourcePath $"""-o "{targetPath}" --pcb packed_codebooks_aoTuV_603.bin"""
    |> validateWw2oggOutput

    processFile revorb targetPath ""
    |> validateRevorbOutput

/// Converts a wem file into a vorbis file.
let wemToOgg (wemFile: string) =
    let oggFile = Path.ChangeExtension(wemFile, "ogg")
    wemToOggImpl wemFile oggFile

/// Converts a wem file into a wave file.
let wemToWav (wemFile: string) =
    wemToOgg wemFile

    let oggFile = Path.ChangeExtension(wemFile, "ogg")
    let targetPath = Path.ChangeExtension(wemFile, "wav")
    oggToWav oggFile targetPath
    File.Delete(oggFile)

/// Does an operation with a wem file converted into a vorbis file temporarily.
let withTempOggFile (f: string -> 'a) (wemPath: string) =
    let tempOggPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ogg")
    wemToOggImpl wemPath tempOggPath
    let res = f tempOggPath
    File.Delete(tempOggPath)
    res
