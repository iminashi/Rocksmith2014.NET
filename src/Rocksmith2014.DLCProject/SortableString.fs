namespace Rocksmith2014.DLCProject

open System

type SortableString =
    { Value: string; SortValue: string }

    /// Creates a sortable string with the given value and optional sort value.
    static member Create(value, ?sort) =
        let sort = defaultArg sort null
        { Value = value
          SortValue =
            if String.IsNullOrWhiteSpace sort then
                StringValidator.removeArticles value
            else
                sort
            |> StringValidator.sortField }

    static member Empty = { Value = String.Empty; SortValue = String.Empty }
