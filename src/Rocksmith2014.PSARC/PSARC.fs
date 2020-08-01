namespace Rocksmith2014.PSARC

open System
open System.IO
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.Interfaces
open Rocksmith2014.Common

type PSARC =
    { Header : Header
      Manifest : ResizeArray<string>
      TOC : ResizeArray<Entry>
      Stream : Stream
      zBlocksSizeList : uint32[] }

    member this.GetName(entry: Entry) = this.Manifest.[entry.ID - 1]

    interface IDisposable with
        member this.Dispose() = this.Stream.Dispose()

module PSARC =
    let private parseTOC (header: Header) (reader: IBinaryReader) =
        let tocFiles = int header.TOCEntries
        let toc = ResizeArray<Entry>(tocFiles)
        for i = 0 to tocFiles - 1 do
            let entry =
                { ID = i
                  NameDigest = reader.ReadBytes(16)
                  zIndexBegin = reader.ReadUInt32()
                  Length = reader.ReadUInt40()
                  Offset = reader.ReadUInt40() }
            toc.Add(entry)

        let tocChunkSize = int (header.TOCEntries * header.TOCEntrySize)
        let bNum = int <| Math.Log(float header.BlockSizeAlloc, 256.0)
        let tocSize = int (header.TOCLength - 32u)
        let zNum = (tocSize - tocChunkSize) / bNum
        let read = 
            match bNum with
            | 2 -> fun _ -> uint32 (reader.ReadUInt16()) // 64KB
            | 3 -> fun _ -> reader.ReadUInt24() // 16MB
            | 4 -> fun _ -> reader.ReadUInt32() // 4GB
            | _ -> failwith "Unexpected bNum"
        let zLengths = Array.init zNum read

        toc, zLengths

    let private inflateInternal (entry: Entry)
                                (blockSizeList: uint32[])
                                (blockSize: int)
                                (psarcStream: Stream)
                                (output: Stream) =
        let mutable zChunkID = int entry.zIndexBegin
        psarcStream.Position <- int64 entry.Offset
        let reader = BigEndianBinaryReader(psarcStream) :> IBinaryReader
    
        while output.Length < int64 entry.Length do
            match int blockSizeList.[zChunkID] with
            | 0 ->
                // Raw, full cluster used
                output.Write(reader.ReadBytes(blockSize), 0, blockSize)
            | size -> 
                let array = reader.ReadBytes(size)

                // Check for zlib header
                if array.[0] = 0x78uy && array.[1] = 0xDAuy then
                    use memory = new MemoryStream(array)
                    Compression.unzip memory output
                else
                    // Uncompressed
                    output.Write(array, 0, array.Length)

            zChunkID <- zChunkID + 1

        output.Position <- 0L
        output.Flush()

    let inflateEntry (entry: Entry) (psarc: PSARC) (output: Stream) =
        inflateInternal entry psarc.zBlocksSizeList (int psarc.Header.BlockSizeAlloc) psarc.Stream output

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

        use data = MemoryStreamPool.Default.GetStream()
        inflateInternal toc.[0] zLengths (int header.BlockSizeAlloc) stream data
        toc.RemoveAt(0)
        use mReader = new StreamReader(data, true)
        let manifest = ResizeArray(mReader.ReadToEnd().Split('\n'))

        { Header = header
          Manifest = manifest
          TOC = toc
          Stream = stream
          zBlocksSizeList = zLengths }

    let extractFiles (baseDirectory: string) (psarc: PSARC) =
        for entry in psarc.TOC do
            let path = Path.Combine(baseDirectory, psarc.GetName(entry).Replace('/', '\\'))
            Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
            use file = File.Create(path)
            inflateEntry entry psarc file
