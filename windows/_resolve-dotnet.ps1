# Returns the path to a `dotnet` executable that actually has an SDK installed.
# Prefers one on PATH, then a user-local install under ~\.dotnet (where the
# official dotnet-install.ps1 script puts it). Throws if none has an SDK.
$ErrorActionPreference = 'Stop'

$candidates = @()
$onPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if ($onPath) { $candidates += $onPath }
$local = Join-Path $HOME '.dotnet\dotnet.exe'
if (Test-Path $local) { $candidates += $local }

foreach ($c in $candidates) {
    $sdks = $null
    try { $sdks = & $c --list-sdks 2>$null } catch { }
    if ($sdks) { return $c }
}

throw "No .NET 8 SDK found. Install it from https://dot.net (e.g. 'winget install Microsoft.DotNet.SDK.8')."
