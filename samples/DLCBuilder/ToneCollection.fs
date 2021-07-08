module DLCBuilder.ToneCollection

open Dapper
open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
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

type ITonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetTones : string option * int -> OfficialTone seq

type private ToneDefinition = { Definition : string }

let private dbPath =
    Path.Combine(Configuration.appDataFolder, "tones", "official.db")

let private connectionString = $"Data Source={dbPath};Read Only=True"

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

let createApi () =
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    { new ITonesApi with
        member _.Dispose() = connection.Dispose()

        member _.GetToneById (id: int64) =
            $"SELECT definition FROM tones WHERE id = {id}"
            |> connection.Query<ToneDefinition>  
            |> Seq.tryHead
            |> Option.map deserialize

        member _.GetTones (searchString, pageNumber) =
            createQuery searchString pageNumber
            |> connection.Query<OfficialTone>
    }

type State =
    { Api : ITonesApi
      Tones : OfficialTone array
      SearchString : string
      CurrentPage : int
      TotalPages : int }

    static member Init () =
        let api = createApi()
        let tones = api.GetTones(None, 1) |> Seq.toArray
        let totalPages =
            tones
            |> Array.tryHead
            |> Option.map (fun x -> ceil (float x.TotalRows / 5.) |> int)
            |> Option.defaultValue 0

        { Api = api
          Tones = tones
          SearchString = String.Empty
          CurrentPage = 1
          TotalPages = totalPages }
