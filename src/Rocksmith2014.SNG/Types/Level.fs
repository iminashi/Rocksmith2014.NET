namespace Rocksmith2014.SNG

open Rocksmith2014.Common
open BinaryHelpers

type Level =
    { Difficulty : int32
      Anchors : Anchor array
      AnchorExtensions : AnchorExtension array
      HandShapes : FingerPrint array
      Arpeggios : FingerPrint array
      Notes : Note array
      AverageNotesPerIteration : float32 array
      NotesInPhraseIterationsExclIgnored : int32 array
      NotesInPhraseIterationsAll : int32 array }

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

    static member Read(reader: IBinaryReader) =
        { Difficulty = reader.ReadInt32()
          Anchors = readArray reader Anchor.Read
          AnchorExtensions = readArray reader AnchorExtension.Read
          HandShapes = readArray reader FingerPrint.Read
          Arpeggios = readArray reader FingerPrint.Read
          Notes = readArray reader Note.Read
          AverageNotesPerIteration = readArray reader (fun r -> r.ReadSingle())
          NotesInPhraseIterationsExclIgnored = readArray reader (fun r -> r.ReadInt32())
          NotesInPhraseIterationsAll = readArray reader (fun r -> r.ReadInt32()) }
