[<AutoOpen>]
module Rocksmith2014.DD.Types

open Rocksmith2014.XML

type XmlEntity =
    | XmlNote of Note
    | XmlChord of Chord

type RequestTarget = ChordTarget of Chord | HandShapeTarget of HandShape

type TemplateRequest = { OriginalId: int16
                         NoteCount: byte
                         Target: RequestTarget }

type DifficultyRange = { Low: byte; High: byte }

type BeatDivision = int

type PhraseSearch = SearchDisabled | WithThreshold of int

type GeneratorConfig = { PhraseSearch: PhraseSearch }
