module DLCBuilder.Utils

open Pfim
open System
open System.Runtime.InteropServices
open Avalonia.Platform
open Avalonia.Media.Imaging
open Avalonia

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
    
let loadBitmap (fileName: string) =
    if fileName.EndsWith("dds", StringComparison.OrdinalIgnoreCase) then
        avaloniaBitmapFromDDS fileName
    else
        new Bitmap(fileName)
