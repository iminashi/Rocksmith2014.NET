module Rocksmith2014.Conversion.ConvertVocals

open Rocksmith2014.SNG.Types
open Rocksmith2014.SNG
open Rocksmith2014.XML
open Nessos.Streams
open System.IO

let sngToXml (sng:SNG) =
    sng.Vocals
    |> Stream.ofArray
    |> Stream.map SngToXml.convertVocal
    |> Stream.toResizeArray
    
let convertSngFileToXml fileName platform =
    let vocals = SNGFile.readPacked fileName platform |> sngToXml
    let target = Path.ChangeExtension(fileName, "xml")
    Vocals.Save(target, vocals)

let extractGlyphData (sng:SNG) =
    let glyphs =
        sng.SymbolDefinitions
        |> Stream.ofArray
        |> Stream.map SngToXml.convertSymbolDefinition
        |> Stream.toResizeArray

    GlyphDefinitions(TextureWidth = sng.SymbolsTextures.[0].Width,
                     TextureHeight = sng.SymbolsTextures.[0].Height,
                     Glyphs = glyphs)
