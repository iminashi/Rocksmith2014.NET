namespace Rocksmith2014.PSARC

open Rocksmith2014.Common.Interfaces
open System.Text

type Header =
    { Magic : string
      VersionMajor : uint16
      VersionMinor : uint16
      CompressionMethod : string
      TOCLength : uint32
      TOCEntrySize : uint32
      TOCEntries : uint32
      BlockSizeAlloc : uint32
      ArchiveFlags : uint32 }

    member this.IsEncrypted = this.ArchiveFlags = 4u

module Header =
    let Default =
        { Magic = "PSAR"
          VersionMajor = 1us
          VersionMinor = 4us
          CompressionMethod = "zlib"
          TOCLength = 0u
          TOCEntrySize = 30u
          TOCEntries = 0u
          BlockSizeAlloc = 65536u
          ArchiveFlags = 0u }

    let read (reader: IBinaryReader) =
        let magic = Encoding.ASCII.GetString(reader.ReadBytes(4))
        if magic <> "PSAR" then
            failwith "PSARC header magic check failed."
        else
            { Magic = magic
              VersionMajor = reader.ReadUInt16()
              VersionMinor = reader.ReadUInt16()
              CompressionMethod = Encoding.ASCII.GetString(reader.ReadBytes(4))
              TOCLength = reader.ReadUInt32()
              TOCEntrySize = reader.ReadUInt32()
              TOCEntries = reader.ReadUInt32()
              BlockSizeAlloc = reader.ReadUInt32()
              ArchiveFlags = reader.ReadUInt32() }
        