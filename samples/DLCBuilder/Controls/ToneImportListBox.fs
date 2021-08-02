// fsharplint:disable MemberNames
namespace DLCBuilder

open Avalonia.Controls
open Avalonia.Controls.Selection
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open Rocksmith2014.Common.Manifest

type ToneImportListBox() as this =
    inherit ListBox()

    let selectedTones = SelectionModel<Tone>(SingleSelect = false)
    let mutable selectionChangedCallback : Tone list -> unit = ignore

    do this.Selection <- selectedTones

    interface IStyleable with member _.StyleKey = typeof<ListBox>

    member _.OnSelectedItemsChangedCallback
        with get() : Tone list -> unit = selectionChangedCallback
        and set(v) =
            selectionChangedCallback <- v
            selectedTones.SelectionChanged
                .Add (fun _ -> selectedTones.SelectedItems |> Seq.toList |> selectionChangedCallback)

    static member onSelectedTonesChanged<'t when 't :> ToneImportListBox> fn =
        let getter : 't -> (Tone list -> unit) = fun c -> c.OnSelectedItemsChangedCallback
        let setter : ('t * (Tone list -> unit)) -> unit = fun (c, f) -> c.OnSelectedItemsChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<'t>.CreateProperty<Tone list -> unit>("OnSelectedTonesChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

[<AutoOpen>]
module ToneImportListBox =
    let create (attrs: IAttr<ToneImportListBox> list): IView<ToneImportListBox> =
        ViewBuilder.Create<ToneImportListBox>(attrs)
