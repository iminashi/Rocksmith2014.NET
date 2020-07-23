﻿namespace Rocksmith2014.SNG

open Interfaces
open BinaryHelpers
open System.IO

/// Leftover from RS1, no equivalent in RS2014 XML.
type Action = 
    { Time : float32
      ActionName : string }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.Write this.Time
            writeZeroTerminatedUTF8String 256 this.ActionName writer

    static member Read(reader : BinaryReader) =
        { Time = reader.ReadSingle()
          ActionName = readZeroTerminatedUTF8String 256 reader }
