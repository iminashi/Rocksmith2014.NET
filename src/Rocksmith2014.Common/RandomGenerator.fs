module Rocksmith2014.Common.RandomGenerator

open System

let private rand = Random()

/// Returns a non-negative random integer.
let next () = rand.Next()

/// Returns a random integer between min (inclusive) and max (exclusive).
let nextInRange min max = rand.Next(min, max)

/// Returns a random lower case alphabet.
let nextAlphabet () = char <| rand.Next(int 'a', int 'z' + 1)
