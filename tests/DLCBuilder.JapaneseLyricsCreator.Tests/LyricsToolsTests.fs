module LyricsToolsTests

open Expecto
open JapaneseLyricsCreator
open Rocksmith2014.XML

[<Tests>]
let tests =
    testList "Lyrics Tools Tests" [
        testCase "Japanese sentence can be hyphenated" <| fun _ ->
            let input = "庭には鶏がいる"

            let result = LyricsTools.hyphenate input

            Expect.equal result [ "庭-"; "に-"; "は-"; "鶏-"; "が-"; "い-"; "る" ] "Sentence was hyphenated correctly"

        testCase "Combining hiragana is combined correctly" <| fun _ ->
            let input = "にゃあ"

            let result = LyricsTools.hyphenate input

            Expect.equal result [ "にゃ-"; "あ" ] "Word was hyphenated correctly"

        testCase "Parentheses are combined correctly" <| fun _ ->
            let input = "にゃあ(ニャー)"

            let result = LyricsTools.hyphenate input
            
            Expect.equal result [ "にゃ-"; "あ-"; "(ニャー)" ] "String was hyphenated correctly"

        testCase "Japanese quotation marks are combined correctly" <| fun _ ->
            let input = "猫：「吾輩は」"

            let result = LyricsTools.hyphenate input
            
            Expect.equal result [ "猫：-"; "「吾-"; "輩-"; "は」" ] "String was hyphenated correctly"

        testCase "Single combination is applied correctly"  <| fun _ ->
            let replacements = [ { LineNumber = 0; Index = 2 } ]
            let lines = [| [| "test"; "of"; "com-"; "bi-"; "na-"; "tion" |] |]

            let result = LyricsTools.applyCombinations replacements lines

            Expect.equal result [| [| "test"; "of"; "combi-"; "na-"; "tion" |] |] "Correct syllable was combined"

        testCase "Multiple combinations are applied correctly"  <| fun _ ->
            let replacements = [ { LineNumber = 0; Index = 2 }; { LineNumber = 0; Index = 3 }; { LineNumber = 0; Index = 4 } ]
            let lines = [| [| "test"; "of"; "com-"; "bi-"; "na-"; "tion" |] |]

            let result = LyricsTools.applyCombinations replacements lines

            Expect.equal result [| [| "test"; "of"; "combination" |] |] "Correct syllables were combined"

        testCase "Vocals are combined correctly"  <| fun _ ->
            let v1 = Vocal(1234, 500, "some-")
            let v2 = Vocal(1800, 200, "thing+")

            let combined = LyricsTools.combineVocals v1 v2

            Expect.equal combined.Time 1234 "Time is correct"
            Expect.equal combined.Length 766 "Length is correct"
            Expect.equal combined.Lyric "something+" "Vocal is correct"

        testCase "Hyphenation can be matched correctly"  <| fun _ ->
            let line = [| "test"; "of"; "com-"; "bi-"; "na-"; "tion" |]
            let word = "combination"

            let result = LyricsTools.matchHyphenation word line

            Expect.equal result [| "com-"; "bi-"; "na-"; "tion" |] "Correct array was returned"
    ]
