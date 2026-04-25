# NethLogger Build Script (Universal)
# This script works whether called from the root or from within the NethLogger folder.

# Get the absolute path of the directory containing this script
$scriptPath = [System.IO.Path]::GetFullPath($PSScriptRoot)

# Define paths relative to the script location
$gameRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptPath ".."))
$managed = Join-Path $gameRoot "SimplePlanes 2_Data\Managed"
$bepcore = Join-Path $scriptPath "BepInEx\core"
$outDir = Join-Path $scriptPath "BepInEx\plugins"

if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force }

$refs = @(
    (Join-Path $managed "UnityEngine.dll"),
    (Join-Path $managed "UnityEngine.CoreModule.dll"),
    (Join-Path $managed "UnityEngine.UI.dll"),
    (Join-Path $managed "UnityEngine.InputLegacyModule.dll"),
    (Join-Path $managed "UnityEngine.PhysicsModule.dll"),
    (Join-Path $managed "netstandard.dll"),
    (Join-Path $bepcore "BepInEx.Core.dll"),
    (Join-Path $bepcore "BepInEx.Unity.Mono.dll")
)

$srcPath = Join-Path $scriptPath "NethTelemetry.cs"
$outPath = Join-Path $outDir "NethTelemetry.dll"

# Generate a response file to pass arguments to CSC.
# This is the most reliable way to handle complex paths with spaces and quotes across different PowerShell versions.
$rspPath = Join-Path $scriptPath "build.rsp"
$rspContent = @(
    "/target:library",
    "/out:`"$outPath`""
)
foreach ($ref in $refs) {
    $rspContent += "/reference:`"$ref`""
}
$rspContent += "`"$srcPath`""

# Write the response file using UTF8 (no BOM preferred, but UTF8 is standard)
$rspContent | Out-File -FilePath $rspPath -Encoding UTF8

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

Write-Host "Compiling NethLogger v7.3.0..." -ForegroundColor Cyan
& $csc "@$rspPath"

# Cleanup the temporary response file
if (Test-Path $rspPath) { Remove-Item $rspPath }

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: NethTelemetry.dll generated at $outPath" -ForegroundColor Green
} else {
    Write-Host "FAILED: Compilation errors detected." -ForegroundColor Red
}
