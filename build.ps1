# NethLogger Build Script (Universal)
# This script works whether called from the root or from within the NethLogger folder.

$scriptPath = $PSScriptRoot
if (!$scriptPath) { $scriptPath = Get-Location }

# Root is the parent of NethLogger (the game root)
$gameRoot = Join-Path $scriptPath ".."
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

$refArgs = $refs | ForEach-Object { "/r:`"$_`"" }
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outPath = Join-Path $outDir "NethTelemetry.dll"
$srcPath = Join-Path $scriptPath "NethTelemetry.cs"

Write-Host "Compiling NethLogger v7.3.0..." -ForegroundColor Cyan
& $csc /target:library "/out:$outPath" $refArgs $srcPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: NethTelemetry.dll generated at $outPath" -ForegroundColor Green
} else {
    Write-Host "FAILED: Compilation errors detected." -ForegroundColor Red
}
