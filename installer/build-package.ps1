param([string]$DotNet = 'dotnet', [string]$OutputDirectory = '', [string]$NuGetSource = '')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'artifacts\packages' }
$staging = Join-Path $repoRoot ('artifacts\package-' + [guid]::NewGuid().ToString('N'))
$app = Join-Path $staging 'app'
$updater = Join-Path $staging 'updater'
New-Item -ItemType Directory -Force -Path $app,$updater,$OutputDirectory | Out-Null
$sourceArgs = @()
if ($NuGetSource) { $sourceArgs = @('--source', $NuGetSource) }
& $DotNet publish (Join-Path $repoRoot 'Rivada.Restaurant.WinForms.csproj') -c Release -r win-x64 --self-contained true -o $app @sourceArgs
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }
& $DotNet publish (Join-Path $PSScriptRoot 'Updater\Updater.csproj') -c Release -r win-x64 --self-contained true -o $updater @sourceArgs
if ($LASTEXITCODE -ne 0) { throw 'Updater publish failed.' }
$manifest = [ordered]@{}
Get-ChildItem -LiteralPath $app -File -Recurse | ForEach-Object {
    if ($_.Name -match '^license' -or $_.Name -match '\.db($|-)') { throw 'Unexpected data file in publish output.' }
    if ($_.Extension -ne '.pdb') {
        $relative = $_.FullName.Substring($app.Length + 1)
        $manifest[$relative] = (Get-FileHash -LiteralPath $_.FullName).Hash
    }
}
$manifest | ConvertTo-Json | Set-Content (Join-Path $app 'package-manifest.json') -Encoding UTF8
Get-ChildItem -LiteralPath $updater -Filter 'Rivada.Guncelle.*' | Where-Object {$_.Extension -ne '.pdb'} | Copy-Item -Destination $app
Copy-Item (Join-Path $PSScriptRoot 'KULLANIM.txt') $app
$zipPath = Join-Path $OutputDirectory 'Rivada-POS-1.6.0-Stok-Sifreli-Guncelleme.zip'
if (Test-Path -LiteralPath $zipPath) { throw 'Output ZIP already exists. Choose another output directory.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
    Get-ChildItem -LiteralPath $app -File -Recurse | Where-Object {$_.Extension -ne '.pdb'} | ForEach-Object {
        $relative = $_.FullName.Substring($app.Length + 1).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$_.FullName,$relative,'Optimal') | Out-Null
    }
} finally { $archive.Dispose() }
Write-Output $zipPath
