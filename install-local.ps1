param([string]$GameManagedPath = 'D:/SteamLibrary/steamapps/common/Cities Skylines II/Cities2_Data/Managed')
$ErrorActionPreference = 'Stop'
dotnet build "$PSScriptRoot/CityStreetOverlay.csproj" -c Release --nologo "-p:GameManagedPath=$GameManagedPath"
if ($LASTEXITCODE -ne 0) { throw 'Build fehlgeschlagen.' }
$modDirectory = Join-Path ($env:LOCALAPPDATA + 'Low') 'Colossal Order/Cities Skylines II/Mods/CityStreetOverlay'
New-Item -ItemType Directory -Path $modDirectory -Force | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/bin/Release/net48/CityStreetOverlay.dll" -Destination $modDirectory -Force
Copy-Item -LiteralPath "$PSScriptRoot/bin/Release/net48/CityStreetOverlay.mjs" -Destination $modDirectory -Force
Copy-Item -LiteralPath "$PSScriptRoot/bin/Release/net48/CityStreetOverlay.css" -Destination $modDirectory -Force
Write-Host "Lokaler Prototyp installiert: $modDirectory"
Write-Host 'Spiel neu starten, Stadt laden und das Karten-Symbol in der Mod-Leiste oben links anklicken.'
