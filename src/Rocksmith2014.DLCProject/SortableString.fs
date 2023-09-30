namespace Rocksmith2014.DLCProject

open System

type SortableString =
    { Value: string; SortValue: string }

    /// Returns true if the string's value and sort value are null or empty.
    member this.IsEmpty =
        String.IsNullOrEmpty(this.Value) && String.IsNullOrEmpty(this.SortValue)

    /// Creates a sortable string with the given value and optional sort value.
    static member Create(value, ?sort) =
        let sort = sort |> Option.bind Option.ofString

        { Value = value
          SortValue =
            sort
            |> Option.map StringValidator.sortField
            |> Option.defaultWith (fun () -> StringValidator.useOfficialSortValueIfFound value) }

    static member Empty =
        { Value = String.Empty
          SortValue = String.Empty }
