[<AutoOpen>]
module Rocksmith2014.EOF.ImportTypes

open Rocksmith2014.XML

type ImportedArrangement =
    { Data: InstrumentalArrangement
      CustomName: string }

type EOFProTracks =
    { PartGuitar: ImportedArrangement array
      PartBass: ImportedArrangement array
      PartBonus: ImportedArrangement option
      PartVocals: Vocal seq }

    member this.GetAnyInstrumental =
        this.PartGuitar
        |> Array.tryItem 0
        |> Option.orElse (this.PartBass |> Array.tryItem 0)
        |> Option.orElse this.PartBonus
        |> Option.defaultWith (fun () -> failwith "One instrumental arrangement needed for EOF export.")
