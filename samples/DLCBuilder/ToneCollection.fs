module DLCBuilder.ToneCollection

open Dapper
open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject

[<CLIMutable>]
type DbTone =
    { Id: int64
      Artist: string
      Title: string
      Name: string
      BassTone: bool
      Description: string
      TotalRows: int64 }

type DbToneData =
    { Artist: string
      ArtistSort: string
      Title: string
      TitleSort: string
      Name: string
      BassTone: bool
      Description: string
      Definition: string }

type IOfficialTonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetToneDataById : int64 -> DbToneData option
    abstract member GetTones : string option * int -> DbTone array

type IUserTonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetTones : string option * int -> DbTone array
    abstract member GetToneDataById : int64 -> DbToneData option
    abstract member DeleteToneById : int64 -> unit
    abstract member AddTone : DbToneData -> unit
    abstract member UpdateData : int64 * DbToneData -> unit

let private officialTonesDbPath =
    Path.Combine(Configuration.appDataFolder, "tones", "official.db")

let private userTonesDbPath =
    Path.Combine(Configuration.appDataFolder, "tones", "user.db")

let private officialTonesConnectionString = $"Data Source={officialTonesDbPath};Read Only=True"
let private userTonesConnectionString = $"Data Source={userTonesDbPath}"

let private createConnection (connectionString: string) =
    new SQLiteConnection(connectionString)
    |> apply (fun c -> c.Open())

let private serializerOptions () =
    JsonSerializerOptions(WriteIndented = false, IgnoreNullValues = true)
    |> apply (fun options -> options.Converters.Add(JsonFSharpConverter()))

let private deserialize (definition: string) =
    JsonSerializer.Deserialize<ToneDto>(definition, serializerOptions())
    |> Tone.fromDto

let private serialize (tone: Tone) =
    let dto = Tone.toDto { tone with SortOrder = None; MacVolume = None }
    JsonSerializer.Serialize(dto, serializerOptions())

let private executeQuery (connection: SQLiteConnection) (searchString: string option) pageNumber =
    let limit = 5
    let offset = (pageNumber - 1) * limit

    let whereClause =
        match searchString with
        | Some _ ->
            "WHERE LOWER(name || description || artist || title || artist || description) LIKE @like"
        | None ->
            String.Empty

    let sql =
        $"""
        WITH Data_CTE
        AS
        (
            SELECT id, artist, artistsort, title, titlesort, name, basstone, description
            FROM tones
            {whereClause}
        ),
        Count_CTE
        AS
        (
            SELECT COUNT(*) AS TotalRows FROM Data_CTE
        )
        SELECT *
        FROM Data_CTE
        CROSS JOIN Count_CTE
        ORDER BY artistsort, titlesort, name
        LIMIT {limit}
        OFFSET {offset}
        """

    match searchString with
    | Some searchString ->
        let like =
            searchString.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            |> String.concat "%"
            |> sprintf "%%%s%%"
        connection.Query<DbTone>(sql, dict [ "like", box like ])
    | None ->
        connection.Query<DbTone>(sql)

let tryCreateOfficialTonesApi () =
    officialTonesDbPath
    |> File.tryMap (fun _ ->
        let connection = createConnection officialTonesConnectionString

        { new IOfficialTonesApi with
            member _.Dispose() = connection.Dispose()

            member _.GetToneById(id: int64) =
                $"SELECT definition FROM tones WHERE id = {id}"
                |> connection.Query<string>
                |> Seq.tryHead
                |> Option.map deserialize

            member _.GetToneDataById(id: int64) =
                $"SELECT artist, artistsort, title, titlesort, name, basstone, description, definition FROM tones WHERE id = {id}"
                |> connection.Query<DbToneData>
                |> Seq.tryHead

            member _.GetTones(searchString, pageNumber) =
                executeQuery connection searchString pageNumber
                |> Seq.toArray })

let private ensureUserTonesDbCreated () =
    if not <| File.Exists userTonesDbPath then
        Directory.CreateDirectory(Path.GetDirectoryName userTonesDbPath) |> ignore
        SQLiteConnection.CreateFile userTonesDbPath

        use connection = createConnection userTonesConnectionString

        let sql =
            """CREATE TABLE Tones (
               Id INTEGER NOT NULL UNIQUE PRIMARY KEY AUTOINCREMENT,
               Artist VARCHAR(100) NOT NULL,
               ArtistSort VARCHAR(100) NOT NULL,
               Title VARCHAR(100) NOT NULL,
               TitleSort VARCHAR(100) NOT NULL,
               Name VARCHAR(100) NOT NULL,
               BassTone BOOLEAN NOT NULL,
               Description VARCHAR(100) NOT NULL,
               Definition VARCHAR(8000) NOT NULL)"""

        using (new SQLiteCommand(sql, connection))
              (fun x -> x.ExecuteNonQuery() |> ignore)

