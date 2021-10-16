module DLCBuilder.Downloader

open System.IO
open System.Net.Http
open System

let ToneDBUrl = "https://github.com/iminashi/tones/raw/main/official.db"

let private reportProgress =
    (ProgressReporters.DownloadFile :> IProgress<DownloadId * float>).Report

let downloadFile (url: string) (targetPath: string) id =
    async {
        use client = new HttpClient()

        let! response =
            client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false)

        use! stream =
            response
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync()
                .ConfigureAwait(false)

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)) |> ignore
        use file = File.Create(targetPath)

        match response.Content.Headers.ContentLength |> Option.ofNullable with
        | None ->
            reportProgress (id, -1.)
            do! stream.CopyToAsync(file)
        | Some contentLength ->
            let buffer = Array.zeroCreate<byte> 8192

            let rec copyBuffer reads totalRead =
                async {
                    let! bytesRead = stream.AsyncRead(buffer, 0, buffer.Length)

                    if bytesRead <> 0 then
                        if reads % 15 = 0 then
                            let progress = float totalRead / float contentLength * 100.
                            reportProgress (id, progress)

                        do! file.AsyncWrite(buffer, 0, bytesRead)
                        do! copyBuffer (reads + 1) (totalRead + int64 bytesRead)
                }

            do! copyBuffer 0 0L
    }
