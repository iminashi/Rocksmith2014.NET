namespace Rocksmith2014.Audio

open System
open System.Numerics
open System.Runtime.CompilerServices
open SimpleSimd

// Ported from: https://github.com/xuan525/R128Normalization
// Functionality for calculating integrated loudness once.

[<IsReadOnly; Struct>]
/// Store mean square infos of previous sample blocks from the time the measurement was started (for integrated loudness)
type internal MeanSquareLoudness = { MeanSquare : float; Loudness : float }

[<IsReadOnly; Struct>]
type internal VectorSquare = interface IFunc<Vector<float>, Vector<float>> with member _.Invoke(param) = param * param

[<IsReadOnly; Struct>]
type internal Square = interface IFunc<float, float> with member _.Invoke(param) = param * param

[<AutoOpen>]
module internal Helpers =
    let getChannelWeight = function 3 | 4 -> 1.41 | _ -> 1.

    let inline loudness mean = -0.691 + 10. * Math.Log10 mean
    
    let meanSquareAverage = function
        | [||] -> 0.
        | arr -> Array.averageBy (fun x -> x.MeanSquare) arr
    
    let inline gate gateLoudness = Array.filter (fun x -> x.Loudness > gateLoudness)

type LufsMeter(blockDuration, overlap, sampleRate, numChannels) =
    let precedingMeanSquareLoudness = ResizeArray<MeanSquareLoudness>()

    // Initialize momentary loudness
    let blockSampleCount = int <| round (blockDuration * sampleRate)
    let stepSampleCount = int <| round (float blockSampleCount * (1. - overlap))
    let blockStepCount = blockSampleCount / stepSampleCount

    let mutable stepBufferPosition = 0
    let mutable stepBuffer: float[][] = Array.init numChannels (fun _ -> Array.zeroCreate stepSampleCount)
    // Buffer for calculating mean square for current sample block (for momentary loudness) double[step][channel][sample]
    let blockBuffer: float[][][] =
        Array.init blockStepCount (fun _ -> Array.init numChannels (fun _ -> Array.zeroCreate stepSampleCount))

    let preFilter =
        SecondOrderIIRFilter(
            1.53512485958697,   // b0
            -2.69169618940638,  // b1
            1.19839281085285,   // b2
            -1.69065929318241,  // a1
            0.73248077421585,   // a2
            sampleRate,
            numChannels)

    let highPassFilter =
        SecondOrderIIRFilter(
            1.0,                // b0
            -2.0,               // b1
            1.0,                // b2
            -1.99004745483398,  // a1
            0.99007225036621,   // a2
            sampleRate,
            numChannels)

    new(sampleRate, numChannels) = LufsMeter(0.4, 0.75, sampleRate, numChannels)

    member _.ProcessBuffer(buffer: float[][]) =
        // “K” frequency weighting
        preFilter.ProcessBuffer buffer
        highPassFilter.ProcessBuffer buffer

        // Initialize the process
        let mutable bufferPosition = 0
        let bufferSampleCount = buffer.[0].Length
        while bufferPosition + (stepSampleCount - stepBufferPosition) < bufferSampleCount do
            // Enough to fill a step
            for channel = 0 to numChannels - 1 do
                Array.Copy(buffer.[channel], bufferPosition, stepBuffer.[channel], stepBufferPosition, stepSampleCount - stepBufferPosition)

            bufferPosition <- bufferPosition + stepSampleCount - stepBufferPosition

            // Swap buffer
            let temp = blockBuffer.[0]
            for i = 1 to blockBuffer.Length - 1 do
                blockBuffer.[i - 1] <- blockBuffer.[i]
            blockBuffer.[blockBuffer.Length - 1] <- stepBuffer
            stepBuffer <- temp
            stepBufferPosition <- 0

            // Calculate momentary loudness
            let mutable momentaryMeanSquare = 0.
            for channel = 0 to numChannels - 1 do
                let channelSquaredSum =
                    blockBuffer
                    |> Array.sumBy (fun stepBuffer ->
                        let a = ReadOnlySpan(stepBuffer.[channel])
                        SimdOps.Sum(&a, VectorSquare(), Square()))
                let channelMeanSquare = channelSquaredSum / float (blockStepCount * stepSampleCount)
                let channelWeight = getChannelWeight channel
                momentaryMeanSquare <- momentaryMeanSquare + channelWeight * channelMeanSquare

            let momentaryLoudness = loudness momentaryMeanSquare

            // Calculate integrated loudness
            let meanSquareLoudness = { MeanSquare = momentaryMeanSquare; Loudness = momentaryLoudness }
            precedingMeanSquareLoudness.Add meanSquareLoudness

        // Process remaining samples
        let remainingLength = bufferSampleCount - bufferPosition
        for channel = 0 to numChannels - 1 do
            Array.Copy(buffer.[channel], bufferPosition, stepBuffer.[channel], stepBufferPosition, remainingLength)
        stepBufferPosition <- remainingLength

    member _.GetIntegratedLoudness () =
        // Gating of 400 ms blocks (overlapping by 75%), where two thresholds are used:
        // The first at −70 LKFS
        let absoluteGateGamma = -70.
        let absoluteGatedLoudness = precedingMeanSquareLoudness.ToArray() |> gate absoluteGateGamma
        let absoluteGatedMeanSquare = meanSquareAverage absoluteGatedLoudness
        
        // The second at −10 dB relative to the level measured after application of the first threshold.
        let relativeGateGamma = loudness absoluteGatedMeanSquare - 10.
        let relativeGatedLoudness = absoluteGatedLoudness |> gate relativeGateGamma
        let relativeGatedMeanSquare = meanSquareAverage relativeGatedLoudness

        loudness relativeGatedMeanSquare
