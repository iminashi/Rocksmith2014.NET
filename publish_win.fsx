#load "publish.fsx"

open Publish

cleanPublishDirectory()
publishUpdater Windows
publishBuilder Windows
createZipArchive Windows
|> createGitHubRelease
