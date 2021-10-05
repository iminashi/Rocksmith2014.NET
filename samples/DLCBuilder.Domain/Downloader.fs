module DLCBuilder.Downloader

open System.IO
open System.Net.Http

let private dbUrl = "https://github.com/iminashi/tones/raw/main/official.db"

let downloadTonesDatabase () =
    async {
        use client = new HttpClient()

        let! response = client.GetAsync(dbUrl)
        response.EnsureSuccessStatusCode() |> ignore
        use! stream = response.Content.ReadAsStreamAsync()

        let targetDirectory = Path.Combine(Configuration.appDataFolder, "tones")
        Directory.CreateDirectory(targetDirectory) |> ignore

        let targetPath = Path.Combine(targetDirectory, "official.db")
        use file = File.Create(targetPath)
        do! stream.CopyToAsync(file)
    }
