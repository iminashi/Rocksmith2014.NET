#load "publish.fsx"

open Publish

cleanPublishDirectory()
publishBuilder Linux
createZipArchive Linux
|> addFileToRelease
