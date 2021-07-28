namespace Rocksmith2014.Common.Manifest

open System

type ToneDescriptor =
    { Name : string; Aliases : string list; UIName : string; IsExtra : bool }

    override this.ToString() = this.Name

module ToneDescriptor =
    let all = [|
        { Name = "Acoustic"
          Aliases = [ "acoustic"; "acc"; "12str" ]
          UIName = "$[35721]ACOUSTIC"
          IsExtra = false }

        { Name = "Banjo"
          Aliases = [ "banjo" ]
          UIName = "$[27201]BANJO"
          IsExtra = true }

        { Name = "Bass"
          Aliases = [ "bass" ]
          UIName = "$[35715]BASS"
          IsExtra = false }

        { Name = "Chorus"
          Aliases = [ "chorus" ]
          UIName = "$[35723]CHORUS"
          IsExtra = false }

        { Name = "Clean"
          Aliases = [ "clean" ]
          UIName = "$[35720]CLEAN"
          IsExtra = false }

        { Name = "Crunch"
          Aliases = [ "crunch" ]
          UIName = "$[27156]CRUNCH"
          IsExtra = true }

        { Name = "Delay"
          Aliases = [ "delay" ]
          UIName = "$[35753]DELAY"
          IsExtra = false }

        // Unused in official content
        { Name = "Direct"
          Aliases = [ "direct" ]
          UIName = "$[35752]DIRECT"
          IsExtra = false }

        { Name = "Distortion"
          Aliases = [ "dist" ]
          UIName = "$[35722]DISTORTION"
          IsExtra = false }

        { Name = "Echo"
          Aliases = [ "echo" ]
          UIName = "$[35754]ECHO"
          IsExtra = false }

        { Name = "Effect"
          Aliases = [ "effect"; "pitch" ]
          UIName = "$[35733]EFFECT"
          IsExtra = false }

        { Name = "Emulated"
          Aliases = [ "emu" ]
          UIName = "$[27119]EMULATED"
          IsExtra = true }

        { Name = "Filter"
          Aliases = [ "filter"; "wah"; "talk" ]
          UIName = "$[35729]FILTER"
          IsExtra = false }

        { Name = "Flanger"
          Aliases = [ "flange" ]
          UIName = "$[35731]FLANGER"
          IsExtra = false }

        { Name = "Fuzz"
          Aliases = [ "fuzz" ]
          UIName = "$[35756]FUZZ"
          IsExtra = false }

        { Name = "High Gain"
          Aliases = [ "high"; "higain" ]
          UIName = "$[35755]HIGH GAIN"
          IsExtra = false }

        { Name = "Lead"
          Aliases = [ "lead"; "solo" ]
          UIName = "$[35724]LEAD"
          IsExtra = false }

        { Name = "Low Output"
          Aliases = [ "low" ]
          UIName = "$[35732]LOW OUTPUT"
          IsExtra = false }

        { Name = "Mandolin"
          Aliases = [ "mandolin" ]
          UIName = "$[27202]MANDOLIN"
          IsExtra = true }

        { Name = "Multi Effect"
          Aliases = [ "multi" ]
          UIName = "$[35751]MULTI-EFFECT"
          IsExtra = false }

        { Name = "Octave"
          Aliases = [ "8va"; "8vb"; "oct" ]
          UIName = "$[35719]OCTAVE"
          IsExtra = false }

        { Name = "Overdrive"
          Aliases = [ "od"; "drive" ]
          UIName = "$[35716]OVERDRIVE"
          IsExtra = false }

        { Name = "Phaser"
          Aliases = [ "phase" ]
          UIName = "$[35730]PHASER"
          IsExtra = false }

        { Name = "Piano"
          Aliases = [ "piano" ]
          UIName = "$[29495]PIANO"
          IsExtra = true }

        { Name = "Processed"
          Aliases = [ "synth"; "sustain" ]
          UIName = "$[35734]PROCESSED"
          IsExtra = false }

        { Name = "Reverb"
          Aliases = [ "verb" ]
          UIName = "$[35726]REVERB"
          IsExtra = false }

        { Name = "Rotary"
          Aliases = [ "roto" ]
          UIName = "$[35725]ROTARY"
          IsExtra = false }

        { Name = "Special Effect"
          Aliases = [ "swell"; "organ"; "sitar"; "sax" ]
          UIName = "$[35750]SPECIAL EFFECT"
          IsExtra = false }

        { Name = "Tremolo"
          Aliases = [ "trem" ]
          UIName = "$[35727]TREMOLO"
          IsExtra = false }

        { Name = "Ukulele"
          Aliases = [ "uke" ]
          UIName = "$[27204]UKULELE"
          IsExtra = true }

        { Name = "Vibrato"
          Aliases = [ "vib" ]
          UIName = "$[35728]VIBRATO"
          IsExtra = false }

        // Unused in official content
        { Name = "Vocal"
          Aliases = [ "vocal"; "vox" ]
          UIName = "$[35718]VOCAL"
          IsExtra = false } |]

    /// A dictionary for converting a UI name into a tone descriptor.
    let uiNameToDesc =
        all
        |> Array.map (fun x -> x.UIName, x)
        |> readOnlyDict

    /// Tries to infer tone descriptors from the given tone name.
    let tryInfer (name: string) =
        Array.FindAll(all, (fun descriptor ->
            descriptor.Aliases
            |> List.exists (fun alias -> name.Contains(alias, StringComparison.OrdinalIgnoreCase))))

    /// Returns an array of tone descriptors inferred from the tone name, or the clean tone descriptor as default.
    let getDescriptionsOrDefault (name: string) =
        match tryInfer name with
        | [||] ->
            // Use "Clean" as the default
            Array.singleton uiNameToDesc.["$[35720]CLEAN"]
        | descriptors ->
            descriptors |> Array.truncate 3

    /// Returns a description name for the given UI name.
    let uiNameToName (uiName: string) = uiNameToDesc.[uiName].Name

    /// Combines an array of UI names into a string of description names separated by spaces.
    let combineUINames (uiNames: string array) =
        String.Join(" ", Array.map uiNameToName uiNames)
