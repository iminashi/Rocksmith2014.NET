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
    member val Fusion: TargetLocation -> unit = ignore with get, set
    member val Split: TargetLocation -> unit = ignore with get, set

    override this.OnKeyDown(e) =
        match e.Key with
        | Key.Space ->
            createLocation this.Location e
            |> this.Fusion
        | Key.S ->
            createLocation this.Location e
            |> this.Split
        | _ ->
            base.OnKeyDown(e)

    override this.OnPointerPressed(e) =
        let point = e.GetCurrentPoint(this)
        if point.Properties.IsLeftButtonPressed then
            createLocation this.Location e
            |> this.Fusion
        elif point.Properties.IsRightButtonPressed then
            createLocation this.Location e
            |> this.Split

    override this.OnGotFocus(e) =
        base.OnGotFocus(e)
        this.BringIntoView()

    override _.StyleKeyOverride = typeof<TextBlock>

    static member onClick(fn: TargetLocation -> unit) =
        let getter (c: LyricsCreatorTextBlock) = c.Fusion
        let setter: LyricsCreatorTextBlock * (TargetLocation -> unit) -> unit = fun (c, f) -> c.Fusion <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<LyricsCreatorTextBlock>.CreateProperty<TargetLocation -> unit>
            ("Fusion", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member onRightClick(fn: TargetLocation -> unit) =
        let getter (c: LyricsCreatorTextBlock) = c.Split
        let setter: LyricsCreatorTextBlock * (TargetLocation -> unit) -> unit = fun (c, f) -> c.Split <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<LyricsCreatorTextBlock>.CreateProperty<TargetLocation -> unit>
            ("Split", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member location(loc: int * int) =
        let getter (c: LyricsCreatorTextBlock) = c.Location
        let setter: LyricsCreatorTextBlock * (int * int) -> unit = fun (c, v) -> c.Location <- v

        AttrBuilder<LyricsCreatorTextBlock>.CreateProperty<int * int>
            ("Location", loc, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module LyricsCreatorTextBlock =
    let create (attrs: IAttr<LyricsCreatorTextBlock> list) : IView<LyricsCreatorTextBlock> =
        ViewBuilder.Create<LyricsCreatorTextBlock>(attrs)
