module Rocksmith2014.PSARC.Utils

open System.IO
open System
open Rocksmith2014.Common

let inline internal getZType (header: Header) =
    int <| Math.Log(float header.BlockSizeAlloc, 256.0)

/// Reads the ToC and block size table from the given reader.
let internal readToC (header: Header) (reader: IBinaryReader) =
    let toc = ResizeArray.init (int header.ToCEntryCount) (Entry.Read reader)

    let zType = getZType header
    let tocSize = int (header.ToCEntryCount * header.ToCEntrySize)
    let blockSizeTableLength = int header.ToCLength - Header.Length - tocSize
    let blockSizeCount = blockSizeTableLength / zType
    let read = 
        match zType with
        | 2 -> fun _ -> uint32 (reader.ReadUInt16()) // 64KB
        | 3 -> fun _ -> reader.ReadUInt24() // 16MB
        | 4 -> fun _ -> reader.ReadUInt32() // 4GB
        | _ -> failwith "Unexpected zType"
    let blockSizes = Array.init blockSizeCount read

    toc, blockSizes

/// Returns true if the given named entry should not be zipped.
let internal usePlain (entry: NamedEntry) =
    // WEM -> Packed vorbis data, zipping usually pointless
    // SNG -> Already zlib packed
    // AppId -> Very small file (6-7 bytes), unpacked in official files
    // 7z -> Already compressed (found in cache.psarc)
    List.exists (fun x -> String.endsWith x entry.Name) [ ".wem"; ".sng"; "appid"; "7z" ]

/// Returns a file stream for a temporary file that will be deleted when the stream is closed.
let getTempFileStream () =
    new FileStream(
        Path.GetTempFileName(),
        FileMode.Create,
        FileAccess.ReadWrite,
        FileShare.None,
        4096,
        FileOptions.DeleteOnClose ||| FileOptions.Asynchronous) :> Stream

/// Returns a file stream for reading a file to be added into a PSARC.
let getFileStreamForRead (fileName: string) =
    new FileStream(
        fileName,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        4096,
        FileOptions.SequentialScan ||| FileOptions.Asynchronous)

/// Returns a file stream for opening an existing PSARC file.
let openFileStreamForPSARC (fileName: string) =
    new FileStream(
        fileName,
        FileMode.Open,
        FileAccess.ReadWrite,
        FileShare.None,
        65536,
        FileOptions.Asynchronous)

/// Returns a file stream for creating a new PSARC file.
let createFileStreamForPSARC (fileName: string) =
    new FileStream(
        fileName,
        FileMode.Create,
        FileAccess.ReadWrite,
        FileShare.None,
        65536,
        FileOptions.Asynchronous)

/// Fixes the platform-specific directory separator character when extracting files from a PSARC.
let fixDirSeparator (path: string) =
    if Path.DirectorySeparatorChar = '/' then path else path.Replace('/', '\\')

/// Finds all files in the given path and its subdirectories.
let getAllFiles path = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
