module JapaneseLyricsCreator.MessageHandler

open Rocksmith2014.XML

let private isWithinBoundsForCombination array index1 index2 =
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

let private createVocals matchedLines =
    matchedLines
    |> Array.collect (Array.map (fun matched ->
        let oldVocal = matched.Vocal

        let lyric =
            match matched.Japanese with
            | Some jp ->
                if oldVocal.Lyric.EndsWith('+') && not <| jp.EndsWith('+') then
                    jp + "+"
                else
                    jp
            | None ->
                oldVocal.Lyric

        Vocal(
            Time = oldVocal.Time,
            Note = oldVocal.Note,
            Length = oldVocal.Length,
            Lyric = lyric
        )))
    |> ResizeArray

let private addFusionOrSplit state f location =
    let modifications = f location :: state.ModificationsForJapanese

    let japaneseLines =
        LyricsTools.createJapaneseLines state.MatchedLines modifications state.JapaneseLyrics

    let matchedLines =
        updateMatchedLines state.MatchedLines japaneseLines

    LyricsCreatorState.addUndo state

    { state with
        ModificationsForJapanese = modifications
        JapaneseLines = japaneseLines
        MatchedLines = matchedLines }, Effect.Nothing

let update state msg =
    match msg with
    | SaveLyricsToFile targetPath ->
        Vocals.Save(targetPath, createVocals state.MatchedLines)

        state, Effect.AddVocalsToProject targetPath

    | SetJapaneseLyrics jLyrics ->
        let japaneseLines =
            LyricsTools.createJapaneseLines state.MatchedLines state.ModificationsForJapanese jLyrics

        let matchedLines =
            updateMatchedLines state.MatchedLines japaneseLines

        LyricsCreatorState.addUndo state

        { state with MatchedLines = matchedLines
                     JapaneseLyrics = jLyrics
                     JapaneseLines = japaneseLines }, Effect.Nothing

    | CombineJapaneseWithNext location ->
        if not <| isWithinBoundsForCombination state.JapaneseLines location.LineNumber location.Index then
            state, Effect.Nothing
        else
            addFusionOrSplit state Fusion location

    | SplitJapanese location ->
        addFusionOrSplit state Split location

    | CombineSyllableWithNext { Index = index; LineNumber = lineNumber } ->
        if not <| isWithinBoundsForCombination state.MatchedLines lineNumber index then
            state, Effect.Nothing
        else
            let newLines =
                state.MatchedLines
                |> Array.mapi (fun linei line ->
                    if linei = lineNumber then
                        line
                        |> Array.choosei (fun i syllable ->
                            if i = index + 1 then
                                None
                            elif i = index then
                                let vocal = LyricsTools.combineVocals syllable.Vocal line[index + 1].Vocal
                                Some { syllable with Vocal = vocal }
                            else
                                Some syllable)
                    else
                        line)

            let matchedLines =
                updateMatchedLines newLines state.JapaneseLines

            LyricsCreatorState.addUndo state
            { state with MatchedLines = matchedLines }, Effect.Nothing

    | UndoLyricsChange ->
        LyricsCreatorState.tryUndo state, Effect.Nothing
