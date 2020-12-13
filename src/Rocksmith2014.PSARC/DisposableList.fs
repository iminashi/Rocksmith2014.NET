namespace Rocksmith2014.PSARC

open System

type DisposableList<'a when 'a :> IDisposable>(items: 'a list) =
    member _.Items = items

    interface IDisposable with
        member this.Dispose() =
            this.Items |> List.iter (fun x -> x.Dispose())
