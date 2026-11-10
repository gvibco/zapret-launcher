param(
    [string]$SourceDir = "build/zapret-src",
    [string]$Output = "assets/engine.zip"
)

$ErrorActionPreference = "Stop"
$stage = "build/engine-stage"
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stage | Out-Null

$required = @("winws.exe", "WinDivert.dll", "WinDivert64.sys", "cygwin1.dll")
foreach ($name in $required) {
    $src = Join-Path $SourceDir "bin/$name"
    if (-not (Test-Path $src)) { throw "Missing $src" }
    Copy-Item $src $stage
}

Copy-Item (Join-Path $SourceDir "bin") $stage -Recurse
Copy-Item (Join-Path $SourceDir "lists") $stage -Recurse
New-Item -ItemType Directory -Path (Split-Path $Output) -Force | Out-Null
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $Output -Force
Write-Host "Wrote $Output"
