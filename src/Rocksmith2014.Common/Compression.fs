module Rocksmith2014.Common.Compression

open System.IO
open ICSharpCode.SharpZipLib.Zip.Compression.Streams
open ICSharpCode.SharpZipLib.Zip.Compression

let zip (inStream: Stream) (outStream: Stream) =
    use zipStream = new DeflaterOutputStream(outStream, Deflater(Deflater.BEST_COMPRESSION), IsStreamOwner=false)
    inStream.CopyTo(zipStream)

let unzip (inStream: Stream) (outStream: Stream) =
    use inflateStream = new InflaterInputStream(inStream)
    inflateStream.CopyTo(outStream)
