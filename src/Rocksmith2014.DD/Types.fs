[<AutoOpen>]
module Rocksmith2014.DD.Types

open System.Collections.Generic
open Rocksmith2014.XML

type RequestTarget =
    | ChordTarget of chordTarget: Chord
    | HandShapeTarget of handshapeTarget: HandShape

type TemplateRequest =
    { OriginalId: int16
      NoteCount: byte
      FromHighestNote: bool
      Target: RequestTarget }

[<RequireQualifiedAccess>]
type LevelCountGeneration =
    | Simple
    | MLModel
    /// Generates the same number of levels for all phrases. For testing purposes.
    | Constant of levelCount: int

type GeneratorConfig =
    { PhraseSearchThreshold: int option
      LevelCountGeneration: LevelCountGeneration }

type internal DifficultyRange = { Low: float; High: float }

type internal NoteScore = int
type internal NoteTime = int

type internal ScoreMap = IReadOnlyDictionary<NoteScore, DifficultyRange>
