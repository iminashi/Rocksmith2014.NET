namespace Rocksmith2014.PSARC

open System
open System.IO
open System.Text
open System.Buffers
open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.BinaryWriters

type EditMode = InMemory | TempFiles

type EditOptions =
    { Mode: EditMode; EncryptTOC: bool }

    /// Unpack entries into memory and encrypt the TOC.
    static member Default = { Mode = InMemory; EncryptTOC = true }

type PSARC internal (source: Stream, header: Header, toc: ResizeArray<Entry>, blockSizeTable: uint32[]) =
    let mutable blockSizeTable = blockSizeTable
    let buffer = ArrayPool<byte>.Shared.Rent (int header.BlockSizeAlloc)

    let inflateEntry (entry: Entry) (output: Stream) = async {
        let blockSize = int header.BlockSizeAlloc
        let mutable zIndex = int entry.ZIndexBegin
        source.Position <- int64 entry.Offset

        // Determine if the file is compressed from the first block
        let isCompressed =
            source.Read(buffer, 0, 2) |> ignore
            source.Seek(-2L, SeekOrigin.Current) |> ignore
            Utils.hasZlibHeader buffer

        while output.Length < int64 entry.Length do
            match int blockSizeTable.[zIndex] with
            | 0 ->
                // Raw, full cluster used
                let! _bytesRead = source.AsyncRead(buffer, 0, blockSize)
                do! output.AsyncWrite(buffer, 0, blockSize)
            | size ->
                let! _bytesRead = source.AsyncRead(buffer, 0, size)

                // Confirm that the zlib header is present
                // The Toolkit creates archives where the wem files are compressed, but may contain uncompressed blocks
                if isCompressed && Utils.hasZlibHeader buffer then
                    use memory = new MemoryStream(buffer, 0, size)
                    Compression.unzip memory output
                else
                    do! output.AsyncWrite(buffer, 0, size)

            zIndex <- zIndex + 1

        output.Position <- 0L
        do! output.FlushAsync() }

    let mutable manifest =
        if toc.Count > 1 then
            // Initialize the manifest if a TOC was given in the constructor
            use data = MemoryStreamPool.Default.GetStream()
            inflateEntry toc.[0] data |> Async.RunSynchronously
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
            writer.Write name)
        memory

    let getName (entry: Entry) = manifest.[entry.ID - 1]

    let addPlainData blockSize (deflatedData: ResizeArray<Stream>) (zLengths: ResizeArray<uint32>) (data: Stream) =
        deflatedData.Add data
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
    let deflateEntries (entries: NamedEntry list) = async {
        // Add the manifest as the first entry
        let entries = { Name = String.Empty; Data = createManifestData() }::entries

        let protoEntries = ResizeArray<Entry * int64>(entries.Length)
        let deflatedData = ResizeArray<Stream>()
        let blockSize = int header.BlockSizeAlloc
        let zLengths = ResizeArray<uint32>()

        for entry in entries do
            let proto = Entry.CreateProto entry (uint32 zLengths.Count)

            let! totalLength = async {
                if Utils.usePlain entry then 
                    return addPlainData blockSize deflatedData zLengths entry.Data
                else
                    let! length = Compression.blockZip blockSize deflatedData zLengths entry.Data
                    do! entry.Data.DisposeAsync()
                    return length }

            protoEntries.Add(proto, totalLength)

        return protoEntries.ToArray(), deflatedData, zLengths.ToArray() }

    /// Updates the table of contents with the given proto-entries and block size table.
    let updateToc (protoEntries: (Entry * int64)[]) (blockTable: uint32[]) zType encrypt =
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
            if i <> 0 then toc.Add entry
            entry.Write tocWriter)

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
            Cryptography.encrypt tocData source tocData.Length
        else
            tocData.CopyTo source

    let createNamedEntries mode = async {
        let getTargetStream =
            match mode with
            | InMemory  -> fun () -> MemoryStreamPool.Default.GetStream() :> Stream
            | TempFiles -> Utils.getTempFileStream

        let! entries =
            toc
            |> Seq.map (fun entry -> async {
                let data = getTargetStream ()
                do! inflateEntry entry data
                return { Name = getName entry; Data = data } })
            |> Async.Sequential

        return new DisposableList<_>(List.ofArray entries) }

    let tryFindEntry name =
        match List.tryFindIndex ((=) name) manifest with
        | None -> raise <| FileNotFoundException($"PSARC did not contain file '{name}'", name)
        | Some i -> toc.[i]

    /// Gets the manifest.
    member _.Manifest = manifest

    /// Gets the table of contents.
    member _.TOC = toc.AsReadOnly() 

    /// Inflates the given entry into the output stream.
    member _.InflateEntry (entry: Entry, output: Stream) = inflateEntry entry output

    /// Inflates the entry with the given file name into the output stream.
    member _.InflateFile (name: string, output: Stream) = async {
        let entry = tryFindEntry name
        do! inflateEntry entry output }

    /// Inflates the entry with the given file name into the target file.
    member this.InflateFile (name: string, targetFile: string) = async {
        use file = File.Create targetFile
        do! this.InflateFile(name, file) }

    /// Returns an in-memory read stream for the entry with the given name.
    member _.GetEntryStream (name: string) = async {
        let entry = tryFindEntry name
        let memory = MemoryStreamPool.Default.GetStream(name, int entry.Length)
        do! inflateEntry entry memory
        return memory }

    /// Extracts all the files from the PSARC into the given directory.
    member _.ExtractFiles (baseDirectory: string, ?progress: IProgress<float>) = async {
        let reportFrequency = max 4 (toc.Count / 50)
        for i = 0 to toc.Count - 1 do
            let entry = toc.[i]
            let path = Path.Combine(baseDirectory, Utils.fixDirSeparator (getName entry))
            Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
            use file = File.Create path
            do! inflateEntry entry file
            match progress with
            | Some progress when i % reportFrequency = 0 ->
                progress.Report(float (i + 1) / float toc.Count * 100.)
            | _ ->
                () }

    /// Edits the contents of the PSARC with the given edit function.
    member _.Edit (options: EditOptions, editFunc: NamedEntry list -> NamedEntry list) = async {
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
        header.Write writer

        // Update and write TOC entries
        updateToc protoEntries blockTable zType options.EncryptTOC

        // Write the data to the source stream
        for dataEntry in data do
            dataEntry.Position <- 0L
            do! dataEntry.CopyToAsync(source)
            do! dataEntry.DisposeAsync()

        do! source.FlushAsync() }

    /// Creates a new empty PSARC using the given stream.
    static member CreateEmpty(stream) = new PSARC(stream, Header(), ResizeArray(), Array.empty)

    /// Creates a new PSARC into the given stream with the given contents.
    static member Create(stream, encrypt, content) = async {
        let options = { Mode = InMemory; EncryptTOC = encrypt }
        use psarc = PSARC.CreateEmpty(stream)
        do! psarc.Edit(options, fun _ -> content) }

    /// Creates a new PSARC file with the given contents.
    static member Create(fileName, encrypt, content) = async {
        use file = Utils.createFileStreamForPSARC fileName
        do! PSARC.Create(file, encrypt, content) }

    /// Packs all the files in the directory and subdirectories into a PSARC file with the given filename.
    static member PackDirectory(path, targetFile: string, encrypt) = async {
        do! PSARC.Create(targetFile, encrypt, [
            for file in Utils.getAllFiles path do
                let name = Path.GetRelativePath(path, file).Replace('\\', '/')
                { Name = name; Data = Utils.getFileStreamForRead file } ]
        ) }

    /// Initializes a PSARC from the input stream.
    static member Read (input: Stream) = 
        let reader = BigEndianBinaryReader(input) :> IBinaryReader
        let header = Header.Read reader
        let tocSize = int header.ToCLength - Header.Length

        let toc, zLengths =
            if header.IsEncrypted then
                use decStream = MemoryStreamPool.Default.GetStream()
                Cryptography.decrypt input decStream header.ToCLength

                if decStream.Length <> int64 tocSize then failwith "ToC decryption failed: Incorrect ToC size."

                Utils.readToC header (BigEndianBinaryReader(decStream))
            else
                Utils.readToC header reader

        new PSARC(input, header, toc, zLengths)

    /// Initializes a PSARC from a file with the given name.
    static member ReadFile (fileName: string) =
        Utils.openFileStreamForPSARC fileName
        |> PSARC.Read

    // IDisposable implementation.
    interface IDisposable with
        member _.Dispose() =
            source.Dispose()
            ArrayPool.Shared.Return buffer
