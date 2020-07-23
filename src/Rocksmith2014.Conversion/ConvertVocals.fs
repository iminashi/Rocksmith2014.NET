module Rocksmith2014.Conversion.ConvertVocals

open Rocksmith2014.SNG
open Rocksmith2014.XML
open Rocksmith2014.Conversion
open System.IO
open System.Reflection
open System.Text
open Microsoft.Extensions.FileProviders

let private defaultTextures =
    [| { Font = @"assets\ui\lyrics\lyrics.dds"
         FontPathLength = 27
         Width = 1024
         Height = 512 } |]

let private defaultSymbols =
    lazy (let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
          use stream = embeddedProvider.GetFileInfo("default_symbols.bin").CreateReadStream()
          use reader = new BinaryReader(stream)
          BinaryHelpers.readArray reader SymbolDefinition.Read)

let private defaultHeaders =
    [| SymbolsHeader.Default; { SymbolsHeader.Default with ID = 1 } |]

let sngToXml (sng: SNG) =
    sng.Vocals
    |> Utils.mapToResizeArray SngToXml.convertVocal
    
let extractGlyphData (sng: SNG) =
    let glyphs =
        sng.SymbolDefinitions
        |> Utils.mapToResizeArray SngToXml.convertSymbolDefinition

    GlyphDefinitions(Font = sng.SymbolsTextures.[0].Font,
                     TextureWidth = sng.SymbolsTextures.[0].Width,
                     TextureHeight = sng.SymbolsTextures.[0].Height,
                     Glyphs = glyphs)

let xmlToSng (glyphs: GlyphDefinitions option) (xml: ResizeArray<Vocal>) =
    let vocals = xml |> Utils.mapToArray XmlToSng.convertVocal

    let headers, textures, symbols =
        match glyphs with
        | None -> defaultHeaders, defaultTextures, defaultSymbols.Value

        | Some glyphs ->
            [| SymbolsHeader.Default |],
            [| { Font = glyphs.Font
                 FontPathLength = Encoding.UTF8.GetByteCount(glyphs.Font)
                 Width = glyphs.TextureWidth
                 Height = glyphs.TextureHeight } |],
            glyphs.Glyphs
            |> Utils.mapToArray XmlToSng.convertSymbolDefinition

    { SNG.Empty with Vocals = vocals
                     SymbolsHeaders = headers
                     SymbolsTextures = textures
                     SymbolDefinitions = symbols }

let sngFileToXml sngFile targetFile platform =
    let vocals = SNGFile.readPacked sngFile platform |> sngToXml
    Vocals.Save(targetFile, vocals)
