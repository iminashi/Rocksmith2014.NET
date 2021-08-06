module DLCBuilder.IdRegenerationHelper

open System
open Rocksmith2014.DLCProject

let private requestConfirmationEvent = Event<Guid list * AsyncReplyChannel<bool>>()
let private idsGeneratedEvent = Event<Map<Guid, Arrangement>>()

let RequestConfirmation = requestConfirmationEvent.Publish
let NewIdsGenerated = idsGeneratedEvent.Publish

type private MailBoxMessage =
    | RequestConfirmation of Guid list * AsyncReplyChannel<bool>
    | NewIdsGenerated of Map<Guid, Arrangement>

let private mb = MailboxProcessor.Start(fun mbox ->
    let rec loop () = async {
        match! mbox.Receive() with
        | RequestConfirmation (arrangementPaths, replyChannel) ->
            requestConfirmationEvent.Trigger(arrangementPaths, replyChannel)
        | NewIdsGenerated idMap ->
            idsGeneratedEvent.Trigger(idMap)

        return! loop () }
    loop ())

let getConfirmation ids =
    mb.PostAndAsyncReply(fun reply -> RequestConfirmation(ids, reply))

let postNewIds newIdMap =
    mb.Post(NewIdsGenerated newIdMap)
