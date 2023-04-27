module DLCBuilder.Downloader

open System.IO
open System.Net.Http
open System

let ToneDBUrl = "https://github.com/iminashi/tones/raw/main/official.db"

let private reportProgress =
    (ProgressReporters.DownloadFile :> IProgress<DownloadId * float>).Report

let downloadFile (url: string) (targetPath: string) (id: DownloadId) =
    backgroundTask {
        use client = new HttpClient()

        let! response =
            client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)

        use! stream =
            response
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync()

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)) |> ignore
        use file = File.Create(targetPath)

        match response.Content.Headers.ContentLength |> Option.ofNullable with
        | Some contentLength ->
            let buffer = Array.zeroCreate<byte> 8192
            let mutable reads = 0
            let mutable totalRead = 0L

            while totalRead < contentLength do
                let! bytesRead = stream.ReadAsync(buffer, 0, buffer.Length)
                if bytesRead > 0 then
                    if reads % 15 = 0 then
                        let progress = float totalRead / float contentLength * 100.
                        reportProgress (id, progress)

                    do! file.WriteAsync(buffer, 0, bytesRead)
                    reads <- reads + 1
                    totalRead <- totalRead + int64 bytesRead
        | None ->
            reportProgress (id, -1.)
            do! stream.CopyToAsync(file)
    }
