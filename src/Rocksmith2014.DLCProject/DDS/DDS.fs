module Rocksmith2014.DLCProject.DDS

open ImageMagick

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
