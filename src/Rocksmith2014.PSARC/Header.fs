namespace Rocksmith2014.PSARC

open Rocksmith2014.Common
open System.Text
open System

[<Sealed>]
type internal Header() =
    /// The length of a PSARC header in bytes (32).
    static member Length = 32

    /// The file identifier string "PSAR".
    member val Magic = "PSAR"

    /// The major file version, should be 1.
    member val VersionMajor = 1us with get, set

    /// The minor file version, should be 4.
    member val VersionMinor = 4us with get, set

    /// The method used for compressing the data, should be "zlib".
    member val CompressionMethod = "zlib"

    /// The length of the ToC in bytes, including the header (32 bytes) and the block size table.
    member val ToCLength = 0u with get, set

    /// The size of a ToC entry, default: 30 bytes.
    member val ToCEntrySize = 30u with get, set

    /// The number of entries in the table of contents.
    member val ToCEntryCount = 0u with get, set

    /// The maximum size of a block, default: 64KB.
    member val BlockSizeAlloc = 65536u with get, set

    /// Configuration flags for the PSARC file.
    member val ArchiveFlags = 0u with get, set

    /// Returns true if the header is encrypted.
    member this.IsEncrypted = this.ArchiveFlags = 4u

    /// Writes a PSARC header into the writer.
    member this.Write (writer: IBinaryWriter) =
        writer.WriteBytes "PSAR"B
        writer.WriteUInt16 this.VersionMajor
        writer.WriteUInt16 this.VersionMinor
        writer.WriteBytes "zlib"B
        writer.WriteUInt32 this.ToCLength
        writer.WriteUInt32 this.ToCEntrySize
        writer.WriteUInt32 this.ToCEntryCount
        writer.WriteUInt32 this.BlockSizeAlloc
        writer.WriteUInt32 this.ArchiveFlags

    /// Reads a PSARC header from the reader.
    static member Read (reader: IBinaryReader) =
        let magic = Encoding.ASCII.GetString(reader.ReadBytes 4)
        let versionMaj = reader.ReadUInt16()
        let versionMin = reader.ReadUInt16()
        let compressionMethod = Encoding.ASCII.GetString(reader.ReadBytes 4)

        if magic <> "PSAR" then
            failwith "PSARC header magic check failed."
        elif compressionMethod <> "zlib" then
            raise <| NotSupportedException($"Unsupported PSARC compression method: {compressionMethod}.")
        else
            Header(VersionMajor = versionMaj,
                   VersionMinor = versionMin,
                   ToCLength = reader.ReadUInt32(),
                   ToCEntrySize = reader.ReadUInt32(),
                   ToCEntryCount = reader.ReadUInt32(),
                   BlockSizeAlloc = reader.ReadUInt32(),
                   ArchiveFlags = reader.ReadUInt32())
