module BinaryFileWriter

open System.IO
open System.Text

type Writer = BinaryWriter -> unit
 
let toFile path (f: Writer) = using (new BinaryWriter(File.Create(path))) f

let toStream s (f: Writer) = using (new BinaryWriter(s)) f
 
type BinaryWriterBuilder () =
    member inline _.Yield (str: string) =
        fun (b: BinaryWriter) ->
            let bytes = Encoding.ASCII.GetBytes(str)
            b.Write(bytes.Length |> int16)
            b.Write(bytes)

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
