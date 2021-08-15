// fsharplint:disable MemberNames
namespace DLCBuilder

open Avalonia.Controls
open Avalonia.Controls.Selection
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open Rocksmith2014.Common.Manifest

[<Sealed>]
type ToneImportListBox() =
    inherit ListBox()

    let selectedTones = SelectionModel<Tone>(SingleSelect = false)
    let mutable selectionChangedCallback : Tone list -> unit = ignore

    do base.Selection <- selectedTones

    interface IStyleable with member _.StyleKey = typeof<ListBox>

    member _.OnSelectedItemsChangedCallback
        with get() : Tone list -> unit = selectionChangedCallback
        and set(v) =
            selectionChangedCallback <- v
            selectedTones.SelectionChanged
                .Add (fun _ -> selectedTones.SelectedItems |> Seq.toList |> selectionChangedCallback)

    static member onSelectedTonesChanged fn =
        let getter (c: ToneImportListBox) = c.OnSelectedItemsChangedCallback
        let setter : (ToneImportListBox * (Tone list -> unit)) -> unit = fun (c, f) -> c.OnSelectedItemsChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<ToneImportListBox>.CreateProperty<Tone list -> unit>("OnSelectedTonesChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

[<RequireQualifiedAccess>]
module ToneImportListBox =
    let create (attrs: IAttr<ToneImportListBox> list): IView<ToneImportListBox> =
        ViewBuilder.Create<ToneImportListBox>(attrs)
