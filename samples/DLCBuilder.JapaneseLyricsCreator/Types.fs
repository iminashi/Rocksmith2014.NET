namespace JapaneseLyricsCreator

open Rocksmith2014.XML

type MatchedSyllable =
    { Vocal: Vocal
      Japanese: string option }

type Msg =
    | SetJapaneseLyrics of lyrics : string
    | CombineSyllableWithNext of lineNumber : int * index : int
    | CombineJapaneseWithNext of lineNumber : int * index : int
    | UndoLyricsChange
    | SaveLyricsToFile of targetPath : string
