module VolumeTests

open Expecto
open Rocksmith2014.Audio

let private testVolumeCalculation testFile =
    let vol = Volume.calculate testFile

    Expect.floatClose Accuracy.high vol -0.2 "Volume is correct"

[<Tests>]
let volumeTests =
    testList "Volume Calculation Tests" [
        test "Volume can be calculated for wave file" { testVolumeCalculation TestFiles.WaveFile }
        test "Volume can be calculated for vorbis file" { testVolumeCalculation TestFiles.VorbisFile }
        test "Volume can be calculated for FLAC file" { testVolumeCalculation TestFiles.FlacFile }
    ]
