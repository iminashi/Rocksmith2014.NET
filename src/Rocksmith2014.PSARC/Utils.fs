module Rocksmith2014.PSARC.Utils

open System.IO

let getTempFileStream () =
    new FileStream(
        Path.GetTempFileName(),
        FileMode.Create,
        FileAccess.ReadWrite,
        FileShare.None,
        4096,
        FileOptions.DeleteOnClose ||| FileOptions.Asynchronous) :> Stream

let getFileStreamForRead (fileName: string) =
    new FileStream(
        fileName,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        4096,
        FileOptions.SequentialScan ||| FileOptions.Asynchronous)

let openFileStreamForPSARC (fileName: string) =
    new FileStream(
        fileName,
        FileMode.Open,
        FileAccess.ReadWrite,
        FileShare.None,
        65536,
        FileOptions.Asynchronous)

let createFileStreamForPSARC (fileName: string) =
    new FileStream(
        fileName,
        FileMode.Create,
        FileAccess.ReadWrite,
        FileShare.None,
        65536,
        FileOptions.Asynchronous)

let fixDirSeparator (path: string) =
    if Path.DirectorySeparatorChar = '/' then path else path.Replace('/', '\\')
