open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.PSARC

type ToneData =
    { Tone: ToneDto
      ArtistName: string
      Title: string
      IsBass: bool }

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
        |> Array.choose (fun m ->
            match m with
            | None ->
                None
            | Some m when isNull m.Tones ->
                None
            | Some m ->
                let isBass =
                    m.ArrangementProperties
                    |> Option.exists (fun x -> x.pathBass = 1uy)
                Some(m.Tones |> Array.map (fun dto ->
                    { Tone = dto
                      ArtistName = m.ArtistName.Trim()
                      Title = m.SongName.Trim()
                      IsBass = isBass })))
        |> Array.concat
        |> Array.distinctBy (fun x -> x.Tone.Key) }

let insertSql =
    """INSERT INTO tones(artist, title, name, basstone, description, definition)
       VALUES (@artist, @title, @name, @basstone, @description, @definition)"""

let scanPsarcs (connection: SQLiteConnection) directory =
    Directory.EnumerateFiles(directory, "*.psarc")
    |> Seq.distinctBy (fun path ->
        // Ignore _p & _m duplicate files
        let fn = Path.GetFileNameWithoutExtension path
        fn.Substring(0, fn.Length - 2))
    |> Seq.map (fun path -> async {
        printfn "PSARC file %s" (Path.GetFileNameWithoutExtension path)

        let! tones = async {
            use psarc = PSARC.ReadFile path
            return! getUniqueTones psarc }

        printfn "Inserting into DB tones:"

        tones
        |> Array.iter (fun data ->
            let description =
                match data.Tone.ToneDescriptors with
                | null ->
                    String.Empty
                | descs ->
                    String.Join("|", Array.map ToneDescriptor.uiNameToName descs)
            let definition =
                JsonSerializer.Serialize({ data.Tone with SortOrder = Nullable()
                                                          MacVolume = null }, options)

            printfn "    \"%s\" (%s - %s)" data.Tone.Name data.ArtistName data.Title

            use command = new SQLiteCommand(insertSql, connection)
            command.Parameters.AddWithValue("@artist", data.ArtistName) |> ignore
            command.Parameters.AddWithValue("@title", data.Title) |> ignore
            command.Parameters.AddWithValue("@name", data.Tone.Name) |> ignore
            command.Parameters.AddWithValue("@basstone", data.IsBass) |> ignore
            command.Parameters.AddWithValue("@description", description) |> ignore
            command.Parameters.AddWithValue("@definition", definition) |> ignore

            command.ExecuteNonQuery() |> ignore
        )
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
        Title VARCHAR(100) NOT NULL,
        Name VARCHAR(100) NOT NULL,
        BassTone BOOLEAN NOT NULL,
        Description VARCHAR(100) NOT NULL,
        Definition VARCHAR(8000) NOT NULL)"""

    scanPsarcs connection argv.[0]

    0
