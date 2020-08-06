module Rocksmith2014.Common.Compression

open System
open System.IO
open ICSharpCode.SharpZipLib.Zip.Compression.Streams
open ICSharpCode.SharpZipLib.Zip.Compression

/// Zips the data from the input stream into the output stream in zlib format, using the best compression.
let zip (inStream: Stream) (outStream: Stream) = async {
    use zipStream = new DeflaterOutputStream(outStream, Deflater(Deflater.BEST_COMPRESSION), IsStreamOwner = false)
    do! inStream.CopyToAsync(zipStream, 65536) }

/// Unzips zlib data from the input stream into the output stream.
let unzip (inStream: Stream) (outStream: Stream) = async {
    use inflateStream = new InflaterInputStream(inStream, IsStreamOwner = false)
    do! inflateStream.CopyToAsync(outStream, 65536) }

/// Divides the input stream into zipped blocks with the maximum block size.
let blockZip blockSize (deflatedData: ResizeArray<Stream>) (zLengths: ResizeArray<uint32>) (input: Stream) = async {
    let mutable totalSize = 0L
    input.Position <- 0L
    
    while input.Position < input.Length do
        let size = Math.Min(int (input.Length - input.Position), blockSize)
        let buffer = Array.zeroCreate<byte> size
        let! bytesRead = input.AsyncRead(buffer)
        let pStream = new MemoryStream(buffer)
        let zStream = MemoryStreamPool.Default.GetStream()
    
        let! data, length =
            async {
                do! zip pStream zStream
                let packedSize = int zStream.Length
                if packedSize < blockSize then
                    do! pStream.DisposeAsync()
                    return zStream, packedSize
                else
                    // Edge case: the size of the zipped data is equal to, or greater than the block size
                    assert (bytesRead = int pStream.Length)
                    do! zStream.DisposeAsync()
                    return pStream, bytesRead
            }
        deflatedData.Add data
        zLengths.Add(uint32 length)
        totalSize <- totalSize + int64 length
    return totalSize }
