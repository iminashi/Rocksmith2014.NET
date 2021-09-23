module Rocksmith2014.Conversion.Tests.VocalsConversion

open Expecto
open Rocksmith2014
open Rocksmith2014.Common
open Rocksmith2014.Conversion
open Rocksmith2014.SNG
open System.IO

let roundTrip (vocals: ResizeArray<XML.Vocal>) =
    async {
        use mem = new MemoryStream()
        let converted = ConvertVocals.xmlToSng FontOption.DefaultFont vocals
        do! SNG.savePacked mem PC converted
        mem.Position <- 0L

        let! sng = SNG.fromStream mem PC
        return ConvertVocals.sngToXml sng
    }

[<Tests>]
let tests =
    testList "Vocals Conversion Tests" [
        testAsync "Simple lyrics round trip conversion" {
            let source =
                ResizeArray(
                    seq {
                        XML.Vocal(5000, 100, "Test+")
                        XML.Vocal(5400, 100, "Lyrics+")
                    }
                )

            let! xml = roundTrip source

            Expect.hasLength xml 2 "Count of vocals is same"
            Expect.equal xml.[0].Lyric "Test+" "Lyric is same"
        }

        testAsync "Lyrics round trip conversion truncates long lyric" {
            let source =
                ResizeArray(seq { XML.Vocal(5000, 100, "This line of lyrics is more than 48 characters when encoded in UTF8.") })

            let! xml = roundTrip source

            // The space allowed for a single lyric is 48 bytes in the SNG format
            // With the necessary null terminator, the resulting length should be 47
            Expect.equal xml.[0].Lyric.Length 47 "Lyric was truncated to 47 characters"
            Expect.equal xml.[0].Lyric "This line of lyrics is more than 48 characters " "Lyric string was truncated"
        }
    ]
