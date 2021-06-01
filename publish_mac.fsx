#load "publish.fsx"

open Publish

cleanPublishDirectory()
publishUpdater MacOS
publishBuilder MacOS
createZipArchive MacOS
|> addFileToRelease
