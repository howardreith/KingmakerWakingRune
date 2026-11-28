<#
.SYNOPSIS
  Self-tests of Waking Rune's Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md, scripts/KwrReservation.ps1)
  and of KwrRuntime.ps1's admission, ownership, restoration-release and recovery paths.
.DESCRIPTION
  Runs only against stand-ins under a temporary folder, with a mutex name and record path private to this run: never the
  production reservation, the live game, Steam, the owner's profile, the registry or any other lab. Operations run as
  separate powershell.exe processes (scripts/Test-NativeReservation.Contender.ps1) that execute KwrRuntime.ps1's own
  functions, through its dispatcher, against stand-in labs that share nothing but the reservation. Emits PASS/FAIL lines
  and a RESULT line; exits 1 on any failure.
#>
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'KwrReservation.ps1')

$root = Join-Path ([IO.Path]::GetTempPath()) ('kwr-reservation-test-' + [guid]::NewGuid().ToString('N'))
$mutexName = 'Global\KingmakerNativeTest.WakingRuneSelfTest.' + [guid]::NewGuid().ToString('N')
$results = New-Object System.Collections.Generic.List[string]
$processes = New-Object System.Collections.Generic.List[System.Diagnostics.Process]
$contender = Join-Path $PSScriptRoot 'Test-NativeReservation.Contender.ps1'
$runtimeText = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'KwrRuntime.ps1'))

function Get-Convention { [pscustomobject]@{ MutexName = $mutexName; RecordPath = (Join-Path $root 'programdata\KingmakerNativeTest\reservation.v1.json') } }
function Get-Lab([string] $Lab) { Join-Path $root $Lab }
function Get-LabState([string] $Lab) { Join-Path (Get-Lab $Lab) 'runtime-state\transaction.json' }
function Get-Signals { Join-Path $root 'signals' }
function Get-Sha256([string] $Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
function Get-TreeDigest([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return 'absent' }
    $full = (Resolve-Path -LiteralPath $Path).Path
    return (@(Get-ChildItem -LiteralPath $full -Recurse -File -Force | Sort-Object FullName | ForEach-Object { $_.FullName.Substring($full.Length) + '=' + (Get-Sha256 $_.FullName) }) -join ';')
}

# A stand-in lab: game Mods (optionally with the owner's accepted install), loader settings and log, profile, one Steam
# account's mirror and a registry file.
function New-StandInLab([string] $Lab, [switch] $ExistingMod) {
    $r = Get-Lab $Lab
    foreach ($d in @('game\Mods', 'game\UnityModManager', 'profile\Saved Games', 'steam\userdata\111\640820\remote', 'registry', 'runtime-backups', 'runtime-evidence')) { New-Item -ItemType Directory -Force -Path (Join-Path $r $d) | Out-Null }
    Set-Content -LiteralPath (Join-Path $r 'game\UnityModManager\Params.xml') -Value '<Param><ShowOnStart>1</ShowOnStart></Param>'
    Set-Content -LiteralPath (Join-Path $r 'game\UnityModManager\Log.txt') -Value 'loader log'
    Set-Content -LiteralPath (Join-Path $r 'game\Mods\OtherMod.txt') -Value 'another mod'
    Set-Content -LiteralPath (Join-Path $r 'profile\Saved Games\Owner_1.zks') -Value 'owner save'
    Set-Content -LiteralPath (Join-Path $r 'steam\userdata\111\640820\remote\Owner_1.zks') -Value 'owner save mirror'
    Set-Content -LiteralPath (Join-Path $r 'registry\key.reg') -Value 'owner registry'
    if ($ExistingMod) { New-Item -ItemType Directory -Force -Path (Join-Path $r 'game\Mods\KingmakerWakingRune') | Out-Null; Set-Content -LiteralPath (Join-Path $r 'game\Mods\KingmakerWakingRune\KingmakerWakingRune.dll') -Value 'owner accepted build' }
}
function Get-SharedDigest([string] $Lab) { $r = Get-Lab $Lab; return (@('game', 'profile', 'steam', 'registry') | ForEach-Object { Get-TreeDigest (Join-Path $r $_) }) -join '|' }

function Reset-Root {
    foreach ($p in $processes) { if (-not $p.HasExited) { $p.Kill(); [void] $p.WaitForExit(5000) } }
    $processes.Clear()
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Get-Signals) | Out-Null
    New-StandInLab 'lab-a'; New-StandInLab 'lab-b'
}

