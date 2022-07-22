namespace Rocksmith2014.DLCProject

open System

type SortableString =
    { Value: string; SortValue: string }

    /// Creates a sortable string with the given value and optional sort value.
    static member Create(value, ?sort) =
        let sort = sort |> Option.bind Option.ofString

        { Value = value
          SortValue =
            sort
            |> Option.map StringValidator.sortField
            |> Option.defaultWith (fun () -> StringValidator.useOfficialSortValueIfFound value) }

    /// Returns true if the string's value or sort value is null or empty.
    static member IsEmpty(str) =
        String.IsNullOrEmpty(str.Value) || String.IsNullOrEmpty(str.SortValue)

    static member Empty =
        { Value = String.Empty
          SortValue = String.Empty }
