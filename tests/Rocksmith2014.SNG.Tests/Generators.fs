module Rocksmith2014.SNG.Tests.Generators

open FsCheck
open Rocksmith2014.SNG

/// Does not generate NaN or infinity floating point values.
type Overrides() =
    static member Float32() =
        Arb.Default.Float32()
        |> Arb.filter (fun f -> not <| System.Single.IsNaN(f) && not <| System.Single.IsInfinity(f))

    static member Float() =
        Arb.Default.Float()
        |> Arb.filter (fun f -> not <| System.Double.IsNaN(f) && not <| System.Double.IsInfinity(f))

let private genTime = Overrides.Float32() |> Arb.toGen
let private genSByte = Arb.Default.SByte() |> Arb.toGen
let private genMidiNote = Gen.choose(0, 127)

/// Generates non-empty strings that are shorter than the given maximum length.
let private genString maxLength =
    Arb.Default.NonEmptyString()
    |> Arb.filter (fun (NonEmptyString s) -> s.Length < maxLength)
    |> Arb.convert (fun ns -> ns.Get) NonEmptyString
    |> Arb.toGen

type Generators() =
    static member Vocal() =
        let genLyric = genString 48
        let createVocal time note length lyric =
            { Time = time; Note = note; Length = length; Lyric = lyric }

        createVocal <!> genTime <*> genMidiNote <*> genTime <*> genLyric
        |> Arb.fromGen

    static member Phrase() =
        let genName = genString 32
        let genDiff = Gen.choose(0, 29)
        let genPiL = Gen.choose(0, 10000)
        let createPhrase solo disp ign diff pil name=
            { Solo = solo
              Disparity = disp
              Ignore = ign
              MaxDifficulty = diff
              IterationCount = pil
              Name = name }

        createPhrase <!> genSByte <*> genSByte <*> genSByte <*> genDiff <*> genPiL <*> genName
        |> Arb.fromGen

    static member Chord() =
        let genName = genString 32
        let genMask =
            Arb.Default.UInt32()
            |> Arb.convert LanguagePrimitives.EnumOfValue LanguagePrimitives.EnumToValue
            |> Arb.toGen
        let genSByteArr = Gen.arrayOfLength 6 genSByte
        let genNotes = Gen.arrayOfLength 6 genMidiNote
        let createChord mask frets fingers notes name =
            { Mask = mask; Frets = frets; Fingers = fingers; Notes = notes; Name = name }

        createChord <!> genMask <*> genSByteArr <*> genSByteArr <*> genNotes <*> genName
        |> Arb.fromGen
    
    static member SymbolsTexture() =
        let genFont = genString 128
        let genSize = Gen.choose(256, 2048)
        let createTexture font width height =
            { Font = font; FontPathLength = font.Length; Width = width; Height = height }

        createTexture <!> genFont <*> genSize <*> genSize
        |> Arb.fromGen

    static member Event() =
        let genName = genString 128
        let createEvent time name = { Time = time; Name = name }

        createEvent <!> genTime <*> genName
        |> Arb.fromGen