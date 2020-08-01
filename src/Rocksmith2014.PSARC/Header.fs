namespace Rocksmith2014.PSARC

open Rocksmith2014.Common.Interfaces
open System.Text

type internal Header() =
    member val Magic = "PSAR"
    member val VersionMajor = 1us with get, set
    member val VersionMinor = 4us with get, set
    member val CompressionMethod = "zlib"
    member val TOCLength = 0u with get, set
    member val TOCEntrySize = 30u with get, set
    member val TOCEntries = 0u with get, set
    member val BlockSizeAlloc = 65536u with get, set
    member val ArchiveFlags = 0u with get, set

    member this.IsEncrypted = this.ArchiveFlags = 4u

    member this.Write (writer: IBinaryWriter) =
        writer.WriteBytes (Encoding.ASCII.GetBytes(this.Magic))
        writer.WriteUInt16 this.VersionMajor
        writer.WriteUInt16 this.VersionMinor
        writer.WriteBytes (Encoding.ASCII.GetBytes(this.CompressionMethod))
        writer.WriteUInt32 this.TOCLength
        writer.WriteUInt32 this.TOCEntrySize
        writer.WriteUInt32 this.TOCEntries
        writer.WriteUInt32 this.BlockSizeAlloc
        writer.WriteUInt32 this.ArchiveFlags

    static member Read (reader: IBinaryReader) =
        let magic = Encoding.ASCII.GetString(reader.ReadBytes(4))
        let versionMaj, versionMin = reader.ReadUInt16(), reader.ReadUInt16()
        let compressionMethod = Encoding.ASCII.GetString(reader.ReadBytes(4))

        if magic <> "PSAR" then
            failwith "PSARC header magic check failed."
        elif compressionMethod <> "zlib" then
            failwith "Unsupported compression type."
        else
            Header(VersionMajor = versionMaj,
                   VersionMinor = versionMin,
                   TOCLength = reader.ReadUInt32(),
                   TOCEntrySize = reader.ReadUInt32(),
                   TOCEntries = reader.ReadUInt32(),
                   BlockSizeAlloc = reader.ReadUInt32(),
                   ArchiveFlags = reader.ReadUInt32())
