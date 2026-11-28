<#
.SYNOPSIS
  One KwrRuntime operation against a stand-in lab, run as its own process by scripts/Test-NativeReservation.ps1.
.DESCRIPTION
  Loads KwrRuntime.ps1's own functions (parsed from the script; its top level, which would address the real game, never
  runs) and scripts/KwrReservation.ps1, points every root at the stand-in lab, and calls the script's dispatcher
  (Invoke-KwrOperation) exactly as the script's last line does. Only the boundaries that would reach the real machine are
  replaced: package validation and installation (a fixed stand-in DLL), the Steam cloud check, game-process lookup and
  registry import/export (a file). Hooks pause the operation at named points until the test releases them; a barrier
  makes several contenders start together. Outcome: <Label>.done or <Label>.refused (with the message) in the signals
  folder.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $Operation,
    [Parameter(Mandatory = $true)][string] $Label,
    [Parameter(Mandatory = $true)][string] $LabRoot,
    [Parameter(Mandatory = $true)][string] $Signals,
    [Parameter(Mandatory = $true)][string] $MutexName,
    [Parameter(Mandatory = $true)][string] $RecordPath,
    [string] $RunId,
    [string] $PauseAt = '',
    [string] $Barrier = '',
    [switch] $KeepInstalled,
    [switch] $FakeGameAfterAdmission,
    [switch] $FailExistingSnapshot
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# KwrRuntime's script variables, at stand-in roots.
$StateRoot = Join-Path $LabRoot 'runtime-state'
$BackupRoot = Join-Path $LabRoot 'runtime-backups'
$EvidenceRoot = Join-Path $LabRoot 'runtime-evidence'
$StatePath = Join-Path $StateRoot 'transaction.json'
$GameDir = Join-Path $LabRoot 'game'
$ModsRoot = Join-Path $GameDir 'Mods'
$ModDir = Join-Path $ModsRoot 'KingmakerWakingRune'
$UmmParams = Join-Path $GameDir 'UnityModManager\Params.xml'
$UmmLog = Join-Path $GameDir 'UnityModManager\Log.txt'
$ProfileDir = Join-Path $LabRoot 'profile'
$RegKey = 'stand-in'
$AppId = '640820'
$SteamUserDataRoot = Join-Path $LabRoot 'steam\userdata'
$PackagePath = Join-Path $LabRoot 'candidate.zip'
$Recon = $false; $Diagnostic = $false; $LaunchArgs = @(); $TimeoutSeconds = 5; $Seconds = 1; $Name = $null; $FaultName = $null
$Keys = $null; $Text = $null; $X = 0; $Y = 0; $X2 = 0; $Y2 = 0; $Button = 'Left'; $Delta = -120

. (Join-Path $PSScriptRoot 'KwrReservation.ps1')
$ReservationConvention = [pscustomobject]@{ MutexName = $MutexName; RecordPath = $RecordPath }

$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'KwrRuntime.ps1'), [ref] $tokens, [ref] $errors)
$functions = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] }, $false) | ForEach-Object { $_.Extent.Text }) -join "`r`n"
$functions = $functions.Replace('function Invoke-Collect {', 'function Invoke-CollectOriginal {').Replace('function Get-SteamMirror {', 'function Get-SteamMirrorOriginal {')
. ([scriptblock]::Create($functions))

function Wait-Hook([string] $Hook) {
    if ($PauseAt -cne $Hook) { return }
    Set-Content -LiteralPath (Join-Path $Signals "$Label.paused-$Hook") -Value ''
    $go = Join-Path $Signals "$Label.go-$Hook"
    $until = (Get-Date).AddSeconds(120)
    while (-not (Test-Path -LiteralPath $go)) { if ((Get-Date) -gt $until) { throw "no signal to leave $Hook" }; Start-Sleep -Milliseconds 50 }
}

# The real machine's boundaries, replaced.
$candidateBytes = [Text.Encoding]::ASCII.GetBytes('stand-in candidate dll')
$candidateSha = ([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($candidateBytes))).Replace('-', '').ToLowerInvariant()
function Get-GameProcesses { if ($FakeGameAfterAdmission) { @([pscustomobject]@{ Id = 4242 }) } else { @() } }
function Get-OwnedProcess($s) { $null }
function Assert-SteamCloudOffline { }
function New-KwrValidatedPackageCopy { param([string] $Path, [switch] $Diagnostic) [pscustomobject]@{ Source = $Path; Sha256 = 'stand-in-zip'; DllSha256 = $candidateSha; SourceProvenance = 'stand-in'; InstallMode = 'stand-in'; PrivateDirectory = $null } }
function Remove-KwrValidatedPackageCopy($v) { }
function Install-KwrValidatedPackageCopy($v, [string] $Root) {
    Wait-Hook 'install'
    $mod = Join-Path $Root 'KingmakerWakingRune'
    New-Item -ItemType Directory -Path $mod | Out-Null
    [IO.File]::WriteAllBytes((Join-Path $mod 'KingmakerWakingRune.dll'), $candidateBytes)
}
function Expand-KwrValidatedPackageCopy($v) {
    $staged = Join-Path $LabRoot ('staged-' + [guid]::NewGuid().ToString('N') + '\KingmakerWakingRune')
    New-Item -ItemType Directory -Path $staged | Out-Null
    [IO.File]::WriteAllBytes((Join-Path $staged 'KingmakerWakingRune.dll'), $candidateBytes)
    Wait-Hook 'reinstall-before-placement'
    return $staged
}
function Export-Registry([string] $path) { Copy-Item -LiteralPath (Join-Path $LabRoot 'registry\key.reg') -Destination $path -Force }
function Import-KwrRegistry([string] $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $LabRoot 'registry\key.reg') -Force }
function Get-SteamMirror { Add-Content -LiteralPath (Join-Path $LabRoot 'probes.log') -Value $Label; Get-SteamMirrorOriginal }
function Invoke-Collect { Invoke-CollectOriginal; Wait-Hook 'after-collect' }
function robocopy.exe {
    if ($FailExistingSnapshot -and [string] $args[1] -like '*\mod-existing') { $global:LASTEXITCODE = 16; return }
    & (Join-Path $env:WINDIR 'System32\Robocopy.exe') @args
}

if ($Barrier) {
    Set-Content -LiteralPath (Join-Path $Signals "$Label.ready") -Value ''
    $until = (Get-Date).AddSeconds(60)
    while (-not (Test-Path -LiteralPath $Barrier)) { if ((Get-Date) -gt $until) { throw 'barrier never opened' }; Start-Sleep -Milliseconds 5 }
}
try {
    Invoke-KwrOperation $Operation | Out-Null
    Set-Content -LiteralPath (Join-Path $Signals "$Label.done") -Value ''
    exit 0
}
catch {
    Set-Content -LiteralPath (Join-Path $Signals "$Label.refused") -Value $_.Exception.Message
    exit 3
}
