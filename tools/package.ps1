# Builds everything, runs the tests and makes the release files in dist\:
#   ChainsawWarriorAccess-v<version>.zip   one folder, CWAccess, to extract into the game folder (setup included)
#   CWAccess-Setup-v<version>.exe          installer / updater with the same files built in (works offline)
# Needs the game installed (the mod compiles against its DLLs).
param([string]$GameDir = "", [switch]$SkipTests)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

& (Join-Path $PSScriptRoot "fetch-deps.ps1")

[xml]$props = Get-Content (Join-Path $root "Directory.Build.props")
$version = $props.Project.PropertyGroup.Version
$entry = Get-Content (Join-Path $root "src\CWAccess\Entry.cs") -Raw
if ($entry -notmatch "Version = `"$([regex]::Escape($version))`"") { throw "Entry.Version does not match Directory.Build.props ($version)." }

$extra = @("-c", "Release", "-p:DeployToGame=false")
if ($GameDir) { $extra += "-p:GameDir=$GameDir" }
foreach ($p in "CWAccess.Boot", "CWAccess", "CWAccess.SpeechHost") {
    dotnet build "src\$p\$p.csproj" @extra
    if ($LASTEXITCODE -ne 0) { throw "Build of $p failed." }
}
if (-not $SkipTests) {
    dotnet test "tests\CWAccess.Tests\CWAccess.Tests.csproj" -c Release
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
}

$dist = Join-Path $root "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
$out = Join-Path $dist "CWAccess"
New-Item -ItemType Directory -Force (Join-Path $out "Languages") | Out-Null
$files = @(
    "src\CWAccess\bin\Release\net35\CWAccess.dll",
    "src\CWAccess.Boot\bin\Release\net35\CWAccess.Boot.dll",
    "src\CWAccess.SpeechHost\bin\Release\net472\CWAccess.SpeechHost.exe",
    "lib\harmony\0Harmony.dll",
    "lib\harmony\MonoMod.RuntimeDetour.dll",
    "lib\harmony\MonoMod.Utils.dll",
    "lib\harmony\Mono.Cecil.dll",
    "lib\nvda\nvdaControllerClient32.dll"
)
foreach ($f in $files) { Copy-Item (Join-Path $root $f) $out }
Copy-Item (Join-Path $root "src\CWAccess\Languages\*.txt") (Join-Path $out "Languages")
Copy-Item (Join-Path $root "README.md") (Join-Path $out "CWAccess-README.md")
Copy-Item (Join-Path $root "LICENSE") (Join-Path $out "LICENSE.txt")
Copy-Item (Join-Path $root "THIRD-PARTY-NOTICES.md") (Join-Path $out "THIRD-PARTY-NOTICES.md")
Copy-Item (Join-Path $root "lib\nvda\license.txt") (Join-Path $out "NVDA-controller-client-license.txt")

# Entries are written one by one with "/" separators (CreateFromDirectory on Windows PowerShell 5 uses "\",
# which other unzip tools turn into odd file names).
function Write-Zip([string]$zipPath) {
    $archive = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($f in Get-ChildItem $out -File -Recurse) {
            $rel = $f.FullName.Substring($out.Length + 1).Replace("\", "/")
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $f.FullName, "CWAccess/" + $rel, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose() }
}

# 1. The payload (everything but the setup), built into the setup program.
$payload = Join-Path $dist "payload.zip"
Write-Zip $payload
dotnet build "src\CWAccess.Installer\CWAccess.Installer.csproj" @extra "-p:PayloadZip=$payload"
if ($LASTEXITCODE -ne 0) { throw "Build of the setup failed." }
$setup = Join-Path $root "src\CWAccess.Installer\bin\Release\net472\CWAccess-Setup.exe"

# 2. The release zip: the payload plus the setup (run it from the CWAccess folder after extracting).
Copy-Item $setup $out
$zip = Join-Path $dist "ChainsawWarriorAccess-v$version.zip"
Write-Zip $zip
Copy-Item $setup (Join-Path $dist "CWAccess-Setup-v$version.exe")
Remove-Item $payload

& (Join-Path $PSScriptRoot "check-binaries.ps1") -Folder $dist
if ($LASTEXITCODE -ne 0) { throw "Do not publish: a program file in dist looks modified." }
foreach ($f in Get-ChildItem $dist -File) {
    Write-Host ("{0}  SHA256 {1}" -f $f.Name, (Get-FileHash $f.FullName -Algorithm SHA256).Hash)
}
