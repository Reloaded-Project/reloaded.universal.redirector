$publishDirectory = "Publish"
Remove-Item $publishDirectory -Recurse

& .\PublishMonitor.ps1
& .\PublishRedirector.ps1
& .\PublishRedirectorMonitor.ps1
