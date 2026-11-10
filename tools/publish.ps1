param(
    [string]$Output = "dist"
)

$ErrorActionPreference = "Stop"
Remove-Item $Output -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish src/ZapretLauncher.App -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -o $Output

Get-Item (Join-Path $Output "ZapretLauncher.exe") | Select-Object FullName, Length
