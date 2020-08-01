namespace Rocksmith2014.PSARC

open System
open System.IO
open System.Collections.Immutable
open System.Text
open Rocksmith2014.Common
open Rocksmith2014.Common.Interfaces
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.BinaryWriters

type EditMode = InMemory | TempFiles

type PSARC internal (source: Stream, header: Header, toc: ResizeArray<Entry>, blockSizeTable: uint32[]) =
    static let parseTOC (header: Header) (reader: IBinaryReader) =
        let tocFileCount = int header.TOCEntries
        let toc = ResizeArray<Entry>(tocFileCount)
        for i = 0 to tocFileCount - 1 do
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

    let mutable blockSizeTable = blockSizeTable

    let inflateEntry (entry: Entry) (output: Stream) =
        let blockSize = int header.BlockSizeAlloc
        let mutable zChunkID = int entry.zIndexBegin
        source.Position <- int64 entry.Offset
        let reader = BigEndianBinaryReader(source) :> IBinaryReader
    
        while output.Length < int64 entry.Length do
            match int blockSizeTable.[zChunkID] with
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

    let shouldZip (entry: NamedEntry) =
        not (entry.Name.EndsWith(".wem") || entry.Name.EndsWith(".sng") || entry.Name.EndsWith("appid"))

    let mutable manifest =
        if toc.Count > 1 then
            use data = MemoryStreamPool.Default.GetStream()
            inflateEntry toc.[0] data
            toc.RemoveAt(0)
            use mReader = new StreamReader(data, true)
            mReader.ReadToEnd().Split('\n')
        else
            [||]

    let createManifestData () =
        let memory = new MemoryStream()
        use writer = new BinaryWriter(memory, Encoding.ASCII, true)
        for i = 0 to manifest.Length - 1 do
            if i <> 0 then writer.Write('\n')
            writer.Write(manifest.[i])
        memory

    let getName (entry: Entry) = manifest.[entry.ID - 1]

    let deflateEntries (entries: ResizeArray<NamedEntry>) =
        let deflatedData = ResizeArray<Entry * ResizeArray<byte[]>>()
        let blockSize = int header.BlockSizeAlloc
        let zLengths = ResizeArray<uint32>()

        // Add the manifest as the first entry
        entries.Insert(0, { Name = String.Empty; Data = createManifestData() })

        for i = 0 to entries.Count - 1 do
            let entry = entries.[i]
            let zList = ResizeArray<byte[]>()
            let zBegin = uint32 zLengths.Count
            entry.Data.Position <- 0L
            let doZip = shouldZip entry

            while entry.Data.Position < entry.Data.Length do
                let size = Math.Min(int entry.Data.Length, blockSize)
                let array = Array.zeroCreate<byte> size
                let bytesRead = entry.Data.Read(array, 0, size)

                let data, length =
                    if doZip then
                        use pStream = new MemoryStream(array)
                        use zStream = new MemoryStream()
                        Compression.zip pStream zStream
                        let packedSize = int zStream.Length
                        if packedSize <= blockSize then
                            zStream.ToArray(), packedSize
                        else
                            array, bytesRead
                    else 
                        array, bytesRead
                zList.Add(data)
                zLengths.Add(uint32 length)
            
            let e = 
                { NameDigest = Cryptography.md5Hash entry.Name
                  zIndexBegin = zBegin
                  Length = uint64 entry.Data.Length
                  Offset = 0UL // Will be set later
                  ID = i }

            entry.Data.Dispose()

            deflatedData.Add((e, zList))

        deflatedData, zLengths

    member val Manifest = manifest.ToImmutableArray()
    member val TOC = toc.AsReadOnly()

    member _.GetName (entry: Entry) = getName entry
    member _.InflateEntry (entry: Entry, output: Stream) = inflateEntry entry output

    member _.ExtractFiles (baseDirectory: string) =
        for entry in toc do
            let path = Path.Combine(baseDirectory, (getName entry).Replace('/', '\\'))
            Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
            use file = File.Create path
            inflateEntry entry file

    member _.Edit (mode: EditMode, editFunc: ResizeArray<NamedEntry> -> unit) =
        // Map the table of contents to entry names and data
        let namedEntries =
            let getTargetStream =
                match mode with
                | InMemory -> fun () -> MemoryStreamPool.Default.GetStream() :> Stream
                | TempFiles -> fun () -> File.OpenWrite(Path.GetTempFileName()) :> Stream

            toc
            |> Seq.map (fun e -> 
                let data = getTargetStream ()
                inflateEntry e data
                { Name = getName e
                  Data = data })
            |> Seq.toArray

        // Call the edit function that mutates the resize array
        let editList = ResizeArray<NamedEntry>(namedEntries)
        editFunc editList

        // Update the manifest
        manifest <- editList |> Seq.map (fun e -> e.Name) |> Array.ofSeq
        
        // Deflate entries
        let dataEntries, blockTable = deflateEntries editList

        source.Position <- 0L
        let writer = BigEndianBinaryWriter(source) :> IBinaryWriter

        // Update header
        let bNum = int <| Math.Log(float header.BlockSizeAlloc, 256.0)
        header.TOCLength <- uint (32 + dataEntries.Count * int header.TOCEntrySize + blockTable.Count * bNum)
        header.TOCEntries <- uint dataEntries.Count
        header.Write writer 

        // Update and write TOC entries
        toc.Clear()
        let mutable offset = uint64 header.TOCLength
        for i = 0 to dataEntries.Count - 1 do
            let protoEntry = fst dataEntries.[i]
            let entry = { protoEntry with Offset = offset }
            offset <- offset + entry.Length
            // Don't add the manifest to the TOC
            if i <> 0 then
                toc.Add(entry)
            entry.Write writer

        // Update and write the block sizes table
        blockSizeTable <-
            blockTable
            |> Seq.map(fun z -> if z = header.BlockSizeAlloc then 0u else z)
            |> Seq.toArray
        let write =
            match bNum with
            | 2 -> fun v -> writer.WriteUInt16(uint16 v)
            | 3 -> fun v -> writer.WriteUInt24(v)
            | 4 -> fun v -> writer.WriteUInt32(v)
            | _ -> failwith "Unexpected bNum."

        blockSizeTable |> Seq.iter write

        // Encrypt TOC
        // TODO

        // Write the data to the source stream
        let mutable zCounter = 0
        for i = 0 to dataEntries.Count - 1 do
            let data = snd dataEntries.[i]
            for j = 0 to data.Count - 1 do
                source.Write(data.[j], 0, int blockTable.[zCounter])
                zCounter <- zCounter + 1

        let last = toc.[toc.Count - 1]
        source.SetLength(int64 (last.Offset + last.Length))
        source.Flush()

        // Ensure that all entries that were inflated are disposed
        namedEntries |> Array.iter (fun e -> e.Data.Dispose())

    static member Create(stream) = new PSARC(stream, Header(), ResizeArray(), [||])

    static member Read (input: Stream) = 
        let reader = BigEndianBinaryReader(input) :> IBinaryReader
        let header = Header.Read reader
        let tocSize = int (header.TOCLength - 32u)

        let toc, zLengths =
            if header.IsEncrypted then
                use decStream = MemoryStreamPool.Default.GetStream()

                Cryptography.decrypt input decStream header.TOCLength

                if decStream.Length <> int64 tocSize then failwith "TOC decryption failed: Incorrect TOC size."

                parseTOC header (BigEndianBinaryReader(decStream))
            else
                parseTOC header reader

        new PSARC(input, header, toc, zLengths)

    interface IDisposable with member _.Dispose() = source.Dispose()
