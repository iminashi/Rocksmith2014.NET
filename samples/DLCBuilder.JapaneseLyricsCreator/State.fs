namespace JapaneseLyricsCreator

open System
open System.Collections.Generic

type LyricsCreatorState =
    { MatchedLines : MatchedSyllable array array
      CombinedJapanese : (int * int) list
      JapaneseLyrics : string
      JapaneseLines : string array array }

module LyricsCreatorState =
    let private undoStates = Stack<LyricsCreatorState>()

    let canUndo() = undoStates.Count > 0
    let pushState = undoStates.Push
    let popState = undoStates.Pop

    let tryUndo currentState =
        if canUndo() then
            popState()
        else
            currentState

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
          CombinedJapanese = List.empty }
