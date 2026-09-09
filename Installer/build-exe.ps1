param(
    [string]$ProjectDir = (Split-Path -Parent $PSScriptRoot),
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$InstallerDir = Split-Path -Parent $PSCommandPath
$publishDir = Join-Path $InstallerDir "publish"

# 0. Generate .ico from .png if not exists
$icoPath = Join-Path (Join-Path $ProjectDir "Resources") "InventarioApp.ico"
if (-not (Test-Path $icoPath)) {
    Write-Host "Generating .ico from .png..." -ForegroundColor Cyan
    $pngPath = $icoPath -replace '\.ico$', '.png'
    & "$InstallerDir\convert-to-ico.ps1" -PngPath $pngPath -IcoPath $icoPath
}

# 1. Clean and publish self-contained
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
Write-Host "Publishing app (self-contained)..." -ForegroundColor Cyan
dotnet publish "$ProjectDir\InventarioAppDesktop.csproj" `
    -c $Configuration -r $Runtime --self-contained true `
    -o $publishDir `
    -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# 2. Compile Inno Setup installer (detecta Inno Setup 6 o 7)
$iscc = Get-ChildItem "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 7\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $iscc) {
    throw "ISCC.exe not found (Inno Setup 6 o 7 no instalado)."
}

Write-Host "Compiling installer..." -ForegroundColor Cyan
& $iscc "$InstallerDir\setup.iss" /Q
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }

# 3. Cleanup publish
Remove-Item -Recurse -Force $publishDir

# 4. Show result
$exePath = Get-ChildItem "$InstallerDir\InventarioAppDesktopSetup-*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($exePath) {
    Write-Host "`nInstaller created: $($exePath.FullName)" -ForegroundColor Green
    Write-Host "Size: $([math]::Round($exePath.Length / 1MB, 2)) MB" -ForegroundColor Green
} else {
    Write-Host "Installer created, but .exe not found." -ForegroundColor Yellow
}
