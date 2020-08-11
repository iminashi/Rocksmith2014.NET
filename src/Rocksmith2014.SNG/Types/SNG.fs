namespace Rocksmith2014.SNG

open Rocksmith2014.Common.Interfaces
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.BinaryWriters
open Rocksmith2014.Common
open BinaryHelpers
open System.IO

type NoteCounts =
    { Easy : int; Medium : int; Hard : int; Ignored : int }

    static member Empty = { Easy = 0; Medium = 0; Hard = 0; Ignored = 0 }

type SNG =
    { Beats : Beat[]
      Phrases : Phrase[]
      Chords : Chord[]
      ChordNotes : ChordNotes[]
      Vocals : Vocal[]
      SymbolsHeaders : SymbolsHeader[]
      SymbolsTextures : SymbolsTexture[]
      SymbolDefinitions : SymbolDefinition[]
      PhraseIterations : PhraseIteration[]
      PhraseExtraInfo : PhraseExtraInfo[]
      NewLinkedDifficulties : NewLinkedDifficulty[]
      Actions : Action[]
      Events : Event[]
      Tones : Tone[]
      DNAs : DNA[]
      Sections : Section[]
      Levels : Level[]
      MetaData : MetaData
      NoteCounts : NoteCounts }

    interface IBinaryWritable with
        member this.Write(writer) =
            let inline write a = writeArray writer a

            write this.Beats
            write this.Phrases
            write this.Chords
            write this.ChordNotes
            write this.Vocals
            if this.Vocals.Length > 0 then
                write this.SymbolsHeaders
                write this.SymbolsTextures
                write this.SymbolDefinitions
            write this.PhraseIterations
            write this.PhraseExtraInfo
            write this.NewLinkedDifficulties
            write this.Actions
            write this.Events
            write this.Tones
            write this.DNAs
            write this.Sections
            write this.Levels
            (this.MetaData :> IBinaryWritable).Write writer

    static member Read(reader: IBinaryReader) =
        let inline read f = readArray reader f

        let beats = read Beat.Read
        let phrases = read Phrase.Read
        let chords = read Chord.Read
        let chordNotes = read ChordNotes.Read
        let vocals = read Vocal.Read

        { Beats = beats
          Phrases = phrases
          Chords = chords
          ChordNotes = chordNotes
          Vocals = vocals
          SymbolsHeaders = if vocals.Length > 0 then read SymbolsHeader.Read else [||]
          SymbolsTextures = if vocals.Length > 0 then read SymbolsTexture.Read else [||]
          SymbolDefinitions = if vocals.Length > 0 then read SymbolDefinition.Read else [||]
          PhraseIterations = read PhraseIteration.Read
          PhraseExtraInfo = read PhraseExtraInfo.Read
          NewLinkedDifficulties = read NewLinkedDifficulty.Read
          Actions = read Action.Read
          Events = read Event.Read
          Tones = read Tone.Read
          DNAs = read DNA.Read
          Sections = read Section.Read
          Levels = read Level.Read
          MetaData = MetaData.Read reader
          NoteCounts = NoteCounts.Empty }

module SNG =
    let Empty =
        { Beats = [||]; Phrases = [||]; Chords = [||]; ChordNotes = [||]
          Vocals = [||]; SymbolsHeaders = [||]; SymbolsTextures = [||]; SymbolDefinitions = [||]
          PhraseIterations = [||]; PhraseExtraInfo = [||]; NewLinkedDifficulties = [||]
          Actions = [||]; Events = [||]; Tones = [||]; DNAs = [||]; Sections = [||]; Levels = [||]
          MetaData = MetaData.Empty; NoteCounts = NoteCounts.Empty }

    /// Decrypts and unpacks an SNG from the input stream into the output stream.
    let unpack (input: Stream) (output: Stream) platform = async {
        use decrypted = MemoryStreamPool.Default.GetStream()
        let reader = BinaryReaders.getReader decrypted platform
        
        Cryptography.decryptSNG input decrypted platform
        
        let plainLength = reader.ReadUInt32()
        do! Compression.unzip decrypted output
        output.Position <- 0L }
    
    /// Packs and encrypts an SNG from the input stream into the output stream.
    let pack (input: Stream) (output: Stream) platform = async {
        let header = 3
        let writer = BinaryWriters.getWriter output platform
        writer.WriteInt32 0x4A
        writer.WriteInt32 header
        
        use payload = MemoryStreamPool.Default.GetStream()
        // Write the uncompressed length
        (BinaryWriters.getWriter payload platform).WriteInt32 (input.Length |> int32)
        do! Compression.zip input payload
        
        payload.Position <- 0L
        Cryptography.encryptSNG payload output platform None }
    
    /// Writes the given SNG into the output stream (unpacked/unencrypted).
    let write (output: Stream) (sng: SNG) =
        let writer = LittleEndianBinaryWriter(output)
        (sng :> IBinaryWritable).Write writer

    /// Unpacks the given encrypted SNG file and saves it with an "_unpacked.sng" postfix.
    let unpackFile fileName platform = async {
        use file = File.OpenRead fileName
        let targetFile =
            Path.Combine
                (Path.GetDirectoryName(fileName),
                 Path.GetFileNameWithoutExtension(fileName)
                 + "_unpacked.sng")
    
        use out = File.Open(targetFile, FileMode.Create, FileAccess.Write)
        do! unpack file out platform }
    
    /// Reads an SNG from the stream.
    let fromStream (input: Stream) platform = async {
        use memory = MemoryStreamPool.Default.GetStream()
        let reader = BinaryReaders.getReader memory platform
    
        do! unpack input memory platform
        return SNG.Read reader }
    
    /// Reads an encrypted SNG file. 
    let readPackedFile fileName platform = async {
        use file = File.OpenRead fileName
        use memory = MemoryStreamPool.Default.GetStream()
        let reader = BinaryReaders.getReader memory platform
    
        do! unpack file memory platform
        return SNG.Read reader }
    
    /// Saves an SNG (packed/encrypted) with the given filename.
    let savePackedFile fileName platform (sng: SNG) = async {
        use memory = MemoryStreamPool.Default.GetStream()
        let writer = BinaryWriters.getWriter memory platform
        (sng :> IBinaryWritable).Write writer
        memory.Position <- 0L
    
        use out = File.Open(fileName, FileMode.Create, FileAccess.Write)
        do! pack memory out platform }
    
    /// Reads an unpacked SNG from the given file.
    let readUnpackedFile fileName =
        use stream = File.OpenRead fileName
        let reader = LittleEndianBinaryReader(stream)
    
        SNG.Read reader
    
    /// Saves an SNG (plain) with the given filename.
    let saveUnpackedFile fileName (sng: SNG) =
        use stream = File.Open(fileName, FileMode.Create, FileAccess.Write)
        let writer = LittleEndianBinaryWriter(stream)
    
        (sng :> IBinaryWritable).Write writer
