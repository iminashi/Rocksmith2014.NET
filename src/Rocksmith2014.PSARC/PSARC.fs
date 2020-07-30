namespace Rocksmith2014.PSARC

open System
open System.IO
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.Interfaces
open Rocksmith2014.Common
open System.Buffers

type PSARC =
    { Header : Header
      Manifest : Entry
      TOC : ResizeArray<Entry>
      Stream : Stream
      zBlocksSizeList : uint32[] }

    interface IDisposable with
        member this.Dispose() = this.Stream.Dispose()

module PSARC =
    let private parseTOC (header: Header) (reader: IBinaryReader) =
        let tocFiles = int header.TOCEntries
        let tocSize = int (header.TOCLength - 32u)
        let toc = ResizeArray<Entry>(tocFiles)
        for i = 0 to tocFiles - 1 do
            let entry =
                { ID = i
                  NameDigest = reader.ReadBytes(16)
                  zIndexBegin = reader.ReadUInt32()
                  Length = reader.ReadUInt40()
                  Offset = reader.ReadUInt40() 
                  Name = "" }
            toc.Add(entry)

        let tocChunkSize = int (header.TOCEntries * header.TOCEntrySize)
        let bNum = int <| Math.Log(float header.BlockSizeAlloc, 256.0)
        let zNum = (tocSize - tocChunkSize) / bNum
        let zLengths = Array.zeroCreate<uint32> zNum
        for i = 0 to zNum - 1 do
            zLengths.[i] <-
                match bNum with
                | 2 -> uint32 (reader.ReadUInt16())
                | 3 -> reader.ReadUInt24()
                | 4 -> reader.ReadUInt32()
                | _ -> failwith "Unexpected bNum"

        toc, zLengths

    let inflateEntry (entry: Entry) (psarc: PSARC) (output: Stream) =
        let mutable zChunkID = int entry.zIndexBegin
        let blockSize = int psarc.Header.BlockSizeAlloc
        let input = psarc.Stream
        input.Position <- int64 entry.Offset
        let reader = BigEndianBinaryReader(input) :> IBinaryReader
        
        while output.Length < int64 entry.Length do
            match int psarc.zBlocksSizeList.[zChunkID] with
            | 0 ->
                // Raw, full cluster used
                output.Write(reader.ReadBytes(blockSize), 0, blockSize)
            | size -> 
                let array = reader.ReadBytes(size)

                // Check for zlib header
                if array.[0] = 0x78uy && array.[1] = 0xDAuy then
                    use memory = MemoryStreamPool.Default.GetStream(array)
                    Compression.unzip memory output
                else
                    // Uncompressed
                    output.Write(array, 0, array.Length)

            zChunkID <- zChunkID + 1

        output.Position <- 0L
        output.Flush()

    let read (stream: Stream) = 
        let reader = BigEndianBinaryReader(stream) :> IBinaryReader
        let header = Header.read reader
        let tocSize = int (header.TOCLength - 32u)

        let toc, zLengths =
            if header.IsEncrypted then
                use decStream = MemoryStreamPool.Default.GetStream()

                Cryptography.decrypt stream decStream header.TOCLength

                if decStream.Length <> int64 tocSize then
                    failwith "TOC decryption failed: Incorrect TOC size."

                parseTOC header (BigEndianBinaryReader(decStream))
            else
                parseTOC header reader

        let psarc = 
            { Header = header
              Manifest = toc.[0]
              TOC = toc
              Stream = stream
              zBlocksSizeList = zLengths }

        if header.CompressionMethod <> "zlib" then
            failwith "Unsupported compression type."
        else
            toc.RemoveAt(0)
            use data = MemoryStreamPool.Default.GetStream()
            inflateEntry psarc.Manifest psarc data
            use mReader = new StreamReader(data, true)
            let lines = mReader.ReadToEnd().Split('\n')

            for i = 0 to lines.Length - 1 do
                toc.[i] <- { toc.[i] with Name = lines.[i] }
        psarc

    let extractFiles (baseDirectory: string) (psarc: PSARC) =
        for entry in psarc.TOC do
            let path = Path.Combine(baseDirectory, entry.Name.Replace('/', '\\'))
            Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
            use file = File.Create(path)
            inflateEntry entry psarc file
