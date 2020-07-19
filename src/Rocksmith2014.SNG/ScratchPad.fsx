open System.IO
open System.Security.Cryptography
open System.Collections.Generic

let sngKeyPC = 
    [|  0xCBuy; 0x64uy; 0x8Duy; 0xF3uy; 0xD1uy; 0x2Auy; 0x16uy; 0xBFuy
        0x71uy; 0x70uy; 0x14uy; 0x14uy; 0xE6uy; 0x96uy; 0x19uy; 0xECuy
        0x17uy; 0x1Cuy; 0xCAuy; 0x5Duy; 0x2Auy; 0x14uy; 0x2Euy; 0x3Euy
        0x59uy; 0xDEuy; 0x7Auy; 0xDDuy; 0xA1uy; 0x8Auy; 0x3Auy; 0x30uy |]

sngKeyPC.Length

let f = File.OpenRead @"F:\RS2014Customs\characters.txt"


// Write zlib header
//outStream.Write([| 0x78uy; 0xDAuy |], 0, 2)
//use deflateStream = new DeflateStream(outStream, CompressionLevel.Optimal, true)
//inStream.CopyTo(deflateStream)
//deflateStream.Flush()

//let buffer = Array.zeroCreate<byte> 4
//BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(), adler32 inStream)
//outStream.Write(buffer, 0, 4)

//let adler32 (data:Stream) =
//    data.Position <- 0L
//    use reader = new BinaryReader(data, Encoding.UTF8, true)
//    let modulus = 65521u
//    let mutable a = 1u
//    let mutable b = 0u

//    for i = 0 to (int32 data.Length) - 1 do
//        a <- (a + (uint (reader.ReadByte()))) % modulus
//        b <- (b + a) % modulus

//    (b <<< 16) ||| a


//use deflateStream = new DeflateStream(inStream, CompressionMode.Decompress)
//deflateStream.CopyTo(outStream)



//let zlibHeader = reader.ReadUInt16()
//decrypted.Position <- decrypted.Position - 2L

// 78 DA - Best Compression
//if zlibHeader = 0x78DAus || zlibHeader = 0xDA78us then


let increment (arr:byte[]) =
    let rec inc index =
        arr.[index] <- arr.[index] + 1uy
        if arr.[index] = 0uy && index <> 0 then
            inc (index - 1)
    inc (arr.Length - 1)

let aesCtrTransform (input:Stream) (output:Stream) (key:byte[]) (iv:byte[]) =
    use aes = new AesManaged (Mode = CipherMode.ECB, Padding = PaddingMode.None)
    let blockSize = aes.BlockSize / 8
    if iv.Length <> blockSize then failwith "IV size must be same as block size."

    let counter = Array.copy iv
    let xorMasks = Queue<byte>()
    let counterEncryptor = aes.CreateEncryptor(key, Array.zeroCreate<byte> blockSize)

    while input.Position < input.Length do
        if xorMasks.Count = 0 then
            let counterModeBlock = Array.zeroCreate<byte> blockSize
            ignore <| counterEncryptor.TransformBlock(counter, 0, counter.Length, counterModeBlock, 0)
            increment counter
            counterModeBlock |> Array.iter xorMasks.Enqueue

        let mask = xorMasks.Dequeue()
        let byte = input.ReadByte() |> byte
        output.WriteByte(byte ^^^ mask)