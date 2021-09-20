namespace Elmish

[<RequireQualifiedAccess>]
module Cmd =
    module OfAsync =
        /// Command that will evaluate an async block to an optional message.
        let optionalResult (task: Async<'msg option>) : Cmd<'msg> =
            let bind dispatch =
                async {
                    let! r = task
                    Option.iter dispatch r
                }

            [ bind >> Async.Start ]
