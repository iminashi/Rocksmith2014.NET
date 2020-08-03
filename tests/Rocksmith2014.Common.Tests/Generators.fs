module Generators

open FsCheck
open Expecto
open System

/// Does not generate NaN floating point values.
type Overrides() =
    static member Float32() =
        Arb.Default.Float32()
        |> Arb.filter (fun f -> not <| System.Single.IsNaN(f))

    static member Float() =
        Arb.Default.Float()
        |> Arb.filter (fun f -> not <| System.Double.IsNaN(f))

module Custom =
    type UInt24 = UInt24 of uint32
    type UInt40 = UInt40 of uint64

    let UInt24Arb =
        Arb.convert
            (fun (DoNotSize x) -> x &&& 0xFF_FFFFu |> UInt24)
            (fun (UInt24 x) -> x |> DoNotSize)
            Arb.from

    let UInt40Arb =
        Arb.convert
            (fun (DoNotSize x) -> x &&& 0xFF_FFFF_FFFFUL |> UInt40)
            (fun (UInt40 x) -> x |> DoNotSize)
            Arb.from

let config = { FsCheckConfig.defaultConfig with arbitrary = [typeof<Overrides>;typeof<Custom.UInt24>.DeclaringType]
                                                startSize = Int32.MinValue
                                                endSize = Int32.MaxValue }
let testProp name = testPropertyWithConfig config name