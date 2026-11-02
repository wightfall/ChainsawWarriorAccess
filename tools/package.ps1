# Builds everything and makes the release zip: dist\ChainsawWarriorAccess-v<version>.zip with one folder,
# CWAccess, to extract into the game folder. Needs the game installed (the mod compiles against its DLLs).
param([string]$GameDir = "")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

& (Join-Path $PSScriptRoot "fetch-deps.ps1")

[xml]$props = Get-Content (Join-Path $root "Directory.Build.props")
$version = $props.Project.PropertyGroup.Version
$entry = Get-Content (Join-Path $root "src\CWAccess\Entry.cs") -Raw
if ($entry -notmatch "Version = `"$([regex]::Escape($version))`"") { throw "Entry.Version does not match Directory.Build.props ($version)." }

$extra = @("-c", "Release", "-p:DeployToGame=false")
if ($GameDir) { $extra += "-p:GameDir=$GameDir" }
foreach ($p in "CWAccess.Boot", "CWAccess", "CWAccess.SpeechHost", "CWAccess.Installer") {
    dotnet build "src\$p\$p.csproj" @extra
    if ($LASTEXITCODE -ne 0) { throw "Build of $p failed." }
}

$dist = Join-Path $root "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
$out = Join-Path $dist "CWAccess"
New-Item -ItemType Directory -Force $out | Out-Null
$files = @(
    "src\CWAccess\bin\Release\net35\CWAccess.dll",
    "src\CWAccess.Boot\bin\Release\net35\CWAccess.Boot.dll",
    "src\CWAccess.SpeechHost\bin\Release\net472\CWAccess.SpeechHost.exe",
    "src\CWAccess.Installer\bin\Release\net472\CWAccess-Setup.exe",
    "lib\harmony\0Harmony.dll",
    "lib\harmony\MonoMod.RuntimeDetour.dll",
    "lib\harmony\MonoMod.Utils.dll",
    "lib\harmony\Mono.Cecil.dll",
    "lib\nvda\nvdaControllerClient32.dll"
)
foreach ($f in $files) { Copy-Item (Join-Path $root $f) $out }
Copy-Item (Join-Path $root "README.md") (Join-Path $out "CWAccess-README.md")
Copy-Item (Join-Path $root "LICENSE") (Join-Path $out "LICENSE.txt")
Copy-Item (Join-Path $root "THIRD-PARTY-NOTICES.md") (Join-Path $out "THIRD-PARTY-NOTICES.md")
Copy-Item (Join-Path $root "lib\nvda\license.txt") (Join-Path $out "NVDA-controller-client-license.txt")

$zip = Join-Path $dist "ChainsawWarriorAccess-v$version.zip"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
# Entries are written one by one with "/" separators (CreateFromDirectory on Windows PowerShell 5 uses "\",
# which other unzip tools turn into odd file names).
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($f in Get-ChildItem $out -File) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $f.FullName, "CWAccess/" + $f.Name, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose() }
Write-Host "Package: $zip"
Get-FileHash $zip -Algorithm SHA256 | ForEach-Object { Write-Host "SHA256: $($_.Hash)" }