function Test-Case([string] $Name, [scriptblock] $Body) {
    Reset-Root
    try { & $Body; $results.Add("PASS $Name") }
    catch { $results.Add("FAIL ${Name}: $($_.Exception.Message)") }
}
function Assert-That([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }
function Assert-Throws([scriptblock] $Body, [string] $Fragment, [string] $Message) {
    try { & $Body } catch {
        if ($_.Exception.Message -notlike "*$Fragment*") { throw "$Message threw '$($_.Exception.Message)', expected '*$Fragment*'" }
        return
    }
    throw "$Message did not throw"
}

# Starts one KwrRuntime operation in its own process.
function Start-Op([string] $Operation, [string] $Label, [string] $Lab, [string] $RunId = '', [string] $PauseAt = '', [string] $Barrier = '', [switch] $KeepInstalled, [switch] $FakeGameAfterAdmission, [switch] $FailExistingSnapshot) {
    $c = Get-Convention
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $contender + '"'), '-Operation', $Operation, '-Label', $Label,
        '-LabRoot', ('"' + (Get-Lab $Lab) + '"'), '-Signals', ('"' + (Get-Signals) + '"'), '-MutexName', $c.MutexName, '-RecordPath', ('"' + $c.RecordPath + '"'))
    if ($RunId) { $arguments += @('-RunId', $RunId) }
    if ($PauseAt) { $arguments += @('-PauseAt', $PauseAt) }
    if ($Barrier) { $arguments += @('-Barrier', ('"' + $Barrier + '"')) }
    if ($KeepInstalled) { $arguments += '-KeepInstalled' }
    if ($FakeGameAfterAdmission) { $arguments += '-FakeGameAfterAdmission' }
    if ($FailExistingSnapshot) { $arguments += '-FailExistingSnapshot' }
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $processes.Add($process)
    return $process
}

function Wait-Signal([string] $Label, [string[]] $What, [int] $Seconds = 90) {
    $until = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $until) {
        foreach ($candidate in $What) {
            $path = Join-Path (Get-Signals) "$Label.$candidate"
            if (Test-Path -LiteralPath $path) { return [pscustomobject]@{ What = $candidate; Text = [string] (Get-Content -LiteralPath $path -Raw) } }
        }
        Start-Sleep -Milliseconds 50
    }
    throw "$Label reached none of: $($What -join ', ')"
}

# Runs one operation to its end: waits on the process handle, then reads its outcome.
function Invoke-Op([string] $Operation, [string] $Label, [string] $Lab, [string] $RunId = '', [switch] $KeepInstalled, [switch] $FakeGameAfterAdmission, [switch] $FailExistingSnapshot) {
    $p = Start-Op $Operation $Label $Lab $RunId -KeepInstalled:$KeepInstalled -FakeGameAfterAdmission:$FakeGameAfterAdmission -FailExistingSnapshot:$FailExistingSnapshot
    if (-not $p.WaitForExit(120000)) { throw "$Label did not finish" }
    return Wait-Signal $Label @('done', 'refused') 5
}
function Assert-Done($Outcome, [string] $What) { if ($Outcome.What -ne 'done') { throw "$What was refused: $($Outcome.Text)" } }
function Assert-Refused($Outcome, [string] $Fragment, [string] $What) {
    if ($Outcome.What -ne 'refused') { throw "$What was not refused" }
    if ($Outcome.Text -notlike "*$Fragment*") { throw "$What was refused for another reason: $($Outcome.Text)" }
}
function Release-Hook([string] $Label, [string] $Hook) { Set-Content -LiteralPath (Join-Path (Get-Signals) "$Label.go-$Hook") -Value 'go' }
function Get-Record { if (Test-Path -LiteralPath (Get-Convention).RecordPath) { Get-Content -LiteralPath (Get-Convention).RecordPath -Raw | ConvertFrom-Json } else { $null } }
function Get-Journal([string] $Lab) { if (Test-Path -LiteralPath (Get-LabState $Lab)) { Get-Content -LiteralPath (Get-LabState $Lab) -Raw | ConvertFrom-Json } else { $null } }

# A peer lab that tries to begin while the winner holds the reservation: refused by the reservation, with no journal,
# no Steam probe and nothing of its shared stand-ins changed; the winner's record unchanged.
function Assert-PeerRefused([string] $Label, [string] $PeerLab, [string] $WinnerRunId) {
    $record = Get-Sha256 (Get-Convention).RecordPath
    $shared = Get-SharedDigest $PeerLab
    Assert-Refused (Invoke-Op 'Begin' $Label $PeerLab $Label) "reserved by KingmakerWakingRune run $WinnerRunId" "peer $Label"
    Assert-That (-not (Test-Path -LiteralPath (Get-LabState $PeerLab))) "peer $Label wrote a journal"
    Assert-That (-not (Test-Path -LiteralPath (Join-Path (Get-Lab $PeerLab) 'probes.log'))) "peer $Label probed Steam"
    Assert-That ((Get-SharedDigest $PeerLab) -eq $shared) "peer $Label changed its shared stand-ins"
    Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record) "the winner's record changed while $Label was refused"
}

