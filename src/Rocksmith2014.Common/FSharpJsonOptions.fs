namespace Rocksmith2014.Common

open System.Text.Json
open System.Text.Json.Serialization

type FSharpJsonOptions() =
    static member Create(?indent, ?ignoreNull) =
        JsonSerializerOptions(
            WriteIndented = defaultArg indent false,
            IgnoreNullValues = defaultArg ignoreNull false
        )
        |> apply (fun options -> options.Converters.Add(JsonFSharpConverter()))
