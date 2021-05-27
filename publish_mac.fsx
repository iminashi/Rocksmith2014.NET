#load "publish.fsx"

open Publish

cleanPublishDirectory()
publishBuilder MacOS
createZipArchive MacOS
|> addFileAndPublish
