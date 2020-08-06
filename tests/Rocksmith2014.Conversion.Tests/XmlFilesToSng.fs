module Rocksmith2014.Conversion.Tests.XmlFilesToSng

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.XML
open Rocksmith2014.Conversion
open Rocksmith2014.SNG

[<Tests>]
let sngToXmlConversionTests =
  testList "XML Files → SNG" [

    testCase "Vocals (Default Font)" <| fun _ ->
        let xml = Vocals.Load("vocals.xml")
        
        let sng = ConvertVocals.xmlToSng DefaultFont xml

        Expect.equal sng.Vocals.Length xml.Count "Vocal count is same"
        Expect.equal sng.SymbolDefinitions.Length 192 "Symbol definition count is correct"

    testCase "Vocals (Custom Font)" <| fun _ ->
        let xml = Vocals.Load("vocals.xml")
        let customFont = GlyphDefinitions.Load("vocals.glyphs.xml")

        let sng = ConvertVocals.xmlToSng (CustomFont customFont) xml

        Expect.equal sng.Vocals.Length xml.Count "Vocal count is same"
        Expect.equal sng.SymbolDefinitions.Length customFont.Glyphs.Count "Symbol definition count is correct"
        Expect.equal sng.SymbolsTextures.[0].Width customFont.TextureWidth "Texture width is correct"
        Expect.equal sng.SymbolsTextures.[0].Height customFont.TextureHeight "Texture height is correct"

    testCase "Japanese Vocals (Custom Font)" <| fun _ ->
        let xml = Vocals.Load("jvocals.xml")
        let customFont = GlyphDefinitions.Load("jvocals.glyphs.xml")
        ConvertVocals.xmlToSng (CustomFont customFont) xml
        |> SNG.savePackedFile "jvocals_test.sng" PC
        |> Async.RunSynchronously

        let sng = SNG.readPackedFile "jvocals_test.sng" PC |> Async.RunSynchronously

        Expect.equal sng.Vocals.Length xml.Count "Vocal count is same"
        Expect.equal sng.Vocals.[0].Lyric "夏-" "Vocal #1 is correct"
        Expect.equal sng.Vocals.[9].Lyric "跡+" "Vocal #9 is correct"
        Expect.equal sng.SymbolDefinitions.Length customFont.Glyphs.Count "Symbol definition count is correct"
        Expect.equal sng.SymbolDefinitions.[1].Symbol "が" "Symbol #1 is correct"

    testCase "Instrumental" <| fun _ ->
        let xml = InstrumentalArrangement.Load("instrumental.xml")
        
        let sng = ConvertInstrumental.xmlToSng xml

        Expect.equal sng.Beats.Length xml.Ebeats.Count "Beat count is same"
        Expect.equal sng.Phrases.Length xml.Phrases.Count "Phrase count is same"
        Expect.equal sng.Chords.Length xml.ChordTemplates.Count "Chord template count is same"
        Expect.equal sng.Vocals.Length 0 "Vocals count is zero"
        Expect.equal sng.SymbolsHeaders.Length 0 "Symbol headers count is zero"
        Expect.equal sng.SymbolsTextures.Length 0 "Symbol textures count is zero"
        Expect.equal sng.SymbolDefinitions.Length 0 "Symbol definitions count is zero"
        Expect.equal sng.PhraseIterations.Length xml.PhraseIterations.Count "Phrase iteration count is same"
        Expect.equal sng.NewLinkedDifficulties.Length xml.NewLinkedDiffs.Count "Linked difficulties count is same"
        Expect.equal sng.Events.Length xml.Events.Count "Event count is same"
        Expect.equal sng.Tones.Length xml.Tones.Changes.Count "Tone change count is same"
        Expect.equal sng.DNAs.Length 2 "DNA count is correct"
        Expect.equal sng.Sections.Length xml.Sections.Count "Section count is same"
        Expect.equal sng.Levels.Length xml.Levels.Count "Level count is same"
  ]
