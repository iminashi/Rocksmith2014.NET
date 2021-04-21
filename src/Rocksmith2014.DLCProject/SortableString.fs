namespace Rocksmith2014.DLCProject

open System

type SortableString =
    { Value: string; SortValue: string }

    /// Returns a sort value used in official files for certain artist names.
    static member private TryGetOfficialArtistNameSortValue(value: string) =
        (* Earth, Wind & Fire:
             Both "Earth, Wind & Fire" and "Earth wind and fire" are used.
           Queensrÿche:
             Both "Queensrÿche" and "Queensryche" are used. *)

        match value.ToLowerInvariant() with
        | "a day to remember" ->
            Some "A Day To Remember"
        | "a flock of seagulls" ->
            Some "A Flock of Seagulls"
        | "b.b. king" ->
            Some "BB King"
        | "blink-182" ->
            Some "Blink 182"
        | "blue öyster cult" ->
            Some "Blue Oyster Cult"
        | "booker t. & the m.g.'s" ->
            Some "Booker T & The MGs"
        | "bobby \"blue\" bland" ->
            Some "Bobby Blue Bland"
        | "brooks & dunn" ->
            Some "Brooks Dunn"
        | "foster the people" ->
            Some "Foster People"
        | "grace potter & the nocturnals" ->
            Some "Grace Potter and The Nocturnals"
        | "hail the sun" ->
            Some "Hail Sun"
        | "the mamas & the papas" ->
            Some "Mamas and The Papas"
        | "motörhead" ->
            Some "Motorhead"
        | "mötley crüe" ->
            Some "Motley Crue"
        | "t. rex" ->
            Some "T Rex"
        | _ ->
            None

    /// Creates a sortable string with the given value and optional sort value.
    static member Create(value, ?sort) =
        let sort = defaultArg sort null
        { Value = value
          SortValue =
            if String.IsNullOrWhiteSpace sort then
                match SortableString.TryGetOfficialArtistNameSortValue value with
                | Some sortValue -> sortValue
                | None -> StringValidator.removeArticles value
            else
                sort
            |> StringValidator.sortField }

    /// Returns true if the string's value or sort value is null or empty.
    static member IsEmpty(str) =
        String.IsNullOrEmpty str.Value || String.IsNullOrEmpty str.SortValue

    static member Empty = { Value = String.Empty; SortValue = String.Empty }
