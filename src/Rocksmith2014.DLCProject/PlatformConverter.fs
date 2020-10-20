module Rocksmith2014.DLCProject.PlatformConverter

open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.SNG
open System.IO

let convertGraph (data: Stream) =
    let text = using (new StreamReader(data)) (fun reader -> reader.ReadToEnd())
    let newText = text.Replace("bin/generic", "bin/macos")
                      .Replace("audio/windows", "audio/mac")
                      .Replace("dx9", "macos")
    let newData = MemoryStreamPool.Default.GetStream()
    use writer = new StreamWriter(newData, leaveOpen = true)
    writer.Write newText
    newData

let convertSNG (data: Stream) = async {
    use unpacked = MemoryStreamPool.Default.GetStream()
    do! SNG.unpack data unpacked PC
    data.Position <- 0L
    data.SetLength 0L
    do! SNG.pack unpacked data Mac }

let pcToMac (psarc: PSARC) = async {
    do! psarc.Edit({ Mode = InMemory; EncyptTOC = true }, fun entries ->
        let updated =
            List.ofSeq entries
            |> List.map (fun e ->
                if e.Name.Contains "audio/windows" then
                    { e with Name = e.Name.Replace("audio/windows", "audio/mac") }
                elif e.Name.Contains "bin/generic" then
                    convertSNG e.Data |> Async.RunSynchronously
                    { e with Name = e.Name.Replace("bin/generic", "bin/macos") }
                elif e.Name.EndsWith "aggregategraph.nt" then
                    { e with Data = convertGraph e.Data }
                else e)
        entries.Clear()
        entries.AddRange(updated)
    ) }
