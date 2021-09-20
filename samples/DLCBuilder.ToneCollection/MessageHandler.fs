module ToneCollection.MessageHandler

open Rocksmith2014.Common.Manifest
open System
open CollectionState

let editUserTone edit (userTone: DbToneData) =
    match edit with
    | UserToneEdit.SetArtist artist ->
        { userTone with
            Artist = artist
            ArtistSort = artist }

    | UserToneEdit.SetArtistSort artistSort ->
        { userTone with ArtistSort = artistSort }

    | UserToneEdit.SetTitle title ->
        { userTone with
            Title = title
            TitleSort = title }

    | UserToneEdit.SetTitleSort titleSort ->
        { userTone with TitleSort = titleSort }

    | UserToneEdit.SetName name ->
        { userTone with Name = name }

    | UserToneEdit.SetIsBass isBass ->
        { userTone with BassTone = isBass }

    | UserToneEdit.RemoveArtistInfo ->
        { userTone with
            Artist = String.Empty
            ArtistSort = String.Empty
            Title = String.Empty
            TitleSort = String.Empty }

let update (state: ToneCollectionState) msg =
    match msg with
    | SearchCollection searchString ->
        changeSearch searchString state, Effect.Nothing

    | ChangeCollection activeTab ->
        changeCollection activeTab state, Effect.Nothing

    | ChangePage direction ->
        let page = state.QueryOptions.PageNumber + match direction with Right -> 1 | Left -> -1
        if page < 1 || page > state.TotalPages then
            state, Effect.Nothing
        else
            changePage page state, Effect.Nothing

    | SelectedToneChanged selectedTone ->
        { state with SelectedTone = selectedTone }, Effect.Nothing

    | ShowUserToneEditor ->
        let data =
            match state with
            | { ActiveCollection = ActiveCollection.User api
                SelectedTone = Some { Id = id } }->
                api.GetToneDataById id
            | _ ->
                None

        { state with EditingUserTone = data }, Effect.Nothing

    | HideUserToneEditor ->
        { state with EditingUserTone = None }, Effect.Nothing

    | EditUserToneData edit ->
        let editedTone =
            state.EditingUserTone
            |> Option.map (editUserTone edit)

        { state with EditingUserTone = editedTone }, Effect.Nothing

    | ApplyUserToneEdit ->
        match state with
        | { EditingUserTone = Some data
            ActiveCollection = ActiveCollection.User api } ->
            api.UpdateData(data)
        | _ ->
            ()

        refresh { state with EditingUserTone = None }, Effect.Nothing

    | AddOfficialToneToUserCollection ->
        state
        |> addSelectedToneToUserCollection

        state, Effect.ShowToneAddedToCollectionMessage

    | AddSelectedToneFromCollection ->
        let effect =
            option {
                let! selectedTone = state.SelectedTone
                let! tone = Database.getToneFromCollection state.ActiveCollection selectedTone.Id

                // Needed if the user has changed the tone name in the collection
                let tone =
                    { tone with
                        Name = selectedTone.Name
                        Key = selectedTone.Name }

                return Effect.AddToneToProject tone
            }
            |> Option.defaultValue Effect.Nothing

        state, effect

    | DeleteSelectedUserTone ->
        match state.SelectedTone with
        | Some selectedTone ->
            deleteUserTone selectedTone.Id state, Effect.Nothing
        | None ->
            state, Effect.Nothing
