module Rocksmith2014.DLCProject.DDS

open ImageMagick
open System.IO
open Rocksmith2014.Common

type Compression = DXT1 | DXT5
type Resize = Resize of width:int * height:int | NoResize

type DDSOptions =
    { Compression: Compression
      Resize: Resize }

let convertToDDS (sourceFile: string) (output: Stream) (options: DDSOptions) =
    use image = new MagickImage(sourceFile)

    image.Settings.SetDefine(MagickFormat.Dds, "compression", options.Compression.ToString().ToLowerInvariant())
    image.Settings.SetDefine(MagickFormat.Dds, "mipmaps", "0")

    match options.Resize with
    | NoResize -> ()
    | Resize (width, height) ->
        image.Resize (MagickGeometry(width, height, IgnoreAspectRatio = true))

    image.Format <- MagickFormat.Dds
    image.Write output

let createCoverArtImages (sourceFile: string) =
    [| Resize(64, 64); Resize(128, 128); Resize(256, 256) |]
    |> Array.Parallel.map (fun size ->
        let data = MemoryStreamPool.Default.GetStream()
        convertToDDS sourceFile data { Compression = DXT1; Resize = size }
        data)
