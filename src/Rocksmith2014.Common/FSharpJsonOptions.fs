namespace Rocksmith2014.Common

open System.Text.Json
open System.Text.Json.Serialization

type FSharpJsonOptions() =
    static member Create(?indent, ?ignoreNull) =
        let ignoreNull = defaultArg ignoreNull false

        JsonSerializerOptions(
            WriteIndented = defaultArg indent false,
            DefaultIgnoreCondition = if ignoreNull then JsonIgnoreCondition.WhenWritingNull else JsonIgnoreCondition.Never
        )
        |> apply (fun options -> options.Converters.Add(JsonFSharpConverter()))
