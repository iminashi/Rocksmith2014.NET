module DLCBuilder.AvaloniaBitmapLoader

open Avalonia
open Avalonia.Media.Imaging
open Avalonia.Platform
open Pfim
open Rocksmith2014.Common
open System.Runtime.InteropServices

let mutable private cached : Bitmap option = None

/// Converts a DDS bitmap into an Avalonia bitmap.
let private avaloniaBitmapFromDDS (fileName: string) =
    use image = Pfim.FromFile fileName
    let pxFormat, data, stride =
        match image.Format with
        | ImageFormat.R5g6b5 ->
            PixelFormat.Rgb565, image.Data, image.Stride
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
        | _ ->
            PixelFormat.Bgra8888, image.Data, image.Stride
    let pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned)
    let address = pinnedArray.AddrOfPinnedObject()
    let bm = new Bitmap(pxFormat, AlphaFormat.Unpremul, address, PixelSize(image.Width, image.Height), Vector(96., 96.), stride)
    pinnedArray.Free()
    bm

/// Tries to load and cache a bitmap from the given path, returning false if the loading fails.
let private tryLoadBitmap path =
    cached |> Option.iter (fun x -> x.Dispose())

    try
        cached <-
            match path with
            | EndsWith "dds" ->
                avaloniaBitmapFromDDS path
            | _ ->
                new Bitmap(path)
            |> Some
        true
    with _ ->
        false

/// Disposes of any cached bitmap.
let private invalidate () =
    cached |> Option.iter (fun x -> x.Dispose())
    cached <- None

/// Returns the cached bitmap if possible, None otherwise.
let getBitmap () = cached

let createInterface () =
    { new IBitmapLoader with
        member _.InvalidateCache() = invalidate ()
        member _.TryLoad path = tryLoadBitmap path }
