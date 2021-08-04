module DLCBuilder.FocusHelper

open System.Collections.Generic
open Avalonia
open Avalonia.Input
open Avalonia.Controls.ApplicationLifetimes
open Rocksmith2014.Common

let private window =
    lazy (Application.Current.ApplicationLifetime :?> ClassicDesktopStyleApplicationLifetime).MainWindow

let private previouslyFocused = Stack<IInputElement>()

let storeFocusedElement () =
    if notNull FocusManager.Instance.Current then
        previouslyFocused.Push(FocusManager.Instance.Current)

    // Blur the focus from the element
    window.Value.Focus()

let restoreFocus () =
    if previouslyFocused.Count > 0 then
        previouslyFocused.Pop().Focus()
    else
        window.Value.Focus()

// Restores the focus to the oldest element in the stack.
let restoreRootFocus () =
    let mutable element : IInputElement = null
    while previouslyFocused.Count > 0 do element <- previouslyFocused.Pop()
    if notNull element then
        element.Focus()
    else
        window.Value.Focus()
