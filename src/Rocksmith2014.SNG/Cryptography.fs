module Rocksmith2014.SNG.Cryptography

open System.Security.Cryptography
open System.Runtime.Intrinsics.X86
open System.IO
open System
open Rocksmith2014.Common

let private sngKeyPC =
    "\xCB\x64\x8D\xF3\xD1\x2A\x16\xBF\x71\x70\x14\x14\xE6\x96\x19\xEC\x17\x1C\xCA\x5D\x2A\x14\x2E\x3E\x59\xDE\x7A\xDD\xA1\x8A\x3A\x30"B

let private sngKeyMac =
    "\x98\x21\x33\x0E\x34\xB9\x1F\x70\xD0\xA4\x8C\xBD\x62\x59\x93\x12\x69\x70\xCE\xA0\x91\x92\xC0\xE6\xCD\xA6\x76\xCC\x98\x38\x28\x9D"B

let private getKey = function
    | PC -> sngKeyPC
    | Mac -> sngKeyMac

/// Increments a number that is represented as an array of bytes.
let private increment (arr: byte[]) =
    let rec inc index =
        arr.[index] <- arr.[index] + 1uy
        if arr.[index] = 0uy && index <> 0 then
            inc (index - 1)
    inc (arr.Length - 1)

// Disable unsafe code warning
#nowarn "9"

// At the moment, .NET Core does not support AES CTR, so it is implemented here via ECB.

/// AES CTR encryption utilizing SSE2 intrinsics. Slightly faster than the non SIMD version.
let private aesCtrTransformSIMD (input: Stream) (output: Stream) (key: byte[]) (iv: byte[]) =
    use aes = new AesManaged (Mode = CipherMode.ECB, Padding = PaddingMode.None)
    let blockSize = 16
    let counterEncryptor = aes.CreateEncryptor(key, null)

    let counter = Array.copy iv
    let buffer = Array.zeroCreate<byte> blockSize
    use bufPtr = fixed buffer
    let counterModeBlock = Array.zeroCreate<byte> blockSize
    use ctrPtr = fixed counterModeBlock

    while input.Position < input.Length do
        ignore <| counterEncryptor.TransformBlock(counter, 0, counter.Length, counterModeBlock, 0)
        increment counter
    
        let bytesRead = input.Read(buffer, 0, blockSize)
        if bytesRead = blockSize then
            let v1 = Sse2.LoadVector128 bufPtr
            let v2 = Sse2.LoadVector128 ctrPtr
            Sse2.Store(bufPtr, Sse2.Xor(v1, v2))
            output.Write(buffer, 0, bytesRead)
        else
            for i = 0 to bytesRead - 1 do
                output.WriteByte(buffer.[i] ^^^ counterModeBlock.[i])

/// Based on https://stackoverflow.com/a/51188472
let private aesCtrTransform (input: Stream) (output: Stream) (key: byte[]) (iv: byte[]) =
    use aes = new AesManaged (Mode = CipherMode.ECB, Padding = PaddingMode.None)
    let blockSize = 16
    let ctr = Array.copy iv
    let counterEncryptor = aes.CreateEncryptor(key, null)
    let buffer = (Array.zeroCreate<byte> blockSize).AsSpan()
    let counterModeBlock = Array.zeroCreate<byte> blockSize

    while input.Position < input.Length do
        ignore <| counterEncryptor.TransformBlock(ctr, 0, blockSize, counterModeBlock, 0)
        increment ctr

        let bytesRead = input.Read(buffer)
        for i = 0 to bytesRead - 1 do
            output.WriteByte(buffer.[i] ^^^ counterModeBlock.[i])

/// Decrypts an encrypted SNG from the input stream into the output stream.
let decryptSNG (input: Stream) (output: Stream) (platform: Platform) =
    let reader = BinaryReaders.getReader input platform
    if reader.ReadUInt32() <> 0x4Au then invalidOp "Not a valid SNG file."
    let header = reader.ReadUInt32()
    let iv = reader.ReadBytes(16)
    let key = getKey platform

    if Sse2.IsSupported then
        aesCtrTransformSIMD input output key iv
    else
        aesCtrTransform input output key iv

    output.Flush()
    output.Position <- 0L

/// Encrypts a plain SNG from the input stream into the output stream.
let encryptSNG (input: Stream) (output: Stream) (platform: Platform) (iv: byte[] option) =
    let key = getKey platform
    let iv = iv |> Option.defaultWith (fun _ -> Array.zeroCreate<byte> 16)

    output.Write(iv, 0, iv.Length)

    aesCtrTransform input output key iv
