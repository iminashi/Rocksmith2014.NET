module Rocksmith2014.Conversion.ConvertVocals

open Rocksmith2014.SNG
open Rocksmith2014.XML
open Rocksmith2014.Conversion
open Rocksmith2014.SNG.BinaryReaders
open System.Reflection
open System.Text
open Microsoft.Extensions.FileProviders

/// The default symbol textures used in SNG files that use the default font.
let private defaultTextures =
    [| { Font = @"assets\ui\lyrics\lyrics.dds"
         FontPathLength = 27
         Width = 1024
         Height = 512 } |]

/// The default symbol definitions used in SNG files that use the default font.
let private defaultSymbols =
    lazy (let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
          use stream = embeddedProvider.GetFileInfo("default_symbols.bin").CreateReadStream()
          let reader = LittleEndianBinaryReader(stream)
          BinaryHelpers.readArray reader SymbolDefinition.Read)

/// The headers used in SNG files that use the default font.
let private defaultHeaders =
    [| SymbolsHeader.Default; { SymbolsHeader.Default with ID = 1 } |]

/// Converts an SNG vocals arrangement into a list of XML vocals.
let sngToXml (sng: SNG) =
    sng.Vocals
    |> Utils.mapToResizeArray SngToXml.convertVocal
    
/// Extracts glyph data from the given SNG.
let extractGlyphData (sng: SNG) =
    let glyphs =
        sng.SymbolDefinitions
        |> Utils.mapToResizeArray SngToXml.convertSymbolDefinition

    GlyphDefinitions(Font = sng.SymbolsTextures.[0].Font,
                     TextureWidth = sng.SymbolsTextures.[0].Width,
                     TextureHeight = sng.SymbolsTextures.[0].Height,
                     Glyphs = glyphs)

/// Converts a list of XML vocals into SNG.
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

/// Converts a vocals SNG file into an XML file.
let sngFileToXml sngFile targetFile platform =
    let vocals = SNGFile.readPacked sngFile platform |> sngToXml
    Vocals.Save(targetFile, vocals)

/// Converts a vocals XML file into an SNG file.
let xmlFileToSng xmlFile targetFile customFont platform =
    let glyphs =
        customFont
        |> Option.map (fun fn -> 
            let g = GlyphDefinitions.Load fn
            g.Font <- "lyrics.dds"
            g)

    Vocals.Load xmlFile
    |> xmlToSng glyphs 
    |> SNGFile.savePacked targetFile platform
