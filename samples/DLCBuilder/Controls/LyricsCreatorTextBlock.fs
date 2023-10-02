// fsharplint:disable MemberNames
namespace DLCBuilder

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Interactivity
open JapaneseLyricsCreator

[<Sealed>]
type LyricsCreatorTextBlock() =
    inherit TextBlock()

    let createLocation (line, index) (e: RoutedEventArgs) =
        e.Handled <- true
        { LineNumber = line; Index = index }

    member val Location = (0, 0) with get, set
    member val Dispatch: CombinationLocation -> unit = ignore with get, set

    override this.OnKeyDown(e) =
        if e.Key = Key.Space then
            createLocation this.Location e
            |> this.Dispatch
        else
            base.OnKeyDown(e)

    override this.OnPointerPressed(e) =
        createLocation this.Location e
        |> this.Dispatch

    override this.OnGotFocus(e) =
        base.OnGotFocus(e)
        this.BringIntoView()

    override _.StyleKeyOverride = typeof<TextBlock>

    static member onClick(fn: CombinationLocation -> unit) =
        let getter (c: LyricsCreatorTextBlock) = c.Dispatch
        let setter: LyricsCreatorTextBlock * (CombinationLocation -> unit) -> unit = fun (c, f) -> c.Dispatch <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<LyricsCreatorTextBlock>.CreateProperty<CombinationLocation -> unit>
            ("Dispatch", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member location(loc: int * int) =
        let getter (c: LyricsCreatorTextBlock) = c.Location
        let setter: LyricsCreatorTextBlock * (int * int) -> unit = fun (c, v) -> c.Location <- v

        AttrBuilder<LyricsCreatorTextBlock>.CreateProperty<int * int>
            ("Location", loc, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module LyricsCreatorTextBlock =
    let create (attrs: IAttr<LyricsCreatorTextBlock> list) : IView<LyricsCreatorTextBlock> =
        ViewBuilder.Create<LyricsCreatorTextBlock>(attrs)