let createUserTonesApi () =
    ensureUserTonesDbCreated()

    let connection = createConnection userTonesConnectionString

    { new IUserTonesApi with
        member _.Dispose() = connection.Dispose()

        member _.GetToneById(id: int64) =
            $"SELECT definition FROM tones WHERE id = {id}"
            |> connection.Query<string>
            |> Seq.tryHead
            |> Option.map deserialize

        member _.GetTones(searchString, pageNumber) =
            executeQuery connection searchString pageNumber
            |> Seq.toArray

        member _.GetToneDataById(id: int64) =
            $"SELECT artist, artistsort, title, titlesort, name, basstone, description, definition FROM tones WHERE id = {id}"
            |> connection.Query<DbToneData>
            |> Seq.tryHead

        member _.UpdateData(id: int64, data: DbToneData) =
            let sql =
                $"""UPDATE tones
                    SET artist = @artist,
                        artistsort = @artistSort,
                        title = @title,
                        titlesort = @titleSort,
                        name = @name,
                        basstone = @basstone
                    WHERE id = {id}"""
            connection.Execute(sql, data) |> ignore

        member _.AddTone(data: DbToneData) =
            let sql =
                """INSERT INTO tones(artist, artistSort, title, titleSort, name, basstone, description, definition)
                   VALUES (@artist, @artistSort, @title, @titleSort, @name, @basstone, @description, @definition)"""
            connection.Execute(sql, data) |> ignore

        member _.DeleteToneById(id: int64) =
            let sql = $"DELETE FROM tones WHERE id = {id}"
            using (new SQLiteCommand(sql, connection))
                  (fun x -> x.ExecuteNonQuery() |> ignore) }

[<RequireQualifiedAccess>]
type ActiveCollection =
    | Official of IOfficialTonesApi option
    | User of IUserTonesApi

[<RequireQualifiedAccess>]
type ActiveTab =
    | Official
    | User

let createCollection = function
    | ActiveTab.Official ->
        tryCreateOfficialTonesApi() |> ActiveCollection.Official
    | ActiveTab.User ->
        createUserTonesApi() |> ActiveCollection.User

let disposeCollection = function
    | ActiveCollection.Official maybeApi ->
        maybeApi
        |> Option.iter (fun x -> x.Dispose())
    | ActiveCollection.User api ->
        api.Dispose()

let getToneById collection id =
    match collection with
    | ActiveCollection.Official maybeApi ->
        maybeApi
        |> Option.bind (fun x -> x.GetToneById id)
    | ActiveCollection.User api ->
        api.GetToneById id

let getTones collection searchString page =
    match collection with
    | ActiveCollection.Official maybeApi ->
        maybeApi
        |> Option.map (fun x -> x.GetTones(searchString, page))
        |> Option.defaultValue Array.empty
    | ActiveCollection.User api ->
        api.GetTones(searchString, page)

let private addToneDataToUserCollection (data: DbToneData) =
    use collection = createUserTonesApi()
    collection.AddTone data

let addToUserCollection (project: DLCProject) (tone: Tone) =
    let description = String.Join("|", Array.map ToneDescriptor.uiNameToName tone.ToneDescriptors)

    let isBass =
        tone.ToneDescriptors |> Array.contains "$[35715]BASS"

    { Artist = project.ArtistName.Value.Trim() |> String.truncate 100
      ArtistSort = project.ArtistName.SortValue.Trim() |> String.truncate 100
      Title = project.Title.Value.Trim() |> String.truncate 100
      TitleSort = project.Title.SortValue.Trim() |> String.truncate 100
      Name = tone.Name |> String.truncate 100
      BassTone = isBass
      Description = description
      Definition = serialize tone }
    |> addToneDataToUserCollection

type State =
    { ActiveCollection : ActiveCollection
      Tones : DbTone array
      SelectedTone : DbTone option
      SearchString : string option
      EditingUserTone : (int64 * DbToneData) option
      CurrentPage : int
      TotalPages : int }

module State =
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
