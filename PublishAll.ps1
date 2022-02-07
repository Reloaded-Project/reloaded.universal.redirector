
# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD


./Publish.ps1 -ProjectPath "Reloaded.Universal.Redirector/Reloaded.Universal.Redirector.csproj" `
              -PackageName "Reloaded.Universal.Redirector" `
              -PublishOutputDir "Publish/ToUpload/Redirector" `
              -MakeDelta true -UseGitHubDelta true `
              -GitHubUserName Reloaded-Project -GitHubRepoName reloaded.universal.redirector -GitHubFallbackPattern reloaded.universal.redirector.zip

./Publish.ps1 -ProjectPath "Reloaded.Universal.Monitor/Reloaded.Universal.Monitor.csproj" `
              -PackageName "Reloaded.Universal.Monitor" `
              -PublishOutputDir "Publish/ToUpload/Monitor" `
              -MakeDelta true -UseGitHubDelta true `
              -GitHubUserName Reloaded-Project -GitHubRepoName reloaded.universal.redirector -GitHubFallbackPattern reloaded.universal.monitor.zip

./Publish.ps1 -ProjectPath "Reloaded.Universal.RedirectorMonitor/Reloaded.Universal.RedirectorMonitor.csproj" `
              -PackageName "Reloaded.Universal.RedirectorMonitor" `
              -PublishOutputDir "Publish/ToUpload/RedirectorMonitor" `
              -MakeDelta true -UseGitHubDelta true `
              -GitHubUserName Reloaded-Project -GitHubRepoName reloaded.universal.redirector -GitHubFallbackPattern reloaded.universal.redirectormonitor.zip

Pop-Location