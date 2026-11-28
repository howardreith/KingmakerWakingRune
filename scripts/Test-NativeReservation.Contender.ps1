<#
.SYNOPSIS
  One KwrRuntime operation against a stand-in lab, run as its own process by scripts/Test-NativeReservation.ps1.
.DESCRIPTION
  Loads KwrRuntime.ps1's own functions (parsed from the script; its top level, which would address the real game, never
  runs) and scripts/KwrReservation.ps1, points every root at the stand-in lab, and calls the script's dispatcher
  (Invoke-KwrOperation) exactly as the script's last line does. Only the boundaries that would reach the real machine are
  replaced: package validation and installation (a fixed stand-in DLL), the Steam cloud check, game-process lookup and
  registry import/export (a file), and the Steam client (a stand-in launcher that runs -LauncherSeconds). Hooks pause the operation at named points until the test releases them; a barrier
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
    [switch] $FailExistingSnapshot,
    [int] $LauncherSeconds = 1,
    [int] $RecorderSeconds = 1,
    [int] $DelegateAfterSeconds = 0,
    [int] $CloseWait = 2,
    [string] $PackageName = 'candidate.zip',
    [string] $RuntimeScript = '',
    [switch] $SkipRegistryExport,
    [switch] $RunIdSuffixLF
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
$PackagePath = Join-Path $LabRoot $PackageName
if ($RunIdSuffixLF) { $RunId = $RunId + "`n" }
$CloseWaitSeconds = $CloseWait
$FfmpegExe = Join-Path $LabRoot 'stand-in-recorder.cmd'
Set-Content -LiteralPath $FfmpegExe -Value "@ping -n $($RecorderSeconds + 1) 127.0.0.1 > nul" -Encoding ASCII
$SteamExe = Join-Path $LabRoot 'stand-in-steam.cmd'
# A launcher stand-in; with -DelegateAfterSeconds it exits at once after starting a detached child that, after the delay,
# starts the game stand-in (a ping with the marker -w 4173, found by Get-GameProcesses below).
$launcherLine = if ($DelegateAfterSeconds -gt 0) { "@start `"`" /min cmd /c `"ping -n $($DelegateAfterSeconds + 1) 127.0.0.1 >nul & ping -n 40 -w 4173 127.0.0.1 >nul`"" } else { "@ping -n $($LauncherSeconds + 1) 127.0.0.1 > nul" }
Set-Content -LiteralPath $SteamExe -Value $launcherLine -Encoding ASCII
$Recon = $false; $Diagnostic = $false; $LaunchArgs = @(); $TimeoutSeconds = 5; $Seconds = 1; $Name = $null; $FaultName = $null
$Keys = $null; $Text = $null; $X = 0; $Y = 0; $X2 = 0; $Y2 = 0; $Button = 'Left'; $Delta = -120

. (Join-Path $PSScriptRoot 'KwrReservation.ps1')
# Hooks inside the close: after the journal is retired (before the lease is released) and after the lease is released.
${function:Exit-KwrReservationOriginal} = ${function:Exit-KwrReservation}
function Exit-KwrReservation($Convention, $Admitted) { Wait-Hook 'before-lease-release'; Exit-KwrReservationOriginal $Convention $Admitted; Wait-Hook 'after-lease-release' }
$ReservationConvention = [pscustomobject]@{ MutexName = $MutexName; RecordPath = $RecordPath }

$tokens = $null; $errors = $null
$runtime = if ($RuntimeScript) { $RuntimeScript } else { Join-Path $PSScriptRoot 'KwrRuntime.ps1' }
$ast = [Management.Automation.Language.Parser]::ParseFile($runtime, [ref] $tokens, [ref] $errors)
$functions = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] }, $false) | ForEach-Object { $_.Extent.Text }) -join "`r`n"
$functions = $functions.Replace('function Publish-KwrInitialJournal(', 'function Publish-KwrInitialJournalOriginal(').Replace('function Update-KwrOutstanding(', 'function Update-KwrOutstandingOriginal(').Replace('function Invoke-Collect {', 'function Invoke-CollectOriginal {').Replace('function Get-SteamMirror {', 'function Get-SteamMirrorOriginal {').Replace('function Assert-KwrSharedMutation(', 'function Assert-KwrSharedMutationOriginal(')
. ([scriptblock]::Create($functions))

function Wait-Hook([string] $Hook) {
    if (@($PauseAt -split ',') -cnotcontains $Hook) { return }
    $safe = $Hook -replace '[^A-Za-z0-9.-]', '-'   # signal file names
    Set-Content -LiteralPath (Join-Path $Signals "$Label.paused-$safe") -Value ''
    $go = Join-Path $Signals "$Label.go-$safe"
    $until = (Get-Date).AddSeconds(120)
    while (-not (Test-Path -LiteralPath $go)) { if ((Get-Date) -gt $until) { throw "no signal to leave $Hook" }; Start-Sleep -Milliseconds 50 }
}

