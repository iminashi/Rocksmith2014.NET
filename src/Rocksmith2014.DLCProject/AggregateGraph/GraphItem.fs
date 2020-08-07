namespace Rocksmith2014.DLCProject

open System
open System.IO

type GraphItem =
    { UUID: Guid
      LLID: Guid option
      Tags: string list
      Canonical: string
      Name: string
      RelPath: string
      LogPath: string option }

module GraphItem =
    let private lineTemplate = "<urn:uuid:{0}> <http://emergent.net/aweb/1.0/{1}> \"{2}\"."

    let write (writer: StreamWriter) (item: GraphItem) =
        let uuid = item.UUID.ToString()

        item.Tags
        |> List.iter (fun t -> writer.WriteLine(lineTemplate, uuid, TagType.Tag, t))

        writer.WriteLine(lineTemplate, uuid, TagType.Canonical, item.Canonical)
        writer.WriteLine(lineTemplate, uuid, TagType.Name, item.Name)
        
        item.LLID
        |> Option.iter (fun l -> writer.WriteLine(lineTemplate, uuid, TagType.LLID, l))
        
        item.LogPath
        |> Option.iter (fun l -> writer.WriteLine(lineTemplate, uuid, TagType.LogPath, l))
        
        writer.Write(lineTemplate, uuid, TagType.RelPath, item.RelPath)
