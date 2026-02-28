namespace Rocksmith2014.DLCProject

open System
open System.IO
open Rocksmith2014.Common
open Rocksmith2014.Common.Platform

type GraphItem =
    { UUID: Guid
      LLID: Guid option
      Tags: string list
      Canonical: string
      Name: string
      RelPath: string
      LogPath: string option }

module GraphItem =
    let private lineTemplate =
        "<urn:uuid:{0}> <http://emergent.net/aweb/1.0/{1}> \"{2}\"."

    let private zeroes = Array.zeroCreate<byte> 8

    let private newLLID () =
        Guid(RandomGenerator.next(), 0s, 0s, zeroes)

    let private getPlatformTag = function
        | PC -> Tag.DX9
        | Mac -> Tag.MacOS

    let private make name canonical tags rp lp =
        let llid = lp |> Option.map (fun _ -> newLLID ())

        { Name = name
          Canonical = canonical
          RelPath = rp
          LogPath = lp
          Tags = tags
          UUID = Guid.NewGuid()
          LLID = llid }

    /// Creates a graph item.
    let normal name canonical extension tags =
        let path = $"%s{canonical}/%s{name}.%s{extension}"
        make name canonical tags path None

    /// Creates a graph item with an LLID when the relative and logical paths are identical.
    let llid name canonical extension tags =
        let path = $"%s{canonical}/%s{name}.%s{extension}"
        make name canonical tags path (Some path)

    /// Creates a graph item for an SNG file.
    let sng name platform =
        let canonical =
            $"/songs/bin/%s{getPathPart platform Path.SNG}"

        let rp = $"%s{canonical}/%s{name}.sng"
        let lp = Some $"/songs/bin/%s{name}.sng"

        make name canonical [ Tag.Application; Tag.MusicgameSong; if platform = Mac then Tag.MacOS ] rp lp

    /// Creates a graph item for a DDS image.
    let dds name canonical = llid name canonical "dds" [ Tag.DDS; Tag.Image ]

    /// Creates a graph item for a BNK file.
    let bnk name platform =
        let canonical =
            $"/audio/%s{getPathPart platform Path.Audio}"

        let rp = $"%s{canonical}/%s{name}.bnk"
        let lp = Some $"/audio/%s{name}.bnk"

        make name canonical [ Tag.Audio; Tag.WwiseSoundBank; getPlatformTag platform ] rp lp

    /// Writes the graph item into the writer.
    let write (writer: StreamWriter) (item: GraphItem) =
        let uuid = item.UUID.ToString()

        item.Tags
        |> List.iter (fun t -> writer.WriteLine(lineTemplate, uuid, TagType.Tag, t))

        writer.WriteLine(lineTemplate, uuid, TagType.Canonical, item.Canonical)
        writer.WriteLine(lineTemplate, uuid, TagType.Name, item.Name)

        match item.LLID, item.LogPath with
        | Some llid, Some logPath ->
            writer.WriteLine(lineTemplate, uuid, TagType.LLID, llid)
            writer.WriteLine(lineTemplate, uuid, TagType.LogPath, logPath)
        | None, None ->
            ()
        | _ ->
            failwith "LLID and Log Path must both have a value."

        writer.Write(lineTemplate, uuid, TagType.RelPath, item.RelPath)