# The real machine's boundaries, replaced.
$candidateBytes = [Text.Encoding]::ASCII.GetBytes('stand-in candidate dll')
$candidateSha = ([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($candidateBytes))).Replace('-', '').ToLowerInvariant()
function Get-GameProcesses {
    Add-Content -LiteralPath (Join-Path $LabRoot 'game-probes.log') -Value $Label
    if ($FakeGameAfterAdmission -or (Test-Path -LiteralPath (Join-Path $LabRoot 'fake-game.flag'))) { return @([pscustomobject]@{ Id = 4242 }) }
    $found = @()
    $pidFile = Join-Path $LabRoot 'fake-game.pid'
    if (Test-Path -LiteralPath $pidFile) { $found += @(Get-Process -Id ([int] (Get-Content -LiteralPath $pidFile -Raw)) -ErrorAction SilentlyContinue) }
    if ($DelegateAfterSeconds -gt 0 -or (Test-Path -LiteralPath (Join-Path $LabRoot 'delegated.flag'))) {
        $found += @(Get-CimInstance Win32_Process -Filter "Name='PING.EXE'" | Where-Object { $_.CommandLine -like '*-w 4173*' } | ForEach-Object { Get-Process -Id $_.ProcessId -ErrorAction SilentlyContinue })
    }
    return @($found | Where-Object { $null -ne $_ })
}
function Assert-SteamCloudOffline { }
function New-KwrValidatedPackageCopy { param([string] $Path, [switch] $Diagnostic) [pscustomobject]@{ Source = $Path; Sha256 = 'stand-in-zip'; DllSha256 = $candidateSha; SourceProvenance = 'stand-in'; InstallMode = 'stand-in'; PrivateDirectory = $null } }
function Remove-KwrValidatedPackageCopy($v) { }
function Install-KwrValidatedPackageCopy($v, [string] $Root) {
    Wait-Hook 'install'
    $mod = Join-Path $Root 'KingmakerWakingRune'
    New-Item -ItemType Directory -Path $mod | Out-Null
    [IO.File]::WriteAllBytes((Join-Path $mod 'KingmakerWakingRune.dll'), $candidateBytes)
    Set-Content -LiteralPath (Join-Path $mod 'Info.json') -Value '{"Id":"KingmakerWakingRune"}'
}
function Expand-KwrValidatedPackageCopy($v) {
    $staged = Join-Path $LabRoot ('staged-' + [guid]::NewGuid().ToString('N') + '\KingmakerWakingRune')
    New-Item -ItemType Directory -Path $staged | Out-Null
    [IO.File]::WriteAllBytes((Join-Path $staged 'KingmakerWakingRune.dll'), $candidateBytes)
    Set-Content -LiteralPath (Join-Path $staged 'Info.json') -Value '{"Id":"KingmakerWakingRune"}'
    Wait-Hook 'reinstall-before-placement'
    return $staged
}
function Export-Registry([string] $path) { Copy-Item -LiteralPath (Join-Path $LabRoot 'registry\key.reg') -Destination $path -Force }
function Import-KwrRegistry([string] $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $LabRoot 'registry\key.reg') -Force }
# A pause point at every ownership guard: -PauseAt 'assert:<stage>'.
function Assert-KwrSharedMutation([string] $Stage) { Wait-Hook "assert:$Stage"; Assert-KwrSharedMutationOriginal $Stage }
# A pause point after Begin (or previous-format recovery) holds the lease but before its operation lock and journal.
${function:Enter-KwrOperationLockOriginal} = ${function:Enter-KwrOperationLock}
function Enter-KwrOperationLock([string] $StateRoot) { Wait-Hook 'after-lease'; Enter-KwrOperationLockOriginal $StateRoot }
# The game stand-in: a process whose PID the test writes to fake-game.pid; its window and foreground are assumed.
function Get-GameWindow { $p = Get-Process -Id $PID; [pscustomobject]@{ Process = $p; Rect = [pscustomobject]@{ Left = 0; Top = 0; Right = 640; Bottom = 360 }; State = (Get-State) } }
function Assert-GameForeground($w) { }
function Publish-KwrInitialJournal($s, $Lease, [string] $RunDirectory) { Wait-Hook 'initial-publication'; Publish-KwrInitialJournalOriginal $s $Lease $RunDirectory }
function Update-KwrOutstanding($s, [string] $Id, [scriptblock] $Change) { Wait-Hook 'before-pid-publication'; Update-KwrOutstandingOriginal $s $Id $Change }
# Steam's app state, as the test sets it in steam-app.json; absent file: no signal.
function Get-KwrSteamAppState { $f = Join-Path $LabRoot 'steam-app.json'; if (Test-Path -LiteralPath $f) { Get-Content -LiteralPath $f -Raw | ConvertFrom-Json } else { $null } }
# The owned game: the journal's game PID with its recorded start, whatever its name (the stand-in is a ping).
function Get-OwnedProcess($s) {
    if (-not $s -or -not (Get-KwrProp $s 'gamePid')) { return $null }
    $p = Get-Process -Id $s.gamePid -ErrorAction SilentlyContinue
    if ($p -and $p.StartTime.ToString('o') -eq $s.gameStart) { return $p }
    return $null
}
if ($SkipRegistryExport) { function Export-Registry([string] $path) { } }
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
    if ($RuntimeScript) {
        # A previous KwrRuntime version: its own snapshot trees and operation function, as its top level defined them.
        $SnapshotTrees = @(@{ Name = 'profile'; Path = $ProfileDir }, @{ Name = 'steam-remote'; Path = (Join-Path (@(Get-ChildItem -LiteralPath $SteamUserDataRoot -Directory)[0].FullName) "$AppId\remote") })
        $output = & "Invoke-$Operation"
    }
    else { $output = Invoke-KwrOperation $Operation }
    [IO.File]::WriteAllText((Join-Path $Signals "$Label.out"), (@($output) -join "`n"), [Text.UTF8Encoding]::new($false))
    Set-Content -LiteralPath (Join-Path $Signals "$Label.done") -Value ''
    exit 0
}
catch {
    Set-Content -LiteralPath (Join-Path $Signals "$Label.refused") -Value $_.Exception.Message
    exit 3
}
