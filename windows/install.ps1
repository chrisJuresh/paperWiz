#requires -version 5
<#
    Installs paperWiz as a real Windows app for the current user (no admin needed):
      * builds a self-contained PaperWiz.exe (bundles .NET — runs on any Win10/11 box)
      * copies it to %LOCALAPPDATA%\Programs\paperWiz
      * adds Start Menu + Desktop shortcuts (with the app icon)
      * registers it under Installed Apps so it shows up and can be uninstalled

    After running, find "paperWiz" in the Start menu (or on the Desktop),
    right-click it and choose "Pin to taskbar".

    Usage:  .\install.ps1            (build + install)
            .\install.ps1 -Launch    (also open it when done)
#>
param(
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$root       = $PSScriptRoot
$proj       = Join-Path $root 'src\PaperWiz\PaperWiz.csproj'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\paperWiz'
$exePath    = Join-Path $installDir 'PaperWiz.exe'
$stageDir   = Join-Path $root 'publish'

# --- locate the .NET SDK -------------------------------------------------------
$dotnet = & (Join-Path $root '_resolve-dotnet.ps1')

# --- build a self-contained single-file exe ------------------------------------
Write-Host "Building paperWiz..." -ForegroundColor Cyan
& $dotnet publish $proj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $stageDir --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }

$staged = Join-Path $stageDir 'PaperWiz.exe'
if (-not (Test-Path $staged)) { throw "Expected build output not found: $staged" }

# --- install -------------------------------------------------------------------
Write-Host "Installing to $installDir" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $installDir | Out-Null

# Close any running instance so its .exe can be replaced (like any app updater).
$running = Get-Process -Name 'PaperWiz' -ErrorAction SilentlyContinue
$wasRunning = [bool]$running
foreach ($p in $running) {
    Write-Host "Closing the running paperWiz to update it (pid $($p.Id))..." -ForegroundColor Yellow
    try { $p.CloseMainWindow() | Out-Null } catch { }
    if (-not $p.WaitForExit(3000)) { try { $p.Kill() } catch { }; $p.WaitForExit(3000) | Out-Null }
}

Copy-Item $staged $exePath -Force
Copy-Item (Join-Path $root 'uninstall.ps1') (Join-Path $installDir 'uninstall.ps1') -Force
Copy-Item (Join-Path $root 'uninstall.bat') (Join-Path $installDir 'uninstall.bat') -Force

# --- shortcuts -----------------------------------------------------------------
$shell    = New-Object -ComObject WScript.Shell
$startDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$desktop  = [Environment]::GetFolderPath('Desktop')

function New-Shortcut($linkPath) {
    $sc = $shell.CreateShortcut($linkPath)
    $sc.TargetPath       = $exePath
    $sc.WorkingDirectory = $installDir
    $sc.IconLocation     = "$exePath,0"
    $sc.Description       = "Per-monitor wallpapers that share your wallpaper's colour"
    $sc.Save()
}
New-Shortcut (Join-Path $startDir 'paperWiz.lnk')
New-Shortcut (Join-Path $desktop  'paperWiz.lnk')

# --- restore the wallpaper after Windows sign-in (silent, exits immediately) ----------
$runReg = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
New-Item -Path $runReg -Force | Out-Null
New-ItemProperty -Path $runReg -Name 'paperWiz' -PropertyType String `
    -Value "`"$exePath`" --restore-wallpaper" -Force | Out-Null

# --- register under "Installed Apps" (HKCU, no admin) --------------------------
$reg = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\paperWiz'
New-Item -Path $reg -Force | Out-Null
$sizeKb = [int]((Get-Item $exePath).Length / 1024)
Set-ItemProperty $reg 'DisplayName'     'paperWiz'
Set-ItemProperty $reg 'DisplayVersion'  '2.0.0'
Set-ItemProperty $reg 'Publisher'       'chrisJuresh'
Set-ItemProperty $reg 'DisplayIcon'     $exePath
Set-ItemProperty $reg 'InstallLocation' $installDir
Set-ItemProperty $reg 'UninstallString' "powershell -ExecutionPolicy Bypass -File `"$installDir\uninstall.ps1`""
Set-ItemProperty $reg 'EstimatedSize'   $sizeKb -Type DWord
Set-ItemProperty $reg 'NoModify'        1 -Type DWord
Set-ItemProperty $reg 'NoRepair'        1 -Type DWord

Write-Host ""
Write-Host "paperWiz is installed." -ForegroundColor Green
Write-Host "  * Start menu: search 'paperWiz'"
Write-Host "  * Desktop:    paperWiz shortcut"
Write-Host "  -> To keep it on the taskbar: right-click it and choose 'Pin to taskbar'."
Write-Host ""

# Reopen it if we had to close a running copy, or if -Launch was requested.
if ($Launch -or $wasRunning) { Start-Process $exePath }
