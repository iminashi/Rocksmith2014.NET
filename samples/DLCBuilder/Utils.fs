module DLCBuilder.Utils

open Pfim
open System
open System.IO
open System.Runtime.InteropServices
open System.Text.Json
open System.Text.Json.Serialization
open Avalonia.Platform
open Avalonia.Media.Imaging
open Avalonia
open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.DLCProject.Manifest

/// Converts a Pfim DDS bitmap into an Avalonia bitmap.
let private avaloniaBitmapFromDDS (fileName: string) =
    use image = Pfim.FromFile fileName
    let pxFormat, data, stride =
        match image.Format with
        | ImageFormat.R5g6b5 -> PixelFormat.Rgb565, image.Data, image.Stride
        | ImageFormat.Rgb24 ->
            let pixels = image.DataLen / 3
            let newDataLen = pixels * 4
            let newData = Array.zeroCreate<byte> newDataLen
            for i = 0 to pixels - 1 do
                newData.[i * 4] <- image.Data.[i * 3]
                newData.[i * 4 + 1] <- image.Data.[i * 3 + 1]
                newData.[i * 4 + 2] <- image.Data.[i * 3 + 2]
                newData.[i * 4 + 3] <- 255uy

            let stride = image.Width * 4
            PixelFormat.Bgra8888, newData, stride
        | _ -> PixelFormat.Bgra8888, image.Data, image.Stride
    let pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned)
    let addr = pinnedArray.AddrOfPinnedObject()
    let bm = new Bitmap(pxFormat, addr, PixelSize(image.Width, image.Height), Vector(96., 96.), stride)
    pinnedArray.Free()
    bm

/// Loads a bitmap from the given path.
let loadBitmap (fileName: string) =
    if String.endsWith "dds" fileName then
        avaloniaBitmapFromDDS fileName
    else
        new Bitmap(fileName)

/// Imports tones from a PSARC file.
let importTonesFromPSARC (psarcPath: string) = async {
    use psarc = PSARC.ReadFile psarcPath
    let! jsons =
        psarc.Manifest
        |> List.filter (String.endsWith "json")
        |> List.map (fun x -> async {
            let data = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(x, data)
            return data })
        |> Async.Sequential

    let! manifests =
        jsons
        |> Array.map (fun data -> async {
            try
                let! a = using data Manifest.fromJsonStream
                return Some (Manifest.getSingletonAttributes a)
            with _ -> return None })
        |> Async.Parallel

    return
        manifests
        |> Array.choose (Option.bind (fun a -> Option.ofObj a.Tones))
        |> Array.concat
        |> Array.distinctBy (fun x -> x.Key) }

let createRecentList newFile recentList =
    let list = List.remove newFile recentList
    newFile::list
    |> List.truncate 3

let private recentFile =
    let appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rs2-dlcbuilder")
    Path.Combine(appData, "recent.json")

let private jsonOptions =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    options

let saveRecentFiles (recentList: string list) = async {
    Directory.CreateDirectory(Path.GetDirectoryName recentFile) |> ignore
    use file = File.Create recentFile
    do! JsonSerializer.SerializeAsync(file, recentList, jsonOptions) }

let loadRecentFiles () = async {
    if not <| File.Exists recentFile then
        return []
    else
        use file = File.OpenRead recentFile
        return! JsonSerializer.DeserializeAsync<string list>(file, jsonOptions) }

let previewPathFromMainAudio (audioPath: string) =
    let dir = Path.GetDirectoryName audioPath
    let fn = Path.GetFileNameWithoutExtension audioPath
    let ext = Path.GetExtension audioPath
    Path.Combine(dir, $"{fn}_preview{ext}")
