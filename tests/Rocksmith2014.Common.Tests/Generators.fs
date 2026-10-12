module Generators

open System
open Expecto
open FsCheck
open FsCheck.FSharp

/// Does not generate NaN floating point values.
type Overrides() =
    static member Float32() =
        ArbMap.defaults.ArbFor<float32>()
        |> Arb.filter (Single.IsNaN >> not)

    static member Float() =
        ArbMap.defaults.ArbFor<float>()
        |> Arb.filter (Double.IsNaN >> not)

module Custom =
    type UInt24 = UInt24 of uint32
    type UInt40 = UInt40 of uint64

    let UInt24Arb =
        Arb.convert
            (fun (DoNotSize x) -> x &&& 0xFF_FFFFu |> UInt24)
            (fun (UInt24 x) -> x |> DoNotSize)
            (ArbMap.defaults.ArbFor<DoNotSize<uint32>>())

    let UInt40Arb =
        Arb.convert
            (fun (DoNotSize x) -> x &&& 0xFF_FFFF_FFFFUL |> UInt40)
            (fun (UInt40 x) -> x |> DoNotSize)
            (ArbMap.defaults.ArbFor<DoNotSize<uint64>>())

let config =
    { FsCheckConfig.defaultConfig with
          arbitrary =
              [ typeof<Overrides>
                typeof<Custom.UInt24>.DeclaringType ]
          startSize = Int32.MinValue
          endSize = Int32.MaxValue }

let testProp name = testPropertyWithConfig config name
