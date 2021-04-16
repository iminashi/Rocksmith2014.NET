namespace Rocksmith2014.Conversion

open System.Runtime.CompilerServices
open Rocksmith2014.XML

[<Extension>]
type InstrumentalArrangementExtension =
    [<Extension>]
    /// Converts the instrumental arrangement into SNG.
    static member inline ToSng(arr: InstrumentalArrangement) = ConvertInstrumental.xmlToSng arr
