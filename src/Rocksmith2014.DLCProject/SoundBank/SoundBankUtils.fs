module Rocksmith2014.DLCProject.SoundBankUtils

open Rocksmith2014.Common.Interfaces
open Rocksmith2014.Common
open System.IO

/// Calculates a Fowler–Noll–Vo hash for a string.
let fnvHash (str: string) =
    (2166136261u, str.ToLower().ToCharArray())
    ||> Array.fold (fun hash ch -> (hash * 16777619u) ^^^ (uint32 ch))

/// Seeks to the beginning of the stream and returns a binary reader for the platform.
let initReader (stream: Stream) platform =
    stream.Position <- 0L
    BinaryReaders.getReader stream platform

/// Seeks the stream from the current position by the given offset.
let seek (stream: Stream) offset =
    stream.Seek(offset, SeekOrigin.Current) |> ignore

/// Skips a section that is preceded by its length in a BNK file.
let skipSection (stream: Stream) (reader: IBinaryReader) =
    let length = reader.ReadUInt32()
    seek stream (int64 length)

/// Seeks to the section that has the given name and returns its length.
let rec seekToSection (stream: Stream) (reader: IBinaryReader) name =
    if stream.Position >= stream.Length then
        Error $"Could not find {name} section."
    else
        let magic = reader.ReadBytes 4
        if magic <> name then
            skipSection stream reader
            seekToSection stream reader name
        else
            // Return the length of the section
            Ok <| reader.ReadUInt32()

/// Seeks to the hierarchy object that has the given ID.
let seekToObject (stream: Stream) (reader: IBinaryReader) typeId =
    let objCount = reader.ReadUInt32()

    let rec doSeek objIndex =
        if objIndex = objCount then
            Error $"Could not find object with ID {typeId}."
        else
            let currentId = reader.ReadInt8()
            if currentId <> typeId then
                skipSection stream reader
                doSeek (objIndex + 1u)
            else
                Ok ()

    doSeek 0u

/// Returns the padding size for the header chunk.
let getHeaderPaddingSize = function PC | Mac -> 3

module Result =
    /// Maps the result to Error e -> Error e | Ok x -> Ok ().
    let ignore x = Result.map ignore x
        
type ResultBuilder() =
    member _.Bind(x, f) = Result.bind f x
    member _.Return(x) = Ok x

let result = ResultBuilder()
