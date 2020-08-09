module Rocksmith2014.DLCProject.DDS

open ImageMagick
open System.IO

type Compression = DXT1 | DXT5
type Resize = Resize of width:int * height:int | NoResize

type DDSOptions =
    { Compression: Compression
      Resize: Resize }

let convertToDDS (sourceFile: string) (targetFile: string) (options: DDSOptions) =
    use image = new MagickImage(sourceFile)

    image.Settings.SetDefine(MagickFormat.Dds, "compression", options.Compression.ToString().ToLowerInvariant())
    image.Settings.SetDefine(MagickFormat.Dds, "mipmaps", "0")

    match options.Resize with
    | NoResize -> ()
    | Resize (width, height) ->
        image.Resize (MagickGeometry(width, height, IgnoreAspectRatio = true))

    image.Write targetFile

let createCoverArtImages (sourceFile: string) =
    let dir = Path.GetDirectoryName(sourceFile)
    [| Path.Combine(dir, "cover_64.dds"), Resize(64, 64)
       Path.Combine(dir, "cover_128.dds"), Resize(128, 128)
       Path.Combine(dir, "cover_256.dds"), Resize(256, 256) |]
    |> Array.Parallel.iter (fun (target, size) -> convertToDDS sourceFile target { Compression = DXT1; Resize = size })
