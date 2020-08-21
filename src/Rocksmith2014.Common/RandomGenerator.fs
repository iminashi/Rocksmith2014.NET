module Rocksmith2014.Common.RandomGenerator

open System

let private rand = Random()

let next () = rand.Next()

let nextInRange min max = rand.Next(min, max)

let nextAlphabet () = char <| rand.Next(int 'a', int 'z' + 1)
