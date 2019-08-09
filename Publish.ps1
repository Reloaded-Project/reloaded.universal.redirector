# Project Output Paths
$monitorOutputPath        		= "Reloaded.Universal.Monitor/bin"
$redirectorOutputPath      		= "Reloaded.Universal.Redirector/bin"
$redirectorMonitorOutputPath = "Reloaded.Universal.RedirectorMonitor/bin"

$monitorPublishName       	= "Monitor.zip"
$redirectorPublishName      = "Redirector.zip"
$redirectorMonitorPublishName = "RedirectorMonitor.zip"

$solutionName = "Reloaded.Universal.Redirector.sln"
$publishDirectory = "Publish"

if ([System.IO.Directory]::Exists($publishDirectory)) {
	Get-ChildItem $publishDirectory -Include * -Recurse | Remove-Item -Force -Recurse
}

# Build
dotnet restore $solutionName
dotnet clean $solutionName
dotnet build -c Release $solutionName

# Cleanup
Get-ChildItem $monitorOutputPath -Include *.pdb -Recurse | Remove-Item -Force -Recurse
Get-ChildItem $monitorOutputPath -Include *.xml -Recurse | Remove-Item -Force -Recurse

Get-ChildItem $redirectorOutputPath -Include *.pdb -Recurse | Remove-Item -Force -Recurse
Get-ChildItem $redirectorOutputPath -Include *.xml -Recurse | Remove-Item -Force -Recurse

Get-ChildItem $redirectorMonitorOutputPath -Include *.pdb -Recurse | Remove-Item -Force -Recurse
Get-ChildItem $redirectorMonitorOutputPath -Include *.xml -Recurse | Remove-Item -Force -Recurse

# Make compressed directory
if (![System.IO.Directory]::Exists($publishDirectory)) {
    New-Item $publishDirectory -ItemType Directory
}

# Compress
Add-Type -A System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory( $monitorOutputPath + '/Release', 'Publish/' + $monitorPublishName)
[IO.Compression.ZipFile]::CreateFromDirectory( $redirectorOutputPath + '/Release', 'Publish/' + $redirectorPublishName)
[IO.Compression.ZipFile]::CreateFromDirectory( $redirectorMonitorOutputPath + '/Release', 'Publish/' + $redirectorMonitorPublishName)