# NethLogger Build Script (Portable)
$root = ".." # Assumes script is in NethLogger subfolder
$managed = "$root\SimplePlanes 2_Data\Managed"
$bepcore = "$root\NethLogger\BepInEx\core"
$outDir = "$root\NethLogger\BepInEx\plugins"

if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir }

$refs = @(
    "$managed\UnityEngine.dll",
    "$managed\UnityEngine.CoreModule.dll",
    "$managed\UnityEngine.UI.dll",
    "$managed\UnityEngine.InputLegacyModule.dll",
    "$managed\UnityEngine.PhysicsModule.dll",
    "$managed\netstandard.dll",
    "$bepcore\BepInEx.Core.dll",
    "$bepcore\BepInEx.Unity.Mono.dll"
)

$refArgs = $refs | ForEach-Object { "/reference:`"$_`"" }
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outPath = "$outDir\NethTelemetry.dll"
$srcPath = "NethTelemetry.cs"

Write-Host "Compiling NethLogger v2.7.1..." -ForegroundColor Cyan
& $csc /target:library "/out:$outPath" $refArgs "$srcPath"

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: NethTelemetry.dll generated at $outPath" -ForegroundColor Green
} else {
    Write-Host "FAILED: Compilation errors detected." -ForegroundColor Red
}
