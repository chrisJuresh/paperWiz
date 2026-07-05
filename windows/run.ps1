#requires -version 5
<#
    Builds and launches paperWiz (Windows) in Debug.
    Usage:  .\run.ps1
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'src\PaperWiz\PaperWiz.csproj'
$dotnet = & (Join-Path $root '_resolve-dotnet.ps1')

& $dotnet run --project $proj -c Debug
