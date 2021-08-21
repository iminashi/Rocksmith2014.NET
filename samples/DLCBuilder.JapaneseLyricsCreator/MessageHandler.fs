module JapaneseLyricsCreator.MessageHandler

open Rocksmith2014.XML

let private isWithinBounds array index1 index2 =
    array
    |> Array.tryItem index1
    |> Option.exists (fun subArray -> index2 < Array.length subArray - 1)

let private updateMatchedLines matchedLines japaneseLines =
    matchedLines
    |> Array.mapi (fun lineNumber line ->
        line
        |> Array.mapi (fun i syllable ->
            let jp =
                japaneseLines
                |> Array.tryItem lineNumber
                |> Option.bind (Array.tryItem i)

            { syllable with Japanese = jp }))

let update state msg =
    match msg with
    | SaveLyricsToFile targetPath ->
        let vocals =
            state.MatchedLines
            |> Array.collect (fun line ->
                line
                |> Array.map (fun matched ->
                    let vocal = Vocal(matched.Vocal)
                    matched.Japanese
                    |> Option.iter (fun jp ->
                        vocal.Lyric <-
                            if matched.Vocal.Lyric.EndsWith "+" && not <| jp.EndsWith "+" then
                                jp + "+"
                            else
                                jp)
                    vocal))
            |> ResizeArray

        Vocals.Save(targetPath, vocals)
        state, Effect.AddVocalsToProject targetPath

    | SetJapaneseLyrics jLyrics ->
        let japaneseLines =
            LyricsTools.createJapaneseLines state.MatchedLines state.CombinedJapanese jLyrics
        let matchedLines =
            updateMatchedLines state.MatchedLines japaneseLines

        LyricsCreatorState.addUndo state
        { state with MatchedLines = matchedLines
                     JapaneseLyrics = jLyrics
                     JapaneseLines = japaneseLines }, Effect.Nothing

    | CombineJapaneseWithNext location ->
        if not <| isWithinBounds state.JapaneseLines location.LineNumber location.Index then
            state, Effect.Nothing
        else
            let combinedJp = location::state.CombinedJapanese

            let japaneseLines =
                LyricsTools.createJapaneseLines state.MatchedLines combinedJp state.JapaneseLyrics

            let matchedLines =
                updateMatchedLines state.MatchedLines japaneseLines

            LyricsCreatorState.addUndo state
            { state with CombinedJapanese = combinedJp
                         JapaneseLines = japaneseLines
                         MatchedLines = matchedLines }, Effect.Nothing

    | CombineSyllableWithNext { Index = index; LineNumber = lineNumber } ->
        if not <| isWithinBounds state.MatchedLines lineNumber index then
            state, Effect.Nothing
        else
            let newLines =
                state.MatchedLines
                |> Array.mapi (fun linei line ->
                    if linei = lineNumber then
                        line
                        |> Array.choosei (fun i x ->
                            if i = index + 1 then
                                None
                            elif i = index then
                                let first = line.[index]
                                let vNext = line.[index + 1].Vocal

                                Some { first with Vocal = LyricsTools.combineVocals first.Vocal vNext }
                            else
                                Some x)
                    else
                        line)

            let matchedLines =
                updateMatchedLines newLines state.JapaneseLines

            LyricsCreatorState.addUndo state
            { state with MatchedLines = matchedLines }, Effect.Nothing

    | UndoLyricsChange ->
        LyricsCreatorState.tryUndo state, Effect.Nothing
