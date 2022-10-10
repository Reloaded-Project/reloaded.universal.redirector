# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/Reloaded.Universal.Redirector/*" -Force -Recurse
dotnet publish "./Reloaded.Universal.Redirector.csproj" -c Release -o "$env:RELOADEDIIMODS/Reloaded.Universal.Redirector" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location