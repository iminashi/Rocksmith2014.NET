namespace Rocksmith2014.Common

open System.Text.Json
open System.Text.Json.Serialization

type FSharpJsonOptions() =
    static member Create(?indent, ?ignoreNull) =
        JsonSerializerOptions(
            WriteIndented = defaultArg indent false,
            DefaultIgnoreCondition = defaultArg ignoreNull JsonIgnoreCondition.Never
        )
        |> apply (fun options -> options.Converters.Add(JsonFSharpConverter()))