function New-Record([string] $Lab, [int] $HolderProcessId, [string] $HolderStartUtc, [string] $RunId, [string] $StatePath = '', [string] $Token = '') {
    $c = Get-Convention
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $c.RecordPath) | Out-Null
    $record = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Lab; runId = $RunId; purpose = 'stand-in'; token = $(if ($Token) { $Token } else { [guid]::NewGuid().ToString('N') })
        holderProcessId = $HolderProcessId; holderStartUtc = $HolderStartUtc; host = $env:COMPUTERNAME; createdUtc = (Get-Date).ToUniversalTime().ToString('o') }
    if ($StatePath) { $record.statePath = [IO.Path]::GetFullPath($StatePath) }
    [IO.File]::WriteAllText($c.RecordPath, ($record | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    return $record
}
function Get-DeadProcessId {
    $p = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', '1', '127.0.0.1') -WindowStyle Hidden -PassThru
    [void] $p.WaitForExit(10000); return $p.Id
}
function Get-OwnStart { (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o') }

try {
    # --- identity and wiring ---------------------------------------------------------------------------------------
    Test-Case 'the production convention is the neutral protocol identity and names no other lab' {
        $c = Get-KwrReservationConvention
        Assert-That ($c.MutexName -ceq 'Global\KingmakerNativeTest.Reservation.v1') "mutex $($c.MutexName)"
        Assert-That ($c.RecordPath -eq (Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json')) "record $($c.RecordPath)"
        foreach ($file in @('KwrReservation.ps1', 'KwrRuntime.ps1')) {
            $text = Get-Content -LiteralPath (Join-Path $PSScriptRoot $file) -Raw
            foreach ($foreign in @('KingmakerFlight', 'MountedCombat', 'BuffPlanner', 'C:\Dev\')) { Assert-That ($text.IndexOf($foreign, [StringComparison]::OrdinalIgnoreCase) -lt 0) "$file names $foreign" }
        }
    }

    Test-Case 'R1-F1/F2 wiring: distinct convention and lease names; no Steam discovery before admission' {
        Assert-That (-not ($runtimeText -match '(?i)\$reservation\b')) 'a variable named $reservation remains (PowerShell names are case-insensitive)'
        $tokens = $null; $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'KwrRuntime.ps1'), [ref] $tokens, [ref] $errors)
        $topLevel = @($ast.EndBlock.Statements | Where-Object { $_ -isnot [Management.Automation.Language.FunctionDefinitionAst] } | ForEach-Object { $_.Extent.Text }) -join "`n"
        Assert-That ($topLevel -notmatch 'Get-ChildItem|Get-SteamMirror|Get-Process') 'the script top level probes the machine before admission'
        $begin = $runtimeText.Substring($runtimeText.IndexOf('function Invoke-Begin {'), $runtimeText.IndexOf('function Invoke-Reinstall {') - $runtimeText.IndexOf('function Invoke-Begin {'))
        $last = -1
        foreach ($marker in @('Enter-KwrTransactionReservation $ReservationConvention $StatePath $RunId', 'Enter-KwrOperationLock $StateRoot', 'if (Get-State)', 'Get-GameProcesses', 'New-KwrValidatedPackageCopy', 'Get-SteamMirror', 'reservationToken = $lease.token; steamMirror = $mirror', '$stateSaved = $true', 'Save-Tree', "`$s.phase = 'snapshotted'", 'Remove-Item -LiteralPath $ModDir')) {
            $at = $begin.IndexOf($marker); Assert-That ($at -gt $last) "Begin: '$marker' is missing or out of order"; $last = $at
        }
        Assert-That ($runtimeText.Contains("if (`$Operation -eq 'Status') { `$s = Get-State;")) 'Status is not independent of admission and Steam'
    }

    # --- admission ---------------------------------------------------------------------------------------------------
    Test-Case 'a lab alone (no peer, no reservation folder yet) begins, operates and ends' {
        Assert-That (-not (Test-Path -LiteralPath (Split-Path -Parent (Get-Convention).RecordPath))) 'stand-in folder pre-exists'
        $before = Get-SharedDigest 'lab-a'
        Assert-Done (Invoke-Op 'Begin' 'solo-begin' 'lab-a' 'solo') 'Begin'
        $record = Get-Record; $journal = Get-Journal 'lab-a'
        Assert-That ($record.lab -ceq 'KingmakerWakingRune' -and $record.runId -ceq 'solo' -and $record.token -ceq $journal.reservationToken -and $journal.phase -eq 'installed') 'record and journal disagree'
        Assert-Done (Invoke-Op 'Close' 'solo-close' 'lab-a') 'Close'
        Assert-Done (Invoke-Op 'End' 'solo-end' 'lab-a' 'solo') 'End'
        Assert-That ($null -eq (Get-Record) -and $null -eq (Get-Journal 'lab-a')) 'record or journal left behind'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $before) 'the shared stand-ins were not restored exactly'
    }

    Test-Case 'R1-F1 a Begin refused after acquisition (game process found) releases the lease; the next contender is admitted' {
        Assert-Refused (Invoke-Op 'Begin' 'f1-refused' 'lab-a' 'run-f1' -FakeGameAfterAdmission) 'Kingmaker is running' 'Begin with a game process'
        Assert-That ($null -eq (Get-Record)) 'the refused Begin leaked the reservation'
        Assert-That ($null -eq (Get-Journal 'lab-a')) 'the refused Begin wrote a journal'
        Assert-Done (Invoke-Op 'Begin' 'f1-next' 'lab-b' 'f1-next') 'the next contender'
        Assert-Done (Invoke-Op 'End' 'f1-next-end' 'lab-b' 'f1-next') 'End'
    }

    Test-Case 'R1-F9 both arrival orders: a peer is refused inside and between every winner operation, then admitted' {
        foreach ($order in @(@('win-A', 'lab-a', 'B', 'lab-b'), @('win-B2', 'lab-b', 'A2', 'lab-a'))) {
            $w = $order[0]; $wLab = $order[1]; $l = $order[2]; $lLab = $order[3]
            foreach ($lab in @('lab-a', 'lab-b')) { Remove-Item -LiteralPath (Join-Path (Get-Lab $lab) 'probes.log') -ErrorAction SilentlyContinue }
            $steps = @(@('Begin', 'install', $w), @('Reinstall', 'reinstall-before-placement', ''), @('End', 'after-collect', $w))
            $i = 0
            foreach ($step in $steps) {
                $i++
                $p = Start-Op $step[0] "$w-$($step[0])" $wLab $step[2] -PauseAt $step[1]
                $paused = Wait-Signal "$w-$($step[0])" @("paused-$($step[1])", 'refused')
                if ($paused.What -eq 'refused') { throw "winner $w $($step[0]) was refused: $($paused.Text)" }
                Assert-PeerRefused "$l-in-$i" $lLab $w
                Release-Hook "$w-$($step[0])" $step[1]
                if (-not $p.WaitForExit(60000)) { throw "winner $w $($step[0]) did not finish" }
                Assert-Done (Wait-Signal "$w-$($step[0])" @('done', 'refused') 5) "winner $w $($step[0])"
                if ($step[0] -ne 'End') { Assert-PeerRefused "$l-between-$i" $lLab $w }   # winner process exited; transaction open
            }
            Assert-That ($null -eq (Get-Record) -and $null -eq (Get-Journal $wLab)) "winner $w left its record or journal"
            Assert-Done (Invoke-Op 'Begin' "$l-after" $lLab "$l-after") "$l after release"
            Assert-Done (Invoke-Op 'End' "$l-after-end" $lLab "$l-after") "$l End"
        }
    }

    Test-Case 'R1-F9 synchronized arrivals from a shared barrier admit exactly one' {
        foreach ($round in 1..3) {
            $barrier = Join-Path (Get-Signals) "barrier-$round"
            $x = Start-Op 'Begin' "X$round" 'lab-a' "X-run-$round" -Barrier $barrier
            $y = Start-Op 'Begin' "Y$round" 'lab-b' "Y-run-$round" -Barrier $barrier
            [void] (Wait-Signal "X$round" @('ready')); [void] (Wait-Signal "Y$round" @('ready'))
            Set-Content -LiteralPath $barrier -Value 'go'
            if (-not $x.WaitForExit(60000) -or -not $y.WaitForExit(60000)) { throw "round ${round}: a contender did not finish" }
            $ox = Wait-Signal "X$round" @('done', 'refused') 5; $oy = Wait-Signal "Y$round" @('done', 'refused') 5
            Assert-That (@(@($ox.What, $oy.What) | Where-Object { $_ -eq 'done' }).Count -eq 1) "round ${round}: $($ox.What) / $($oy.What)"
            if ($ox.What -eq 'done') { $win = "X-run-$round"; $winLab = 'lab-a'; $lose = $oy; $loseLab = 'lab-b' } else { $win = "Y-run-$round"; $winLab = 'lab-b'; $lose = $ox; $loseLab = 'lab-a' }
            Assert-Refused $lose "reserved by KingmakerWakingRune run $win" "round ${round} loser"
            Assert-That ($null -eq (Get-Journal $loseLab) -and -not (Test-Path -LiteralPath (Join-Path (Get-Lab $loseLab) 'game\Mods\KingmakerWakingRune'))) "round ${round}: the loser mutated"
            Assert-That (-not (Test-Path -LiteralPath (Join-Path (Get-Lab $loseLab) 'probes.log'))) "round ${round}: the loser probed Steam"
            Assert-Done (Invoke-Op 'End' "$win-end" $winLab $win) "round ${round} End"
            Remove-Item -LiteralPath (Join-Path (Get-Lab $winLab) 'probes.log') -ErrorAction SilentlyContinue
        }
    }

    # --- Steam mirror -------------------------------------------------------------------------------------------------
    Test-Case 'R1-F2 a refused contender probes nothing; a replaced Steam mirror is left untouched; Status needs no Steam' {
        $p = Start-Op 'Begin' 'f2-winner' 'lab-a' 'run-f2' -PauseAt 'install'
        [void] (Wait-Signal 'f2-winner' @('paused-install'))
        Assert-PeerRefused 'f2-peer' 'lab-b' 'run-f2'
        Release-Hook 'f2-winner' 'install'; [void] $p.WaitForExit(60000)
        Assert-Done (Wait-Signal 'f2-winner' @('done', 'refused') 5) 'Begin'
        Assert-That (@(Get-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'probes.log')).Count -eq 1) 'Begin did not probe exactly once'
        $userdata = Join-Path (Get-Lab 'lab-a') 'steam\userdata'
        Move-Item -LiteralPath (Join-Path $userdata '111') -Destination (Join-Path $root 'aside-111')
        New-Item -ItemType Directory -Force -Path (Join-Path $userdata '222\640820\remote') | Out-Null
        Set-Content -LiteralPath (Join-Path $userdata '222\640820\remote\Other_1.zks') -Value 'another account'
        $replacement = Get-TreeDigest (Join-Path $userdata '222'); $record = Get-Sha256 (Get-Convention).RecordPath
        Assert-Refused (Invoke-Op 'End' 'f2-end-1' 'lab-a' 'run-f2') 'Steam save mirror is now' 'End with a replaced mirror'
        Assert-That ((Get-TreeDigest (Join-Path $userdata '222')) -eq $replacement) 'the replacement mirror was touched'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record -and $null -ne (Get-Journal 'lab-a')) 'the journal or reservation was not kept'
        Remove-Item -LiteralPath $userdata -Recurse -Force
        Assert-Done (Invoke-Op 'Status' 'f2-status' 'lab-a') 'Status without any Steam userdata'
        New-Item -ItemType Directory -Force -Path $userdata | Out-Null
        Move-Item -LiteralPath (Join-Path $root 'aside-111') -Destination (Join-Path $userdata '111')
        Assert-Done (Invoke-Op 'End' 'f2-end-2' 'lab-a' 'run-f2') 'End with the snapshotted mirror back'
        Assert-That ($null -eq (Get-Record)) 'reservation kept after a verified End'
    }

    # --- same-lab coordination and completion ------------------------------------------------------------------------
    Test-Case 'R1-F3 a paused Reinstall holds the operation: End and a peer are refused and the lease stays' {
        Assert-Done (Invoke-Op 'Begin' 'f3-begin' 'lab-a' 'run-f3') 'Begin'
        $token = (Get-Record).token
        $p = Start-Op 'Reinstall' 'f3-reinstall' 'lab-a' -PauseAt 'reinstall-before-placement'
        [void] (Wait-Signal 'f3-reinstall' @('paused-reinstall-before-placement'))
        Assert-Refused (Invoke-Op 'End' 'f3-end' 'lab-a' 'run-f3') 'Another Waking Rune operation is running' 'End during Reinstall'
        Assert-PeerRefused 'f3-peer' 'lab-b' 'run-f3'
        Assert-That ((Get-Record).token -ceq $token -and (Get-Journal 'lab-a').reservationToken -ceq $token) 'the lease or journal changed during the paused Reinstall'
        Release-Hook 'f3-reinstall' 'reinstall-before-placement'; [void] $p.WaitForExit(60000)
        Assert-Done (Wait-Signal 'f3-reinstall' @('done', 'refused') 5) 'Reinstall'
        Assert-Done (Invoke-Op 'End' 'f3-end-2' 'lab-a' 'run-f3') 'End after Reinstall'
    }

    Test-Case 'R1-F3 racing completions and a successor Begin: the successor''s journal and lease survive' {
        Assert-Done (Invoke-Op 'Begin' 'race-begin' 'lab-a' 'race') 'Begin'
        $barrier = Join-Path (Get-Signals) 'barrier-race'
        $e1 = Start-Op 'End' 'race-end-1' 'lab-a' 'race' -Barrier $barrier
        $e2 = Start-Op 'End' 'race-end-2' 'lab-a' 'race' -Barrier $barrier
        [void] (Wait-Signal 'race-end-1' @('ready')); [void] (Wait-Signal 'race-end-2' @('ready'))
        Set-Content -LiteralPath $barrier -Value 'go'
        [void] $e1.WaitForExit(60000); [void] $e2.WaitForExit(60000)
        $o1 = Wait-Signal 'race-end-1' @('done', 'refused') 5; $o2 = Wait-Signal 'race-end-2' @('done', 'refused') 5
        Assert-That (@(@($o1.What, $o2.What) | Where-Object { $_ -eq 'done' }).Count -eq 1) "completions: $($o1.What) / $($o2.What)"
        Assert-Done (Invoke-Op 'Begin' 'succ-begin' 'lab-a' 'successor') 'the successor Begin'
        $journal = Get-Sha256 (Get-LabState 'lab-a'); $record = Get-Sha256 (Get-Convention).RecordPath
        Assert-Refused (Invoke-Op 'End' 'race-end-3' 'lab-a' 'race') 'End needs -RunId of the open transaction (successor)' 'a stale completion after the successor began'
        Assert-That ((Get-Sha256 (Get-LabState 'lab-a')) -eq $journal -and (Get-Sha256 (Get-Convention).RecordPath) -eq $record) 'the successor''s journal or lease changed'
        Assert-Done (Invoke-Op 'End' 'succ-end' 'lab-a' 'successor') 'the successor End'
    }

    # --- restoration requirements -------------------------------------------------------------------------------------
    Test-Case 'R1-F4 a missing required baseline: End restores nothing and keeps the journal and lease' {
        Assert-Done (Invoke-Op 'Begin' 'f4a-begin' 'lab-a' 'f4a') 'Begin'
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'profile\Saved Games\Test_1.zks') -Value 'test save'
        Remove-Item -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-backups\f4a\profile-inventory.json')
        $shared = Get-SharedDigest 'lab-a'; $record = Get-Sha256 (Get-Convention).RecordPath
        Assert-Refused (Invoke-Op 'End' 'f4a-end' 'lab-a' 'f4a') 'Required baselines are missing or incomplete: profile-inventory.json' 'End without the profile inventory'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared) 'End wrote despite the missing baseline'
        Assert-That ($null -ne (Get-Journal 'lab-a') -and (Get-Sha256 (Get-Convention).RecordPath) -eq $record) 'the journal or lease was not kept'
        Assert-PeerRefused 'f4a-peer' 'lab-b' 'f4a'
    }

    Test-Case 'R1-F4 an interrupted prior-mod backup changes nothing shared; End closes it and a peer is admitted' {
        New-StandInLab 'lab-a' -ExistingMod
        $shared = Get-SharedDigest 'lab-a'
        Assert-Refused (Invoke-Op 'Begin' 'f4b-begin' 'lab-a' 'f4b' -FailExistingSnapshot) 'robocopy snapshot of the existing mod failed' 'Begin with an interrupted backup'
        Assert-That ((Get-Journal 'lab-a').phase -eq 'snapshotting' -and $null -ne (Get-Record)) 'the unresolved Begin did not keep its journal and lease'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared) 'the owner''s install or another shared stand-in changed'
        Assert-PeerRefused 'f4b-peer' 'lab-b' 'f4b'
        Assert-Done (Invoke-Op 'End' 'f4b-end' 'lab-a' 'f4b') 'End of the unmutated transaction'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared -and $null -eq (Get-Record)) 'End changed shared state or kept the lease'
        Assert-Done (Invoke-Op 'Begin' 'f4b-peer-2' 'lab-b' 'f4b-peer-2') 'the peer after release'
        Assert-Done (Invoke-Op 'End' 'f4b-peer-2-end' 'lab-b' 'f4b-peer-2') 'the peer End'
    }

    Test-Case 'R1-F4 Uninstall then End -KeepInstalled is refused; End then restores the owner''s install' {
        New-StandInLab 'lab-a' -ExistingMod
        $shared = Get-SharedDigest 'lab-a'
        Assert-Done (Invoke-Op 'Begin' 'f4c-begin' 'lab-a' 'f4c') 'Begin'
        Assert-Done (Invoke-Op 'Uninstall' 'f4c-uninstall' 'lab-a') 'Uninstall'
        Assert-Refused (Invoke-Op 'End' 'f4c-keep' 'lab-a' 'f4c' -KeepInstalled) '-KeepInstalled needs the recorded candidate installed intact' 'End -KeepInstalled without the candidate'
        Assert-That ($null -ne (Get-Journal 'lab-a') -and $null -ne (Get-Record) -and (Test-Path -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-backups\f4c\mod-existing\KingmakerWakingRune.dll'))) 'the journal, lease or prior-mod snapshot was not kept'
        Assert-PeerRefused 'f4c-peer' 'lab-b' 'f4c'
        Assert-Done (Invoke-Op 'End' 'f4c-end' 'lab-a' 'f4c') 'End'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared -and $null -eq (Get-Record)) 'the owner''s install was not restored exactly or the lease was kept'
        Assert-Done (Invoke-Op 'Begin' 'f4c-keep-begin' 'lab-a' 'f4c2') 'Begin'
        Assert-Done (Invoke-Op 'End' 'f4c-keep-end' 'lab-a' 'f4c2' -KeepInstalled) 'End -KeepInstalled with the candidate installed'
        Assert-That ($null -eq (Get-Record)) 'a verified -KeepInstalled End kept the lease'
    }

    # --- orphans, lifecycle, legacy ------------------------------------------------------------------------------------
    Test-Case 'R1-F5 a lost active journal is not proof: a new Begin refuses and the old lease is preserved' {
        Assert-Done (Invoke-Op 'Begin' 'f5-begin' 'lab-a' 'run-f5') 'Begin'
        Remove-Item -LiteralPath (Get-LabState 'lab-a')
        $record = Get-Sha256 (Get-Convention).RecordPath; $shared = Get-SharedDigest 'lab-a'
        Assert-Refused (Invoke-Op 'Begin' 'f5-again' 'lab-a' 'f5-again') 'reserved by KingmakerWakingRune run run-f5' 'Begin over a lost journal'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record -and (Get-SharedDigest 'lab-a') -eq $shared) 'the old lease or shared state changed'
    }

    Test-Case 'R1-F5 an acquisition interrupted before the journal needs the owner-supervised ReleaseOrphan' {
        $state = Get-LabState 'lab-a'
        [void] (New-Record 'KingmakerWakingRune' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'cut' $state)
        Assert-Refused (Invoke-Op 'Begin' 'f5o-begin' 'lab-a' 'f5o') 'reserved by KingmakerWakingRune run cut' 'Begin over an orphan'
        Assert-Refused (Invoke-Op 'ReleaseOrphan' 'f5o-wrong' 'lab-a' 'other') 'not Waking Rune run other' 'ReleaseOrphan naming another run'
        Assert-Refused (Invoke-Op 'ReleaseOrphan' 'f5o-peer' 'lab-b' 'cut') 'not Waking Rune run cut of this lab' 'ReleaseOrphan from another lab root'
        Assert-Done (Invoke-Op 'ReleaseOrphan' 'f5o-release' 'lab-a' 'cut') 'ReleaseOrphan'
        Assert-Done (Invoke-Op 'Begin' 'f5o-begin-2' 'lab-a' 'f5o') 'Begin after release'
        Assert-Refused (Invoke-Op 'ReleaseOrphan' 'f5o-active' 'lab-a' 'f5o') 'journal exists' 'ReleaseOrphan of an active transaction'
        Assert-Done (Invoke-Op 'End' 'f5o-end' 'lab-a' 'f5o') 'End'
    }

    Test-Case 'R1-F5 a close interrupted after verified restoration is finished from its closed journal (mismatch refused)' {
        $state = Get-LabState 'lab-a'
        New-Item -ItemType Directory -Force -Path (Split-Path $state) | Out-Null
        $closed = Join-Path (Split-Path $state) 'transaction.closed.json'
        $r = New-Record 'KingmakerWakingRune' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'closed-run' $state
        [ordered]@{ runId = 'closed-run'; reservationToken = ([guid]::NewGuid().ToString('N')) } | ConvertTo-Json | Set-Content -LiteralPath $closed -Encoding UTF8
        Assert-Refused (Invoke-Op 'Begin' 'f5c-mismatch' 'lab-a' 'next') 'reserved by KingmakerWakingRune run closed-run' 'Begin with a mismatched closed journal'
        [ordered]@{ runId = 'closed-run'; reservationToken = $r.token } | ConvertTo-Json | Set-Content -LiteralPath $closed -Encoding UTF8
        Assert-Done (Invoke-Op 'Begin' 'f5c-begin' 'lab-a' 'next') 'Begin after an interrupted close'
        Assert-That ((Get-Record).runId -ceq 'next' -and -not (Test-Path -LiteralPath $closed)) 'the interrupted close was not finished'
        Assert-Done (Invoke-Op 'End' 'f5c-end' 'lab-a' 'next') 'End'
    }

    Test-Case 'R1-F6 a journal without a token is refused by every operation and nothing is written' {
        $state = Get-LabState 'lab-a'
        New-Item -ItemType Directory -Force -Path (Split-Path $state) | Out-Null
        [ordered]@{ runId = 'legacy'; phase = 'installed' } | ConvertTo-Json | Set-Content -LiteralPath $state -Encoding UTF8
        $journal = Get-Sha256 $state
        foreach ($op in @('Close', 'Reinstall', 'Collect', 'End')) { Assert-Refused (Invoke-Op $op "f6-$op" 'lab-a' 'legacy') 'carries no reservation token' "$op on a legacy journal" }
        Assert-That ((Get-Sha256 $state) -eq $journal -and $null -eq (Get-Record)) 'the legacy journal changed or a lease was created'
        Assert-Done (Invoke-Op 'Status' 'f6-status' 'lab-a') 'Status of a legacy journal'
    }

    # --- record validation ---------------------------------------------------------------------------------------------
    Test-Case 'R1-F7 malformed or case-changed records with a live PID refuse acquisition, release and orphan release, byte-identical' {
        $c = Get-Convention
        $state = Get-LabState 'lab-a'
        $good = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = 'KingmakerWakingRune'; runId = 'm'; purpose = 'p'; token = ('a' * 32); holderProcessId = $PID; holderStartUtc = (Get-OwnStart); host = 'h'; createdUtc = '2026-11-28T00:00:00Z'; statePath = [IO.Path]::GetFullPath($state) }
        $variants = @(
            @{ protocol = 'Kingmaker-native-test-reservation/1' }, @{ holderProcessId = "$PID" }, @{ token = ('A' * 32) }, @{ holderStartUtc = 'yesterday' },
            @{ createdUtc = $null }, @{ lab = 7 }, @{ statePath = 5 })
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $c.RecordPath) | Out-Null
        foreach ($v in $variants) {
            $r = [ordered]@{}; foreach ($k in $good.Keys) { $r[$k] = $good[$k] }; foreach ($k in $v.Keys) { $r[$k] = $v[$k] }
            [IO.File]::WriteAllText($c.RecordPath, ($r | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
            $bytes = Get-Sha256 $c.RecordPath
            $what = ($v.Keys | ForEach-Object { "$_=$($v[$_])" }) -join ','
            Assert-Throws { Enter-KwrTransactionReservation $c $state 'x' } 'malformed' "acquire ($what)"
            Assert-Throws { Exit-KwrReservation $c ('a' * 32) } 'malformed' "release ($what)"
            Assert-Throws { Remove-KwrOrphanReservation $c $state 'm' } 'malformed' "orphan release ($what)"
            Assert-That ((Get-Sha256 $c.RecordPath) -eq $bytes) "the record changed ($what)"
        }
        [IO.File]::WriteAllBytes($c.RecordPath, [byte[]](0x7B, 0x22, 0xFF, 0xFE, 0x22, 0x7D))
        Assert-Throws { Enter-KwrTransactionReservation $c $state 'x' } 'malformed' 'acquire (invalid UTF-8)'
        Assert-That ((Get-Content -LiteralPath $c.RecordPath -Encoding Byte).Count -eq 6) 'the invalid record changed'
        Assert-That (Test-KwrReservationHolderAlive ([pscustomobject]@{ holderProcessId = $PID; holderStartUtc = (Get-OwnStart) })) 'a live holder counted as gone'
        Assert-That (-not (Test-KwrReservationHolderAlive ([pscustomobject]@{ holderProcessId = $PID; holderStartUtc = '2020-01-01T00:00:00.0000000Z' }))) 'a reused PID counted as alive'
        Assert-That (-not (Test-KwrReservationHolderAlive ([pscustomobject]@{ holderProcessId = (Get-DeadProcessId); holderStartUtc = '2020-01-01T00:00:00.0000000Z' }))) 'a dead holder counted as alive'
    }

    # --- ownership loss -------------------------------------------------------------------------------------------------
    Test-Case 'R1-F8 a lease replaced between collection and restoration: nothing restored, nothing overwritten, first incident kept' {
        Assert-Done (Invoke-Op 'Begin' 'f8-begin' 'lab-a' 'run-f8') 'Begin'
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'profile\Saved Games\Test_1.zks') -Value 'test save'
        $p = Start-Op 'End' 'f8-end' 'lab-a' 'run-f8' -PauseAt 'after-collect'
        [void] (Wait-Signal 'f8-end' @('paused-after-collect'))
        [void] (New-Record 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-run')
        $replacement = Get-Sha256 (Get-Convention).RecordPath; $shared = Get-SharedDigest 'lab-a'
        Release-Hook 'f8-end' 'after-collect'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f8-end' @('done', 'refused') 5) 'no longer holds' 'End after the lease was replaced'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $replacement -and (Get-SharedDigest 'lab-a') -eq $shared) 'the replacement record or the shared stand-ins changed'
        Assert-That ($null -ne (Get-Journal 'lab-a')) 'the journal was removed'
        $run = Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f8'
        $incidentPath = Join-Path $run 'reservation-incident.json'
        $incident = Get-Content -LiteralPath $incidentPath -Raw | ConvertFrom-Json
        Assert-That ($incident.expected.runId -ceq 'run-f8' -and $incident.observedRecord -like '*kbp-run*' -and $incident.stage -like 'before restoring*' -and $incident.release -like 'not released*') 'the incident lacks expected/observed ownership, stage or release status'
        $first = Get-Sha256 $incidentPath
        $evidence = Get-TreeDigest $run
        Assert-Refused (Invoke-Op 'Collect' 'f8-collect' 'lab-a') 'no longer holds' 'Collect after the loss'
        Assert-Refused (Invoke-Op 'End' 'f8-retry' 'lab-a' 'run-f8') 'no longer holds' 'End retry'
        Assert-That ((Get-Sha256 $incidentPath) -eq $first) 'the first incident was overwritten'
        Assert-That (@(Get-ChildItem -LiteralPath $run -Filter 'reservation-incident-*.json').Count -eq 2) 'later incidents were not kept beside the first'
        foreach ($entry in ($evidence -split ';')) { if ($entry -and -not ((Get-TreeDigest $run) -split ';' -contains $entry)) { throw "earlier evidence changed: $entry" } }
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $replacement -and (Get-SharedDigest 'lab-a') -eq $shared) 'a retry changed the replacement record or shared state'
    }

    Test-Case 'each collection gets its own evidence folder' {
        Assert-Done (Invoke-Op 'Begin' 'col-begin' 'lab-a' 'col') 'Begin'
        Assert-Done (Invoke-Op 'Collect' 'col-1' 'lab-a') 'Collect 1'
        Start-Sleep -Milliseconds 20
        Assert-Done (Invoke-Op 'Collect' 'col-2' 'lab-a') 'Collect 2'
        Assert-That (@(Get-ChildItem -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-evidence\col') -Directory -Filter 'collect-*').Count -eq 2) 'collections share a folder'
        Assert-Done (Invoke-Op 'End' 'col-end' 'lab-a' 'col') 'End'
    }

    Test-Case 'an abandoned mutex does not block the next acquisition' {
        $c = Get-Convention
        $script = "`$m = New-Object System.Threading.Mutex(`$false, '$($c.MutexName)'); [void] `$m.WaitOne(); exit 0"
        $p = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-Command', $script) -WindowStyle Hidden -PassThru
        [void] $p.WaitForExit(30000)
        $record = Enter-KwrTransactionReservation $c (Get-LabState 'lab-a') 'after-abandon'
        Assert-That ((Get-Record).token -ceq $record.token) 'not acquired after an abandoned mutex'
        Exit-KwrReservation $c $record.token
    }
}
finally {
    foreach ($p in $processes) { if (-not $p.HasExited) { $p.Kill(); [void] $p.WaitForExit(5000) } }
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue }
}

$results | ForEach-Object { Write-Host $_ }
$failed = @($results | Where-Object { $_ -like 'FAIL*' }).Count
Write-Host "RESULT total=$($results.Count) passed=$($results.Count - $failed) failed=$failed"
if ($failed -gt 0 -or $results.Count -eq 0) { exit 1 }
exit 0
