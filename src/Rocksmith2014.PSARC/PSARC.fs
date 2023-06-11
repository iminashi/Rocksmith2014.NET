namespace Rocksmith2014.PSARC

open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.BinaryWriters
open System
open System.Buffers
open System.IO
open System.Text
open System.Threading

type EditMode = InMemory | TempFiles

type EditOptions =
    { Mode: EditMode
      EncryptTOC: bool }

    /// Unpack entries into memory and encrypt the TOC.
    static member Default = { Mode = InMemory; EncryptTOC = true }

type PSARC internal (source: Stream, header: Header, toc: ResizeArray<Entry>, blockSizeTable: uint32 array) =
    let mutable blockSizeTable = blockSizeTable
    let inflateSemphore = new SemaphoreSlim(1, 1)

    let buffer =
        ArrayPool<byte>.Shared.Rent(int header.BlockSizeAlloc)

    let inflateEntry (entry: Entry) (output: Stream) =
        backgroundTask {
            let blockSize = int header.BlockSizeAlloc
            let mutable zIndex = int entry.ZIndexBegin

            try
                do! inflateSemphore.WaitAsync()
                source.Position <- int64 entry.Offset

                while output.Length < int64 entry.Length do
                    match int blockSizeTable[zIndex] with
                    | 0 ->
                        // Raw, full cluster used
                        do! source.ReadExactlyAsync(buffer, 0, blockSize)
                        do! output.WriteAsync(buffer, 0, blockSize)
                    | size ->
                        do! source.ReadExactlyAsync(buffer, 0, size)

                        if Utils.hasZlibHeader buffer then
                            try
                                use memory = new MemoryStream(buffer, 0, size)
                                Compression.unzip memory output
                            with _ ->
                                (* audio.psarc contains a block for a wem file that starts with the same bytes as a zlib header.
                                    Wem files are not compressed in official PSARC files.

                                    Whether a file is compressed cannot be determined from the first block.
                                    The Toolkit can create archives that contain SNG files that are partially compressed. *)
                                do! output.WriteAsync(buffer, 0, size)
                        else
                            do! output.WriteAsync(buffer, 0, size)

                    zIndex <- zIndex + 1
            finally
                inflateSemphore.Release() |> ignore

            output.Position <- 0L
            do! output.FlushAsync()
        }

    let mutable manifest =
        if toc.Count > 1 then
            // Initialize the manifest if a TOC was given in the constructor
            use data = MemoryStreamPool.Default.GetStream()
            (inflateEntry toc[0] data).Wait()
            toc.RemoveAt 0
            use reader = new StreamReader(data, true)
            [ while reader.Peek() >= 0 do reader.ReadLine() ]
        else
            []

    /// Creates the data for the manifest.
    let createManifestData () =
        let memory = MemoryStreamPool.Default.GetStream()
        use writer = new StreamWriter(memory, Encoding.ASCII, leaveOpen = true, NewLine = "\n")

        manifest
        |> List.iteri (fun i name ->
            if i <> 0 then writer.WriteLine()
            writer.Write(name))

        memory

    let getName (entry: Entry) = manifest[entry.ID - 1]

    let addPlainData blockSize (deflatedData: ResizeArray<Stream>) (zLengths: ResizeArray<uint32>) (data: Stream) =
        deflatedData.Add(data)

        if data.Length <= int64 blockSize then
            zLengths.Add(uint32 data.Length)
        else
            // Calculate the number of blocks needed
            let blockCount = int (data.Length / int64 blockSize)
            let lastBlockSize = data.Length - int64 (blockCount * blockSize)
            zLengths.AddRange(Seq.replicate blockCount (uint32 blockSize))
            if lastBlockSize <> 0L then zLengths.Add(uint32 lastBlockSize)

        data.Length

    /// Deflates the data in the given named entries.
    let deflateEntries (entries: NamedEntry list) =
        backgroundTask {
            // Add the manifest as the first entry
            let entries = { Name = String.Empty; Data = createManifestData () } :: entries

            let protoEntries = ResizeArray<Entry * int64>(entries.Length)
            let deflatedData = ResizeArray<Stream>()
            let blockSize = int header.BlockSizeAlloc
            let zLengths = ResizeArray<uint32>()

            for entry in entries do
                let proto = Entry.CreateProto entry (uint32 zLengths.Count)

                let! totalLength =
                    backgroundTask {
                        if Utils.usePlain entry then
                            return addPlainData blockSize deflatedData zLengths entry.Data
                        else
                            let! length = Compression.blockZip blockSize deflatedData zLengths entry.Data
                            do! entry.Data.DisposeAsync()
                            return length
                    }

                protoEntries.Add(proto, totalLength)

            return protoEntries.ToArray(), deflatedData, zLengths.ToArray()
        }

    /// Updates the table of contents with the given proto-entries and block size table.
    let updateToc (protoEntries: (Entry * int64) array) (blockTable: uint32 array) zType encrypt =
        use tocData = MemoryStreamPool.Default.GetStream()
        let tocWriter = BigEndianBinaryWriter(tocData) :> IBinaryWriter

        // Update and write the table of contents
        toc.Clear()
        let mutable offset = uint64 header.ToCLength

        protoEntries
        |> Array.iteri (fun i (proto, size) ->
            let entry = { proto with Offset = offset; ID = i }
            offset <- offset + uint64 size
            // Don't add the manifest to the ToC
            if i <> 0 then toc.Add(entry)
            entry.Write(tocWriter))

        // Update and write the block sizes table
        blockSizeTable <-
            blockTable
            |> Array.map (fun x -> if x = header.BlockSizeAlloc then 0u else x)

        let write =
            match zType with
            | 2 -> (uint16 >> tocWriter.WriteUInt16)
            | 3 -> tocWriter.WriteUInt24
            | 4 -> tocWriter.WriteUInt32
            | _ -> failwith "Unexpected zType."

        blockSizeTable |> Array.iter write

        tocData.Position <- 0L
        if encrypt then
            Cryptography.encrypt tocData source
            // Ignore the zero padding from the encryption (https://github.com/dotnet/runtime/issues/85205)
            source.Position <- int64 header.ToCLength
        else
            tocData.CopyTo(source)

    let createNamedEntries mode =
        async {
            let getTargetStream =
                match mode with
                | InMemory  -> fun () -> MemoryStreamPool.Default.GetStream() :> Stream
                | TempFiles -> Utils.getTempFileStream

            let! entries =
                toc
                |> Seq.map (fun entry ->
                    async {
                        let data = getTargetStream ()
                        do! inflateEntry entry data |> Async.AwaitTask
                        return { Name = getName entry; Data = data }
                    })
                |> Async.Sequential

            return new DisposableList<_>(List.ofArray entries)
        }

    let tryFindEntry name =
        match List.tryFindIndex ((=) name) manifest with
        | None -> raise <| FileNotFoundException($"PSARC did not contain file '{name}'", name)
        | Some i -> toc[i]

    /// Gets the manifest.
    member _.Manifest = manifest

    /// Gets the table of contents.
    member _.TOC = toc.AsReadOnly()

    /// Inflates the given entry into the output stream.
    member _.InflateEntry(entry: Entry, output: Stream) = inflateEntry entry output

    /// Inflates the entry with the given file name into the output stream.
    member _.InflateFile(name: string, output: Stream) =
        backgroundTask {
            let entry = tryFindEntry name
            do! inflateEntry entry output
        }

    /// Inflates the entry with the given file name into the target file.
    member this.InflateFile(name: string, targetFile: string) =
        backgroundTask {
            use file = File.Create(targetFile)
            do! this.InflateFile(name, file)
        }

    /// Returns an in-memory read stream for the entry with the given name.
    member _.GetEntryStream(name: string) =
        backgroundTask {
            let entry = tryFindEntry name
            let memory = MemoryStreamPool.Default.GetStream(name, int entry.Length)
            do! inflateEntry entry memory
            return memory
        }

    /// Extracts all the files from the PSARC into the given directory.
    member _.ExtractFiles(baseDirectory: string, ?progress: float -> unit) =
        backgroundTask {
            let reportFrequency = max 4 (toc.Count / 50)
            let reportProgress =
                match progress with
                | Some progress ->
                    fun i -> if i % reportFrequency = 0 then progress (float (i + 1) / float toc.Count * 100.)
                | None ->
                    ignore

            for i = 0 to toc.Count - 1 do
                let entry = toc[i]

                let path =
                    Path.Combine(baseDirectory, Utils.fixDirSeparator (getName entry))

                Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore

                use file = File.Create(path)
                do! inflateEntry entry file
                reportProgress i
        }

    /// Edits the contents of the PSARC with the given edit function.
    member _.Edit(options: EditOptions, editFunc: NamedEntry list -> NamedEntry list) =
        backgroundTask {
            // Map the table of contents to entry names and data
            use! namedEntries = createNamedEntries options.Mode

            // Call the edit function that returns a new list
            let editList = editFunc namedEntries.Items

            // Update the manifest
            manifest <- List.map (fun entry -> entry.Name) editList

            // Deflate entries
            let! protoEntries, data, blockTable = deflateEntries editList

            source.Position <- 0L
            source.SetLength 0L
            let writer = BigEndianBinaryWriter(source) :> IBinaryWriter

            // Update header
            let zType = Utils.getZType header
            header.ToCLength <- uint (Header.Length + protoEntries.Length * int header.ToCEntrySize + blockTable.Length * zType)
            header.ToCEntryCount <- uint protoEntries.Length
            header.ArchiveFlags <- if options.EncryptTOC then 4u else 0u
            header.Write(writer)

            // Update and write TOC entries
            updateToc protoEntries blockTable zType options.EncryptTOC

            // Write the data to the source stream
            for dataEntry in data do
                dataEntry.Position <- 0L
                do! dataEntry.CopyToAsync(source)
                do! dataEntry.DisposeAsync()

            do! source.FlushAsync()
        }

    /// Creates a new empty PSARC using the given stream.
    static member CreateEmpty(stream) = new PSARC(stream, Header(), ResizeArray(), Array.empty)

    /// Creates a new PSARC into the given stream with the given contents.
    static member Create(stream, encrypt, content) =
        backgroundTask {
            let options = { Mode = InMemory; EncryptTOC = encrypt }
            use psarc = PSARC.CreateEmpty(stream)
            do! psarc.Edit(options, fun _ -> content)
        }

    /// Creates a new PSARC file with the given contents.
    static member Create(fileName, encrypt, content) =
        backgroundTask {
            use file = Utils.createFileStreamForPSARC fileName
            do! PSARC.Create(file, encrypt, content)
        }

    /// Packs all the files in the directory and subdirectories into a PSARC file with the given filename.
    static member PackDirectory(path, targetFile: string, encrypt) =
        backgroundTask {
            do! PSARC.Create(targetFile, encrypt, [
                for file in Utils.getAllFiles path do
                    let name = Path.GetRelativePath(path, file).Replace('\\', '/')
                    { Name = name; Data = Utils.getFileStreamForRead file } ]
            )
        }

    /// Initializes a PSARC from the input stream.
    static member Read(input: Stream) =
        let reader = BigEndianBinaryReader(input) :> IBinaryReader
        let header = Header.Read(reader)
        let tocSize = int header.ToCLength - Header.Length

        let toc, zLengths =
            if header.IsEncrypted then
                use decStream = Cryptography.decrypt input tocSize

                if decStream.Length <> int64 tocSize then
                    failwith "ToC decryption failed: Incorrect ToC size."

                Utils.readToC header (BigEndianBinaryReader(decStream))
            else
                Utils.readToC header reader

        new PSARC(input, header, toc, zLengths)

    /// Initializes a PSARC from a file from the given path.
    static member OpenFile(path: string) =
        Utils.openFileStreamForPSARC path
        |> PSARC.Read

    // IDisposable implementation.
    interface IDisposable with
        member _.Dispose() =
            source.Dispose()
            inflateSemphore.Dispose()
            ArrayPool.Shared.Return(buffer)
