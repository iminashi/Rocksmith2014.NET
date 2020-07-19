module Rocksmith2014.SNG.BinaryHelpers

open System.IO
open System
open System.Text
open Interfaces

let readZeroTerminatedUTF8String length (reader:BinaryReader) =
    let arr = reader.ReadBytes length
    match Array.IndexOf(arr, 0uy) with
    | 0 -> String.Empty
    | -1 -> Encoding.UTF8.GetString arr
    | stringEnd -> Encoding.UTF8.GetString(arr, 0, stringEnd)

let writeZeroTerminatedUTF8String length (str:string) (writer:BinaryWriter) =
    let arr = Array.zeroCreate length
    Encoding.UTF8.GetBytes(str.AsSpan(), arr.AsSpan()) |> ignore
    writer.Write arr

let readArray (reader:BinaryReader) read =
    let length = reader.ReadInt32()
    Array.init length (fun _ -> read reader)

let writeArray<'a when 'a :> IBinaryWritable> (writer:BinaryWriter) (arr : 'a[]) =
    writer.Write(arr.Length)
    arr |> Array.iter (fun e -> e.Write(writer))
