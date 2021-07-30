module DLCBuilder.ToneCollection.Database

open Dapper
open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open DLCBuilder

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

let private getToneById (connection: SQLiteConnection) id =
    $"SELECT definition FROM tones WHERE id = {id}"
    |> connection.Query<string>
    |> Seq.tryHead
    |> Option.map deserialize

let private getToneDataById (connection: SQLiteConnection) id =
    $"SELECT id, artist, artistsort, title, titlesort, name, basstone, description, definition FROM tones WHERE id = {id}"
    |> connection.Query<DbToneData>
    |> Seq.tryHead

let tryCreateOfficialTonesApi () =
    officialTonesDbPath
    |> File.tryMap (fun _ ->
        let connection = createConnection officialTonesConnectionString

        { new IOfficialTonesApi with
            member _.Dispose() = connection.Dispose()

            member _.GetToneById(id: int64) = getToneById connection id
            member _.GetToneDataById(id: int64) = getToneDataById connection id

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

        member _.GetToneById(id: int64) = getToneById connection id
        member _.GetToneDataById(id: int64) = getToneDataById connection id

        member _.GetTones(searchString, pageNumber) =
            executeQuery connection searchString pageNumber
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

let createCollection = function
    | ActiveTab.Official ->
        tryCreateOfficialTonesApi() |> ActiveCollection.Official
    | ActiveTab.User ->
        createUserTonesApi() |> ActiveCollection.User

let getToneFromCollection collection id =
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

let addToneDataToUserCollection (data: DbToneData) =
    use collection = createUserTonesApi()
    collection.AddTone data

let private prepareString (str: string) =
    String.truncate 100 (str.Trim())

let addToUserCollection (project: DLCProject) (tone: Tone) =
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
    |> addToneDataToUserCollection
