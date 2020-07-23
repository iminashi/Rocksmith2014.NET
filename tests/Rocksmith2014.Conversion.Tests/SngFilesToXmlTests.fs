module Rocksmith2014.Conversion.Tests.SngFilesToXmlTests

open Expecto
open Rocksmith2014.SNG
open Rocksmith2014.XML
open Rocksmith2014.Conversion
open System.Globalization

/// Testing function that converts a time in seconds into milliseconds without floating point arithmetic.
let timeConversion (time:float32) =
    Utils.TimeCodeFromFloatString (time.ToString(NumberFormatInfo.InvariantInfo))

[<Tests>]
let sngToXmlConversionTests =
  testList "SNG Files → XML Files" [

    testCase "Vocals" <| fun _ ->
      let sng = SNGFile.readPacked "vocals.sng" PC

      let xml = ConvertVocals.sngToXml sng

      Expect.equal xml.Count sng.Vocals.Length "Vocals count is same"
      for i = 0 to xml.Count - 1 do
        Expect.equal xml.[i].Lyric sng.Vocals.[i].Lyric (sprintf "Lyric #%i is same" i)
        Expect.equal xml.[i].Note (sng.Vocals.[i].Note |> byte) (sprintf "Note #%i is same" i)
        Expect.equal xml.[i].Time (sng.Vocals.[i].Time |> timeConversion) (sprintf "Time #%i is same" i)
        Expect.equal xml.[i].Length (sng.Vocals.[i].Length |> timeConversion) (sprintf "Length #%i is same" i)

    testCase "Extract Glyphs" <| fun _ ->
       let sng = SNGFile.readPacked "vocals.sng" PC

       let xml = ConvertVocals.extractGlyphData sng

       Expect.equal xml.Glyphs.Count sng.SymbolDefinitions.Length "Same glyph count"
       Expect.equal xml.TextureWidth sng.SymbolsTextures.[0].Width "Same texture width"
       Expect.equal xml.TextureHeight sng.SymbolsTextures.[0].Height "Same texture height"

    testCase "Instrumental" <| fun _ ->
        let sng = SNGFile.readPacked "instrumental.sng" PC

        let xml = ConvertInstrumental.sngToXml sng

        Expect.equal xml.Part sng.MetaData.Part "Same part"
        Expect.equal xml.Capo sng.MetaData.CapoFretId "Same capo"
        Expect.equal xml.LastConversionDateTime sng.MetaData.LastConversionDateTime "Same last conversion date"
        Expect.sequenceEqual xml.Tuning.Strings sng.MetaData.Tuning "Same tuning"
        Expect.equal xml.SongLength (timeConversion sng.MetaData.SongLength) "Same song length"
        Expect.equal xml.Phrases.Count sng.Phrases.Length "Same phrase count"
        Expect.equal xml.PhraseIterations.Count sng.PhraseIterations.Length "Same phrase iteration count"
        Expect.equal xml.NewLinkedDiffs.Count sng.NewLinkedDifficulties.Length "Same new linked difficulties count"
        Expect.equal xml.ChordTemplates.Count sng.Chords.Length "Same chord template count"
        Expect.equal xml.Ebeats.Count sng.Beats.Length "Same beat count"
        Expect.equal xml.Tones.Changes.Count sng.Tones.Length "Same tone count"
        Expect.equal xml.Sections.Count sng.Sections.Length "Same section count"
        Expect.equal xml.Events.Count sng.Events.Length "Same event count"
        Expect.equal xml.Levels.Count sng.Levels.Length "Same level count"
        if sng.PhraseExtraInfo.Length > 0 then
            Expect.equal xml.PhraseProperties.Count sng.PhraseExtraInfo.Length "Same phrase property count"
  ]
