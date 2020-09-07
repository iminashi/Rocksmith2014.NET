module Rocksmith2014.PSARC.Utils

open System.IO

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
