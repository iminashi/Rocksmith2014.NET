module Rocksmith2014.Common.Profile

open System.IO
open System.Security.Cryptography
open System.Text
open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.Interfaces
open Rocksmith2014.Common.Manifest
open Newtonsoft.Json

let private profileKey = "\x72\x8B\x36\x9E\x24\xED\x01\x34\x76\x85\x11\x02\x18\x12\xAF\xC0\xA3\xC2\x5D\x02\x06\x5F\x16\x6B\x4B\xCC\x58\xCD\x26\x44\xF2\x9E"B

let private getDecryptStream (input: Stream) =
    use aes = new AesManaged(Mode = CipherMode.ECB, Padding = PaddingMode.None)
    let decryptor = aes.CreateDecryptor(profileKey, null)
    new CryptoStream(input, decryptor, CryptoStreamMode.Read, true)

let private readHeader (stream: Stream) =
    let reader = LittleEndianBinaryReader(stream) :> IBinaryReader
    let magic = reader.ReadBytes 4
    if Encoding.ASCII.GetString magic <> "EVAS" then
        failwith "Profile magic check failed."
    reader.ReadUInt32() |> ignore // Version
    reader.ReadUInt64() |> ignore // Profile ID
    reader.ReadUInt32() |> ignore // Uncompressed length

/// Writes a decrypted Rocksmith 2014 profile from the input stream into the output stream.
let decryptProfile (input: Stream) (output: Stream) = async {
    readHeader input

    use decrypted = MemoryStreamPool.Default.GetStream()
    use dStream = getDecryptStream input

    do! dStream.CopyToAsync decrypted

    decrypted.Position <- 0L
    do! Compression.unzip decrypted output }

/// Reads an array of tones from the profile with the given path.
let importTones (path: string) =
    try
        use profile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan)
        readHeader profile

        use decrypted = getDecryptStream profile
        use unzipped = Compression.getInflateStream decrypted
        use reader = new StreamReader(unzipped, Encoding.UTF8)
        use json = new JsonTextReader(reader)

        // Skip to the CustomTones array
        while json.Read() && not (json.TokenType = JsonToken.StartArray && json.Path = "CustomTones") do ()

        if json.Path = "CustomTones" then
            Ok (JsonSerializer().Deserialize<Tone array> json)
        else
            Error "Profile contains no custom tones."
    with ex -> Error ex.Message
