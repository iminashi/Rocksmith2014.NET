module Rocksmith2014.SNG.SNGFile

open System.IO
open Rocksmith2014.Common.Interfaces
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common
open BinaryWriters

/// Unpacks the given encrypted SNG file and saves it with an "_unpacked.sng" postfix.
let unpackFile fileName platform =
    use file = File.OpenRead fileName
    let targetFile =
        Path.Combine
            (Path.GetDirectoryName(fileName),
             Path.GetFileNameWithoutExtension(fileName)
             + "_unpacked.sng")

    use out = File.Open(targetFile, FileMode.Create, FileAccess.Write)
    SNG.unpack file out platform

let readStream (input: Stream) platform =
    use memory = MemoryStreamPool.Default.GetStream()
    let reader = BinaryReaders.getReader memory platform

    SNG.unpack input memory platform
    SNG.Read reader

/// Reads an encrypted SNG file. 
let readPacked fileName platform =
    use file = File.OpenRead fileName
    use memory = MemoryStreamPool.Default.GetStream()
    let reader = BinaryReaders.getReader memory platform

    SNG.unpack file memory platform
    SNG.Read reader

/// Saves an SNG (packed/encrypted) with the given filename.
let savePacked fileName platform (sng: SNG) =
    use memory = MemoryStreamPool.Default.GetStream()
    let writer = BinaryWriters.getWriter memory platform
    (sng :> IBinaryWritable).Write writer
    memory.Position <- 0L

    use out = File.Open(fileName, FileMode.Create, FileAccess.Write)
    SNG.pack memory out platform

/// Reads an unpacked SNG from the given file.
let readUnpacked fileName =
    use stream = File.OpenRead fileName
    let reader = LittleEndianBinaryReader(stream)

    SNG.Read reader

/// Saves an SNG (plain) with the given filename.
let saveUnpacked fileName (sng: SNG) =
    use stream = File.Open(fileName, FileMode.Create, FileAccess.Write)
    let writer = LittleEndianBinaryWriter(stream)

    (sng :> IBinaryWritable).Write writer
