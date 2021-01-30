module VolumeTests

open Expecto
open Rocksmith2014.Audio

[<Tests>]
let volumeTests =
    testList "Volume Calculation Tests" [
        testCase "Volume can be calculated for wave file" <| fun _ ->
            let vol = Volume.calculate TestFiles.WaveFile

            Expect.floatClose Accuracy.high vol -0.2 "Volume is correct"

        testCase "Volume can be calculated for vorbis file" <| fun _ ->
            let vol = Volume.calculate TestFiles.VorbisFile

            Expect.floatClose Accuracy.high vol -0.2 "Volume is correct"
    ]
