module Rocksmith2014.Common.Compression

open System
open System.IO
open ICSharpCode.SharpZipLib.Zip.Compression.Streams
open ICSharpCode.SharpZipLib.Zip.Compression

/// Zips the data from the input stream into the output stream in zlib format, using the best compression.
let zip (inStream: Stream) (outStream: Stream) =
    use zipStream = new DeflaterOutputStream(outStream, Deflater(Deflater.BEST_COMPRESSION), IsStreamOwner=false)
    inStream.CopyTo(zipStream)

/// Unzips zlib data from the input stream into the output stream.
let unzip (inStream: Stream) (outStream: Stream) =
    use inflateStream = new InflaterInputStream(inStream)
    inflateStream.CopyTo(outStream)

/// Divides the input stream into zipped blocks with the maximum block size.
let blockZip blockSize (deflatedData: ResizeArray<Stream>) (zLengths: ResizeArray<uint32>) (input: Stream) =
    let rec zipBlocks totalSize =
        if input.Position < input.Length then
            let size = Math.Min(int (input.Length - input.Position), blockSize)
            let buffer = Array.zeroCreate<byte> size
            let bytesRead = input.Read(buffer, 0, size)
            let pStream = new MemoryStream(buffer)
            let zStream = MemoryStreamPool.Default.GetStream()

            let data, length =
                zip pStream zStream
                let packedSize = int zStream.Length
                if packedSize < blockSize then
                    pStream.Dispose()
                    zStream, packedSize
                else
                    // Edge case: the size of the zipped data is equal to, or greater than the block size
                    assert (bytesRead = int pStream.Length)
                    zStream.Dispose()
                    pStream, bytesRead
            deflatedData.Add(data)
            zLengths.Add(uint32 length)
            zipBlocks (totalSize + int64 length)
        else
            totalSize

    input.Position <- 0L
    zipBlocks 0L
