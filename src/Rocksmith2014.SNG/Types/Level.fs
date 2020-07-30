namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces
open BinaryHelpers

type Level =
    { Difficulty : int32
      Anchors : Anchor[]
      AnchorExtensions : AnchorExtension[]
      HandShapes : FingerPrint[]
      Arpeggios : FingerPrint[]
      Notes : Note[]
      AverageNotesPerIteration : float32[]
      NotesInPhraseIterationsExclIgnored : int32[]
      NotesInPhraseIterationsAll : int32[] }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteInt32 this.Difficulty
            writeArray writer this.Anchors
            writeArray writer this.AnchorExtensions
            writeArray writer this.HandShapes
            writeArray writer this.Arpeggios
            writeArray writer this.Notes
            writer.WriteInt32 this.AverageNotesPerIteration.Length
            this.AverageNotesPerIteration |> Array.iter writer.WriteSingle
            writer.WriteInt32 this.NotesInPhraseIterationsExclIgnored.Length
            this.NotesInPhraseIterationsExclIgnored |> Array.iter writer.WriteInt32
            writer.WriteInt32 this.NotesInPhraseIterationsAll.Length
            this.NotesInPhraseIterationsAll |> Array.iter writer.WriteInt32

    static member Read(reader : IBinaryReader) =
        { Difficulty = reader.ReadInt32()
          Anchors = readArray reader Anchor.Read
          AnchorExtensions = readArray reader AnchorExtension.Read
          HandShapes = readArray reader FingerPrint.Read
          Arpeggios = readArray reader FingerPrint.Read
          Notes = readArray reader Note.Read
          AverageNotesPerIteration = readArray reader (fun r -> reader.ReadSingle())
          NotesInPhraseIterationsExclIgnored = readArray reader (fun r -> r.ReadInt32())
          NotesInPhraseIterationsAll = readArray reader (fun r -> r.ReadInt32()) }
