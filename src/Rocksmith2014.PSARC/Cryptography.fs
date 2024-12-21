module Rocksmith2014.PSARC.Cryptography

open System
open System.IO
open System.Security.Cryptography
open System.Text

// Disable warning "This byte array literal contains 14 non-ASCII characters. All characters should be < 128y."
#nowarn "FS1253"

let private psarcKey =
    "\xC5\x3D\xB2\x38\x70\xA1\xA2\xF7\x1C\xAE\x64\x06\x1F\xDD\x0E\x11\x57\x30\x9D\xC8\x52\x04\xD4\xC5\xBF\xDF\x25\x09\x0D\xF2\x57\x2C"B

let private createAes () =
    Aes.Create()
    |> apply (fun aes ->
        aes.Key <- psarcKey
        aes.Mode <- CipherMode.CFB
        aes.BlockSize <- 128
        aes.FeedbackSize <- 128
        aes.Padding <- PaddingMode.Zeros)

let private getEncryptStream (output: Stream) =
    use aes = createAes ()
    let encryptor = aes.CreateEncryptor(psarcKey, Array.zeroCreate<byte> 16)
    new CryptoStream(output, encryptor, CryptoStreamMode.Write, true)

/// Decrypts a PSARC header from the input stream.
let decrypt (input: Stream) (length: int32) =
    use aes = createAes ()
    let cipherTextLength = aes.GetCiphertextLengthCfb(length, PaddingMode.Zeros, 128)

    let buffer = Array.zeroCreate<byte> cipherTextLength
    let bytesRead = input.Read(buffer, 0, length) in assert (bytesRead = length)
    let decrypted = aes.DecryptCfb(buffer, Array.zeroCreate<byte> 16, PaddingMode.Zeros, 128)

    // Ignore the zero padding (length < cipherTextLength)
    new MemoryStream(decrypted, 0, length)

/// Encrypts a plain PSARC header from the input stream into the output stream.
let encrypt (input: Stream) (output: Stream) =
    using (getEncryptStream output) input.CopyTo

/// Calculates an MD5 hash for the given string.
let md5Hash (name: string) =
    if String.IsNullOrEmpty(name) then
        Array.zeroCreate<byte> 16
    else
        using (MD5.Create()) (fun md5 -> md5.ComputeHash(Encoding.ASCII.GetBytes(name)))
