<#
.SYNOPSIS
  Gate for the native-test coordination change: the reservation and KwrRuntime admission self-tests, then the existing
  boundary suite (which includes KwrRuntime's runtime fixture cases), each in its own process.
.DESCRIPTION
  scripts/Test-NativeReservation.ps1 runs only against stand-ins. scripts/Test-LoaderBaseline.ps1 tests the committed
  HEAD in a temporary clone (build, package, receipt, validator and the KwrRuntime Reinstall/Close/Launch fixture
  cases). Neither touches the live game, Steam, the owner's profile or the production reservation. Exits 1 if either fails.
#>
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$failed = @()
foreach ($suite in @('Test-NativeReservation.ps1', 'Test-LoaderBaseline.ps1')) {
    Write-Host "== $suite"
    $started = Get-Date
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $suite)
    $code = $LASTEXITCODE
    Write-Host ("== $suite exit=$code ({0:N0}s)" -f ((Get-Date) - $started).TotalSeconds)
    if ($code -ne 0) { $failed += $suite }
}
if ($failed.Count -gt 0) { Write-Host "GATE FAILED: $($failed -join ', ')"; exit 1 }
Write-Host 'GATE PASSED'
exit 0
