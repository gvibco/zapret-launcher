param(
    [string]$Destination = "build/zapret-src"
)

$ErrorActionPreference = "Stop"
if (Test-Path $Destination) {
    Write-Host "Already exists: $Destination"
    exit 0
}

git clone --depth 1 https://github.com/Flowseal/zapret-discord-youtube $Destination
if ($LASTEXITCODE -ne 0) { throw "git clone failed" }
Write-Host "Fetched zapret to $Destination"
