module BinaryWriterBuilder

open Rocksmith2014.EOF.EOFTypes
open System.IO
open System.Text

type Writer = BinaryWriter -> unit
 
let toFile path (f: Writer) = using (new BinaryWriter(File.Create(path))) f

let toStream s (f: Writer) = using (new BinaryWriter(s)) f

let writeString (b: BinaryWriter) (str: string) =
    let bytes = Encoding.ASCII.GetBytes(str)
    b.Write(bytes.Length |> int16)
    b.Write(bytes)
 
type BinaryWriterBuilder () =
    member inline _.Yield (str: string) =
        fun (b: BinaryWriter) -> writeString b str

    member _.Yield (notes: EOFNote array) =
        fun (b: BinaryWriter) ->
            // Number of notes
            b.Write(notes.Length)

            notes
            |> Array.iter (fun note ->
                writeString b note.ChordName
                b.Write(note.ChordNumber)
                b.Write(note.NoteType)
                b.Write(note.BitFlag)
                b.Write(note.GhostBitFlag)
                b.Write(note.Frets)
                b.Write(note.LegacyBitFlags)
                b.Write(note.Position)
                b.Write(note.Length)
                b.Write(note.Flags |> uint)
                note.SlideEndFret |> ValueOption.iter b.Write
                note.BendStrength |> ValueOption.iter b.Write
                note.UnpitchedSlideEndFret |> ValueOption.iter b.Write
                if note.ExtendedNoteFlags <> EOFExtendedNoteFlag.ZERO then b.Write(note.ExtendedNoteFlags |> uint)
            )

    member _.Yield (sections: EOFSection array) =
        fun (b: BinaryWriter) ->
            // Number of sections
            b.Write(sections.Length)

            sections
            |> Array.iter (fun section ->
                // Name
                writeString b section.Name
                // Type
                b.Write(section.Type)
                // Start time (or data)
                b.Write(section.StartTime)
                // End time (or data)
                b.Write(section.EndTime)
                // Flags
                b.Write(section.Flags)
            )

    member inline _.Yield (data: byte array) = fun (b: BinaryWriter) -> b.Write(data)
    member inline _.Yield (i: sbyte) = fun (b: BinaryWriter) -> b.Write(i)
    member inline _.Yield (i: byte) = fun (b: BinaryWriter) -> b.Write(i)
    member inline _.Yield (i: uint16) = fun (b: BinaryWriter) -> b.Write(i)
    member inline _.Yield (i: uint32) = fun (b: BinaryWriter) -> b.Write(i)
    member inline _.Yield (i: int32) = fun (b: BinaryWriter) -> b.Write(i)
    member inline _.Yield (i: int64) = fun (b: BinaryWriter) -> b.Write(i)
    member inline _.Yield (i: byte voption) = fun (b: BinaryWriter) -> i |> ValueOption.iter b.Write

    member inline _.YieldFrom (f: Writer) = f

    member inline _.Zero() = ignore

    member _.Combine (f, g) = fun (b: BinaryWriter) -> f b; g b

    member _.Delay f = fun (b: BinaryWriter) -> (f()) b
        
    member _.For (xs: 'a seq, f: 'a -> Writer) =
        fun (b: BinaryWriter) ->
            use e = xs.GetEnumerator()
            while e.MoveNext() do
                (f e.Current) b
        
    member _.While (p: unit -> bool, f: Writer) =
        fun (b: BinaryWriter) -> while p () do f b
 
let binaryWriter = new BinaryWriterBuilder ()
