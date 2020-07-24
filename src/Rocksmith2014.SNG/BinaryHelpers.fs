module Rocksmith2014.SNG.BinaryHelpers

open System
open System.Text
open Interfaces

let readZeroTerminatedUTF8String length (reader: IBinaryReader) =
    let arr = reader.ReadBytes length
    match Array.IndexOf(arr, 0uy) with
    | 0 -> String.Empty
    | -1 -> Encoding.UTF8.GetString arr
    | stringEnd -> Encoding.UTF8.GetString(arr, 0, stringEnd)

let writeZeroTerminatedUTF8String length (str: string) (writer: IBinaryWriter) =
    let arr = Array.zeroCreate length
    Encoding.UTF8.GetBytes(str.AsSpan(), arr.AsSpan()) |> ignore
    writer.WriteBytes arr

/// Reads an array that is preceded by its length from the IBinaryReader using the given read function.
let readArray (reader: IBinaryReader) read =
    let length = reader.ReadInt32()
    Array.init length (fun _ -> read reader)

/// Writes an array of elements into the IBinaryWriter preceded by its length.
let writeArray<'a when 'a :> IBinaryWritable> (writer: IBinaryWriter) (arr: 'a[]) =
    writer.WriteInt32 arr.Length
    arr |> Array.iter (fun e -> e.Write writer)
