module Rocksmith2014.Common.Profile

open System.IO
open System.Security.Cryptography
open System.Text
open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryReaders
open Rocksmith2014.Common.Manifest
open Newtonsoft.Json
open BinaryWriters

type ProfileHeader = { Version: uint32; ID: uint64; UncompressedLength: uint32 }

let private profileKey = "\x72\x8B\x36\x9E\x24\xED\x01\x34\x76\x85\x11\x02\x18\x12\xAF\xC0\xA3\xC2\x5D\x02\x06\x5F\x16\x6B\x4B\xCC\x58\xCD\x26\x44\xF2\x9E"B

let private getDecryptStream (input: Stream) =
    use aes = new AesManaged(Mode = CipherMode.ECB, Padding = PaddingMode.None)
    let decryptor = aes.CreateDecryptor(profileKey, null)
    new CryptoStream(input, decryptor, CryptoStreamMode.Read, true)

let private getEncryptStream (output: Stream) =
    use aes = new AesManaged(Mode = CipherMode.ECB, Padding = PaddingMode.Zeros)
    let encryptor = aes.CreateEncryptor(profileKey, null)
    new CryptoStream(output, encryptor, CryptoStreamMode.Write, true)

let private readHeader (stream: Stream) =
    let reader = LittleEndianBinaryReader(stream) :> IBinaryReader
    let magic = reader.ReadBytes 4
    if Encoding.ASCII.GetString magic <> "EVAS" then
        failwith "Profile magic check failed."
    { Version = reader.ReadUInt32(); ID = reader.ReadUInt64(); UncompressedLength = reader.ReadUInt32() }

/// Decrypts the Rocksmith 2014 profile data from the input stream into the output stream.
let decrypt (input: Stream) (output: Stream) = async {
    let header = readHeader input

    use decrypted = MemoryStreamPool.Default.GetStream()
    use dStream = getDecryptStream input

    do! dStream.CopyToAsync decrypted

    decrypted.Position <- 0L
    do! Compression.asyncUnzip decrypted output
    return header }

/// Encrypts profile data from the input stream into the output stream.
let private encryptProfileData (input: Stream) (output: Stream) = async {
    use eStream = getEncryptStream output

    do! input.CopyToAsync eStream }

/// Writes the profile data into the target file.
let write (targetFile: string) (profileId: uint64) (jsonData: Stream) = async {
    // Write null-terminator
    jsonData.Seek(0L, SeekOrigin.End) |> ignore
    jsonData.WriteByte 0uy

    use file = File.Create targetFile

    let writer = LittleEndianBinaryWriter(file) :> IBinaryWriter
    writer.WriteBytes "EVAS"B
    writer.WriteUInt32 1u
    writer.WriteUInt64 profileId
    writer.WriteUInt32 (uint32 jsonData.Length)

    use zipped = MemoryStreamPool.Default.GetStream()
    jsonData.Position <- 0L
    do! Compression.asyncZip jsonData zipped

    zipped.Position <- 0L
    do! encryptProfileData zipped file }

/// Reads an array of tones from the profile with the given path.
let importTones (path: string) =
    try
        use profile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan)
        readHeader profile |> ignore

        use decrypted = getDecryptStream profile
        use unzipped = Compression.getInflateStream decrypted
        use reader = new StreamReader(unzipped, Encoding.UTF8)
        use json = new JsonTextReader(reader)

        // Skip to the CustomTones array
        while json.Read() && not (json.TokenType = JsonToken.StartArray && json.Path = "CustomTones") do ()

        if json.Path = "CustomTones" then
            JsonSerializer().Deserialize<ToneDto array> json
            |> Array.map Tone.fromDto
            |> Ok
        else
            Error "Profile contains no custom tones."
    with ex ->
        Error ex.Message
