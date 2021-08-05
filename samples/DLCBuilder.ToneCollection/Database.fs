module ToneCollection.Database

open Dapper
open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject

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

let private getToneById (connection: SQLiteConnection) id =
    $"SELECT definition FROM tones WHERE id = {id}"
    |> connection.Query<string>
    |> Seq.tryHead
    |> Option.map deserialize

let private getToneDataById (connection: SQLiteConnection) id =
    $"SELECT id, artist, artistsort, title, titlesort, name, basstone, description, definition FROM tones WHERE id = {id}"
    |> connection.Query<DbToneData>
    |> Seq.tryHead

let private tryCreateOfficialTonesApi (OfficialDataBasePath dbPath) =
    dbPath
    |> File.tryMap (fun _ ->
        let connection = createConnection $"Data Source={dbPath};Read Only=True"

        { new IOfficialTonesApi with
            member _.Dispose() = connection.Dispose()

            member _.GetToneById(id: int64) = getToneById connection id
            member _.GetToneDataById(id: int64) = getToneDataById connection id

            member _.GetTones(options) =
                executeQuery connection options.Search options.PageNumber
                |> Seq.toArray })

let private ensureUserTonesDbCreated (UserDataBasePath dbPath) connectionString =
    if not <| File.Exists dbPath then
        Directory.CreateDirectory(Path.GetDirectoryName dbPath) |> ignore
        SQLiteConnection.CreateFile dbPath

        use connection = createConnection connectionString

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

let private createUserTonesApi dbPath =
    let connectionString =
        let (UserDataBasePath path) = dbPath in $"Data Source={path}"

    ensureUserTonesDbCreated dbPath connectionString

    let connection = createConnection connectionString

    { new IUserTonesApi with
        member _.Dispose() = connection.Dispose()

        member _.GetToneById(id: int64) = getToneById connection id
        member _.GetToneDataById(id: int64) = getToneDataById connection id

        member _.GetTones(options) =
            executeQuery connection options.Search options.PageNumber
            |> Seq.toArray

        member _.UpdateData(data: DbToneData) =
            let sql =
                $"""UPDATE tones
                    SET artist = @artist,
                        artistsort = @artistSort,
                        title = @title,
                        titlesort = @titleSort,
                        name = @name,
                        basstone = @basstone
                    WHERE id = {data.Id}"""
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

let createConnector officialTonesDbPath userTonesDbPath =
    { new IDatabaseConnector with
        member _.TryCreateOfficialTonesApi() =
            tryCreateOfficialTonesApi officialTonesDbPath
        member _.CreateUserTonesApi() =
            createUserTonesApi userTonesDbPath }

let internal createCollection (connector: IDatabaseConnector) = function
    | ActiveTab.Official ->
        connector.TryCreateOfficialTonesApi() |> ActiveCollection.Official
    | ActiveTab.User ->
        connector.CreateUserTonesApi() |> ActiveCollection.User

let internal getToneFromCollection collection id =
    match collection with
    | ActiveCollection.Official maybeApi ->
        maybeApi
        |> Option.bind (fun x -> x.GetToneById id)
    | ActiveCollection.User api ->
        api.GetToneById id

let internal getTones collection searchOptions =
    match collection with
    | ActiveCollection.Official maybeApi ->
        maybeApi
        |> Option.map (fun x -> x.GetTones(searchOptions))
        |> Option.defaultValue Array.empty
    | ActiveCollection.User api ->
        api.GetTones(searchOptions)

let internal addToneDataToUserCollection (connector: IDatabaseConnector) (data: DbToneData) =
    use collection = connector.CreateUserTonesApi()
    collection.AddTone data

let private prepareString (str: string) =
    String.truncate 100 (str.Trim())

let addToneToUserCollection (connector: IDatabaseConnector) (project: DLCProject) (tone: Tone) =
    let description =
        tone.ToneDescriptors
        |> Array.map ToneDescriptor.uiNameToName
        |> String.concat "|"

    let isBass =
        tone.ToneDescriptors
        |> Array.contains "$[35715]BASS"

    { Id = 0L
      Artist = prepareString project.ArtistName.Value
      ArtistSort = prepareString project.ArtistName.SortValue
      Title = prepareString project.Title.Value
      TitleSort = prepareString project.Title.SortValue
      Name = prepareString tone.Name
      BassTone = isBass
      Description = description
      Definition = serialize tone }
    |> addToneDataToUserCollection connector
