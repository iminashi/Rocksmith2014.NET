module JapaneseLyricsCreator.MessageHandler

open Rocksmith2014.XML

let update lyricsEditorState msg =
    match msg with
    | SetJapaneseLyrics jLyrics ->
        let japaneseLines =
            jLyrics
            |> LyricsTools.hyphenateToSyllableLines
            |> LyricsTools.matchNonJapaneseHyphenation lyricsEditorState.MatchedLines
            |> LyricsTools.applyCombinations lyricsEditorState.CombinedJapanese

        let matchedSyllables =
            lyricsEditorState.MatchedLines
            |> Array.mapi (fun lineNumber line ->
                line
                |> Array.mapi (fun i syllable ->
                    let jp =
                        japaneseLines
                        |> Array.tryItem lineNumber
                        |> Option.bind (Array.tryItem i)

                    { syllable with Japanese = jp }))

        LyricsCreatorState.addUndo lyricsEditorState
        { lyricsEditorState with MatchedLines = matchedSyllables
                                 JapaneseLyrics = jLyrics
                                 JapaneseLines = japaneseLines }

    | CombineJapaneseWithNext (lineNumber, index) ->
        if index >= lyricsEditorState.JapaneseLines.[lineNumber].Length - 1 then
            lyricsEditorState
        else
            let combinedJp = (lineNumber, index)::lyricsEditorState.CombinedJapanese

            let japaneseLines =
                lyricsEditorState.JapaneseLyrics
                |> LyricsTools.hyphenateToSyllableLines
                |> LyricsTools.matchNonJapaneseHyphenation lyricsEditorState.MatchedLines
                |> LyricsTools.applyCombinations combinedJp

            let matchedLines =
                lyricsEditorState.MatchedLines
                |> Array.mapi (fun lineNumber line ->
                    line
                    |> Array.mapi (fun i syllable ->
                        let jp =
                            japaneseLines
                            |> Array.tryItem lineNumber
                            |> Option.bind (Array.tryItem i)

                        { syllable with Japanese = jp }))

            LyricsCreatorState.addUndo lyricsEditorState
            { lyricsEditorState with CombinedJapanese = combinedJp
                                     JapaneseLines = japaneseLines
                                     MatchedLines = matchedLines }

    | CombineSyllableWithNext (lineNumber, index) ->
        if index >= lyricsEditorState.MatchedLines.[lineNumber].Length - 1 then
            lyricsEditorState
        else
            let line = lyricsEditorState.MatchedLines.[lineNumber]

            let v = line.[index].Vocal
            let vNext = line.[index + 1].Vocal

            let combinedVocal =
                let lyric =
                    let first = if v.Lyric.EndsWith "-" then v.Lyric.Substring(0, v.Lyric.Length - 1) else v.Lyric
                    first + vNext.Lyric
                Vocal(v.Time, (vNext.Time + vNext.Length) - v.Time, lyric)

            let combined = { line.[index] with Vocal = combinedVocal }

            let newSyllables =
                lyricsEditorState.MatchedLines
                |> Array.mapi (fun linei line ->
                    if linei = lineNumber then
                        line
                        |> Array.mapi (fun i x ->
                            if i = index + 1 then
                                None
                            elif i = index then
                                Some combined
                            else
                                Some x)
                        |> Array.choose id
                    else
                        line)

            let matchedSyllables =
                newSyllables
                |> Array.mapi (fun lineNumber line ->
                    line
                    |> Array.mapi (fun i syllable ->
                        let jp =
                            lyricsEditorState.JapaneseLines
                            |> Array.tryItem lineNumber
                            |> Option.bind (Array.tryItem i)

                        { syllable with Japanese = jp }))

            LyricsCreatorState.addUndo lyricsEditorState
            { lyricsEditorState with MatchedLines = matchedSyllables }

    | UndoLyricsChange ->
        LyricsCreatorState.tryUndo lyricsEditorState
