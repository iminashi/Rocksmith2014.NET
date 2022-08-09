module BeatWriter

open Rocksmith2014.XML
open EOFTypes
open BinaryFileWriter

let [<Literal>] AnchoredFlag = 1u

let private tsFlagGetter (tsEvents: (int * EOFTimeSignature) list) =
    let tsMap = tsEvents |> readOnlyDict

    fun (time: int) ->
        match tsMap.TryGetValue time with
        | false, _ ->
            0u
        | true, ts ->
            match ts with
            | ``TS 2 | 4`` ->
                512u
            | ``TS 3 | 4`` ->
                8u
            | ``TS 4 | 4`` ->
                4u
            | ``TS 5 | 4`` ->
                16u
            | ``TS 6 | 4`` ->
                32u
            | CustomTS (d, n) ->
                64u
                ||| ((d - 1u) <<< 24)
                ||| ((n - 1u) <<< 16)

let writeBeat
        (inst: InstrumentalArrangement)
        (events: Set<int>)
        (getTsFlag: int -> uint)
        (index: int)
        (beat: Ebeat) =
    binaryWriter {
        let nextBeatTime =
            inst.Ebeats
            |> Seq.tryItem (index + 1)
            |> Option.map (fun x -> x.Time)
            |> Option.defaultValue inst.MetaData.SongLength

        let eventFlag = if events.Contains(beat.Time) then 2u else 0u
        let tsFlag = getTsFlag beat.Time

        // Tempo 
        (nextBeatTime - beat.Time) * 1000 // TODO
        // Position
        beat.Time
        // Flags
        AnchoredFlag ||| eventFlag ||| tsFlag
        // Key signature
        0y
    }

let writeBeats
        (inst: InstrumentalArrangement)
        (events: (int * EOFEvent) array)
        (tsEvents: (int * EOFTimeSignature) list) =
    let eventsSet =
        events
        |> Array.map fst
        |> Set.ofArray

    let getTsFlag = tsFlagGetter tsEvents

    binaryWriter {
        inst.Ebeats.Count

        for (i, b) in inst.Ebeats |> Seq.indexed do
            yield! writeBeat inst eventsSet getTsFlag i b
    }
