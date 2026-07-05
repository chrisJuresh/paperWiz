#requires -version 5
<#
    Publishes paperWiz as a single self-contained .exe that runs on any Windows 10/11
    machine without installing .NET. The result is opened in Explorer when done.
    Usage:  .\publish.ps1
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'src\PaperWiz\PaperWiz.csproj'
$out  = Join-Path $root 'publish'
$dotnet = & (Join-Path $root '_resolve-dotnet.ps1')

& $dotnet publish $proj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $out

Write-Host ""
Write-Host "Published to: $out\PaperWiz.exe" -ForegroundColor Green
if (Test-Path (Join-Path $out 'PaperWiz.exe')) { Start-Process explorer.exe $out }
