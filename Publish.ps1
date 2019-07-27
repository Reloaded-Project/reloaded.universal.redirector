# Remove cached build
$monitorOutputPath = "Reloaded.Universal.Monitor/bin"
$redirectorOutputPath = "Reloaded.Universal.Redirector/bin"
$interfacesOutputPath = "Reloaded.Universal.Redirector.Interfaces/bin"
$redirectorMonitorOutputPath = "Reloaded.Universal.RedirectorMonitor/bin"

if ([System.IO.File]::Exists($monitorOutputPath)) {
	Get-ChildItem $monitorOutputPath -Include * -Recurse | Remove-Item -Force -Recurse
}

if ([System.IO.File]::Exists($redirectorOutputPath)) {
	Get-ChildItem $redirectorOutputPath -Include * -Recurse | Remove-Item -Force -Recurse
}

if ([System.IO.File]::Exists($interfacesOutputPath)) {
	Get-ChildItem $interfacesOutputPath -Include * -Recurse | Remove-Item -Force -Recurse
}

if ([System.IO.File]::Exists($redirectorMonitorOutputPath)) {
	Get-ChildItem $redirectorMonitorOutputPath -Include * -Recurse | Remove-Item -Force -Recurse
}

# Build
dotnet build -c Release Reloaded.Universal.Redirector.sln

# Cleanup
Get-ChildItem $monitorOutputPath -Include *.pdb -Recurse | Remove-Item -Force -Recurse
Get-ChildItem $monitorOutputPath -Include *.xml -Recurse | Remove-Item -Force -Recurse

Get-ChildItem $redirectorOutputPath -Include *.pdb -Recurse | Remove-Item -Force -Recurse
Get-ChildItem $redirectorOutputPath -Include *.xml -Recurse | Remove-Item -Force -Recurse

Get-ChildItem $redirectorMonitorOutputPath -Include *.pdb -Recurse | Remove-Item -Force -Recurse
Get-ChildItem $redirectorMonitorOutputPath -Include *.xml -Recurse | Remove-Item -Force -Recurse

# Make compressed directory
New-Item "Publish" -ItemType Directory

# Compress
Add-Type -A System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory('Reloaded.Universal.Monitor/bin/Release', 'Publish/Monitor.zip')
[IO.Compression.ZipFile]::CreateFromDirectory('Reloaded.Universal.Redirector/bin/Release', 'Publish/Redirector.zip')
[IO.Compression.ZipFile]::CreateFromDirectory('Reloaded.Universal.RedirectorMonitor/bin/Release', 'Publish/RedirectorMonitor.zip')