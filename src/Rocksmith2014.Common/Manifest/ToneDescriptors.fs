namespace Rocksmith2014.Common.Manifest

open System

type ToneDescriptor = { Name : string; Alias : string; UIName : string }

module ToneDescriptor =
    let all = [|
        { Name = "Acoustic"; Alias = "acoustic"; UIName = "$[35721]ACOUSTIC" }
        { Name = "Bass"; Alias = "bass"; UIName = "$[35715]BASS" }
        { Name = "Chorus"; Alias = "chorus"; UIName = "$[35723]CHORUS" }
        { Name = "Clean"; Alias = "clean"; UIName = "$[35720]CLEAN" }
        { Name = "Delay"; Alias = "delay"; UIName = "$[35753]DELAY" }
        { Name = "Direct *"; Alias = "direct"; UIName = "$[35752]DIRECT" }
        { Name = "Distortion"; Alias = "dist"; UIName = "$[35722]DISTORTION" }
        { Name = "Echo"; Alias = "echo"; UIName = "$[35754]ECHO" }
        { Name = "Effect *"; Alias = "effect"; UIName = "$[35733]EFFECT" }
        { Name = "Filter"; Alias = "filter"; UIName = "$[35729]FILTER" }
        { Name = "Flanger"; Alias = "flanger"; UIName = "$[35731]FLANGER" }
        { Name = "Fuzz"; Alias = "fuzz"; UIName = "$[35756]FUZZ" }
        { Name = "High Gain"; Alias = "high"; UIName = "$[35755]HIGH GAIN" }
        { Name = "Lead"; Alias = "lead"; UIName = "$[35724]LEAD" }
        { Name = "Low Output"; Alias = "low"; UIName = "$[35732]LOW OUTPUT" }
        { Name = "Multi Effect"; Alias = "multi"; UIName = "$[35751]MULTI-EFFECT" }
        { Name = "Octave"; Alias = "8va"; UIName = "$[35719]OCTAVE" }
        { Name = "Overdrive"; Alias = "od"; UIName = "$[35716]OVERDRIVE" }
        { Name = "Phaser"; Alias = "phaser"; UIName = "$[35730]PHASER" }
        { Name = "Processed"; Alias = "comp"; UIName = "$[35734]PROCESSED" }
        { Name = "Reverb"; Alias = "verb"; UIName = "$[35726]REVERB" }
        { Name = "Rotary"; Alias = "roto"; UIName = "$[35725]ROTARY" }
        { Name = "Special Effect"; Alias = "sitar"; UIName = "$[35750]SPECIAL EFFECT" }
        { Name = "Tremolo"; Alias = "trem"; UIName = "$[35727]TREMOLO" }
        { Name = "Vibrato"; Alias = "vib"; UIName = "$[35728]VIBRATO" }    
        { Name = "Vocal *"; Alias = "vocal"; UIName = "$[35718]VOCAL" }

        { Name = "** Crunch"; Alias = "crunch"; UIName = "$[27156]CRUNCH" }
        { Name = "** Emulated"; Alias = "emu"; UIName = "$[27119]EMULATED" }
        { Name = "** Slap Bass"; Alias = "slap"; UIName = "$[27151]SLAP_BASS" }
        { Name = "** Banjo"; Alias = "banjo"; UIName = "$[27201]BANJO" }
        { Name = "** Mandolin"; Alias = "mandolin"; UIName = "$[27202]MANDOLIN" }
        { Name = "** Piano"; Alias = "piano"; UIName = "$[29495]PIANO" }
        { Name = "** Ukulele"; Alias = "uke"; UIName = "$[27204]UKULELE" } |]

    let tryInfer (name: string) =
        Array.tryFind (fun x -> name.Contains(x.Alias, StringComparison.OrdinalIgnoreCase)) all

    let getDescriptionOrDefault (name: string) =
        tryInfer name |> Option.defaultValue all.[3] // Use clean as default

    let private uiNameToName (uiName: string) =
        let desc = 
            all
            |> Array.find (fun x -> x.UIName = uiName)
        desc.Name.Trim([| '*'; ' ' |])

    let convertUINames (uiNames: string array) =
        let names = 
            uiNames
            |> Array.map uiNameToName
        String.Join(" ", names)
