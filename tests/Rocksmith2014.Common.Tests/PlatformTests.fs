module PlatformTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.Common.Platform

[<Tests>]
let platformTests =
    testList "Platform Tests" [
        test "Path parts are correct for PC" {
            let paths = [
                getPathPart PC Path.Audio
                getPathPart PC Path.SNG
                getPathPart PC Path.PackageSuffix ]

            Expect.sequenceContainsOrder paths [ "windows"; "generic"; "_p" ] "Path parts are correct" }

        test "Path parts are correct for Mac" {
            let paths = [
                getPathPart Mac Path.Audio
                getPathPart Mac Path.SNG
                getPathPart Mac Path.PackageSuffix ]

            Expect.sequenceContainsOrder paths [ "mac"; "macos"; "_m" ] "Path parts are correct" }
    ]