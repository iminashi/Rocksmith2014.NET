namespace Rocksmith2014.Audio

// Ported from: https://github.com/xuan525/R128Normalization

type SecondOrderIIRFilter(b0At48k, b1At48k, b2At48k, a1At48k, a2At48k, sampleRate, numChannels) =
    let [<Literal>] SampleRate48k = 48_000.

    let mutable b0 = b0At48k
    let mutable b1 = b1At48k
    let mutable b2 = b2At48k
    let mutable a1 = a1At48k
    let mutable a2 = a2At48k

    let z1 = Array.zeroCreate<float> numChannels
    let z2 = Array.zeroCreate<float> numChannels

    do if sampleRate <> SampleRate48k then
        let koverQ = (2. - 2. * a2At48k) / (a2At48k - a1At48k + 1.)
        let k = sqrt ((a1At48k + a2At48k + 1.) / (a2At48k - a1At48k + 1.))
        let q = k / koverQ
        let arctanK = atan k
        let vb = (b0At48k - b2At48k) / (1. - a2At48k)
        let vh = (b0At48k - b1At48k + b2At48k) / (a2At48k - a1At48k + 1.)
        let vl = (b0At48k + b1At48k + b2At48k) / (a1At48k + a2At48k + 1.)

        let k = tan (arctanK * SampleRate48k / sampleRate)
        let commonFactor = 1. / (1. + k / q + k * k)
        b0 <- (vh + vb * k / q + vl * k * k) * commonFactor
        b1 <- 2. * (vl * k * k - vh) * commonFactor
        b2 <- (vh - vb * k / q + vl * k * k) * commonFactor
        a1 <- 2. * (k * k - 1.) * commonFactor
        a2 <- (1. - k / q + k * k) * commonFactor
            
    member _.ProcessBufferGeneral(buffer: float[][]) =
        for channel = 0 to numChannels - 1 do
            let samples = buffer.[channel]

            for i = 0 to samples.Length - 1 do
                let inVal = samples.[i]

                let factorForB0 = inVal - a1 * z1.[channel] - a2 * z2.[channel]
                let outVal = b0 * factorForB0 + b1 * z1.[channel] + b2 * z2.[channel]

                z2.[channel] <- z1.[channel]
                z1.[channel] <- factorForB0

                samples.[i] <- outVal

    member this.ProcessBuffer(buffer: float[][]) =
        // Unroll the outer loop for the usual case of two channel audio
        if numChannels = 2 then
            for i = 0 to buffer.[0].Length - 1 do
                // Channel 1
                let factorForB0 = buffer.[0].[i] - a1 * z1.[0] - a2 * z2.[0]
                let outVal = b0 * factorForB0 + b1 * z1.[0] + b2 * z2.[0]

                z2.[0] <- z1.[0]
                z1.[0] <- factorForB0

                buffer.[0].[i] <- outVal

                // Channel 2
                let factorForB0 = buffer.[1].[i] - a1 * z1.[1] - a2 * z2.[1]
                let outVal = b0 * factorForB0 + b1 * z1.[1] + b2 * z2.[1]

                z2.[1] <- z1.[1]
                z1.[1] <- factorForB0

                buffer.[1].[i] <- outVal
        else
            this.ProcessBufferGeneral buffer
