module ToneCollection.CollectionState

open Rocksmith2014.Common
open Database

let private getTotalPages tones =
    tones
    |> Array.tryHead
    |> Option.map (fun x -> ceil (float x.TotalRows / 5.) |> int)
    |> Option.defaultValue 0

let init (tab: ActiveTab) =
    let collection = createCollection tab
    let tones = getTones collection None 1

    { ActiveCollection = collection
      Tones = tones
      SelectedTone = None
      SearchString = None
      CurrentPage = 1
      EditingUserTone = None
      TotalPages = getTotalPages tones }

let disposeCollection = function
    | ActiveCollection.Official maybeApi ->
        maybeApi
        |> Option.iter (fun x -> x.Dispose())
    | ActiveCollection.User api ->
        api.Dispose()

let changePage page collectionState =
    if page = collectionState.CurrentPage then
        collectionState
    else
        let tones = getTones collectionState.ActiveCollection collectionState.SearchString page

        { collectionState with
            Tones = tones
            CurrentPage = page }

let refresh collectionState =
    let tones = getTones collectionState.ActiveCollection collectionState.SearchString collectionState.CurrentPage

    { collectionState with Tones = tones }

let changeSearch searchString collectionState =
    if searchString = collectionState.SearchString then
        collectionState
    else
        let page = 1
        let tones = getTones collectionState.ActiveCollection searchString page

        { collectionState with
            Tones = tones
            SearchString = searchString
            CurrentPage = page
            TotalPages = getTotalPages tones }

let changeCollection tab collectionState =
    let collection =
        match tab, collectionState.ActiveCollection with
        | ActiveTab.Official, ActiveCollection.Official _
        | ActiveTab.User, ActiveCollection.User _ ->
            collectionState.ActiveCollection

        | newTab, old ->
            disposeCollection old
            createCollection newTab

    if collection = collectionState.ActiveCollection then
        collectionState
    else
        let page = 1
        let searchString = None
        let tones = getTones collection searchString page

        { collectionState with
            ActiveCollection = collection
            Tones = tones
            SearchString = searchString
            CurrentPage = page
            SelectedTone = None
            TotalPages = getTotalPages tones }

let deleteUserTone id collectionState =
    match collectionState.ActiveCollection with
    | ActiveCollection.User api ->
        api.DeleteToneById id
        let tones = getTones collectionState.ActiveCollection collectionState.SearchString collectionState.CurrentPage

        { collectionState with
            Tones = tones
            TotalPages = getTotalPages tones }
    | _ ->
        collectionState

let addSelectedToneToUserCollection collectionState =
    match collectionState with
    | { SelectedTone = Some selected; ActiveCollection = ActiveCollection.Official (Some api) } ->
        api.GetToneDataById selected.Id
        |> Option.iter addToneDataToUserCollection
    | _ ->
        ()

let getSelectedToneDefinition collectionState =
    collectionState.SelectedTone
    |> Option.bind (fun tone ->
        match collectionState.ActiveCollection with
        | ActiveCollection.Official (Some api) ->
            api.GetToneById tone.Id
        | ActiveCollection.User api ->
            api.GetToneById tone.Id
        | _ ->
            None)
