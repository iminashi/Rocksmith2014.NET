namespace Rocksmith2014.Conversion

open System.Runtime.CompilerServices
open Rocksmith2014.XML

[<Extension>]
type InstrumentalArrangementExtension =
    /// Converts the instrumental arrangement into SNG.
    [<Extension>]
    static member inline ToSng(arr: InstrumentalArrangement) = ConvertInstrumental.xmlToSng arr
