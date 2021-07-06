open System
open System.Data.SQLite
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.PSARC

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
                Some(m.Tones |> Array.map (fun dto -> dto, isBass)))
        |> Array.concat
        |> Array.distinctBy (fun (t, _) -> t.Key) }

let getHeaderAttributes (psarc: PSARC) = async {
    let! headerData =
        psarc.Manifest
        |> Seq.find (String.endsWith ".hsan")
        |> psarc.GetEntryStream

    let! manifest = Manifest.fromJsonStream headerData
    return manifest.Entries
           |> Map.toSeq
           |> Seq.find (fun (_, x) -> String.notEmpty x.Attributes.ArtistName)
           |> (fun (_, x) -> x.Attributes) }

let insertSql =
    """INSERT INTO tones(artist, title, name, basstone, description, definition)
       VALUES (@artist, @title, @name, @basstone, @description, @definition)"""

let scanPsarcs (connection: SQLiteConnection) directory =
    Directory.EnumerateFiles(directory, "*.psarc")
    |> Seq.map (fun path -> async {
        printfn "PSARC file %s" (Path.GetFileNameWithoutExtension path)

        use psarc = PSARC.ReadFile path

        let! attributes = getHeaderAttributes psarc
        let! tones = getUniqueTones psarc

        printfn "%s - %s" attributes.ArtistName attributes.SongName
        printfn "Inserting into DB tones:"

        tones
        |> Array.iter (fun (tone, isBass) ->
            let description =
                String.Join("|", Array.map ToneDescriptor.uiNameToName tone.ToneDescriptors)
            let definition =
                JsonSerializer.Serialize({ tone with SortOrder = Nullable()
                                                     MacVolume = null }, options)

            printfn "    \"%s\"" tone.Name

            use command = new SQLiteCommand(insertSql, connection)
            command.Parameters.AddWithValue("@artist", attributes.ArtistName.Trim()) |> ignore
            command.Parameters.AddWithValue("@title", attributes.SongName.Trim()) |> ignore
            command.Parameters.AddWithValue("@name", tone.Name) |> ignore
            command.Parameters.AddWithValue("@basstone", isBass) |> ignore
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
