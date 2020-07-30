module Rocksmith2014.Common.MemoryStreamPool

open Microsoft.IO

let Default = RecyclableMemoryStreamManager()
