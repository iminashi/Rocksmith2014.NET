module Rocksmith2014.PSARC.Utils

open System.IO

let getTempFileStream () =
    new FileStream(
        Path.GetTempFileName(),
        FileMode.Create,
        FileAccess.ReadWrite,
        FileShare.None,
        4096,
        FileOptions.DeleteOnClose) :> Stream

let getFileStreamForRead (fileName: string) =
    new FileStream(
        fileName,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        4096,
        FileOptions.SequentialScan)
