module DLCBuilder.PreviewUtils

open Rocksmith2014.Audio
open Rocksmith2014.DLCProject
open Rocksmith2014.XML
open System
open System.IO

/// Returns a path to the project's audio in wave or vorbis format.
let getOggOrWavAudio (project: DLCProject) =
    let projectAudio = project.AudioFile.Path

    match projectAudio with
    | HasExtension (".wav" | ".ogg") ->
        projectAudio
    | _ ->
        let wavFile = Path.ChangeExtension(projectAudio, "wav")
        let oggFile = Path.ChangeExtension(projectAudio, "ogg")
        if File.Exists(wavFile) then
            wavFile
        elif File.Exists(oggFile) then
            oggFile
        else
            Conversion.wemToOgg projectAudio
            oggFile

/// Creates a preview audio file for the project.
let createAutoPreviewFile project =
    async {
        let audioPath = getOggOrWavAudio project
        let targetPath = Utils.createPreviewAudioPath audioPath

        let previewStart =
            project.Arrangements
            |> List.tryPick Arrangement.pickInstrumental
            |> function
                | Some arr ->
                    let inst = InstrumentalArrangement.Load(arr.XML)

                    inst.Sections
                    |> ResizeArray.tryFind (fun x -> x.Name |> String.startsWith "chorus")
                    |> Option.map (fun x -> float x.Time)
                    |> Option.defaultWith (fun () -> float inst.MetaData.SongLength / 2.)
                    |> TimeSpan.FromMilliseconds
                | None ->
                    failwith "Project does not have any instrumental arrangements."

        Preview.create audioPath targetPath previewStart

        return targetPath
    }
