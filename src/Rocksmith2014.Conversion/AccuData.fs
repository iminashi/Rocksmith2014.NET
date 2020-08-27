namespace Rocksmith2014.Conversion

open System.Collections.Generic
open Rocksmith2014.SNG
open Rocksmith2014
open System.Threading

/// The note count for each hero level and the number of ignored notes in the hard level.
type NoteCountsMutable() =
    [<DefaultValue>] val mutable Easy : int
    [<DefaultValue>] val mutable Medium : int
    [<DefaultValue>] val mutable Hard : int
    [<DefaultValue>] val mutable Ignored : int

    member this.AsImmutable() =
        { Easy = this.Easy; Medium = this.Medium; Hard = this.Hard; Ignored = this.Ignored;  }

/// Represents data that is being accumulated when mapping XML notes/chords into SNG notes.
type AccuData =
    { StringMasks : int8[][]
      ChordNotes : ResizeArray<ChordNotes>
      ChordNotesMap : Dictionary<int, int>
      AnchorExtensions : ResizeArray<AnchorExtension>[]
      NotesInPhraseIterationsExclIgnored : int[][]
      NotesInPhraseIterationsAll : int[][]
      NoteCounts : NoteCountsMutable }

    member this.AddNote(pi: int, difficulty: byte, heroLevels: XML.HeroLevels, ignored: bool) =
        let d = int difficulty
        this.NotesInPhraseIterationsAll.[d].[pi] <- this.NotesInPhraseIterationsAll.[d].[pi] + 1
    
        if not ignored then
            this.NotesInPhraseIterationsExclIgnored.[d].[pi] <- this.NotesInPhraseIterationsExclIgnored.[d].[pi] + 1
    
        if heroLevels.Easy = difficulty then
            Interlocked.Increment(&this.NoteCounts.Easy) |> ignore

        if heroLevels.Medium = difficulty then
            Interlocked.Increment(&this.NoteCounts.Medium) |> ignore

        if heroLevels.Hard = difficulty then
            Interlocked.Increment(&this.NoteCounts.Hard) |> ignore
            if ignored then
                Interlocked.Increment(&this.NoteCounts.Ignored) |> ignore
    
    static member Init(arr: XML.InstrumentalArrangement) =
        { StringMasks = Array.init (arr.Sections.Count) (fun _ -> Array.zeroCreate 36)
          ChordNotes = ResizeArray()
          AnchorExtensions = Array.init arr.Levels.Count (fun _ -> ResizeArray())
          ChordNotesMap = Dictionary()
          NotesInPhraseIterationsExclIgnored = Array.init arr.Levels.Count (fun _ -> Array.zeroCreate (arr.PhraseIterations.Count))
          NotesInPhraseIterationsAll = Array.init arr.Levels.Count (fun _ -> Array.zeroCreate (arr.PhraseIterations.Count))
          NoteCounts = NoteCountsMutable() }
