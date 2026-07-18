#requires -version 5
<#
    Removes everything install.ps1 created (current user, no admin):
    the program files, the Start Menu / Desktop shortcuts, the generated
    wallpaper cache, and the Installed Apps entry.
#>
$ErrorActionPreference = 'SilentlyContinue'

$installDir = Join-Path $env:LOCALAPPDATA 'Programs\paperWiz'
$cacheDir   = Join-Path $env:LOCALAPPDATA 'PaperWiz'
$startLnk   = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\paperWiz.lnk'
$desktopLnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'paperWiz.lnk'
$reg        = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\paperWiz'
$runReg     = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

Remove-Item $startLnk   -Force
Remove-Item $desktopLnk -Force
Remove-Item $cacheDir   -Recurse -Force
Remove-Item $reg        -Recurse -Force
Remove-ItemProperty $runReg -Name 'paperWiz'

# Remove the program files last (this script lives inside $installDir, so schedule
# the directory deletion after the process exits).
if (Test-Path $installDir) {
    Start-Process cmd.exe -WindowStyle Hidden -ArgumentList `
        "/c timeout /t 1 >nul & rmdir /s /q `"$installDir`""
}

Write-Host "paperWiz has been uninstalled." -ForegroundColor Green
