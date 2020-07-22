module Rocksmith2014.Conversion.Tests.XmlFilesToSngFiles

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.Conversion

[<Tests>]
let sngToXmlConversionTests =
  testList "XML Files → SNG Files" [

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
