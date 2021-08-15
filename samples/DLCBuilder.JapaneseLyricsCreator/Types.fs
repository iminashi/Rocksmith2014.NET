namespace JapaneseLyricsCreator

open Rocksmith2014.XML

type MatchedSyllable =
    { Vocal: Vocal
      Japanese: string option }

type CombinationLocation =
    { LineNumber: int
      Index: int }

type Msg =
    | SetJapaneseLyrics of lyrics : string
    | CombineSyllableWithNext of CombinationLocation
    | CombineJapaneseWithNext of CombinationLocation
    | UndoLyricsChange
    | SaveLyricsToFile of targetPath : string

type Effect =
    | Nothing
    | AddVocalsToProject of xmlPath : string
