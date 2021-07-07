module DLCBuilder.ToneCollection

open Dapper
open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common.Manifest

type OfficialTone =
    { Id: int64
      Artist: string
      Title: string
      Name: string
      BassTone: bool
      Description: string }

type ITonesApi =
    inherit IDisposable
    abstract member GetToneById : int64 -> Tone option
    abstract member GetTones : string option -> OfficialTone seq

type private ToneDefinition = { Definition : string }

let private dbPath =
    Path.Combine(Configuration.appDataFolder, "tones", "official.db")

let private connectionString = $"Data Source={dbPath};Read Only=True"

let private deserialize toneDef =
    let options = JsonSerializerOptions(IgnoreNullValues = true)
    options.Converters.Add(JsonFSharpConverter())

    JsonSerializer.Deserialize<ToneDto>(toneDef.Definition, options)
    |> Tone.fromDto

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

        member _.GetTones (searchString: string option) =
            let sql =
                let columns = "id, artist, title, name, basstone, description"

                match searchString with
                | None ->
                    $"SELECT {columns} FROM tones LIMIT 10"
                | Some searchString ->
                    let like =
                        searchString.ToLowerInvariant().Split ' '
                        |> String.concat "%"
                        |> sprintf "%%%s%%"
        
                    $"""SELECT {columns}
                        FROM tones
                        WHERE ( LOWER(name) LIKE '{like}'
                                OR (LOWER(artist || title)) LIKE '{like}'
                                OR LOWER(description) LIKE '{like}' )
                        LIMIT 10"""
        
            connection.Query<OfficialTone> sql
    }
