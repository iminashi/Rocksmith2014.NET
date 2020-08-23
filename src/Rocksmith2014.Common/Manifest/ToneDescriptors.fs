namespace Rocksmith2014.Common.Manifest

open System

type ToneDescriptor =
    { Name : string; Aliases : string list; UIName : string }

    override this.ToString() = this.Name

module ToneDescriptor =
    let all = [|
        { Name = "Acoustic"
          Aliases = [ "acoustic"; "acc"; "12str" ]
          UIName = "$[35721]ACOUSTIC" }

        // Extra from the localization file
        { Name = "Banjo"
          Aliases = [ "banjo" ]
          UIName = "$[27201]BANJO" }

        { Name = "Bass"
          Aliases = [ "bass" ]
          UIName = "$[35715]BASS" }

        { Name = "Chorus"
          Aliases = [ "chorus" ]
          UIName = "$[35723]CHORUS" }

        { Name = "Clean"
          Aliases = [ "clean" ]
          UIName = "$[35720]CLEAN" }

        // Extra from the localization file
        { Name = "Crunch"
          Aliases = [ "crunch" ]
          UIName = "$[27156]CRUNCH" }

        { Name = "Delay"
          Aliases = [ "delay" ]
          UIName = "$[35753]DELAY" }

        // Unused in official content
        { Name = "Direct"
          Aliases = [ "direct" ]
          UIName = "$[35752]DIRECT" }

        { Name = "Distortion"
          Aliases = [ "dist" ]
          UIName = "$[35722]DISTORTION" }

        { Name = "Echo"
          Aliases = [ "echo" ]
          UIName = "$[35754]ECHO" }

        // Unused in official content
        { Name = "Effect"
          Aliases = [ "effect" ]
          UIName = "$[35733]EFFECT" }

        // Extra from the localization file
        { Name = "Emulated"
          Aliases = [ "emu" ]
          UIName = "$[27119]EMULATED" }

        { Name = "Filter"
          Aliases = [ "filter"; "wah"; "talk" ]
          UIName = "$[35729]FILTER" }

        { Name = "Flanger"
          Aliases = [ "flange" ]
          UIName = "$[35731]FLANGER" }

        { Name = "Fuzz"
          Aliases = [ "fuzz" ]
          UIName = "$[35756]FUZZ" }

        { Name = "High Gain"
          Aliases = [ "high"; "higain" ]
          UIName = "$[35755]HIGH GAIN" }

        { Name = "Lead"
          Aliases = [ "lead"; "solo" ]
          UIName = "$[35724]LEAD" }

        { Name = "Low Output"
          Aliases = [ "low" ]
          UIName = "$[35732]LOW OUTPUT" }

        // Extra from the localization file
        { Name = "Mandolin"
          Aliases = [ "mandolin" ]
          UIName = "$[27202]MANDOLIN" }

        { Name = "Multi Effect"
          Aliases = [ "multi" ]
          UIName = "$[35751]MULTI-EFFECT" }

        { Name = "Octave"
          Aliases = [ "8va"; "8vb"; "oct" ]
          UIName = "$[35719]OCTAVE" }

        { Name = "Overdrive"
          Aliases = [ "od"; "drive" ]
          UIName = "$[35716]OVERDRIVE" }

        { Name = "Phaser"
          Aliases = [ "phase" ]
          UIName = "$[35730]PHASER" }

        // Extra from the localization file
        { Name = "Piano"
          Aliases = [ "piano" ]
          UIName = "$[29495]PIANO" }

        { Name = "Processed"
          Aliases = [ "synth"; "sustain" ]
          UIName = "$[35734]PROCESSED" }

        { Name = "Reverb"
          Aliases = [ "verb" ]
          UIName = "$[35726]REVERB" }

        { Name = "Rotary"
          Aliases = [ "roto" ]
          UIName = "$[35725]ROTARY" }

        { Name = "Special Effect"
          Aliases = [ "swell"; "organ"; "sitar"; "sax" ]
          UIName = "$[35750]SPECIAL EFFECT" }

        { Name = "Tremolo"
          Aliases = [ "trem" ]
          UIName = "$[35727]TREMOLO" }

        // Extra from the localization file
        { Name = "Ukulele"
          Aliases = [ "uke" ]
          UIName = "$[27204]UKULELE" }

        { Name = "Vibrato"
          Aliases = [ "vib" ]
          UIName = "$[35728]VIBRATO" }

        // Unused in official content
        { Name = "Vocal"
          Aliases = [ "vocal"; "vox" ]
          UIName = "$[35718]VOCAL" } |]

    let uiNameToDesc =
        all
        |> Array.map (fun x -> x.UIName, x)
        |> dict

    let tryInfer (name: string) =
        Array.FindAll(all, (fun x ->
            x.Aliases
            |> List.exists (fun a -> name.Contains(a, StringComparison.OrdinalIgnoreCase))))

    let getDescriptionsOrDefault (name: string) =
        let descs = tryInfer name
        if descs.Length = 0 then [| all.[3] |] // Use clean as default
        else descs

    let private uiNameToName (uiName: string) =
        Array.find (fun x -> x.UIName = uiName) all

    let combineUINames (uiNames: string array) =
        let names = 
            uiNames
            |> Array.map uiNameToName
        String.Join(" ", names)
