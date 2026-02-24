module DLCBuilder.IdRegenerationHelper

open Rocksmith2014.DLCProject
open System
open System.Threading.Tasks

let private requestConfirmationEvent = Event<ArrangementId list * AsyncReply>()
let private idsGeneratedEvent = Event<Map<ArrangementId, Arrangement>>()

let RequestConfirmation = requestConfirmationEvent.Publish
let NewIdsGenerated = idsGeneratedEvent.Publish

let getConfirmation (ids: ArrangementId list) : Async<bool> =
    async {
        let answer = TaskCompletionSource<bool>()
        let reply = AsyncReply(answer.SetResult)

        requestConfirmationEvent.Trigger(ids, reply)

        return! answer.Task |> Async.AwaitTask
    }

let postNewIds (newIdMap: Map<ArrangementId, Arrangement>) = idsGeneratedEvent.Trigger(newIdMap)
