module BeatWriter

open Rocksmith2014.EOF.EOFTypes
open Rocksmith2014.XML
open BinaryWriterBuilder

let [<Literal>] AnchoredFlag = 1u

let private tsFlagGetter (tsEvents: (int * EOFTimeSignature) list) =
    let tsMap = tsEvents |> readOnlyDict
    let mutable denominator = 4

    fun (time: int) ->
        match tsMap.TryGetValue time with
        | false, _ ->
            0u, denominator
        | true, ts ->
            denominator <-
                match ts with
                | CustomTS (_, d) -> int d
                | _ -> 4

            let flag =
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
                | CustomTS (n, d) ->
                    64u
                    ||| ((n - 1u) <<< 24)
                    ||| ((d - 1u) <<< 16)

            flag, denominator

let private getTempo den nextBeatTime beatTime =
    let tempo =
        let beatLength = float (nextBeatTime - beatTime) * 1000.0
        // Adjust for time signature and round up
        beatLength * (den / 4.0) + 0.5
        |> int

    // Tempo of 0 on the last beat causes strange issues
    if tempo = 0 then 400000 else tempo

let writeBeat
        (inst: InstrumentalArrangement)
        (events: Set<int>)
        (getTs: int -> uint * int)
        (index: int)
        (beat: Ebeat) =
    binaryWriter {
        let nextBeat =
            inst.Ebeats
            |> ResizeArray.tryItem (index + 1)

        let nextBeatTime =
            nextBeat
            |> Option.map (fun x -> x.Time)
            |> Option.defaultValue inst.MetaData.SongLength

        let eventFlag = if events.Contains(index) then 2u else 0u
        let tsFlag, den =
            let flag, den = getTs beat.Time
            // Ignore any time signature change on the last beat
            (if nextBeat.IsSome then flag else 0u), float den

        let tempo = getTempo den nextBeatTime beat.Time

        // Tempo
        tempo
        // Position
        beat.Time
        // Flags
        AnchoredFlag ||| eventFlag ||| tsFlag
        // Key signature
        0y
    }

let writeBeats
        (inst: InstrumentalArrangement)
        (events: EOFEvent array)
        (tsEvents: (int * EOFTimeSignature) list) =
    let eventsSet =
        events
        |> Array.map (fun e -> e.BeatNumber)
        |> Set.ofArray

    let getTsFlag = tsFlagGetter tsEvents

    binaryWriter {
        inst.Ebeats.Count

        for (i, b) in inst.Ebeats |> Seq.indexed do
            yield! writeBeat inst eventsSet getTsFlag i b
    }
