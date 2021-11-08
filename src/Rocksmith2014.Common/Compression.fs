module Rocksmith2014.Common.Compression

open System
open System.IO
open System.IO.Compression

/// Zips the data from the input stream into the output stream in zlib format, asynchronously, using the best compression.
let asyncZip (input: Stream) (output: Stream) = async {
    use zipStream = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen=true)
    do! input.CopyToAsync(zipStream, 65536) }

/// Zips the data from the input stream into the output stream in zlib format, using the best compression.
let zip bufferSize (input: Stream) (output: Stream) =
    use zipStream = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen=true)
    input.CopyTo(zipStream, bufferSize)

/// Unzips zlib data from the input stream into the output stream.
let unzip (input: Stream) (output: Stream) =
    use inflateStream = new ZLibStream(input, CompressionMode.Decompress, leaveOpen=true)
    inflateStream.CopyTo(output, 65536)

/// Asynchronously unzips zlib data from the input stream into the output stream.
let asyncUnzip (input: Stream) (output: Stream) = async {
    use inflateStream = new ZLibStream(input, CompressionMode.Decompress, leaveOpen=true)
    do! inflateStream.CopyToAsync(output, 65536) }

/// Returns an inflate stream for the input stream.
let getInflateStream (input: Stream) = new ZLibStream(input, CompressionMode.Decompress, leaveOpen=true)

/// Divides the input stream into zipped blocks with the maximum block size.
let blockZip blockSize (deflatedData: ResizeArray<Stream>) (zLengths: ResizeArray<uint32>) (input: Stream) = async {
    let mutable totalSize = 0L
    input.Position <- 0L

    while input.Position < input.Length do
        let size = Math.Min(int (input.Length - input.Position), blockSize)
        let buffer = Array.zeroCreate<byte> size
        let! bytesRead = input.AsyncRead(buffer)
        let plainStream = new MemoryStream(buffer)
        let zipStream = MemoryStreamPool.Default.GetStream("", size)

        let data, length =
            zip size plainStream zipStream
            let packedSize = int zipStream.Length
            if packedSize < blockSize then
                plainStream.Dispose()
                zipStream, packedSize
            else
                // Edge case: the size of the zipped data is equal to, or greater than the block size
                assert (bytesRead = int plainStream.Length)
                zipStream.Dispose()
                plainStream, bytesRead

        deflatedData.Add(data)
        zLengths.Add(uint32 length)
        totalSize <- totalSize + int64 length

    return totalSize }
