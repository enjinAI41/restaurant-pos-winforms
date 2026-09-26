param([string]$Configuration='Release', [string]$Runtime='win-x64', [string]$Version='1.6.0', [string]$DotNet='dotnet')
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$publishDir=Join-Path $repoRoot ('artifacts\setup-'+[guid]::NewGuid().ToString('N'))
$outputDir=Join-Path $repoRoot 'artifacts\packages'
New-Item -ItemType Directory -Force -Path $publishDir,$outputDir | Out-Null
& $DotNet publish (Join-Path $repoRoot 'Rivada.Restaurant.WinForms.csproj') -c $Configuration -r $Runtime --self-contained true -o $publishDir -p:Version=$Version
if($LASTEXITCODE -ne 0) {throw 'Publish failed.'}
$iscc=Get-Command ISCC.exe -ErrorAction SilentlyContinue
$compiler=if($iscc){$iscc.Source}else{$null}
foreach($candidate in @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe","$env:ProgramFiles\Inno Setup 6\ISCC.exe","$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")) {
    if(-not $compiler -and (Test-Path -LiteralPath $candidate)) {$compiler=$candidate}
}
if(-not $compiler) {throw 'Inno Setup bulunamadı. ZIP için installer/build-package.ps1 kullanın.'}
& $compiler "/DMyAppVersion=$Version" "/DPublishDir=$publishDir" "/DSetupOutputDir=$outputDir" (Join-Path $PSScriptRoot 'RivadaRestaurantPOS.iss')
if($LASTEXITCODE -ne 0) {throw 'Setup compilation failed.'}
Write-Output $outputDir
