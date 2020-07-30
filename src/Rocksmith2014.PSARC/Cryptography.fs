module Rocksmith2014.PSARC.Cryptography

open System
open System.IO
open System.Security.Cryptography
open System.Runtime.Intrinsics.X86

let private psarcKey =
    "\xC5\x3D\xB2\x38\x70\xA1\xA2\xF7\x1C\xAE\x64\x06\x1F\xDD\x0E\x11\x57\x30\x9D\xC8\x52\x04\xD4\xC5\xBF\xDF\x25\x09\x0D\xF2\x57\x2C"B

// Disable unsafe code warning
#nowarn "9"

/// AES CFB decryption utilizing SSE2 intrinsics.
let private aesCfbDecryptSIMD (input: Stream) (output: Stream) (key: byte[]) length =
    use aes = new AesManaged (Mode = CipherMode.ECB, Padding = PaddingMode.None)
    let blockSize = 16
    let iv = Array.zeroCreate<byte> 16
    let transform = aes.CreateEncryptor(key, iv)

    let buffer = Array.zeroCreate<byte> blockSize
    use bufPtr = fixed buffer
    let vecBlock = Array.zeroCreate<byte> blockSize
    use vecPtr = fixed vecBlock

    while input.Position < length do
        ignore <| transform.TransformBlock(buffer, 0, blockSize, vecBlock, 0)
    
        let toRead = Math.Min(length - input.Position, int64 buffer.Length)
        let bytesRead = input.Read(buffer, 0, int toRead)

        if bytesRead = blockSize then
            let v1 = Sse2.LoadVector128 bufPtr
            let v2 = Sse2.LoadVector128 vecPtr
            Sse2.Store(vecPtr, Sse2.Xor(v1, v2))
            output.Write(vecBlock, 0, bytesRead)
        else
            for i = 0 to bytesRead - 1 do
                output.WriteByte(buffer.[i] ^^^ vecBlock.[i])

let decrypt (input: Stream) (output: Stream) (length: uint32) =
    aesCfbDecryptSIMD input output psarcKey (int64 length)
    output.Flush()
    output.Position <- 0L
