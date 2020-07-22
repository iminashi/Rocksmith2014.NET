module Rocksmith2014.SNG.SNG

open Microsoft.IO
open System.IO
open System
open System.Text
open Interfaces
open Rocksmith2014.SNG.Types

let memoryManager = RecyclableMemoryStreamManager()

/// Decrypts and unpacks an SNG from the input stream into the output stream.
let unpack (input:Stream) (output:Stream) platform =
    use decrypted = memoryManager.GetStream()
    use reader = new BinaryReader(decrypted)

    Cryptography.decryptSNG input decrypted platform

    let plainLength = reader.ReadUInt32()
    Compression.unzip decrypted output
    output.Position <- 0L

/// Packs and decrypts an SNG from the input stream into the output stream.
let pack (input:Stream) (output:Stream) platform =
    let header = 3
    use writer = new BinaryWriter(output)
    writer.Write(0x4A)
    writer.Write(header)

    use payload = memoryManager.GetStream()
    // Write the uncompressed length
    payload.Write(BitConverter.GetBytes(input.Length |> int32), 0, 4)
    Compression.zip input payload

    payload.Position <- 0L
    Cryptography.encryptSNG payload output platform None

/// Writes the given SNG into the output stream (unpacked/unencrypted).
let write (output:Stream) (sng:SNG) =
    use writer = new BinaryWriter(output, Encoding.UTF8, true)
    (sng :> IBinaryWritable).Write writer
