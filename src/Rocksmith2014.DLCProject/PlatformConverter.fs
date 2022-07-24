module Rocksmith2014.DLCProject.PlatformConverter

open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.SNG
open System.IO
open System.Text

/// Replaces PC specific paths and tags with Mac versions.
let private convertGraph (data: Stream) =
    let text =
        using (new StreamReader(data)) (fun reader -> reader.ReadToEnd())

    let newText =
        StringBuilder(text)
            .Replace("bin/generic", "bin/macos")
            .Replace("audio/windows", "audio/mac")
            .Replace("dx9", "macos")

    let newData = MemoryStreamPool.Default.GetStream()
    use writer = new StreamWriter(newData, leaveOpen = true)

    writer.Write newText
    newData

/// Changes the encoding of an SNG from PC to Mac platform.
let private convertSNG (data: Stream) =
    async {
        use unpacked = MemoryStreamPool.Default.GetStream()
        do! SNG.unpack data unpacked PC
        data.Position <- 0L
        data.SetLength 0L
        do! SNG.pack unpacked data Mac
    }

let private convertEntry (entry: NamedEntry) =
    match entry.Name with
    | Contains "audio/windows" ->
        { entry with Name = entry.Name.Replace("audio/windows", "audio/mac") }
    | Contains "bin/generic" ->
        convertSNG entry.Data |> Async.RunSynchronously
        { entry with Name = entry.Name.Replace("bin/generic", "bin/macos") }
    | EndsWith "aggregategraph.nt" ->
        { entry with Data = convertGraph entry.Data }
    | _ ->
        entry

/// Converts a PSARC from PC to Mac platform.
let pcToMac (psarc: PSARC) =
    psarc.Edit(EditOptions.Default, List.map convertEntry)
