module VocalsCheckerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let vocalsTests =
    testList "Arrangement Checker (Vocals)" [
        testCase "Detects character not in default font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "Test+"); Vocal(100, 50, "Nope:あ") })

            let result = VocalsChecker.check None vocals

            Expect.equal result.Head.IssueType (LyricWithInvalidChar('あ', false)) "Issue type is correct"

        testCase "Accepts characters in default font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "Test+"); Vocal(100, 50, "ÄöÖÅå"); Vocal(200, 50, "àè- +?&#\"") })

            let result = VocalsChecker.check None vocals

            Expect.isEmpty result "Issue was created"

        testCase "Ignores special characters ('-', '+') when using custom font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 100, "あ+"); Vocal(50, 50, "あ-"); Vocal(80, 50, "あ") })
            // Custom font does not define characters - or +
            let customFont = GlyphDefinitions(Glyphs = ResizeArray(seq { GlyphDefinition(Symbol = "あ") }))

            let result = VocalsChecker.check (Some customFont) vocals

            Expect.isEmpty result "Issue was created"

        testCase "Detects hyphen not used as a special character when not included in custom font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 100, "あ+"); Vocal(50, 50, "あ--"); Vocal(80, 50, "あ") })
            let customFont = GlyphDefinitions(Glyphs = ResizeArray(seq { GlyphDefinition(Symbol = "あ") }))

            let result = VocalsChecker.check (Some customFont) vocals

            Expect.equal result.Head.IssueType (LyricWithInvalidChar('-', true)) "Issue type is correct"

        testCase "Detects character not in the custom font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "あ+"); Vocal(100, 50, "不") })
            let customFont = GlyphDefinitions(Glyphs = ResizeArray(seq { GlyphDefinition(Symbol = "あ") }))

            let result = VocalsChecker.check (Some customFont) vocals

            Expect.equal result.Head.IssueType (LyricWithInvalidChar('不', true)) "Issue type is correct"

        testCase "Detects lyric that is too long (ASCII)" <| fun _ ->
            let lyric = String.replicate 48 "A"
            let vocals = ResizeArray(seq { Vocal(0, 10, "Test+"); Vocal(0, 50, lyric) })

            let result = VocalsChecker.check None vocals

            Expect.equal result.Head.IssueType (LyricTooLong lyric) "Issue type is correct"

        testCase "Detects lyric that is too long (non-ASCII)" <| fun _ ->
            let lyric = String.replicate 16 "あ" // 48 bytes in UTF8
            let vocals = ResizeArray(seq { Vocal(0, 100, "あ+"); Vocal(0, 50, lyric) })
            let customFont = GlyphDefinitions(Glyphs = ResizeArray(seq { GlyphDefinition(Symbol = "あ") }))

            let result = VocalsChecker.check (Some customFont) vocals

            Expect.hasLength result 1 "One issue created"
            Expect.equal result.Head.IssueType (LyricTooLong lyric) "Issue type is correct"

        testCase "Detects lyrics without line breaks" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "Line"); Vocal(0, 100, "Test+") })

            let result = VocalsChecker.check None vocals

            Expect.hasLength result 1 "One issue created"
            Expect.equal result.Head.IssueType LyricsHaveNoLineBreaks "Issue type is correct"
    ]
