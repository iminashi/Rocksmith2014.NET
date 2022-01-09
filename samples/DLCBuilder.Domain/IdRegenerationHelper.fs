module DLCBuilder.IdRegenerationHelper

open Rocksmith2014.DLCProject
open System
open System.Threading.Tasks

let private requestConfirmationEvent = Event<Guid list * AsyncReply>()
let private idsGeneratedEvent = Event<Map<Guid, Arrangement>>()

let RequestConfirmation = requestConfirmationEvent.Publish
let NewIdsGenerated = idsGeneratedEvent.Publish

let getConfirmation ids = async {
    let answer = TaskCompletionSource<bool>()
    let reply = AsyncReply(answer.SetResult)

    requestConfirmationEvent.Trigger(ids, reply)

    return! answer.Task |> Async.AwaitTask }

let postNewIds newIdMap = idsGeneratedEvent.Trigger(newIdMap)
