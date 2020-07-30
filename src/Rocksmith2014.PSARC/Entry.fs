namespace Rocksmith2014.PSARC

open System.IO

type Entry =
    { NameDigest : byte[]
      zIndexBegin : uint32
      Length : uint64
      Offset : uint64
      //Data : Stream
      ID : int
      Name : string }

