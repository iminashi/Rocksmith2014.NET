namespace JapaneseLyricsCreator

open System

type LyricsCreatorState =
    { MatchedLines : MatchedSyllable array array
      CombinedJapanese : (int * int) list
      JapaneseLyrics : string
      JapaneseLines : string array array
      UndoStates : LimitedStack<LyricsCreatorState> }

module LyricsCreatorState =
    let canUndo state = state.UndoStates.HasItems
    let addUndo state = state.UndoStates.Push state

    let tryUndo state =
        if canUndo state then
            state.UndoStates.Pop()
        else
            state

    let init vocals =
        let matchedLines =
            vocals
            |> Seq.map (fun vocal ->
                { Vocal = vocal
                  Japanese = None })
            |> LyricsTools.toLines

        { MatchedLines = matchedLines
          JapaneseLines = Array.empty
          JapaneseLyrics = String.Empty
          CombinedJapanese = List.empty
          UndoStates = LimitedStack(10) }
