param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Project = "Rivada.Restaurant.WinForms.csproj",
    [string]$PublicKeyPath = "",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $PSScriptRoot "publish\app"
$issPath = Join-Path $PSScriptRoot "RivadaRestaurantPOS.iss"
$outputDir = Join-Path $PSScriptRoot "output"

Write-Host "[1/4] Publish alınıyor..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Push-Location $repoRoot
try {
    dotnet publish $Project -c $Configuration -r $Runtime --self-contained true -o $publishDir
}
finally {
    Pop-Location
}

if (-not [string]::IsNullOrWhiteSpace($PublicKeyPath) -and (Test-Path $PublicKeyPath)) {
    Write-Host "[2/4] license-public-key.xml kopyalanıyor..." -ForegroundColor Cyan
    Copy-Item $PublicKeyPath (Join-Path $publishDir "license-public-key.xml") -Force
}
else {
    Write-Host "[2/4] Public key kopyalanmadı (opsiyonel)." -ForegroundColor Yellow
}

Write-Host "[3/4] Inno Setup compiler aranıyor..." -ForegroundColor Cyan
$iscc = Get-Command iscc -ErrorAction SilentlyContinue
$isccPath = $null
$compil32Path = $null
if ($null -ne $iscc) {
    $isccPath = $iscc.Path
}
else {
    $candidates = @(
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            $isccPath = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($isccPath)) {
    $compil32Candidates = @(
        "$env:ProgramFiles(x86)\Inno Setup 6\Compil32.exe",
        "$env:ProgramFiles\Inno Setup 6\Compil32.exe",
        "$env:LocalAppData\Programs\Inno Setup 6\Compil32.exe"
    )

    foreach ($candidate in $compil32Candidates) {
        if (Test-Path $candidate) {
            $compil32Path = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($isccPath) -and [string]::IsNullOrWhiteSpace($compil32Path)) {
    throw "Inno Setup Compiler (ISCC.exe/Compil32.exe) bulunamadı. Inno Setup kurun veya yolunu PATH'e ekleyin."
}

Write-Host "[4/4] Setup oluşturuluyor..." -ForegroundColor Cyan
if (-not [string]::IsNullOrWhiteSpace($isccPath)) {
    & $isccPath "/DMyAppVersion=$Version" $issPath
}
else {
    # Compil32 GUI derleyicisini komut satırından derleme modunda çalıştır.
    & $compil32Path "/cc" $issPath
}

Write-Host "Tamamlandı. Çıktı klasörü: $outputDir" -ForegroundColor Green
