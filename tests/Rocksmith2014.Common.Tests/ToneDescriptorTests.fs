module ToneDescriptorTests

open Expecto
open Rocksmith2014.Common.Manifest

[<Tests>]
let toneDescriptorTests =
    testList "Tone Descriptor Tests" [
        test "Default description is 'Clean'" {
            let descriptors = ToneDescriptor.getDescriptionsOrDefault "tone_test"
            
            Expect.hasLength descriptors 1 "One descriptor exists"
            Expect.exists descriptors (fun x -> x.Name = "Clean") "Clean descriptor inferred" }
        
        test "Can infer a description" {
            let descriptors = ToneDescriptor.getDescriptionsOrDefault "tone_bass"
        
            Expect.hasLength descriptors 1 "One descriptor exists"
            Expect.exists descriptors (fun x -> x.Name = "Bass") "Bass descriptor inferred" }
        
        test "Can infer a two part description" {
            let descriptors = ToneDescriptor.getDescriptionsOrDefault "tone_8valead"
        
            Expect.hasLength descriptors 2 "Two descriptors exist"
            Expect.exists descriptors (fun x -> x.Name = "Octave") "Octave descriptor inferred"
            Expect.exists descriptors (fun x -> x.Name = "Lead") "Lead descriptor inferred" }
        
        test "Can infer a three part description" {
            let descriptors = ToneDescriptor.getDescriptionsOrDefault "tone_rotophasedrive"
        
            Expect.hasLength descriptors 3 "Three descriptors exist"
            Expect.exists descriptors (fun x -> x.Name = "Rotary") "Rotary descriptor inferred"
            Expect.exists descriptors (fun x -> x.Name = "Phaser") "Phaser descriptor inferred"
            Expect.exists descriptors (fun x -> x.Name = "Overdrive") "Overdrive descriptor inferred" }

        test "Infers at max three descriptors" {
            let descriptors = ToneDescriptor.getDescriptionsOrDefault "tone_rotophasedrivevibtrem"
        
            Expect.hasLength descriptors 3 "Three descriptors exist" }
        
        test "Can combine UI names" {
            let uiNames =
                ToneDescriptor.getDescriptionsOrDefault "tone_accwah"
                |> Array.map (fun x -> x.UIName)
        
            let res = ToneDescriptor.combineUINames uiNames
        
            Expect.equal res "Acoustic Filter" "Combined name is correct" }
    ]
