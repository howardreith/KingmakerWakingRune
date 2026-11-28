<#
.SYNOPSIS
  Gate for the native-test coordination change: the reservation and KwrRuntime admission self-tests, then the existing
  boundary suite (which includes KwrRuntime's runtime fixture cases), each in its own process.
.DESCRIPTION
  scripts/Test-NativeReservation.ps1 uses only disposable resources: stand-in labs under a temporary folder and a mutex
  name and lease record private to the run (never the production reservation, the game, Steam, the owner's profile or
  the registry). scripts/Test-LoaderBaseline.ps1 tests the committed HEAD in a temporary clone (build, package, receipt,
  validator and the KwrRuntime Reinstall/Close/Launch fixture cases); it also measures the installed loader read-only
  (the version and SHA-256 of <game>\Kingmaker_Data\Managed\UnityModManager\UnityModManager.dll, printed as
  "Owner-installed UnityModManager (measured only, not used as a fixture)"), and never launches or modifies the game.
  Exits 1 if either fails.
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
