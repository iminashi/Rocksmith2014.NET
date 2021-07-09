module DLCBuilder.ToneCollection

open Dapper
open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest

[<CLIMutable>]
type OfficialTone =
    { Id: int64
      Artist: string
      Title: string
      Name: string
      BassTone: bool
      Description: string
      TotalRows: int64 }

type IOfficialTonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetTones : string option * int -> OfficialTone array

type IUserTonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetTones : string option * int -> OfficialTone array
    abstract member DeleteToneById : int64 -> unit
    abstract member AddTone : Tone -> unit

type private ToneDefinition = { Definition : string }

let private officialTonesDbPath =
    Path.Combine(Configuration.appDataFolder, "tones", "official.db")

let private userTonesDbPath =
    Path.Combine(Configuration.appDataFolder, "tones", "user.db")

let private officialTonesConnectionString = $"Data Source={officialTonesDbPath};Read Only=True"
let private userTonesConnectionString = $"Data Source={userTonesDbPath}"

let private createConnection (connectionString: string) =
    let connection = new SQLiteConnection(connectionString)
    connection.Open()
    connection

let private deserialize toneDef =
    let options = JsonSerializerOptions(IgnoreNullValues = true)
    options.Converters.Add(JsonFSharpConverter())

    JsonSerializer.Deserialize<ToneDto>(toneDef.Definition, options)
    |> Tone.fromDto

let private createQuery (searchString: string option) pageNumber =
    let limit = 5
    let offset = (pageNumber - 1) * limit

    let whereClause =
        match searchString with
        | Some searchString ->
            let like =
                searchString.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                |> String.concat "%"
                |> sprintf "%%%s%%"
            $"""WHERE ( LOWER(name) LIKE '{like}'
                OR
                LOWER(description || artist || title || artist || description) LIKE '{like}' )"""
        | None ->
            String.Empty

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
    ORDER BY artistsort, titlesort
    LIMIT {limit}
    OFFSET {offset}
    """

let tryCreateOfficialTonesApi () =
    if File.Exists officialTonesDbPath then
        let connection = createConnection officialTonesConnectionString

        { new IOfficialTonesApi with
            member _.Dispose() = connection.Dispose()

            member _.GetToneById(id: int64) =
                $"SELECT definition FROM tones WHERE id = {id}"
                |> connection.Query<ToneDefinition>  
                |> Seq.tryHead
                |> Option.map deserialize

            member _.GetTones(searchString, pageNumber) =
                createQuery searchString pageNumber
                |> connection.Query<OfficialTone>
                |> Seq.toArray }
        |> Some
    else
        None

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
            |> connection.Query<ToneDefinition>  
            |> Seq.tryHead
            |> Option.map deserialize

        member _.GetTones(searchString, pageNumber) =
            createQuery searchString pageNumber
            |> connection.Query<OfficialTone>
            |> Seq.toArray
            
        member this.AddTone(arg1: Tone): unit = 
            raise (System.NotImplementedException())

        member this.DeleteToneById(arg1: int64): unit = 
            raise (System.NotImplementedException()) }

let private getTotalPages tones =
    tones
    |> Array.tryHead
    |> Option.map (fun x -> ceil (float x.TotalRows / 5.) |> int)
    |> Option.defaultValue 0

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

type State =
    { ActiveCollection : ActiveCollection
      Tones : OfficialTone array
      SearchString : string option
      CurrentPage : int
      TotalPages : int }

    member this.Update (?active: ActiveTab, ?searchString: string option, ?page: int) =
        let page = defaultArg page 1
        let searchString = defaultArg searchString this.SearchString

        let collection =
            match active, this.ActiveCollection with
            | None, _
            | Some ActiveTab.Official, ActiveCollection.Official _
            | Some ActiveTab.User, ActiveCollection.User _ ->
                this.ActiveCollection

            | Some newTab, old ->
                disposeCollection old
                createCollection newTab

        let tones =
            if page = this.CurrentPage &&
               searchString = this.SearchString &&
               collection = this.ActiveCollection then
                // Don't query the database if nothing actually changed
                this.Tones
            else
                getTones collection searchString page

        { this with ActiveCollection = collection
                    Tones = tones
                    SearchString = searchString
                    CurrentPage = page
                    TotalPages = getTotalPages tones }

    static member Init (?tab: ActiveTab) =
        let collection = createCollection (defaultArg tab ActiveTab.Official)
        let tones = getTones collection None 1

        { ActiveCollection = collection
          Tones = tones
          SearchString = None
          CurrentPage = 1
          TotalPages = getTotalPages tones }
