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

type EditOptions =
    { Mode: EditMode
      EncyptTOC: bool }

type PSARC internal (source: Stream, header: Header, toc: ResizeArray<Entry>, blockSizeTable: uint32[]) =
    static let getZType (header: Header) =
        int <| Math.Log(float header.BlockSizeAlloc, 256.0)

    /// Reads the TOC and block size table from the given reader.
    static let readTOC (header: Header) (reader: IBinaryReader) =
        let toc = Seq.init (int header.TOCEntries) (Entry.Read reader) |> ResizeArray

        let tocChunkSize = int (header.TOCEntries * header.TOCEntrySize)
        let zType = getZType header
        let tocSize = int (header.TOCLength - 32u)
        let zNum = (tocSize - tocChunkSize) / zType
        let read = 
            match zType with
            | 2 -> fun _ -> uint32 (reader.ReadUInt16()) // 64KB
            | 3 -> fun _ -> reader.ReadUInt24() // 16MB
            | 4 -> fun _ -> reader.ReadUInt32() // 4GB
            | _ -> failwith "Unexpected zType"
        let zLengths = Array.init zNum read

        toc, zLengths

    let mutable blockSizeTable = blockSizeTable

    let inflateEntry (entry: Entry) (output: Stream) =
        let blockSize = int header.BlockSizeAlloc
        let mutable zIndex = int entry.zIndexBegin
        source.Position <- int64 entry.Offset
        let reader = BigEndianBinaryReader(source) :> IBinaryReader
    
        while output.Length < int64 entry.Length do
            match int blockSizeTable.[zIndex] with
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

            zIndex <- zIndex + 1

        output.Position <- 0L
        output.Flush()

    /// Returns true if the given named entry should not be zipped.
    let usePlain (entry: NamedEntry) =
        // WEM -> Packed vorbis data, zipping usually pointless
        // SNG -> Already zlib packed
        // AppId -> Very small file (6-7 bytes), unpacked in official files
        entry.Name.EndsWith(".wem") || entry.Name.EndsWith(".sng") || entry.Name.EndsWith("appid")

    let mutable manifest =
        if toc.Count > 1 then
            // Initialize the manifest if a TOC was given in the constructor
            use data = MemoryStreamPool.Default.GetStream()
            inflateEntry toc.[0] data
            toc.RemoveAt(0)
            use mReader = new StreamReader(data, true)
            mReader.ReadToEnd().Split('\n')
        else
            [||]

    /// Creates the data for the manifest.
    let createManifestData () =
        let memory = new MemoryStream()
        use writer = new BinaryWriter(memory, Encoding.ASCII, true)
        for i = 0 to manifest.Length - 1 do
            if i <> 0 then writer.Write('\n')
            writer.Write(Encoding.ASCII.GetBytes(manifest.[i]))
        memory

    let getName (entry: Entry) = manifest.[entry.ID - 1]

    /// Deflates the data in the given named entries.
    let deflateEntries (entries: ResizeArray<NamedEntry>) =
        let deflatedData = ResizeArray<Stream>()
        let protoEntries = ResizeArray<Entry * int64>()
        let blockSize = int header.BlockSizeAlloc
        let zLengths = ResizeArray<uint32>()

        // Add the manifest as the first entry
        entries.Insert(0, { Name = String.Empty; Data = createManifestData() })

        for i = 0 to entries.Count - 1 do
            let entry = entries.[i]
            let zBegin = uint32 zLengths.Count
            let usePlainData = usePlain entry
            let proto = Entry.CreateProto entry zBegin i

            let totalLength =
                if usePlainData then
                    deflatedData.Add(entry.Data)
                    if entry.Data.Length <= int64 blockSize then
                        zLengths.Add(uint32 entry.Data.Length)
                    else
                        // Calculate the number of blocks needed
                        let blockCount = int (entry.Data.Length / int64 blockSize)
                        let lastBlockSize = entry.Data.Length - int64 (blockCount * blockSize)
                        for _ = 1 to blockCount do zLengths.Add(uint32 blockSize)
                        if lastBlockSize <> 0L then zLengths.Add(uint32 lastBlockSize)
                    entry.Data.Length
                else
                    let mutable zSize = 0L
                    entry.Data.Position <- 0L
                    while entry.Data.Position < entry.Data.Length do
                        let size = Math.Min(int (entry.Data.Length - entry.Data.Position), blockSize)
                        let array = Array.zeroCreate<byte> size
                        let bytesRead = entry.Data.Read(array, 0, size)
                        let pStream = new MemoryStream(array)
                        let zStream = MemoryStreamPool.Default.GetStream()

                        let data, length =
                            Compression.zip pStream zStream
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
                        zSize <- zSize + int64 length
                    // The original data is no longer needed
                    entry.Data.Dispose()
                    zSize

            protoEntries.Add(proto, totalLength)

        protoEntries, deflatedData, zLengths.ToArray()

    /// Updates the table of contents with the given proto-entries and block size table.
    let updateToc (protoEntries: ResizeArray<Entry * int64>) (blockTable: uint32[]) zType encrypt =
        use tocData = MemoryStreamPool.Default.GetStream()
        let tocWriter = BigEndianBinaryWriter(tocData) :> IBinaryWriter
        
        // Update and write the table of contents
        toc.Clear()
        let mutable offset = uint64 header.TOCLength
        for i = 0 to protoEntries.Count - 1 do
            let protoEntry = fst protoEntries.[i]
            let entry = { protoEntry with Offset = offset }
            offset <- offset + ((snd >> uint64) protoEntries.[i])
            // Don't add the manifest to the TOC
            if i <> 0 then
                toc.Add(entry)
            entry.Write tocWriter

        // Update and write the block sizes table
        blockSizeTable <-
            blockTable
            |> Array.map (fun x -> if x = header.BlockSizeAlloc then 0u else x)
        let write =
            match zType with
            | 2 -> fun v -> tocWriter.WriteUInt16(uint16 v)
            | 3 -> fun v -> tocWriter.WriteUInt24(v)
            | 4 -> fun v -> tocWriter.WriteUInt32(v)
            | _ -> failwith "Unexpected zType."

        blockSizeTable |> Array.iter write
        
        tocData.Position <- 0L
        if encrypt then
            Cryptography.encrypt tocData source tocData.Length
        else
            tocData.CopyTo source

    /// Gets the manifest.
    member _.Manifest with get () = manifest.ToImmutableArray()

    /// Gets the table of contents.
    member _.TOC with get () = toc.AsReadOnly() 

    /// Inflates the given entry into the output stream.
    member _.InflateEntry (entry: Entry, output: Stream) = inflateEntry entry output

    /// Extracts all the files from the PSARC into the given directory.
    member _.ExtractFiles (baseDirectory: string) =
        for entry in toc do
            let path = Path.Combine(baseDirectory, (getName entry).Replace('/', '\\'))
            Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
            use file = File.Create path
            inflateEntry entry file

    /// Edits the contents of the PSARC with the given edit function.
    member _.Edit (options: EditOptions, editFunc: ResizeArray<NamedEntry> -> unit) =
        // Map the table of contents to entry names and data
        let namedEntries =
            let getTargetStream =
                match options.Mode with
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
        let protoEntries, data, blockTable = deflateEntries editList

        source.Position <- 0L
        source.SetLength 0L
        let writer = BigEndianBinaryWriter(source) :> IBinaryWriter

        // Update header
        let zType = getZType header
        header.TOCLength <- uint (Header.Length + protoEntries.Count * int header.TOCEntrySize + blockTable.Length * zType)
        header.TOCEntries <- uint protoEntries.Count
        header.ArchiveFlags <- if options.EncyptTOC then 4u else 0u
        header.Write writer 

        // Update and write TOC entries
        updateToc protoEntries blockTable zType options.EncyptTOC

        // Write the data to the source stream
        for dataEntry in data do
            using dataEntry (fun d -> d.Position <- 0L; d.CopyTo source)

        source.Flush()

        // Ensure that all entries that were inflated are disposed
        // If the user removed any entries, their data will be disposed of here
        namedEntries |> Array.iter (fun e -> e.Data.Dispose())

    /// Creates a new empty PSARC using the given stream.
    static member CreateEmpty(stream) = new PSARC(stream, Header(), ResizeArray(), [||])

    /// Creates a new PSARC into the given stream using the creation function.
    static member Create(stream, encrypt, createFun) =
        let options = { Mode = InMemory; EncyptTOC = encrypt }
        using (PSARC.CreateEmpty(stream)) (fun psarc -> psarc.Edit(options, createFun))

    /// Packs all the files in the directory and subdirectories into a PSARC file with the given filename.
    static member PackDirectory(path, targetFile, encrypt) =
        use file = File.Open(targetFile, FileMode.Create, FileAccess.ReadWrite)
        let sourceFiles = Directory.EnumerateFiles(path, "*.*",SearchOption.AllDirectories)
        PSARC.Create(file, encrypt, (fun packFiles ->
            for f in sourceFiles do
                let p = Path.GetRelativePath(path, f)
                let name = p.Replace('\\', '/')
                packFiles.Add( { Name = name; Data = File.OpenRead(f) })))

    /// Initializes a PSARC from the input stream. 
    static member Read (input: Stream) = 
        let reader = BigEndianBinaryReader(input) :> IBinaryReader
        let header = Header.Read reader
        let tocSize = int header.TOCLength - Header.Length

        let toc, zLengths =
            if header.IsEncrypted then
                use decStream = MemoryStreamPool.Default.GetStream()

                Cryptography.decrypt input decStream header.TOCLength

                if decStream.Length <> int64 tocSize then failwith "TOC decryption failed: Incorrect TOC size."

                readTOC header (BigEndianBinaryReader(decStream))
            else
                readTOC header reader

        new PSARC(input, header, toc, zLengths)

    // IDisposable implementation.
    interface IDisposable with member _.Dispose() = source.Dispose()
