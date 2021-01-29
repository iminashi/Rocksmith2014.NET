module DLCBuilder.Audio.Conversion

open System.IO
open System.Diagnostics
open System

let private toolsDir = Path.Combine(AppContext.BaseDirectory, "Tools")
let private ww2oggPath = Path.Combine(toolsDir, "ww2ogg")
let private revorbPath = Path.Combine(toolsDir, "revorb")

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

let allWemToOgg folder =
    processFiles folder "*.wem" ww2oggPath "\"{0}\" --pcb packed_codebooks_aoTuV_603.bin"
    processFiles folder "*.ogg" revorbPath "\"{0}\""
