open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.PSARC
open Dapper

type ToneData =
    { Artist: string
      ArtistSort: string
      Title: string
      TitleSort: string
      Name: string
      Key: string
      BassTone: bool
      Description: string
      Definition: string }

let execute (connection: SQLiteConnection) sql =
    using (new SQLiteCommand(sql, connection))
          (fun x -> x.ExecuteNonQuery() |> ignore)

let databaseFilename = "official.db"

let createDataBase () = SQLiteConnection.CreateFile databaseFilename

let options = JsonSerializerOptions(WriteIndented = false, IgnoreNullValues = true)
options.Converters.Add(JsonFSharpConverter())

let getUniqueTones (psarc: PSARC) = async {
    let! jsons =
        psarc.Manifest
        |> Seq.filter (String.endsWith ".json")
        |> Seq.map psarc.GetEntryStream
        |> Async.Sequential

    let! manifests =
        jsons
        |> Array.map (fun data -> async {
            try
                try
                    let! manifest = Manifest.fromJsonStream data
                    return Some (Manifest.getSingletonAttributes manifest)
                finally
                    data.Dispose()
            with _ ->
                return None })
        |> Async.Parallel

    return
        manifests
        |> Array.Parallel.choose (fun m ->
            match m with
            | None ->
                None
            | Some m when isNull m.Tones ->
                None
            | Some m ->
                m.Tones
                |> Array.map (fun dto ->
                    let isBass =
                        match m.ArrangementProperties with
                        | Some { pathBass = 1uy } ->
                            true
                        | _ ->
                            // Some guitar arrangements may contain a tone from the bass arrangement
                            dto.ToneDescriptors
                            |> Option.ofObj
                            |> Option.exists (Array.contains "$[35715]BASS")

                    let description =
                        match dto.ToneDescriptors with
                        | null ->
                            String.Empty
                        | descs ->
                            String.Join("|", Array.map ToneDescriptor.uiNameToName descs)

                    let definition =
                        JsonSerializer.Serialize({ dto with SortOrder = Nullable()
                                                            MacVolume = null }, options)

                    { Name = dto.Name |> String.truncate 100
                      Key = dto.Key
                      Artist = m.ArtistName.Trim() |> String.truncate 100
                      ArtistSort = m.ArtistNameSort.Trim() |> String.truncate 100
                      Title = m.SongName.Trim() |> String.truncate 100
                      TitleSort = m.SongNameSort.Trim() |> String.truncate 100
                      BassTone = isBass
                      Description = description
                      Definition = definition })
                |> Some)
        |> Array.concat
        |> Array.distinctBy (fun x -> x.Key) }

let insertSql =
    """INSERT INTO tones(artist, artistSort, title, titleSort, name, basstone, description, definition)
       VALUES (@artist, @artistSort, @title, @titleSort, @name, @basstone, @description, @definition)"""

let scanPsarcs (connection: SQLiteConnection) directory =
    seq {
        yield Path.Combine(directory, "songs.psarc")
        yield! Directory.EnumerateFiles(Path.Combine(directory, "dlc"), "*_p.psarc") }
    |> Seq.map (fun path -> async {
        printfn "File %s:" (Path.GetFileName path)

        let! tones = async {
            use psarc = PSARC.ReadFile path
            return! getUniqueTones psarc }

        tones
        |> Array.iter (fun data ->
            printfn "    \"%s\" (%s - %s)" data.Name data.Artist data.Title
            connection.Execute(insertSql, data) |> ignore)
    })
    |> Async.Sequential
    |> Async.Ignore
    |> Async.RunSynchronously

[<EntryPoint>]
let main argv =
    if not <| File.Exists databaseFilename then
        createDataBase ()

    let connectionString = $"Data Source={databaseFilename};"

    use connection = new SQLiteConnection(connectionString)
    connection.Open()
    let execute = execute connection

    execute "DROP TABLE IF EXISTS Tones"

    execute
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

    scanPsarcs connection argv.[0]

    0
