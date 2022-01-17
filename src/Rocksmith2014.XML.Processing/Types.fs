[<AutoOpen>]
module Rocksmith2014.XML.Processing.Types

type IssueType =
    | ApplauseEventWithoutEnd
    | EventBetweenIntroApplause of eventCode: string
    | NoteLinkedToChord
    | LinkNextMissingTargetNote
    | LinkNextSlideMismatch
    | LinkNextFretMismatch
    | LinkNextBendMismatch
    | IncorrectLinkNext
    | UnpitchedSlideWithLinkNext
    | PhraseChangeOnLinkNextNote
    | DoubleHarmonic
    | MissingIgnore
    | SeventhFretHarmonicWithSustain
    | MissingBendValue
    | ToneChangeOnNote
    | NoteInsideNoguitarSection
    | VaryingChordNoteSustains
    | MissingLinkNextChordNotes
    | ChordAtEndOfHandShape
    | FingeringAnchorMismatch
    | AnchorInsideHandShape
    | AnchorInsideHandShapeAtPhraseBoundary
    | AnchorCloseToUnpitchedSlide
    | AnchorNotOnNote of distance: int
    | FirstPhraseNotEmpty
    | NoEndPhrase
    | LyricWithInvalidChar of invalidChar: char
    | LyricTooLong of lyric: string
    | LyricsHaveNoLineBreaks
    | InvalidShowlights

type Issue =
    { Type: IssueType
      TimeCode: int }

let issueCode = function
    | ApplauseEventWithoutEnd -> "I01"
    | EventBetweenIntroApplause _ -> "I02"
    | NoteLinkedToChord -> "I03"
    | LinkNextMissingTargetNote -> "I04"
    | LinkNextSlideMismatch -> "I05"
    | LinkNextFretMismatch -> "I06"
    | LinkNextBendMismatch -> "I07"
    | IncorrectLinkNext -> "I08"
    | UnpitchedSlideWithLinkNext -> "I09"
    | PhraseChangeOnLinkNextNote -> "I10"
    | DoubleHarmonic -> "I11"
    | MissingIgnore -> "I12"
    | SeventhFretHarmonicWithSustain -> "I13"
    | MissingBendValue -> "I14"
    | ToneChangeOnNote -> "I15"
    | NoteInsideNoguitarSection -> "I16"
    | VaryingChordNoteSustains -> "I17"
    | MissingLinkNextChordNotes -> "I18"
    | ChordAtEndOfHandShape -> "I19"
    | FingeringAnchorMismatch -> "I20"
    | AnchorInsideHandShape -> "I21"
    | AnchorInsideHandShapeAtPhraseBoundary -> "I22"
    | AnchorCloseToUnpitchedSlide -> "I23"
    | AnchorNotOnNote _ -> "I24"
    | FirstPhraseNotEmpty -> "I25"
    | NoEndPhrase -> "I26"
    | LyricWithInvalidChar _ -> "V01"
    | LyricTooLong _ -> "V02"
    | LyricsHaveNoLineBreaks -> "V03"
    | InvalidShowlights -> "S01"
