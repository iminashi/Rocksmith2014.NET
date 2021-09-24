module ToneCollection.CollectionState

open Database

let private getTotalPages tones =
    tones
    |> Array.tryHead
    |> Option.map (fun x -> ceil (float x.TotalRows / 5.) |> int)
    |> Option.defaultValue 0

let init (connector: IDatabaseConnector) (tab: ActiveTab) =
    let collection = createCollection connector tab
    let queryOptions = { Search = None; PageNumber = 1 }
    let tones = getTones collection queryOptions

    { ActiveCollection = collection
      Connector = connector
      Tones = tones
      SelectedTone = None
      QueryOptions = queryOptions
      EditingUserTone = None
      TotalPages = getTotalPages tones }

let disposeCollection = function
    | ActiveCollection.Official maybeApi ->
        maybeApi |> Option.iter (fun x -> x.Dispose())
    | ActiveCollection.User api ->
        api.Dispose()

let changePage page collectionState =
    if page = collectionState.QueryOptions.PageNumber then
        collectionState
    else
        let queryOptions = { collectionState.QueryOptions with PageNumber = page }
        let tones = getTones collectionState.ActiveCollection queryOptions

        { collectionState with
            Tones = tones
            QueryOptions = queryOptions }

let refresh collectionState =
    let tones = getTones collectionState.ActiveCollection collectionState.QueryOptions

    { collectionState with Tones = tones }

let changeSearch searchString collectionState =
    if searchString = collectionState.QueryOptions.Search then
        collectionState
    else
        let queryOptions = { Search = searchString; PageNumber = 1 }
        let tones = getTones collectionState.ActiveCollection queryOptions

        { collectionState with
            Tones = tones
            QueryOptions = queryOptions
            TotalPages = getTotalPages tones }

let changeCollection tab collectionState =
    let collection =
        match tab, collectionState.ActiveCollection with
        | ActiveTab.Official, ActiveCollection.Official _
        | ActiveTab.User, ActiveCollection.User _ ->
            collectionState.ActiveCollection

        | newTab, old ->
            disposeCollection old
            createCollection collectionState.Connector newTab

    if collection = collectionState.ActiveCollection then
        collectionState
    else
        let queryOptions = { Search = None; PageNumber = 1 }
        let tones = getTones collection queryOptions

        { collectionState with
            ActiveCollection = collection
            Tones = tones
            QueryOptions = queryOptions
            SelectedTone = None
            TotalPages = getTotalPages tones }

let deleteUserTone id collectionState =
    match collectionState.ActiveCollection with
    | ActiveCollection.User api ->
        api.DeleteToneById(id)
        let tones = getTones collectionState.ActiveCollection collectionState.QueryOptions

        { collectionState with
            Tones = tones
            TotalPages = getTotalPages tones }
    | _ ->
        collectionState

let addSelectedToneToUserCollection collectionState =
    match collectionState with
    | { SelectedTone = Some selected
        ActiveCollection = ActiveCollection.Official (Some api) } ->
        api.GetToneDataById(selected.Id)
        |> Option.iter (addToneDataToUserCollection collectionState.Connector)
    | _ ->
        ()

let getSelectedToneDefinition collectionState =
    collectionState.SelectedTone
    |> Option.bind (fun tone ->
        match collectionState.ActiveCollection with
        | ActiveCollection.Official (Some api) ->
            api.GetToneById(tone.Id)
        | ActiveCollection.User api ->
            api.GetToneById(tone.Id)
        | _ ->
            None)
