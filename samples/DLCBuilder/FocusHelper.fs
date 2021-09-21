module DLCBuilder.FocusHelper

open System.Collections.Generic
open Avalonia.Controls
open Avalonia.Input

let mutable private window: Window option = None

let private previouslyFocused = Stack<IInputElement>()

let init w = window <- Some w

let private focusWindow () =
    window |> Option.iter (fun w -> w.Focus())

let storeFocusedElement () =
    if notNull FocusManager.Instance.Current then
        previouslyFocused.Push(FocusManager.Instance.Current)

    // Blur the focus from the element
    focusWindow ()

let restoreFocus () =
    if previouslyFocused.Count > 0 then
        previouslyFocused.Pop().Focus()
    else
        focusWindow ()

// Restores the focus to the oldest element in the stack.
let restoreRootFocus () =
    let mutable element: IInputElement = null

    while previouslyFocused.Count > 0 do
        element <- previouslyFocused.Pop()

    if notNull element then
        element.Focus()
    else
        focusWindow ()
