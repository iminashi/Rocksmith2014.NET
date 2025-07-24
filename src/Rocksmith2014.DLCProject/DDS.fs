module Rocksmith2014.DLCProject.DDS

open ImageMagick
open System.IO
open System

type Compression =
    | DXT1
    | DXT5

    override this.ToString() =
        match this with
        | DXT1 -> "dxt1"
        | DXT5 -> "dxt5"

type Resize =
    | Resize of width: uint * height: uint
    | NoResize

type DDSFile(size: uint, path: string, deleteOnDispose: bool) =
    member _.Size = size
    member _.Path = path

    interface IDisposable with
        member _.Dispose() = if deleteOnDispose then File.Delete(path)

type DDSOptions =
    { Compression: Compression
      Resize: Resize }

/// Converts the source file into a DDS into the output stream.
let convertToDDS (sourceFile: string) (output: Stream) (options: DDSOptions) =
    use image = new MagickImage(sourceFile, Format = MagickFormat.Dds)

    image.Settings.SetDefine(MagickFormat.Dds, "compression", string options.Compression)
    image.Settings.SetDefine(MagickFormat.Dds, "mipmaps", "0")

    match options.Resize with
    | NoResize ->
        ()
    | Resize (width, height) ->
        let geometry = MagickGeometry(width, height, IgnoreAspectRatio = true)
        image.Resize(geometry)

    image.Write(output)

let private targetSizes = [| 64u; 128u; 256u |]

/// Creates three cover art images from the source file and returns the filenames of the temporary files.
let createCoverArtImages (sourceFile: string) =
    targetSizes
    |> Array.Parallel.map (fun size ->
        // Try to use an existing file if one exists
        let ddsFile = $"{Path.GetFileNameWithoutExtension(sourceFile)}{size}.dds"
        let ddsPath = Path.Combine(Path.GetDirectoryName(sourceFile), ddsFile)

        if File.Exists(ddsPath) then
            new DDSFile(size, ddsPath, deleteOnDispose = false)
        else
            let fileName = Path.GetTempFileName()
            use tempFile = File.Create(fileName)
            let options = { Compression = DXT1; Resize = Resize(size, size) }

            convertToDDS sourceFile tempFile options

            new DDSFile(size, fileName, deleteOnDispose = true))
    |> List.ofArray
