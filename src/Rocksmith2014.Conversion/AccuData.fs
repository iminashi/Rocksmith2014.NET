namespace Rocksmith2014.Conversion

open System
open System.Collections.Generic
open Rocksmith2014.SNG
open Rocksmith2014

/// The note count for each hero level and the number of ignored notes in the hard level.
type NoteCounts =
    { mutable Easy : int
      mutable Medium : int
      mutable Hard : int
      mutable Ignored : int }

/// Represents data that is being accumulated when mapping XML notes/chords into SNG notes.
type AccuData =
    { StringMasks : int8[][]
      ChordNotes : ResizeArray<ChordNotes>
      ChordNotesMap : Dictionary<int, int>
      AnchorExtensions : ResizeArray<AnchorExtension>
      NotesInPhraseIterationsExclIgnored : int[]
      NotesInPhraseIterationsAll : int[]
      NoteCounts : NoteCounts
      mutable FirstNoteTime : int }

    member this.AddNote(pi: int, difficulty: byte, heroLeves: XML.HeroLevels, ignored: bool) =
        this.NotesInPhraseIterationsAll.[pi] <- this.NotesInPhraseIterationsAll.[pi] + 1
    
        if not ignored then
            this.NotesInPhraseIterationsExclIgnored.[pi] <- this.NotesInPhraseIterationsExclIgnored.[pi] + 1
    
        if heroLeves.Easy = difficulty then
            this.NoteCounts.Easy <- this.NoteCounts.Easy + 1

        if heroLeves.Medium = difficulty then
            this.NoteCounts.Medium <- this.NoteCounts.Medium + 1

        if heroLeves.Hard = difficulty then
            this.NoteCounts.Hard <- this.NoteCounts.Hard + 1
            if ignored then
                this.NoteCounts.Ignored <- this.NoteCounts.Ignored + 1
    
    member this.LevelReset() =
        this.AnchorExtensions.Clear()
        Array.Clear(this.NotesInPhraseIterationsAll, 0, this.NotesInPhraseIterationsAll.Length)
        Array.Clear(this.NotesInPhraseIterationsExclIgnored, 0, this.NotesInPhraseIterationsExclIgnored.Length)
    
    static member Init(arr: XML.InstrumentalArrangement) =
        { StringMasks = Array.init (arr.Sections.Count) (fun _ -> Array.zeroCreate 36)
          ChordNotes = ResizeArray()
          AnchorExtensions = ResizeArray()
          ChordNotesMap = Dictionary()
          NotesInPhraseIterationsExclIgnored = Array.zeroCreate (arr.PhraseIterations.Count)
          NotesInPhraseIterationsAll = Array.zeroCreate (arr.PhraseIterations.Count)
          NoteCounts = { Easy = 0; Medium = 0; Hard = 0; Ignored = 0 }
          FirstNoteTime = Int32.MaxValue }
