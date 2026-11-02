# Downloads the third-party binaries the mod ships with into lib\ (not kept in git):
#   lib\harmony : HarmonyX and MonoMod from the BepInEx 5 x86 release (MIT), .NET 3.5 builds that run on Unity 4.
#   lib\nvda    : NV Access's NVDA controller client, 32-bit (LGPL 2.1), renamed nvdaControllerClient32.dll.
param(
    [string]$BepInExVersion = "5.4.23.2",
    [string]$NvdaVersion = "2024.4.2"
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = Split-Path -Parent $PSScriptRoot

function Get-Zip([string]$url, [string]$name) {
    $zip = Join-Path $env:TEMP $name
    Write-Host "Downloading $url"
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    return $zip
}

function Extract([string]$zip, [hashtable]$map) {
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        foreach ($e in $archive.Entries) {
            if ($map.ContainsKey($e.FullName)) {
                $dest = $map[$e.FullName]
                New-Item -ItemType Directory -Force (Split-Path $dest) | Out-Null
                [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $dest, $true)
            }
        }
    } finally { $archive.Dispose() }
}

$harmony = Join-Path $root "lib\harmony"
if (-not (Test-Path (Join-Path $harmony "0Harmony.dll"))) {
    $zip = Get-Zip "https://github.com/BepInEx/BepInEx/releases/download/v$BepInExVersion/BepInEx_win_x86_$BepInExVersion.zip" "BepInEx_win_x86_$BepInExVersion.zip"
    $map = @{}
    foreach ($f in "0Harmony.dll", "MonoMod.RuntimeDetour.dll", "MonoMod.Utils.dll", "Mono.Cecil.dll") { $map["BepInEx/core/$f"] = Join-Path $harmony $f }
    Extract $zip $map
    Remove-Item $zip
    Write-Host "Harmony extracted to $harmony"
}

$nvda = Join-Path $root "lib\nvda"
if (-not (Test-Path (Join-Path $nvda "nvdaControllerClient32.dll"))) {
    $zip = Get-Zip "https://download.nvaccess.org/releases/$NvdaVersion/nvda_${NvdaVersion}_controllerClient.zip" "nvda_controllerClient.zip"
    Extract $zip @{ "x86/nvdaControllerClient.dll" = (Join-Path $nvda "nvdaControllerClient32.dll"); "license.txt" = (Join-Path $nvda "license.txt") }
    Remove-Item $zip
    $sig = Get-AuthenticodeSignature (Join-Path $nvda "nvdaControllerClient32.dll")
    if ($sig.Status -ne "Valid" -or $sig.SignerCertificate.Subject -notlike "*NV Access*") { throw "The NVDA controller client is not signed by NV Access." }
    Write-Host "NVDA controller client extracted to $nvda"
}
Write-Host "Dependencies ready."
