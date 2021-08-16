namespace FSharp.Extensions

open System
open System.Runtime.CompilerServices

[<Extension>]
type SpanExtensions =
    [<Extension>]
    static member inline AllSame(this: Span<'T>, value: 'T) =
        this.Trim(value).IsEmpty
