# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/Reloaded.Universal.Monitor/*" -Force -Recurse
dotnet publish "./Reloaded.Universal.Monitor.csproj" -c Release -o "$env:RELOADEDIIMODS/Reloaded.Universal.Monitor" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location