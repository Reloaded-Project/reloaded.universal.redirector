
# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD


./Publish.ps1 -ProjectPath "Reloaded.Universal.Redirector/Reloaded.Universal.Redirector.csproj" `
              -PackageName "Reloaded.Universal.Redirector" `
			  -ReadmePath ./README-REDIRECTOR.md `
              -PublishOutputDir "Publish/ToUpload/Redirector" `
              -MakeDelta true -UseGitHubDelta true `
              -MetadataFileName "Reloaded.Universal.Redirector.ReleaseMetadata.json" `
              -GitHubUserName Reloaded-Project -GitHubRepoName reloaded.universal.redirector -GitHubFallbackPattern reloaded.universal.redirector.zip -GitHubInheritVersionFromTag false `
			  @args

./Publish.ps1 -ProjectPath "Reloaded.Universal.Monitor/Reloaded.Universal.Monitor.csproj" `
              -PackageName "Reloaded.Universal.Monitor" `
			  -ReadmePath ./README-MONITOR.md `
              -PublishOutputDir "Publish/ToUpload/Monitor" `
              -MakeDelta true -UseGitHubDelta true `
              -MetadataFileName "Reloaded.Universal.Monitor.ReleaseMetadata.json" `
              -GitHubUserName Reloaded-Project -GitHubRepoName reloaded.universal.redirector -GitHubFallbackPattern reloaded.universal.monitor.zip -GitHubInheritVersionFromTag false `
			  @args

./Publish.ps1 -ProjectPath "Reloaded.Universal.RedirectorMonitor/Reloaded.Universal.RedirectorMonitor.csproj" `
              -PackageName "Reloaded.Universal.RedirectorMonitor" `
			  -ReadmePath ./README-REDIRECTORMONITOR.md `
              -PublishOutputDir "Publish/ToUpload/RedirectorMonitor" `
              -MakeDelta true -UseGitHubDelta true `
              -MetadataFileName "Reloaded.Universal.RedirectorMonitor.ReleaseMetadata.json" `
              -GitHubUserName Reloaded-Project -GitHubRepoName reloaded.universal.redirector -GitHubFallbackPattern reloaded.universal.redirectormonitor.zip -GitHubInheritVersionFromTag false `
			  @args

Pop-Location