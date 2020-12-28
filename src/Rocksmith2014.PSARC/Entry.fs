namespace Rocksmith2014.PSARC

open System
open System.IO
open Rocksmith2014.Common.Interfaces

type NamedEntry =
    { Name: string; Data: Stream }

    interface IDisposable with
        member this.Dispose() = this.Data.Dispose()

type Entry =
    { /// MD5 hash of the name of the entry in the manifest.
      NameDigest : byte[]
      /// The starting z-block index for the entry.
      ZIndexBegin : uint32
      /// The length of the plain data for the entry in bytes.
      Length : uint64
      /// The offset in bytes from the start of the PSARC file for the entry.
      Offset : uint64
      /// The ID number for the entry.
      ID : int32 }

    /// Writes this entry into the writer.
    member this.Write (writer: IBinaryWriter) =
        writer.WriteBytes this.NameDigest
        writer.WriteUInt32 this.ZIndexBegin
        writer.WriteUInt40 this.Length
        writer.WriteUInt40 this.Offset

    /// Reads an entry from the reader.
    static member Read (reader: IBinaryReader) index =
        { ID = index
          NameDigest = reader.ReadBytes(16)
          ZIndexBegin = reader.ReadUInt32()
          Length = reader.ReadUInt40()
          Offset = reader.ReadUInt40() }

    /// Creates a "proto-entry" (no offset or ID) from the given named entry.
    static member CreateProto (nEntry: NamedEntry) zBegin =
        { NameDigest = Cryptography.md5Hash nEntry.Name
          ZIndexBegin = zBegin
          Length = uint64 nEntry.Data.Length
          // Will be set later:
          Offset = 0UL; ID = 0 }
