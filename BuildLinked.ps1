# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/RnSArchipelago/*" -Force -Recurse
dotnet publish "./Rabit_and_Steel_Test.csproj" -c Release -o "$env:RELOADEDIIMODS/RnSArchipelago" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location