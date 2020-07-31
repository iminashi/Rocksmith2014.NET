namespace Rocksmith2014.PSARC

type Entry =
    { NameDigest : byte[]
      zIndexBegin : uint32
      Length : uint64
      Offset : uint64
      ID : int }
