module DLCBuilder.ToneCollection

open Dapper
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common.Manifest

type DbTone =
    { Id: int64
      Artist: string
      Title: string
      Name: string
      BassTone: bool
      Description: string }

type DbToneDefinition =
    { Definition : string }

let private dbPath =
    Path.Combine(Configuration.appDataFolder, "tones", "official.db")

let private connectionString = $"Data Source={dbPath};Read Only=True"

let private deserialize dbToneDef =
    let options = JsonSerializerOptions(IgnoreNullValues = true)
    options.Converters.Add(JsonFSharpConverter())

    JsonSerializer.Deserialize<ToneDto>(dbToneDef.Definition, options)
    |> Tone.fromDto

let getToneById (id: int64) =
    use connection = new SQLiteConnection(connectionString)
    connection.Open()

    let results =
        connection.Query<DbToneDefinition>(
            "SELECT definition FROM tones WHERE id = @id",
            dict [ "id", box id ])

    results
    |> Seq.tryHead
    |> Option.map deserialize

let getDbTones () =
    use connection = new SQLiteConnection(connectionString)
    connection.Open()

    """SELECT id, artist, title, name, basstone, description
       FROM tones
       LIMIT 10"""
    |>connection.Query<DbTone>

let searchDbTones (searchString: string) =
    use connection = new SQLiteConnection(connectionString)
    connection.Open()

    let like = $"%%{searchString.ToLowerInvariant()}%%"

    $"""SELECT id, artist, title, name, basstone, description
        FROM tones
        WHERE ( LOWER(name) LIKE '{like}'
                OR LOWER(artist) LIKE '{like}'
                OR LOWER(title) LIKE '{like}'
                OR LOWER(description) LIKE '{like}' )
        LIMIT 10"""
    |>connection.Query<DbTone>
