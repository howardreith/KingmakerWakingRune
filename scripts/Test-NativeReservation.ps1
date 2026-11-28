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
function New-StandInLab([string] $Lab, [switch] $ExistingMod, [switch] $EmptyExistingMod) {
    $r = Get-Lab $Lab
    foreach ($d in @('game\Mods', 'game\UnityModManager', 'profile\Saved Games', 'steam\userdata\111\640820\remote', 'registry', 'runtime-backups', 'runtime-evidence')) { New-Item -ItemType Directory -Force -Path (Join-Path $r $d) | Out-Null }
    Set-Content -LiteralPath (Join-Path $r 'game\UnityModManager\Params.xml') -Value '<Param><ShowOnStart>1</ShowOnStart></Param>'
    Set-Content -LiteralPath (Join-Path $r 'game\UnityModManager\Log.txt') -Value 'loader log'
    Set-Content -LiteralPath (Join-Path $r 'game\Mods\OtherMod.txt') -Value 'another mod'
    Set-Content -LiteralPath (Join-Path $r 'profile\Saved Games\Owner_1.zks') -Value 'owner save'
    Set-Content -LiteralPath (Join-Path $r 'steam\userdata\111\640820\remote\Owner_1.zks') -Value 'owner save mirror'
    Set-Content -LiteralPath (Join-Path $r 'registry\key.reg') -Value 'owner registry'
    if ($EmptyExistingMod) { New-Item -ItemType Directory -Force -Path (Join-Path $r 'game\Mods\KingmakerWakingRune') | Out-Null }
    if ($ExistingMod) { New-Item -ItemType Directory -Force -Path (Join-Path $r 'game\Mods\KingmakerWakingRune') | Out-Null; Set-Content -LiteralPath (Join-Path $r 'game\Mods\KingmakerWakingRune\KingmakerWakingRune.dll') -Value 'owner accepted build' }
}
# A peer lab exists only in the cases that use one.
function Use-Lab([string] $Lab) { if (-not (Test-Path -LiteralPath (Get-Lab $Lab))) { New-StandInLab $Lab } }
function Get-SharedDigest([string] $Lab) { $r = Get-Lab $Lab; return (@('game', 'profile', 'steam', 'registry') | ForEach-Object { Get-TreeDigest (Join-Path $r $_) }) -join '|' }

function Reset-Root {
    foreach ($p in $processes) { if (-not $p.HasExited) { $p.Kill(); [void] $p.WaitForExit(5000) } }
    Stop-Delegated
    $processes.Clear()
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Get-Signals) | Out-Null
    New-StandInLab 'lab-a'
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
function Start-Op([string] $Operation, [string] $Label, [string] $Lab, [string] $RunId = '', [string] $PauseAt = '', [string] $Barrier = '', [switch] $KeepInstalled, [switch] $FakeGameAfterAdmission, [switch] $FailExistingSnapshot, [int] $LauncherSeconds = 0, [string[]] $Extra = @()) {
    Use-Lab $Lab
    $c = Get-Convention
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $contender + '"'), '-Operation', $Operation, '-Label', $Label,
        '-LabRoot', ('"' + (Get-Lab $Lab) + '"'), '-Signals', ('"' + (Get-Signals) + '"'), '-MutexName', $c.MutexName, '-RecordPath', ('"' + $c.RecordPath + '"'))
    if ($RunId) { $arguments += @('-RunId', $RunId) }
    if ($PauseAt) { $arguments += @('-PauseAt', ('"' + $PauseAt + '"')) }
    if ($Barrier) { $arguments += @('-Barrier', ('"' + $Barrier + '"')) }
    if ($KeepInstalled) { $arguments += '-KeepInstalled' }
    if ($FakeGameAfterAdmission) { $arguments += '-FakeGameAfterAdmission' }
    if ($FailExistingSnapshot) { $arguments += '-FailExistingSnapshot' }
    if ($LauncherSeconds) { $arguments += @('-LauncherSeconds', $LauncherSeconds) }
    $arguments += $Extra
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
function Invoke-Op([string] $Operation, [string] $Label, [string] $Lab, [string] $RunId = '', [switch] $KeepInstalled, [switch] $FakeGameAfterAdmission, [switch] $FailExistingSnapshot, [int] $LauncherSeconds = 0, [string[]] $Extra = @()) {
    $p = Start-Op $Operation $Label $Lab $RunId -KeepInstalled:$KeepInstalled -FakeGameAfterAdmission:$FakeGameAfterAdmission -FailExistingSnapshot:$FailExistingSnapshot -LauncherSeconds $LauncherSeconds -Extra $Extra
    if (-not $p.WaitForExit(120000)) { throw "$Label did not finish" }
    return Wait-Signal $Label @('done', 'refused') 5
}
function Assert-Done($Outcome, [string] $What) { if ($Outcome.What -ne 'done') { throw "$What was refused: $($Outcome.Text)" } }
function Assert-Refused($Outcome, [string] $Fragment, [string] $What) {
    if ($Outcome.What -ne 'refused') { throw "$What was not refused" }
    if ($Outcome.Text -notlike "*$Fragment*") { throw "$What was refused for another reason: $($Outcome.Text)" }
}
function Release-Hook([string] $Label, [string] $Hook) { Set-Content -LiteralPath (Join-Path (Get-Signals) ("$Label.go-" + ($Hook -replace '[^A-Za-z0-9.-]', '-'))) -Value 'go' }
function Get-Record { if (Test-Path -LiteralPath (Get-Convention).RecordPath) { Get-Content -LiteralPath (Get-Convention).RecordPath -Raw | ConvertFrom-Json } else { $null } }
function Get-Journal([string] $Lab) { if (Test-Path -LiteralPath (Get-LabState $Lab)) { Get-Content -LiteralPath (Get-LabState $Lab) -Raw | ConvertFrom-Json } else { $null } }

# A peer lab that tries to begin while the winner holds the reservation: refused by the reservation, with no journal,
# no Steam probe and nothing of its shared stand-ins changed; the winner's record unchanged.
function Assert-PeerRefused([string] $Label, [string] $PeerLab, [string] $WinnerRunId) {
    Use-Lab $PeerLab
    $record = Get-Sha256 (Get-Convention).RecordPath
    $shared = Get-SharedDigest $PeerLab
    Assert-Refused (Invoke-Op 'Begin' $Label $PeerLab $Label) "reserved by KingmakerWakingRune run $WinnerRunId" "peer $Label"
    Assert-That (-not (Test-Path -LiteralPath (Get-LabState $PeerLab))) "peer $Label wrote a journal"
    Assert-That (-not (Test-Path -LiteralPath (Join-Path (Get-Lab $PeerLab) 'probes.log'))) "peer $Label probed Steam"
    Assert-That ((Get-SharedDigest $PeerLab) -eq $shared) "peer $Label changed its shared stand-ins"
    Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record) "the winner's record changed while $Label was refused"
}

function New-Record([string] $Lab, [int] $HolderProcessId, [string] $HolderStartUtc, [string] $RunId, [string] $StatePath = 'C:\elsewhere\transaction.json', [string] $Token = '') {
    $c = Get-Convention
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $c.RecordPath) | Out-Null
    $record = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Lab; runId = $RunId; purpose = 'stand-in'; token = $(if ($Token) { $Token } else { [guid]::NewGuid().ToString('N') })
        holderProcessId = $HolderProcessId; holderStartUtc = $HolderStartUtc; host = $env:COMPUTERNAME; createdUtc = (Get-Date).ToUniversalTime().ToString('o') }
    $record.statePath = [IO.Path]::GetFullPath($StatePath)
    [IO.File]::WriteAllText($c.RecordPath, ($record | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    return $record
}
function Get-DeadProcessId {
    $p = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', '1', '127.0.0.1') -WindowStyle Hidden -PassThru
    [void] $p.WaitForExit(10000); return $p.Id
}
function Get-Digests([string] $Lab) { (Get-SharedDigest $Lab) + '#' + $(if (Test-Path -LiteralPath (Get-LabState $Lab)) { Get-Sha256 (Get-LabState $Lab) } else { 'no-journal' }) + '#' + $(if (Test-Path -LiteralPath (Get-Convention).RecordPath) { Get-Sha256 (Get-Convention).RecordPath } else { 'no-lease' }) }
function Start-FakeGame([string] $Lab, [int] $Seconds) {
    $game = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', ($Seconds + 1), '127.0.0.1') -WindowStyle Hidden -PassThru
    $processes.Add($game)
    Set-Content -LiteralPath (Join-Path (Get-Lab $Lab) 'fake-game.pid') -Value $game.Id -NoNewline
    return $game
}
# A change made inside this transaction's Begin window (as the game or Begin itself would), so End can account for it.
function Set-OwnedChange([string] $Lab, [string] $Relative, [string] $Content) {
    $path = Join-Path (Get-Lab $Lab) $Relative
    Set-Content -LiteralPath $path -Value $Content
    $begin = @((Get-Journal $Lab).windows | Where-Object { $_.op -eq 'Begin' })[0]
    $start = [DateTimeOffset]::Parse($begin.startUtc, [Globalization.CultureInfo]::InvariantCulture)
    $end = if ($begin.endUtc) { [DateTimeOffset]::Parse($begin.endUtc, [Globalization.CultureInfo]::InvariantCulture) } else { [DateTimeOffset]::UtcNow }
    (Get-Item -LiteralPath $path).LastWriteTimeUtc = $start.AddTicks([long] (($end - $start).Ticks / 2)).UtcDateTime
}
function Get-Marker([string] $Lab) { @(Get-ChildItem -LiteralPath (Join-Path (Get-Lab $Lab) 'runtime-state') -File -Filter 'transaction.closed.*.json' -ErrorAction SilentlyContinue) }
function Stop-Delegated { Get-CimInstance Win32_Process -Filter "Name='PING.EXE'" | Where-Object { $_.CommandLine -like '*-w 4173*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue } }
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
        foreach ($marker in @('Enter-KwrTransactionReservation $ReservationConvention $StatePath $RunId', 'Enter-KwrOperationLock $StateRoot', 'if (Get-State)', 'Get-GameProcesses', 'New-KwrValidatedPackageCopy', 'Get-SteamMirror', 'reservationToken = $lease.Token; steamMirror = $mirror', '$stateSaved = $true', 'Save-Tree', "`$s.phase = 'snapshotted'", 'Remove-Item -LiteralPath $ModDir')) {
            $at = $begin.IndexOf($marker); Assert-That ($at -gt $last) "Begin: '$marker' is missing or out of order"; $last = $at
        }
        Assert-That ($runtimeText.Contains("if (`$Operation -eq 'Status') { `$s = Get-State;")) 'Status is not independent of admission and Steam'
    }

    # --- admission ---------------------------------------------------------------------------------------------------
    Test-Case 'R2-F7 a lab alone (no peer root, no reservation folder yet) begins, operates and ends' {
        Assert-That (-not (Test-Path -LiteralPath (Split-Path -Parent (Get-Convention).RecordPath))) 'stand-in folder pre-exists'
        Assert-That (-not (Test-Path -LiteralPath (Get-Lab 'lab-b'))) 'a peer root exists'
        $before = Get-SharedDigest 'lab-a'
        Assert-Done (Invoke-Op 'Begin' 'solo-begin' 'lab-a' 'solo') 'Begin'
        $record = Get-Record; $journal = Get-Journal 'lab-a'
        Assert-That ($record.lab -ceq 'KingmakerWakingRune' -and $record.runId -ceq 'solo' -and $record.token -ceq $journal.reservationToken -and $journal.phase -eq 'installed') 'record and journal disagree'
        Assert-Done (Invoke-Op 'Close' 'solo-close' 'lab-a') 'Close'
        Assert-Done (Invoke-Op 'End' 'solo-end' 'lab-a' 'solo') 'End'
        Assert-That ($null -eq (Get-Record) -and $null -eq (Get-Journal 'lab-a')) 'record or journal left behind'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $before) 'the shared stand-ins were not restored exactly'
        Assert-That (-not (Test-Path -LiteralPath (Get-Lab 'lab-b'))) 'a peer root was created'
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
        Set-OwnedChange 'lab-a' 'profile\Saved Games\Test_1.zks' 'test save'
        Remove-Item -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-backups\f4a\profile-inventory.json')
        $shared = Get-SharedDigest 'lab-a'; $record = Get-Sha256 (Get-Convention).RecordPath
        Assert-Refused (Invoke-Op 'End' 'f4a-end' 'lab-a' 'f4a') 'Required baselines are missing or inconsistent: profile-inventory.json missing' 'End without the profile inventory'
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
        Assert-Refused (Invoke-Op 'End' 'f4c-keep' 'lab-a' 'f4c' -KeepInstalled) '-KeepInstalled needs the recorded candidate' 'End -KeepInstalled without the candidate'
        Assert-That ($null -ne (Get-Journal 'lab-a') -and $null -ne (Get-Record) -and (Test-Path -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-backups\f4c\mod-existing\KingmakerWakingRune.dll'))) 'the journal, lease or prior-mod snapshot was not kept'
        Assert-PeerRefused 'f4c-peer' 'lab-b' 'f4c'
        Assert-Done (Invoke-Op 'End' 'f4c-end' 'lab-a' 'f4c') 'End'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared -and $null -eq (Get-Record)) 'the owner''s install was not restored exactly or the lease was kept'
        Assert-Done (Invoke-Op 'Begin' 'f4c-keep-begin' 'lab-a' 'f4c2') 'Begin'
        Assert-Done (Invoke-Op 'End' 'f4c-keep-end' 'lab-a' 'f4c2' -KeepInstalled) 'End -KeepInstalled with the candidate installed'
        Assert-That ($null -eq (Get-Record)) 'a verified -KeepInstalled End kept the lease'
    }

    # --- the close and leftovers ------------------------------------------------------------------------------------
    Test-Case 'R1-F3/R4-F1 the close: journal retired to its token-named marker, operation lock released, lease released last; successor survives' {
        Assert-Done (Invoke-Op 'Begin' 'f3c-begin' 'lab-a' 'run-f3c') 'Begin'
        $p = Start-Op 'End' 'f3c-end' 'lab-a' 'run-f3c' -PauseAt 'assert:before completion,before-lease-release,after-lease-release'
        [void] (Wait-Signal 'f3c-end' @('paused-assert-before-completion'))
        Assert-That ((Get-Journal 'lab-a').phase -eq 'closing') 'the journal does not say closing before retirement'
        Assert-Refused (Invoke-Op 'Begin' 'f3c-succ-0' 'lab-a' 'succ-0') 'reserved by KingmakerWakingRune run run-f3c' 'a successor before the close'
        Release-Hook 'f3c-end' 'assert:before completion'
        [void] (Wait-Signal 'f3c-end' @('paused-before-lease-release'))
        $token = (Get-Record).token
        Assert-That (@(Get-Marker 'lab-a').Count -eq 1 -and (Get-Marker 'lab-a')[0].Name -ceq "transaction.closed.$token.json" -and $null -eq (Get-Journal 'lab-a')) 'the journal was not retired to its token-named marker'
        $lock = [IO.File]::Open((Join-Path (Get-Lab 'lab-a') 'runtime-state\operation.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $lock.Dispose()
        Assert-Refused (Invoke-Op 'Begin' 'f3c-succ-1' 'lab-a' 'succ-1') 'reserved by KingmakerWakingRune run run-f3c' 'a successor while a live closer still holds the lease'
        Assert-PeerRefused 'f3c-peer-1' 'lab-b' 'run-f3c'
        Release-Hook 'f3c-end' 'before-lease-release'
        [void] (Wait-Signal 'f3c-end' @('paused-after-lease-release'))
        Assert-Done (Invoke-Op 'Begin' 'f3c-succ-2' 'lab-a' 'succ-2') 'the successor once the lease is released'
        $journal = Get-Sha256 (Get-LabState 'lab-a'); $record = Get-Sha256 (Get-Convention).RecordPath
        Release-Hook 'f3c-end' 'after-lease-release'; [void] $p.WaitForExit(60000)
        Assert-Done (Wait-Signal 'f3c-end' @('done', 'refused') 5) 'End'
        Assert-That ((Get-Sha256 (Get-LabState 'lab-a')) -eq $journal -and (Get-Sha256 (Get-Convention).RecordPath) -eq $record -and @(Get-Marker 'lab-a').Count -eq 0) 'the closer touched the successor''s journal or lease'
        Assert-Done (Invoke-Op 'End' 'f3c-succ-2-end' 'lab-a' 'succ-2') 'the successor End'
    }

    Test-Case 'R4-F1 an End interrupted after deciding to close: ordinary operations refused, a peer waits, End finishes without a second rollback' {
        Assert-Done (Invoke-Op 'Begin' 'f41-begin' 'lab-a' 'run-f41') 'Begin'
        $p = Start-Op 'End' 'f41-end' 'lab-a' 'run-f41' -PauseAt 'assert:before completion'
        [void] (Wait-Signal 'f41-end' @('paused-assert-before-completion')); $p.Kill(); [void] $p.WaitForExit(10000)
        Assert-That ((Get-Journal 'lab-a').phase -eq 'closing' -and $null -ne (Get-Record)) 'not interrupted after the close decision'
        foreach ($op in @('Reinstall', 'Launch', 'Uninstall', 'Close')) { Assert-Refused (Invoke-Op $op "f41-$op" 'lab-a') 'restoration or close is in progress' "$op after the close decision" }
        Assert-PeerRefused 'f41-peer' 'lab-b' 'run-f41'
        $rollbacks = @(Get-ChildItem -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f41') -Directory -Filter 'pre-rollback-*').Count
        Assert-Done (Invoke-Op 'End' 'f41-end-2' 'lab-a' 'run-f41') 'End finishing the close'
        Assert-That (@(Get-ChildItem -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f41') -Directory -Filter 'pre-rollback-*').Count -eq $rollbacks -and $null -eq (Get-Record) -and @(Get-Marker 'lab-a').Count -eq 0) 'End repeated the rollback or left the lease or marker'
    }

    Test-Case 'R4-F2 a delayed closer cannot delete its successor''s close marker' {
        Assert-Done (Invoke-Op 'Begin' 'f42-begin' 'lab-a' 'run-f42') 'Begin'
        $p = Start-Op 'End' 'f42-end' 'lab-a' 'run-f42' -PauseAt 'after-lease-release'
        [void] (Wait-Signal 'f42-end' @('paused-after-lease-release'))
        Assert-Done (Invoke-Op 'Begin' 'f42-succ' 'lab-a' 'run-f42b') 'the successor'
        $q = Start-Op 'End' 'f42-succ-end' 'lab-a' 'run-f42b' -PauseAt 'before-lease-release'
        [void] (Wait-Signal 'f42-succ-end' @('paused-before-lease-release'))
        $marker = @(Get-Marker 'lab-a'); $record = Get-Sha256 (Get-Convention).RecordPath
        Assert-That ($marker.Count -eq 1 -and $marker[0].Name -like "*$((Get-Record).token)*") 'the successor''s marker is not the only one'
        Release-Hook 'f42-end' 'after-lease-release'; [void] $p.WaitForExit(60000)
        Assert-Done (Wait-Signal 'f42-end' @('done', 'refused') 5) 'the predecessor'
        Assert-That ((Test-Path -LiteralPath $marker[0].FullName) -and (Get-Sha256 (Get-Convention).RecordPath) -eq $record) 'the predecessor removed the successor''s marker or lease'
        Release-Hook 'f42-succ-end' 'before-lease-release'; [void] $q.WaitForExit(60000)
        Assert-Done (Wait-Signal 'f42-succ-end' @('done', 'refused') 5) 'the successor End'
        Assert-That ($null -eq (Get-Record) -and @(Get-Marker 'lab-a').Count -eq 0) 'the successor did not finish its close'
    }

    Test-Case 'R1-F3/R1-F5 an interrupted close: kept while the closer lives, finished by this lab once it is gone; a peer waits' {
        Assert-Done (Invoke-Op 'Begin' 'f3i-begin' 'lab-a' 'run-f3i') 'Begin'
        $p = Start-Op 'End' 'f3i-end' 'lab-a' 'run-f3i' -PauseAt 'before-lease-release'
        [void] (Wait-Signal 'f3i-end' @('paused-before-lease-release'))
        Assert-Refused (Invoke-Op 'Begin' 'f3i-live' 'lab-a' 'live-try') 'reserved by KingmakerWakingRune run run-f3i' 'recovery while the closer is alive'
        $p.Kill(); [void] $p.WaitForExit(10000)
        Assert-That (@(Get-Marker 'lab-a').Count -eq 1 -and (Get-Record).runId -ceq 'run-f3i') 'the interruption did not leave the marker and lease'
        Assert-PeerRefused 'f3i-peer' 'lab-b' 'run-f3i'
        Assert-Done (Invoke-Op 'Begin' 'f3i-next' 'lab-a' 'next-run') 'this lab finishing the interrupted close'
        Assert-That (@(Get-Marker 'lab-a').Count -eq 0 -and (Get-Record).runId -ceq 'next-run') 'the interrupted close was not finished'
        Assert-Done (Invoke-Op 'End' 'f3i-next-end' 'lab-a' 'next-run') 'End'
        Assert-Done (Invoke-Op 'Begin' 'f3i-peer-2' 'lab-b' 'f3i-peer-2') 'the peer after the verified close'
        Assert-Done (Invoke-Op 'End' 'f3i-peer-2-end' 'lab-b' 'f3i-peer-2') 'the peer End'
    }

    Test-Case 'R1-F5 an unattributable closed journal is preserved; Begin refuses before taking a lease' {
        $closed = Join-Path (Get-Lab 'lab-a') 'runtime-state\transaction.closed.bogus.json'
        New-Item -ItemType Directory -Force -Path (Split-Path $closed) | Out-Null
        Set-Content -LiteralPath $closed -Value '{"runId":"interrupted"}'
        $bytes = Get-Sha256 $closed
        Assert-Refused (Invoke-Op 'Begin' 'f5c-begin' 'lab-a' 'next-run') 'close marker that cannot be attributed' 'Begin over an unattributable close marker'
        Assert-That ((Get-Sha256 $closed) -eq $bytes -and $null -eq (Get-Record)) 'the closed journal changed or a lease was kept'
    }

    Test-Case 'R1-F5 a live pre-journal Begin keeps its lease' {
        $p = Start-Op 'Begin' 'f5l-begin' 'lab-a' 'run-f5l' -PauseAt 'after-lease'
        [void] (Wait-Signal 'f5l-begin' @('paused-after-lease'))
        Assert-That ($null -eq (Get-Journal 'lab-a') -and (Get-Record).runId -ceq 'run-f5l') 'not paused between lease and journal'
        Assert-Refused (Invoke-Op 'Begin' 'f5l-same' 'lab-a' 'other-run') 'reserved by KingmakerWakingRune run run-f5l' 'a same-lab Begin'
        Assert-PeerRefused 'f5l-peer' 'lab-b' 'run-f5l'
        Release-Hook 'f5l-begin' 'after-lease'; [void] $p.WaitForExit(60000)
        Assert-Done (Wait-Signal 'f5l-begin' @('done', 'refused') 5) 'the paused Begin'
        Assert-Done (Invoke-Op 'End' 'f5l-end' 'lab-a' 'run-f5l') 'End'
    }

    Test-Case 'R1-F5 an orphan lease (no journal) is never cleared by the tool' {
        Assert-Done (Invoke-Op 'Begin' 'f5-begin' 'lab-a' 'run-f5') 'Begin'
        Remove-Item -LiteralPath (Get-LabState 'lab-a')
        $record = Get-Sha256 (Get-Convention).RecordPath; $shared = Get-SharedDigest 'lab-a'
        Assert-Refused (Invoke-Op 'Begin' 'f5-again' 'lab-a' 'f5-again') 'reserved by KingmakerWakingRune run run-f5' 'Begin over a lost journal'
        foreach ($op in @('Close', 'End', 'Collect')) { Assert-Refused (Invoke-Op $op "f5-$op" 'lab-a' 'run-f5') 'No open Waking Rune transaction' "$op without a journal" }
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record -and (Get-SharedDigest 'lab-a') -eq $shared) 'the orphan lease or shared state changed'
        Assert-That ($runtimeText -notmatch 'ReleaseOrphan' -and -not (Get-Command Remove-KwrOrphanReservation -ErrorAction SilentlyContinue)) 'an orphan-release path remains'
    }

    Test-Case 'R1-F6/R4-F3/R4-F4 previous-format transactions (genuine 6a48fbd runtime): identifiable ones recover, ambiguous ones are preserved' {
        $old = Join-Path $root 'KwrRuntime-6a48fbd.ps1'
        $source = (& git -C (Split-Path $PSScriptRoot) show '6a48fbd:scripts/KwrRuntime.ps1') -join "`n"
        [IO.File]::WriteAllText($old, $source, [Text.UTF8Encoding]::new($false))
        $oldArgs = @('-RuntimeScript', ('"' + $old + '"'))
        # 1. Admission: only End -RunId under a free lease; an interrupted binding is a safe refusal.
        $shared = Get-SharedDigest 'lab-a'
        Assert-Done (Invoke-Op 'Begin' 'f6-old-begin' 'lab-a' 'legacy-run' -Extra $oldArgs) 'the previous runtime''s Begin'
        Assert-That ($null -ne (Get-Journal 'lab-a') -and -not ((Get-Journal 'lab-a').PSObject.Properties.Name -contains 'reservationToken') -and $null -eq (Get-Record)) 'not a previous-format transaction'
        foreach ($op in @('Close', 'Reinstall', 'Collect')) { Assert-Refused (Invoke-Op $op "f6-$op" 'lab-a' 'legacy-run') 'carries no reservation token' "$op on a previous-format journal" }
        Assert-Refused (Invoke-Op 'End' 'f6-wrong' 'lab-a' 'other-run') "must name the previous-format transaction's run" 'End naming another run'
        $p = Start-Op 'End' 'f6-cut' 'lab-a' 'legacy-run' -PauseAt 'after-lease'
        [void] (Wait-Signal 'f6-cut' @('paused-after-lease')); $p.Kill(); [void] $p.WaitForExit(10000)
        Assert-Refused (Invoke-Op 'End' 'f6-after-cut' 'lab-a' 'legacy-run') 'reserved by KingmakerWakingRune run legacy-run' 'End after an interrupted binding (safe refusal)'
        Remove-Item -LiteralPath (Get-Convention).RecordPath   # the owner's decision after investigating, for the rest of this case
        # 2. The Steam account replaced after the old Begin: bound, then refused; recovery-only; restored account recovers.
        $userdata = Join-Path (Get-Lab 'lab-a') 'steam\userdata'
        Move-Item -LiteralPath (Join-Path $userdata '111') -Destination (Join-Path $root 'aside-111')
        New-Item -ItemType Directory -Force -Path (Join-Path $userdata '222\640820\remote') | Out-Null
        Set-Content -LiteralPath (Join-Path $userdata '222\640820\remote\Other.zks') -Value 'another account'
        Assert-Refused (Invoke-Op 'End' 'f6-account' 'lab-a' 'legacy-run') 'Steam mirror cannot be established' 'recovery with a replaced Steam account'
        Assert-That ([bool] (Get-Journal 'lab-a').legacyRecovery -and (Get-Record).runId -ceq 'legacy-run') 'the bound recovery did not keep its journal and lease'
        foreach ($op in @('Launch', 'Reinstall')) { Assert-Refused (Invoke-Op $op "f6-after-$op" 'lab-a') 'under recovery' "$op during recovery" }
        Assert-PeerRefused 'f6-peer' 'lab-b' 'legacy-run'
        Remove-Item -LiteralPath (Join-Path $userdata '222') -Recurse -Force
        Move-Item -LiteralPath (Join-Path $root 'aside-111') -Destination (Join-Path $userdata '111')
        # 3. A required baseline withheld after binding: refused and kept; restored: recovered.
        $reg = Join-Path (Get-Lab 'lab-a') 'runtime-backups\legacy-run\registry.reg'
        Move-Item -LiteralPath $reg -Destination (Join-Path $root 'aside.reg')
        Assert-Refused (Invoke-Op 'End' 'f6-baseline' 'lab-a' 'legacy-run') 'registry.reg missing' 'recovery without a required baseline'
        Assert-That ($null -ne (Get-Record) -and $null -ne (Get-Journal 'lab-a')) 'the failed recovery dropped the lease or journal'
        Move-Item -LiteralPath (Join-Path $root 'aside.reg') -Destination $reg
        Assert-Done (Invoke-Op 'End' 'f6-end' 'lab-a' 'legacy-run') 'recovery with intact inputs'
        Assert-That ($null -eq (Get-Journal 'lab-a') -and $null -eq (Get-Record) -and (Get-SharedDigest 'lab-a') -eq $shared) 'the previous-format transaction was not restored and released'
        Assert-Done (Invoke-Op 'Begin' 'f6-peer-2' 'lab-b' 'f6-peer-2') 'the peer after verified completion'
        Assert-Done (Invoke-Op 'End' 'f6-peer-2-end' 'lab-b' 'f6-peer-2') 'the peer End'
        # 4. A populated prior-installation copy missing a file: preserved; with the file back: recovered exactly.
        New-StandInLab 'lab-p' -ExistingMod
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-p') 'game\Mods\KingmakerWakingRune\Notes.txt') -Value 'owner notes'
        $sharedP = Get-SharedDigest 'lab-p'
        Assert-Done (Invoke-Op 'Begin' 'f6p-begin' 'lab-p' 'legacy-p' -Extra $oldArgs) 'the previous runtime''s Begin over a prior installation'
        $copy = Join-Path (Get-Lab 'lab-p') 'runtime-backups\legacy-p\mod-existing\Notes.txt'
        Move-Item -LiteralPath $copy -Destination (Join-Path $root 'aside-notes.txt')
        Assert-Refused (Invoke-Op 'End' 'f6p-end-1' 'lab-p' 'legacy-p') 'prior-installation copy does not match' 'recovery with an incomplete prior-installation copy'
        Assert-That ($null -ne (Get-Record) -and $null -ne (Get-Journal 'lab-p')) 'the ambiguous recovery was not preserved'
        Move-Item -LiteralPath (Join-Path $root 'aside-notes.txt') -Destination $copy
        Assert-Done (Invoke-Op 'End' 'f6p-end-2' 'lab-p' 'legacy-p') 'recovery with the complete copy'
        Assert-That ((Get-SharedDigest 'lab-p') -eq $sharedP -and $null -eq (Get-Record)) 'the prior installation was not restored exactly'
        # 5. An old Begin interrupted after setting the prior installation aside (no completion fields): recovered exactly.
        New-StandInLab 'lab-q' -ExistingMod
        $sharedQ = Get-SharedDigest 'lab-q'
        $p = Start-Op 'Begin' 'f6q-begin' 'lab-q' 'legacy-q' -PauseAt 'install' -Extra $oldArgs
        [void] (Wait-Signal 'f6q-begin' @('paused-install')); $p.Kill(); [void] $p.WaitForExit(10000)
        Assert-That ((Get-Journal 'lab-q').phase -eq 'snapshotting' -and -not ((Get-Journal 'lab-q').PSObject.Properties.Name -contains 'existingMod') -and -not (Test-Path -LiteralPath (Join-Path (Get-Lab 'lab-q') 'game\Mods\KingmakerWakingRune'))) 'not interrupted after set-aside'
        Assert-Done (Invoke-Op 'End' 'f6q-end' 'lab-q' 'legacy-q') 'recovery of the interrupted old Begin'
        Assert-That ((Get-SharedDigest 'lab-q') -eq $sharedQ -and $null -eq (Get-Record)) 'the interrupted old Begin was not restored exactly'
    }

    Test-Case 'R4-F3 a current journal with a malformed or missing token is refused before any migration; bytes unchanged' {
        Assert-Done (Invoke-Op 'Begin' 'f43-begin' 'lab-a' 'run-f43') 'Begin'
        Remove-Item -LiteralPath (Get-Convention).RecordPath
        $state = Get-LabState 'lab-a'; $shared = Get-SharedDigest 'lab-a'
        foreach ($variant in @('malformed', 'missing')) {
            $j = Get-Journal 'lab-a'
            if ($variant -eq 'malformed') { $j.reservationToken = 'NOT-A-TOKEN' } else { $j.PSObject.Properties.Remove('reservationToken') }
            [IO.File]::WriteAllText($state, ($j | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false)); $bytes = Get-Sha256 $state
            Assert-Refused (Invoke-Op 'End' "f43-$variant" 'lab-a' 'run-f43') 'neither a current nor a previous-format journal' "End on a current journal with a $variant token"
            Assert-That ((Get-Sha256 $state) -eq $bytes -and $null -eq (Get-Record) -and (Get-SharedDigest 'lab-a') -eq $shared) "the $variant-token journal, a lease or shared state changed"
            if ($variant -eq 'malformed') { $j.reservationToken = ('b' * 32) }
        }
    }

    # --- record validation ---------------------------------------------------------------------------------------------
    Test-Case 'R1-F7/R3-F4 neutral records are validated on the protocol fields only, strictly and exactly' {
        $c = Get-Convention
        $state = Get-LabState 'lab-a'
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $c.RecordPath) | Out-Null
        $minimal = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = 'SomeOtherLab'; runId = 'their run'; purpose = 'p'; token = 'not-hex'; holderProcessId = 4; holderStartUtc = 'whenever'; host = 'h'; createdUtc = 'then' }
        [IO.File]::WriteAllText($c.RecordPath, ($minimal | ConvertTo-Json), [Text.UTF8Encoding]::new($false)); $bytes = Get-Sha256 $c.RecordPath
        Assert-Refused (Invoke-Op 'Begin' 'f7-minimal' 'lab-a' 'f7-run') 'reserved by SomeOtherLab run their run' 'Begin under a protocol-fields-only foreign record'
        Assert-That ((Get-Sha256 $c.RecordPath) -eq $bytes) 'the foreign record changed'
        $variants = @(
            @{ protocol = "kingmaker-native-test-reservation/1`n" }, @{ protocol = 'Kingmaker-native-test-reservation/1' }, @{ protocol = "kingmaker-native-test-reservation/1$([char] 0xAD)" },
            @{ host = $null }, @{ token = $null }, @{ holderProcessId = '4' }, @{ lab = 7 }, @{ runId = "r`n" }, @{ createdUtc = 5 })
        foreach ($v in $variants) {
            $r = [ordered]@{}; foreach ($k in $minimal.Keys) { $r[$k] = $minimal[$k] }
            foreach ($k in $v.Keys) { if ($null -eq $v[$k]) { $r.Remove($k) } else { $r[$k] = $v[$k] } }
            [IO.File]::WriteAllText($c.RecordPath, ($r | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
            $bytes = Get-Sha256 $c.RecordPath
            $what = ($v.Keys | ForEach-Object { "$_=$($v[$_])" }) -join ','
            Assert-Throws { Enter-KwrTransactionReservation $c $state 'f7-run' } 'malformed' "acquire ($what)"
            Assert-Throws { Exit-KwrReservation $c (New-KwrAdmitted 'f7-run' ('a' * 32) $state) } 'malformed' "release ($what)"
            Assert-That ((Get-Sha256 $c.RecordPath) -eq $bytes) "the record changed ($what)"
        }
        [IO.File]::WriteAllText($c.RecordPath, ($minimal | ConvertTo-Json), [Text.UnicodeEncoding]::new($false, $true)); $bytes = Get-Sha256 $c.RecordPath
        Assert-Throws { Enter-KwrTransactionReservation $c $state 'f7-run' } 'malformed' 'acquire (UTF-16 record)'
        Assert-That ((Get-Sha256 $c.RecordPath) -eq $bytes) 'the UTF-16 record changed'
        Assert-That ($null -ne (ConvertTo-KwrInstant '2026-11-28T00:00:00+00:00') -and $null -eq (ConvertTo-KwrInstant '2026-11-28T00:00:00') -and (ConvertTo-KwrInstant '2026-11-28T01:00:00+01:00').UtcTicks -eq (ConvertTo-KwrInstant '2026-11-28T00:00:00Z').UtcTicks) 'instants are not compared as instants'
    }

    Test-Case 'R3-F4 a peer record in another implementation''s shape (common fields plus stateRoot) refuses by name and is preserved' {
        $c = Get-Convention
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $c.RecordPath) | Out-Null
        $peer = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = 'KingmakerBuffPlanner'; runId = '20261128T120000Z-mod-load-smoke'; purpose = 'runtime test'
            token = [guid]::NewGuid().ToString('N'); holderProcessId = 4; holderStartUtc = '2026-11-28T12:00:00.0000000Z'; host = $env:COMPUTERNAME
            createdUtc = '2026-11-28T12:00:01.0000000Z'; stateRoot = 'C:\Dev\KingmakerBuffPlannerLab\runtime-state' }
        [IO.File]::WriteAllText($c.RecordPath, ($peer | ConvertTo-Json), [Text.UTF8Encoding]::new($false)); $bytes = Get-Sha256 $c.RecordPath
        Assert-Refused (Invoke-Op 'Begin' 'f34-begin' 'lab-a' 'run-f34') 'reserved by KingmakerBuffPlanner run 20261128T120000Z-mod-load-smoke' 'Begin under a peer record with stateRoot'
        Assert-Refused (Invoke-Op 'End' 'f34-end' 'lab-a' 'run-f34') 'No open Waking Rune transaction' 'End without a transaction'
        Assert-That ((Get-Sha256 $c.RecordPath) -eq $bytes -and $null -eq (Get-Journal 'lab-a')) 'the peer record changed or a journal was written'
    }

    Test-Case 'R1-F7 recovery validates the lease writer and the closer by PID and parsed instant; indeterminate preserves' {
        $state = Get-LabState 'lab-a'
        New-Item -ItemType Directory -Force -Path (Split-Path $state) | Out-Null
        $token = [guid]::NewGuid().ToString('N')
        $marker = Join-Path (Split-Path $state) "transaction.closed.$token.json"
        $live = (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'+00:00'")
        function Set-Case($HolderPid, $HolderStart, $CloserPid, $CloserStart) {
            [void] (New-Record 'KingmakerWakingRune' $HolderPid $HolderStart 'closing-run' $state $token)
            [IO.File]::WriteAllText($marker, ([ordered]@{ runId = 'closing-run'; reservationToken = $token; phase = 'closing'; closerProcessId = $CloserPid; closerStartUtc = $CloserStart } | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
        }
        $dead = Get-DeadProcessId
        Set-Case $PID $live $dead '2020-01-01T00:00:00+00:00'
        $record = Get-Sha256 (Get-Convention).RecordPath; $markerBytes = Get-Sha256 $marker
        Assert-Refused (Invoke-Op 'Begin' 'f7-writer-live' 'lab-a' 'f7-next') 'reserved by KingmakerWakingRune run closing-run' 'recovery while the lease writer (+00:00 instant) is alive'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record -and (Get-Sha256 $marker) -eq $markerBytes) 'bytes changed with a live lease writer'
        Set-Case $dead 'not an instant' $dead '2020-01-01T00:00:00+00:00'
        Assert-Refused (Invoke-Op 'Begin' 'f7-writer-unknown' 'lab-a' 'f7-next') 'reserved by KingmakerWakingRune run closing-run' 'recovery with an indeterminate lease-writer instant'
        Set-Case $dead '2020-01-01T00:00:00+00:00' $PID $live
        Assert-Refused (Invoke-Op 'Begin' 'f7-closer-live' 'lab-a' 'f7-next') 'reserved by KingmakerWakingRune run closing-run' 'recovery while the closer is alive'
        Set-Case $dead '2020-01-01T00:00:00+00:00' $dead '2020-01-01T00:00:00+00:00'
        Assert-Done (Invoke-Op 'Begin' 'f7-abandoned' 'lab-a' 'f7-next') 'recovery of a proven abandoned holder and closer'
        Assert-That (-not (Test-Path -LiteralPath $marker)) 'the marker was not removed'
        Assert-Done (Invoke-Op 'End' 'f7-abandoned-end' 'lab-a' 'f7-next') 'End'
    }

    # --- ownership loss -------------------------------------------------------------------------------------------------
    Test-Case 'R1-F8 a lease replaced while Reinstall is paused in staging: no shared write, replacement kept, incident' {
        Assert-Done (Invoke-Op 'Begin' 'f8r-begin' 'lab-a' 'run-f8r') 'Begin'
        $p = Start-Op 'Reinstall' 'f8r-reinstall' 'lab-a' -PauseAt 'reinstall-before-placement'
        [void] (Wait-Signal 'f8r-reinstall' @('paused-reinstall-before-placement'))
        [void] (New-Record 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-run')
        $replacement = Get-Sha256 (Get-Convention).RecordPath; $shared = Get-SharedDigest 'lab-a'
        Release-Hook 'f8r-reinstall' 'reinstall-before-placement'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f8r-reinstall' @('done', 'refused') 5) 'no longer holds' 'Reinstall after the lease was replaced'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $replacement -and (Get-SharedDigest 'lab-a') -eq $shared) 'Reinstall wrote shared state or changed the replacement'
        $incident = Get-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f8r\reservation-incident.json') -Raw | ConvertFrom-Json
        Assert-That ($incident.operation -eq 'Reinstall' -and $incident.observed.lease -like 'KingmakerBuffPlanner run kbp-run' -and $incident.stage -like 'before publishing the journal' -and $incident.disposition.lease -like 'left as observed*') "inaccurate incident: $($incident | ConvertTo-Json -Compress)"
    }

    Test-Case 'R1-F8 only the journal replaced while Reinstall is paused in staging: Reinstall refuses, journal bytes and installed mod unchanged' {
        Assert-Done (Invoke-Op 'Begin' 'f8j-begin' 'lab-a' 'run-f8j') 'Begin'
        $p = Start-Op 'Reinstall' 'f8j-reinstall' 'lab-a' -PauseAt 'reinstall-before-placement'
        [void] (Wait-Signal 'f8j-reinstall' @('paused-reinstall-before-placement'))
        $j = Get-Journal 'lab-a'; $j.runId = 'replacement-run'; [IO.File]::WriteAllText((Get-LabState 'lab-a'), ($j | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
        $journal = Get-Sha256 (Get-LabState 'lab-a'); $record = Get-Sha256 (Get-Convention).RecordPath; $shared = Get-SharedDigest 'lab-a'
        Release-Hook 'f8j-reinstall' 'reinstall-before-placement'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f8j-reinstall' @('done', 'refused') 5) 'no longer holds' 'Reinstall after the journal was replaced'
        Assert-That ((Get-Sha256 (Get-LabState 'lab-a')) -eq $journal -and (Get-Sha256 (Get-Convention).RecordPath) -eq $record -and (Get-SharedDigest 'lab-a') -eq $shared) 'the replacement journal, the lease or the installed mod changed'
        $incident = Get-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f8j\reservation-incident.json') -Raw | ConvertFrom-Json
        Assert-That ($incident.observed.journal -eq 'run replacement-run' -and $incident.stage -eq 'before publishing the journal') "inaccurate incident: $($incident | ConvertTo-Json -Compress)"
    }

    Test-Case 'R1-F8 only the journal identity replaced before an End restore step: no shared write; first incident survives retries' {
        Assert-Done (Invoke-Op 'Begin' 'f8-begin' 'lab-a' 'run-f8') 'Begin'
        Set-OwnedChange 'lab-a' 'profile\Saved Games\Test_1.zks' 'test save'
        $p = Start-Op 'End' 'f8-end' 'lab-a' 'run-f8' -PauseAt 'after-collect'
        [void] (Wait-Signal 'f8-end' @('paused-after-collect'))
        $journal = Get-Journal 'lab-a'; $journal.runId = 'someone-else'
        [IO.File]::WriteAllText((Get-LabState 'lab-a'), ($journal | ConvertTo-Json -Depth 6))
        $record = Get-Sha256 (Get-Convention).RecordPath; $shared = Get-SharedDigest 'lab-a'
        Release-Hook 'f8-end' 'after-collect'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f8-end' @('done', 'refused') 5) 'no longer holds' 'End after the journal identity changed'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record -and (Get-SharedDigest 'lab-a') -eq $shared) 'End wrote shared state or changed the lease'
        $run = Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f8'
        $incidentPath = Join-Path $run 'reservation-incident.json'
        $incident = Get-Content -LiteralPath $incidentPath -Raw | ConvertFrom-Json
        Assert-That ($incident.expected.runId -ceq 'run-f8' -and $incident.observed.journal -eq 'run someone-else' -and $incident.observed.lease -eq 'as admitted' -and $incident.stage -like 'before*' -and $incident.disposition.lease -like 'left as observed*') "inaccurate incident: $($incident | ConvertTo-Json -Compress)"
        $first = Get-Sha256 $incidentPath
        $journal.runId = 'run-f8'; [IO.File]::WriteAllText((Get-LabState 'lab-a'), ($journal | ConvertTo-Json -Depth 6))
        Set-Content -LiteralPath (Get-Convention).RecordPath -Value 'not json'
        Assert-Refused (Invoke-Op 'End' 'f8-retry' 'lab-a' 'run-f8') 'unreadable' 'End retry over an unreadable record'
        Assert-That ((Get-Sha256 $incidentPath) -eq $first) 'the first incident was overwritten'
        $later = Join-Path (Get-Lab 'lab-a') 'runtime-state\incidents\reservation-incident.json'
        Assert-That ((Test-Path -LiteralPath $later) -and (Get-Content -LiteralPath $later -Raw | ConvertFrom-Json).observed.lease -like 'unknown*') 'the later (admission) incident does not mark the unreadable lease unknown'
        Assert-That ((Get-Content -LiteralPath (Get-Convention).RecordPath -Raw).Trim() -eq 'not json' -and (Get-SharedDigest 'lab-a') -eq $shared) 'the unreadable record or shared state changed'
    }

    # --- dispatch, phases, baselines, outstanding work --------------------------------------------------------------
    Test-Case 'R2-F1 with a foreign lease and no journal, operations stop before any local lock or process probe' {
        Remove-Item -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-state') -Recurse -Force -ErrorAction SilentlyContinue
        [void] (New-Record 'KingmakerMountedCombat' 4 '2020-01-01T00:00:00.0000000Z' 'kmc-run')
        $record = Get-Sha256 (Get-Convention).RecordPath
        foreach ($op in @('Close', 'Reinstall', 'End')) { Assert-Refused (Invoke-Op $op "f21-$op" 'lab-a' 'any-run') 'No open Waking Rune transaction' "$op without a transaction" }
        Assert-Refused (Invoke-Op 'Begin' 'f21-begin' 'lab-a' 'any-run') 'reserved by KingmakerMountedCombat run kmc-run' 'Begin under a foreign lease'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-state\operation.lock'))) 'a local lock was created'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path (Get-Lab 'lab-a') 'game-probes.log'))) 'a process probe ran'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record) 'the foreign lease changed'
    }

    Test-Case 'R2-F2 an incomplete snapshot refuses Fault and other shared operations; End then closes it unmutated' {
        New-StandInLab 'lab-a' -ExistingMod
        $shared = Get-SharedDigest 'lab-a'
        Assert-Refused (Invoke-Op 'Begin' 'f22-begin' 'lab-a' 'run-f22' -FailExistingSnapshot) 'robocopy snapshot of the existing mod failed' 'Begin with an interrupted backup'
        foreach ($op in @('Fault', 'Reinstall', 'Launch', 'Uninstall')) { Assert-Refused (Invoke-Op $op "f22-$op" 'lab-a' 'run-f22') "refused while Begin's snapshot is incomplete" "$op during an incomplete snapshot" }
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared -and $null -ne (Get-Record)) 'shared state changed or the lease was dropped'
        Assert-Done (Invoke-Op 'End' 'f22-end' 'lab-a' 'run-f22') 'End'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared -and $null -eq (Get-Record)) 'the prior install changed or the lease was kept'
        $fault = $runtimeText.Substring($runtimeText.IndexOf('function Invoke-Fault {'), 900)
        Assert-That ($fault -match "phase -notin @\('installed', 'closed', 'running'\)" -and $fault -match 'capture\.flag') 'Fault does not require an installed test deployment'
    }

    Test-Case 'R2-F3 an existing empty mod folder is restored exactly and a peer is admitted' {
        New-StandInLab 'lab-a' -EmptyExistingMod
        $shared = Get-SharedDigest 'lab-a'
        Assert-Done (Invoke-Op 'Begin' 'f23-begin' 'lab-a' 'run-f23') 'Begin'
        Assert-Done (Invoke-Op 'End' 'f23-end' 'lab-a' 'run-f23') 'End'
        $folder = Join-Path (Get-Lab 'lab-a') 'game\Mods\KingmakerWakingRune'
        Assert-That ((Test-Path -LiteralPath $folder -PathType Container) -and @(Get-ChildItem -LiteralPath $folder -Force).Count -eq 0 -and (Get-SharedDigest 'lab-a') -eq $shared) 'the empty folder was not restored exactly'
        Assert-Done (Invoke-Op 'Begin' 'f23-peer' 'lab-b' 'f23-peer') 'the peer'
        Assert-Done (Invoke-Op 'End' 'f23-peer-end' 'lab-b' 'f23-peer') 'the peer End'
    }

    Test-Case 'R2-F4 identities compare ordinally: a copied journal under a linguistically equal root, or a changed run, is refused' {
        Assert-Done (Invoke-Op 'Begin' 'f24-begin' 'lab-a' 'run-f24') 'Begin'
        $twin = "lab-a$([char] 0xAD)"
        Assert-That ((Get-Lab $twin) -ceq (Get-Lab 'lab-a')) 'precondition: PowerShell compares the two roots as equal'
        Use-Lab $twin
        New-Item -ItemType Directory -Force -Path (Split-Path (Get-LabState $twin)) | Out-Null
        Copy-Item -LiteralPath (Get-LabState 'lab-a') -Destination (Get-LabState $twin)
        $record = Get-Sha256 (Get-Convention).RecordPath; $sharedTwin = Get-SharedDigest $twin
        Assert-Refused (Invoke-Op 'Close' 'f24-copy' $twin 'run-f24') 'no longer holds' 'an operation on the copied journal'
        Assert-That ((Get-SharedDigest $twin) -eq $sharedTwin -and (Get-Sha256 (Get-Convention).RecordPath) -eq $record) 'the twin root or the lease changed'
        $j = Get-Journal 'lab-a'; $j.runId = "run-f24$([char] 0xAD)"; [IO.File]::WriteAllText((Get-LabState 'lab-a'), ($j | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false)); $changed = Get-Sha256 (Get-LabState 'lab-a')
        Assert-Refused (Invoke-Op 'Close' 'f24-run' 'lab-a' 'run-f24') 'names no valid run' 'an operation on a journal with a linguistically equal run'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $record -and (Get-Sha256 (Get-LabState 'lab-a')) -eq $changed) 'the record or journal changed'
    }

    Test-Case 'R2-F5 pending launches resolve only from observed outcomes: a delegating launcher, an interrupted worker' {
        Assert-Done (Invoke-Op 'Begin' 'f25-begin' 'lab-a' 'run-f25') 'Begin'
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'delegated.flag') -Value ''
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'steam-app.json') -Value '{"runningAppId":"640820","running":false,"updating":true}'
        Assert-Refused (Invoke-Op 'Launch' 'f25-launch' 'lab-a' -Extra @('-DelegateAfterSeconds', '12')) 'did not start within the timeout' 'Launch whose launcher exits after delegating'
        $work = @((Get-Journal 'lab-a').outstanding)
        Assert-That ($work.Count -eq 1 -and $work[0].kind -eq 'launch' -and $null -ne $work[0].pid -and -not $work[0].resolved) 'the launch was not journaled as pending'
        Assert-Done (Invoke-Op 'Reconcile' 'f25-rec-1' 'lab-a') 'Reconcile while Steam reports the app launching'
        Assert-That (-not @((Get-Journal 'lab-a').outstanding)[0].resolved) 'Reconcile resolved without an observed outcome'
        Assert-Refused (Invoke-Op 'End' 'f25-end-1' 'lab-a' 'run-f25') 'outstanding' 'End while the launch is unresolved'
        Assert-Refused (Invoke-Op 'Reinstall' 'f25-reinstall' 'lab-a') 'outstanding' 'Reinstall while the launch is unresolved'
        Assert-PeerRefused 'f25-peer' 'lab-b' 'run-f25'
        $until = (Get-Date).AddSeconds(40)
        while (-not @(Get-CimInstance Win32_Process -Filter "Name='PING.EXE'" | Where-Object { $_.CommandLine -like '*-w 4173*' }).Count -and (Get-Date) -lt $until) { Start-Sleep -Milliseconds 500 }
        Assert-Done (Invoke-Op 'Reconcile' 'f25-rec-2' 'lab-a') 'Reconcile once the delegated game started'
        $journal = Get-Journal 'lab-a'
        Assert-That ($null -ne @($journal.outstanding)[0].gamePid -and -not @($journal.outstanding)[0].resolved -and @($journal.sessions).Count -eq 1) 'the delegated game was not adopted as an owned session'
        Assert-Refused (Invoke-Op 'End' 'f25-end-2' 'lab-a' 'run-f25') 'outstanding' 'End while the adopted game runs'
        Stop-Delegated
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'steam-app.json') -Value '{"runningAppId":"0","running":false,"updating":false}'
        Assert-Done (Invoke-Op 'Reconcile' 'f25-rec-3' 'lab-a') 'Reconcile after the game exited'
        Assert-That (@((Get-Journal 'lab-a').outstanding)[0].resolved) 'the observed exit did not resolve the launch'
        Assert-Done (Invoke-Op 'End' 'f25-end-3' 'lab-a' 'run-f25') 'End after the proven outcome'
        Assert-Done (Invoke-Op 'Begin' 'f25b-begin' 'lab-a' 'run-f25b') 'Begin'
        $p = Start-Op 'Launch' 'f25b-launch' 'lab-a' -PauseAt 'before-pid-publication' -LauncherSeconds 40
        [void] (Wait-Signal 'f25b-launch' @('paused-before-pid-publication')); $p.Kill(); [void] $p.WaitForExit(10000)
        $work = @((Get-Journal 'lab-a').outstanding)
        Assert-That ($work.Count -eq 1 -and $null -eq $work[0].pid) 'the interrupted launch was not pending without a process'
        Assert-Done (Invoke-Op 'Reconcile' 'f25b-rec' 'lab-a') 'Reconcile of the interrupted launch'
        Assert-That (-not @((Get-Journal 'lab-a').outstanding)[0].resolved) 'an interrupted launch was resolved without evidence'
        Assert-Refused (Invoke-Op 'End' 'f25b-end' 'lab-a' 'run-f25b') 'outstanding' 'End after an interrupted launch'
        Assert-PeerRefused 'f25b-peer' 'lab-b' 'run-f25b'
    }

    Test-Case 'R3-F2 Launch then Record journal both processes through the operations; End waits for the recorder' {
        Assert-Done (Invoke-Op 'Begin' 'f32-begin' 'lab-a' 'run-f32') 'Begin'
        $p = Start-Op 'Launch' 'f32-launch' 'lab-a' -PauseAt 'assert:before launching the game'
        [void] (Wait-Signal 'f32-launch' @('paused-assert-before-launching-the-game'))
        $game = Start-FakeGame 'lab-a' 10
        Release-Hook 'f32-launch' 'assert:before launching the game'; [void] $p.WaitForExit(60000)
        Assert-Done (Wait-Signal 'f32-launch' @('done', 'refused') 5) 'Launch'
        Assert-Done (Invoke-Op 'Record' 'f32-record' 'lab-a' -Extra @('-RecorderSeconds', '25')) 'Record'
        $work = @((Get-Journal 'lab-a').outstanding)
        Assert-That ($work.Count -eq 2 -and $work[0].kind -eq 'launch' -and $work[0].resolved -and $work[1].kind -eq 'recorder' -and $null -ne $work[1].pid) "journaled work: $($work | ConvertTo-Json -Compress)"
        [void] $game.WaitForExit(30000)
        Assert-Done (Invoke-Op 'Close' 'f32-close' 'lab-a') 'Close'
        Assert-Refused (Invoke-Op 'End' 'f32-end-1' 'lab-a' 'run-f32') 'recorder' 'End while the recorder runs'
        $recorder = Get-Process -Id ([int] $work[1].pid) -ErrorAction SilentlyContinue; if ($recorder) { [void] $recorder.WaitForExit(60000) }
        Assert-Done (Invoke-Op 'End' 'f32-end-2' 'lab-a' 'run-f32') 'End after the recorder exited'
    }

    Test-Case 'R2-F6 a game process appearing during collection stops End before restoring and is recorded' {
        Assert-Done (Invoke-Op 'Begin' 'f26-begin' 'lab-a' 'run-f26') 'Begin'
        Set-OwnedChange 'lab-a' 'profile\Saved Games\Test_1.zks' 'test save'
        $p = Start-Op 'End' 'f26-end' 'lab-a' 'run-f26' -PauseAt 'after-collect'
        [void] (Wait-Signal 'f26-end' @('paused-after-collect'))
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'fake-game.flag') -Value ''
        $shared = Get-SharedDigest 'lab-a'
        Release-Hook 'f26-end' 'after-collect'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f26-end' @('done', 'refused') 5) 'appeared during collection' 'End with a game process appearing'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared -and $null -ne (Get-Record) -and $null -ne (Get-Journal 'lab-a')) 'End restored or released'
        $incident = Get-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f26\reservation-incident.json') -Raw | ConvertFrom-Json
        Assert-That ($incident.kind -eq 'unexpected game process' -and @((Get-Journal 'lab-a').unowned).Count -ge 1) 'the process appearance was not recorded'
        Remove-Item -LiteralPath (Join-Path (Get-Lab 'lab-a') 'fake-game.flag')
        Assert-Refused (Invoke-Op 'End' 'f26-end-2' 'lab-a' 'run-f26') 'cannot account for' 'End after unowned activity inside the window'
    }

    Test-Case 'R1-F4/R3-F7 damaged backups and a changed retained Info.json refuse with every digest unchanged; intact inputs recover' {
        Assert-Done (Invoke-Op 'Begin' 'f4d-begin' 'lab-a' 'run-f4d') 'Begin'
        $tx = Join-Path (Get-Lab 'lab-a') 'runtime-backups\run-f4d'
        Set-OwnedChange 'lab-a' 'profile\Saved Games\Test_1.zks' 'test save'
        $before = Get-Digests 'lab-a'
        Move-Item -LiteralPath (Join-Path $tx 'steam-remote\Owner_1.zks') -Destination (Join-Path $root 'aside.zks')
        Assert-Refused (Invoke-Op 'End' 'f4d-1' 'lab-a' 'run-f4d') 'steam-remote snapshot differs from its inventory' 'End with a missing Steam backup file'
        Assert-That ((Get-Digests 'lab-a') -eq $before) 'shared, journal or lease changed after the missing-backup refusal'
        Move-Item -LiteralPath (Join-Path $root 'aside.zks') -Destination (Join-Path $tx 'steam-remote\Owner_1.zks')
        $saved = Join-Path $tx 'profile\Saved Games\Owner_1.zks'
        $time = (Get-Item -LiteralPath $saved).LastWriteTimeUtc; $original = [IO.File]::ReadAllBytes($saved)
        $damaged = [byte[]] $original.Clone(); $damaged[0] = $damaged[0] -bxor 0x01; [IO.File]::WriteAllBytes($saved, $damaged); (Get-Item -LiteralPath $saved).LastWriteTimeUtc = $time
        Assert-Refused (Invoke-Op 'End' 'f4d-2' 'lab-a' 'run-f4d') 'profile snapshot content differs from its SHA-256 manifest' 'End with a same-length, same-time damaged backup'
        Assert-That ((Get-Digests 'lab-a') -eq $before) 'shared, journal or lease changed after the damaged-backup refusal'
        [IO.File]::WriteAllBytes($saved, $original); (Get-Item -LiteralPath $saved).LastWriteTimeUtc = $time
        Copy-Item -LiteralPath (Join-Path $tx 'Params.xml') -Destination (Join-Path $root 'aside.xml')
        Set-Content -LiteralPath (Join-Path $tx 'Params.xml') -Value '<Param><ShowOnStart>0</ShowOnStart></Param>'
        Assert-Refused (Invoke-Op 'End' 'f4d-3' 'lab-a' 'run-f4d') 'Params.xml differs from its recorded digest' 'End with a corrupted settings backup'
        Assert-That ((Get-Digests 'lab-a') -eq $before) 'shared, journal or lease changed after the corrupted-settings refusal'
        Copy-Item -LiteralPath (Join-Path $root 'aside.xml') -Destination (Join-Path $tx 'Params.xml') -Force
        $info = Join-Path (Get-Lab 'lab-a') 'game\Mods\KingmakerWakingRune\Info.json'
        Copy-Item -LiteralPath $info -Destination (Join-Path $root 'aside.json'); Set-Content -LiteralPath $info -Value '{"Id":"changed"}'
        $before = Get-Digests 'lab-a'
        Assert-Refused (Invoke-Op 'End' 'f4d-4' 'lab-a' 'run-f4d' -KeepInstalled) 'Info.json and KingmakerWakingRune.dll' 'End -KeepInstalled with a changed Info.json'
        Assert-That ((Get-Digests 'lab-a') -eq $before) 'shared, journal or lease changed after the -KeepInstalled refusal'
        Copy-Item -LiteralPath (Join-Path $root 'aside.json') -Destination $info -Force
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'game\Mods\KingmakerWakingRune\command.request') -Value 'pending harness command'
        Assert-Done (Invoke-Op 'End' 'f4d-5' 'lab-a' 'run-f4d' -KeepInstalled) 'End -KeepInstalled with intact inputs and a pending command'
        $mod = Join-Path (Get-Lab 'lab-a') 'game\Mods\KingmakerWakingRune'
        $kept = @(Get-ChildItem -LiteralPath $mod -Force | ForEach-Object Name | Sort-Object)
        Assert-That (($kept -join '|') -ceq 'Info.json|KingmakerWakingRune.dll' -and (Get-Sha256 (Join-Path $mod 'Info.json')) -eq (Get-Sha256 (Join-Path $root 'aside.json')) -and $null -eq (Get-Record)) "the kept candidate holds $($kept -join ', '), its Info.json differs, or the lease was kept"
    }

    Test-Case 'R1-F4 a retained Info.json changed after the preflight is caught by the recheck after cleanup' {
        Assert-Done (Invoke-Op 'Begin' 'f4m-begin' 'lab-a' 'run-f4m') 'Begin'
        $p = Start-Op 'End' 'f4m-end' 'lab-a' 'run-f4m' -KeepInstalled -PauseAt 'assert:before restoring Mods'
        [void] (Wait-Signal 'f4m-end' @('paused-assert-before-restoring-Mods'))
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'game\Mods\KingmakerWakingRune\Info.json') -Value '{"Id":"changed late"}'
        Release-Hook 'f4m-end' 'assert:before restoring Mods'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f4m-end' @('done', 'refused') 5) 'not exactly the recorded packaged files' 'End -KeepInstalled with a late manifest change'
        Assert-That ($null -ne (Get-Journal 'lab-a') -and $null -ne (Get-Record)) 'the journal or lease was not kept'
    }

    Test-Case 'R1-F4 a baseline missing before the first shared change: Begin stops, End closes it unmutated' {
        $shared = Get-SharedDigest 'lab-a'
        Assert-Refused (Invoke-Op 'Begin' 'f4b2-begin' 'lab-a' 'run-f4b2' -Extra @('-SkipRegistryExport')) 'Baseline registry.reg was not captured' 'Begin without the registry baseline'
        Assert-That ((Get-Journal 'lab-a').phase -eq 'snapshotting' -and (Get-SharedDigest 'lab-a') -eq $shared) 'Begin passed snapshotting or changed shared state'
        Assert-Done (Invoke-Op 'End' 'f4b2-end' 'lab-a' 'run-f4b2') 'End'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared -and $null -eq (Get-Record)) 'End changed shared state or kept the lease'
    }

    Test-Case 'R2-F6/R4-F5 an owner save changed outside every owned window: End rolls back nothing and keeps everything; an owned change is restored exactly' {
        Assert-Done (Invoke-Op 'Begin' 'f26p-begin' 'lab-a' 'run-f26p') 'Begin'
        $save = Join-Path (Get-Lab 'lab-a') 'profile\Saved Games\Owner_1.zks'
        $time = (Get-Item -LiteralPath $save).LastWriteTimeUtc
        $bytes = [IO.File]::ReadAllBytes($save); $bytes[0] = $bytes[0] -bxor 0x02; [IO.File]::WriteAllBytes($save, $bytes); (Get-Item -LiteralPath $save).LastWriteTimeUtc = $time
        $before = Get-Digests 'lab-a'; $live = Get-Sha256 $save
        Assert-Refused (Invoke-Op 'End' 'f26p-end' 'lab-a' 'run-f26p') 'cannot account for' 'End over a same-length, same-time owner change'
        Assert-That ((Get-Sha256 $save) -eq $live -and (Get-Digests 'lab-a') -eq $before) 'End changed the owner save, other shared state, the journal or the lease'
        Assert-PeerRefused 'f26p-peer' 'lab-b' 'run-f26p'
        $run = Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f26p'
        $incident = Get-Content -LiteralPath (Join-Path $run 'reservation-incident.json') -Raw | ConvertFrom-Json
        Assert-That ($incident.kind -eq 'unattributed changes before rollback' -and @($incident.knownDifferences | Where-Object { $_.path -eq 'Saved Games\Owner_1.zks' -and $null -eq $_.attribution }).Count -eq 1) "inaccurate incident: $($incident | ConvertTo-Json -Compress -Depth 6)"
        $first = Get-Sha256 (Join-Path $run 'reservation-incident.json')
        Assert-Refused (Invoke-Op 'End' 'f26p-retry' 'lab-a' 'run-f26p') 'cannot account for' 'End retry'
        Assert-That ((Get-Sha256 (Join-Path $run 'reservation-incident.json')) -eq $first) 'the first incident was overwritten'
        Remove-Item -LiteralPath (Get-Convention).RecordPath   # the owner's decision after investigating, for the second part
        Assert-Done (Invoke-Op 'Begin' 'f45-begin' 'lab-b' 'run-f45') 'Begin in another lab'
        $original = Get-Sha256 (Join-Path (Get-Lab 'lab-b') 'profile\Saved Games\Owner_1.zks')
        Set-OwnedChange 'lab-b' 'profile\Saved Games\Owner_1.zks' 'changed by the game during the transaction'
        Assert-Done (Invoke-Op 'End' 'f45-end' 'lab-b' 'run-f45') 'End restoring an owned change'
        Assert-That ((Get-Sha256 (Join-Path (Get-Lab 'lab-b') 'profile\Saved Games\Owner_1.zks')) -eq $original) 'the restored owner save does not match the baseline'
    }

    Test-Case 'R4-F7 the differences file is a JSON array covering every resource (zero, one and several entries)' {
        foreach ($case in @(@('lab-z', 'zero'), @('lab-o', 'one'), @('lab-m', 'several'))) {
            $lab = $case[0]; New-StandInLab $lab
            if ($case[1] -eq 'several') { Assert-Done (Invoke-Op 'Begin' "f47-$lab" $lab "run-$lab") 'Begin' }
            else {
                $p = Start-Op 'Begin' "f47-$lab" $lab "run-$lab" -PauseAt 'assert:before changing UMM Params.xml'
                [void] (Wait-Signal "f47-$lab" @('paused-assert-before-changing-UMM-Params.xml')); $p.Kill(); [void] $p.WaitForExit(10000)   # snapshotted, nothing changed
                if ($case[1] -eq 'one') { Set-OwnedChange $lab 'game\UnityModManager\Params.xml' '<Param><ShowOnStart>1</ShowOnStart><Changed /></Param>' }
            }
            Assert-Done (Invoke-Op 'End' "f47-$lab-end" $lab "run-$lab") "End ($($case[1]))"
            $file = @(Get-ChildItem -LiteralPath (Join-Path (Get-Lab $lab) "runtime-evidence\run-$lab") -Recurse -Filter 'differences.json')[0].FullName
            $text = (Get-Content -LiteralPath $file -Raw).Trim()
            $parsed = $text | ConvertFrom-Json; $items = @($parsed)
            Assert-That ($text.StartsWith('[')) "the $($case[1])-difference file is not a JSON array: $text"
            if ($case[1] -eq 'zero') { Assert-That (($text -replace '\s', '') -eq '[]' -and $items.Count -eq 0) "expected no differences: $text" }
            if ($case[1] -eq 'one') { Assert-That ($items.Count -eq 1 -and $items[0].resource -eq 'settings' -and $items[0].kind -eq 'changed') "expected one settings difference: $text" }
            if ($case[1] -eq 'several') { Assert-That ($items.Count -ge 2 -and @($items | Where-Object { $_.resource -eq 'settings' }).Count -eq 1 -and @($items | Where-Object { $_.resource -eq 'mod' }).Count -ge 1) "expected settings and mod differences: $text" }
        }
    }

    Test-Case 'R4-F6 a game appearing during Reinstall staging or before Begin''s first installation change stops before any change' {
        Assert-Done (Invoke-Op 'Begin' 'f46-begin' 'lab-a' 'run-f46') 'Begin'
        $p = Start-Op 'Reinstall' 'f46-reinstall' 'lab-a' -PauseAt 'reinstall-before-placement'
        [void] (Wait-Signal 'f46-reinstall' @('paused-reinstall-before-placement'))
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'fake-game.flag') -Value ''
        $before = Get-SharedDigest 'lab-a'; $record = Get-Sha256 (Get-Convention).RecordPath
        Release-Hook 'f46-reinstall' 'reinstall-before-placement'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f46-reinstall' @('done', 'refused') 5) 'does not own appeared after staging' 'Reinstall with a game appearing during staging'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $before -and (Get-Sha256 (Get-Convention).RecordPath) -eq $record -and (Get-Journal 'lab-a').phase -eq 'installed') 'installed files, settings, lease or journal changed'
        $incident = Get-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f46\reservation-incident.json') -Raw | ConvertFrom-Json
        Assert-That ($incident.kind -eq 'unexpected game process' -and $incident.stage -eq 'after staging the new mod') "inaccurate incident: $($incident | ConvertTo-Json -Compress)"
        Remove-Item -LiteralPath (Get-Convention).RecordPath   # the owner's decision after investigating, for the second part
        Use-Lab 'lab-c'
        $sharedC = Get-SharedDigest 'lab-c'
        $q = Start-Op 'Begin' 'f46-begin-c' 'lab-c' 'run-f46c' -PauseAt 'assert:before changing UMM Params.xml'
        [void] (Wait-Signal 'f46-begin-c' @('paused-assert-before-changing-UMM-Params.xml'))
        Set-Content -LiteralPath (Join-Path (Get-Lab 'lab-c') 'fake-game.flag') -Value ''
        Release-Hook 'f46-begin-c' 'assert:before changing UMM Params.xml'; [void] $q.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f46-begin-c' @('done', 'refused') 5) 'does not own appeared before changing UMM Params.xml' 'Begin with a game appearing before its first change'
        Assert-That ((Get-SharedDigest 'lab-c') -eq $sharedC -and (Get-Journal 'lab-c').phase -eq 'snapshotted') 'Begin changed settings or the Mods folder'
    }

    Test-Case 'R1-F8 ownership lost during initial publication, the close wait and the final lease release: actions stop, replacements and the first incident survive' {
        $p = Start-Op 'Begin' 'f8p-begin' 'lab-a' 'run-f8p' -PauseAt 'initial-publication'
        [void] (Wait-Signal 'f8p-begin' @('paused-initial-publication'))
        Remove-Item -LiteralPath (Get-Convention).RecordPath; [void] (New-Record 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-run'); $replacement = Get-Sha256 (Get-Convention).RecordPath
        Release-Hook 'f8p-begin' 'initial-publication'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f8p-begin' @('done', 'refused') 5) 'lost the Kingmaker native-test reservation before publishing' 'Begin losing its lease before publication'
        Assert-That ($null -eq (Get-Journal 'lab-a') -and (Get-Sha256 (Get-Convention).RecordPath) -eq $replacement) 'a journal was published or the replacement changed'
        $first = Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f8p\reservation-incident.json'; $firstBytes = Get-Sha256 $first
        Assert-Refused (Invoke-Op 'Begin' 'f8p-retry' 'lab-a' 'run-f8p2') 'reserved by KingmakerBuffPlanner' 'a retry under the replacement'
        Assert-That ((Get-Sha256 $first) -eq $firstBytes) 'the first incident changed'
        Remove-Item -LiteralPath (Get-Convention).RecordPath
        Assert-Done (Invoke-Op 'Begin' 'f8w-begin' 'lab-a' 'run-f8w') 'Begin'
        $l = Start-Op 'Launch' 'f8w-launch' 'lab-a' -PauseAt 'assert:before launching the game'
        [void] (Wait-Signal 'f8w-launch' @('paused-assert-before-launching-the-game'))
        $game = Start-FakeGame 'lab-a' 60
        Release-Hook 'f8w-launch' 'assert:before launching the game'; [void] $l.WaitForExit(60000)
        Assert-Done (Wait-Signal 'f8w-launch' @('done', 'refused') 5) 'Launch'
        $c = Start-Op 'Close' 'f8w-close' 'lab-a' -PauseAt 'assert:before closing the owned game' -Extra @('-CloseWait', '6')
        [void] (Wait-Signal 'f8w-close' @('paused-assert-before-closing-the-owned-game'))
        Release-Hook 'f8w-close' 'assert:before closing the owned game'
        Start-Sleep -Seconds 2
        Remove-Item -LiteralPath (Get-Convention).RecordPath; [void] (New-Record 'KingmakerMountedCombat' 4 '2020-01-01T00:00:00.0000000Z' 'kmc-run'); $replacement = Get-Sha256 (Get-Convention).RecordPath
        [void] $c.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f8w-close' @('done', 'refused') 5) 'no longer holds' 'Close losing its lease during the wait'
        Assert-That (-not $game.HasExited -and (Get-Sha256 (Get-Convention).RecordPath) -eq $replacement) 'the game was forced or the replacement changed'
        $incident = Get-Content -LiteralPath (Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f8w\reservation-incident.json') -Raw | ConvertFrom-Json
        Assert-That ($incident.stage -eq 'before forcing the owned game to exit') "inaccurate incident: $($incident | ConvertTo-Json -Compress)"
        $game.Kill(); [void] $game.WaitForExit(10000)
        Use-Lab 'lab-c'
        Remove-Item -LiteralPath (Get-Convention).RecordPath
        Assert-Done (Invoke-Op 'Begin' 'f8r-c-begin' 'lab-c' 'run-f8rc') 'Begin'
        $e = Start-Op 'End' 'f8r-c-end' 'lab-c' 'run-f8rc' -PauseAt 'before-lease-release'
        [void] (Wait-Signal 'f8r-c-end' @('paused-before-lease-release'))
        Remove-Item -LiteralPath (Get-Convention).RecordPath; [void] (New-Record 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-late'); $replacement = Get-Sha256 (Get-Convention).RecordPath
        Release-Hook 'f8r-c-end' 'before-lease-release'; [void] $e.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f8r-c-end' @('done', 'refused') 5) 'Completion failed' 'End losing its lease before the final release'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $replacement -and @(Get-Marker 'lab-c').Count -eq 1) 'the replacement changed or the close marker was removed'
        $lateIncident = Join-Path (Get-Lab 'lab-c') 'runtime-evidence\run-f8rc\reservation-incident.json'
        Assert-That ((Get-Content -LiteralPath $lateIncident -Raw | ConvertFrom-Json).stage -eq 'during completion') 'the completion loss was not recorded independently'
        $lateBytes = Get-Sha256 $lateIncident
        Assert-Refused (Invoke-Op 'End' 'f8r-c-retry' 'lab-c' 'run-f8rc') 'No open Waking Rune transaction' 'End retry after the retired journal'
        Assert-That ((Get-Sha256 $lateIncident) -eq $lateBytes) 'the completion incident changed'
    }

    Test-Case 'R4-F8 an ownership incident after partial restoration records what was done, what remains and where everything was left' {
        Assert-Done (Invoke-Op 'Begin' 'f48-begin' 'lab-a' 'run-f48') 'Begin'
        Set-OwnedChange 'lab-a' 'profile\Saved Games\Test_1.zks' 'test save'
        $p = Start-Op 'End' 'f48-end' 'lab-a' 'run-f48' -PauseAt 'assert:before restoring Mods'
        [void] (Wait-Signal 'f48-end' @('paused-assert-before-restoring-Mods'))
        Remove-Item -LiteralPath (Get-Convention).RecordPath; [void] (New-Record 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-run'); $replacement = Get-Sha256 (Get-Convention).RecordPath
        $mod = Get-TreeDigest (Join-Path (Get-Lab 'lab-a') 'game\Mods\KingmakerWakingRune')
        Release-Hook 'f48-end' 'assert:before restoring Mods'; [void] $p.WaitForExit(60000)
        Assert-Refused (Wait-Signal 'f48-end' @('done', 'refused') 5) 'no longer holds' 'End losing its lease before Mods'
        Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $replacement -and (Get-TreeDigest (Join-Path (Get-Lab 'lab-a') 'game\Mods\KingmakerWakingRune')) -eq $mod) 'the replacement or the installed mod changed'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path (Get-Lab 'lab-a') 'profile\Saved Games\Test_1.zks'))) 'the profile was not restored before the loss (fixture precondition)'
        $path = Join-Path (Get-Lab 'lab-a') 'runtime-evidence\run-f48\reservation-incident.json'
        $incident = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        $done = @($incident.restorationDone) -join '|'
        Assert-That ($done -like '*profile restored*' -and $done -like '*registry verified*' -and $done -like '*Params.xml verified*' -and $done -notlike '*Mods verified*') "restoration done: $done"
        Assert-That (@($incident.knownDifferences | Where-Object { $_ -like 'profile added Saved Games\Test_1.zks*' }).Count -eq 1 -and @($incident.knownDifferences | Where-Object { $_ -like 'settings changed*' }).Count -eq 1) "known differences: $(@($incident.knownDifferences) -join ' | ')"
        $remaining = @($incident.remainingDifferences)
        Assert-That (@($remaining | Where-Object { $_.resource -eq 'mod' }).Count -ge 1 -and @($remaining | Where-Object { $_.resource -in @('settings', 'profile') }).Count -eq 0) "remaining differences: $($remaining | ConvertTo-Json -Compress)"
        Assert-That ($incident.observed.lease -eq 'KingmakerBuffPlanner run kbp-run' -and $incident.disposition.operationLock -like 'released*' -and $incident.disposition.lease -like 'left as observed*') "dispositions: $($incident.disposition | ConvertTo-Json -Compress)"
        $first = Get-Sha256 $path
        Assert-Refused (Invoke-Op 'End' 'f48-retry' 'lab-a' 'run-f48') 'no longer holds' 'End retry'
        Assert-That ((Get-Sha256 $path) -eq $first) 'the first incident was overwritten'
    }

    Test-Case 'R3-F1 a regular file at the mod folder path stops Begin before any change' {
        $target = Join-Path (Get-Lab 'lab-a') 'game\Mods\KingmakerWakingRune'
        Set-Content -LiteralPath $target -Value 'a file, not a folder'
        $shared = Get-SharedDigest 'lab-a'
        $fileBytes = Get-Sha256 $target
        Assert-Refused (Invoke-Op 'Begin' 'f31-begin' 'lab-a' 'run-f31') 'exists but is not a folder' 'Begin with a file at the mod path'
        Assert-That ((Get-SharedDigest 'lab-a') -eq $shared -and $null -eq (Get-Record) -and $null -eq (Get-Journal 'lab-a')) 'the file or settings changed, or a lease or journal remained'
        Assert-Refused (Invoke-Op 'End' 'f31-end' 'lab-a' 'run-f31') 'No open Waking Rune transaction' 'End after the refused Begin'
        Assert-That ((Test-Path -LiteralPath $target -PathType Leaf) -and (Get-Sha256 $target) -eq $fileBytes -and (Get-SharedDigest 'lab-a') -eq $shared) 'the file at the mod path did not survive Begin and End'
    }

    Test-Case 'R3-F3 a run ID ending in a line feed is refused before any record or journal; a valid contender is admitted' {
        Assert-Refused (Invoke-Op 'Begin' 'f33-begin' 'lab-a' 'run-f33' -Extra @('-RunIdSuffixLF')) 'A -RunId of 3-80 safe characters is required' 'Begin with a trailing line feed'
        Assert-That ($null -eq (Get-Record) -and $null -eq (Get-Journal 'lab-a') -and -not (Test-Path -LiteralPath (Split-Path -Parent (Get-Convention).RecordPath))) 'a record, journal or reservation folder was created'
        Assert-Done (Invoke-Op 'Begin' 'f33-valid' 'lab-b' 'run-f33') 'a valid contender'
        Assert-Done (Invoke-Op 'End' 'f33-valid-end' 'lab-b' 'run-f33') 'End'
    }

    Test-Case 'R3-F5 incidents from rejected journals go to the lab''s incidents folder, never a journal-chosen path' {
        Assert-Done (Invoke-Op 'Begin' 'f35-begin' 'lab-a' 'run-f35') 'Begin'
        $j = Get-Journal 'lab-a'; $j.runId = 'another-run'; [IO.File]::WriteAllText((Get-LabState 'lab-a'), ($j | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
        $evidence = Get-TreeDigest (Join-Path (Get-Lab 'lab-a') 'runtime-evidence')
        Assert-Refused (Invoke-Op 'Close' 'f35-other' 'lab-a') 'no longer holds' 'a journal naming another run'
        $incidents = Join-Path (Get-Lab 'lab-a') 'runtime-state\incidents'
        $first = Get-Content -LiteralPath (Join-Path $incidents 'reservation-incident.json') -Raw | ConvertFrom-Json
        Assert-That ($first.expected.claimedRunId -ceq 'another-run' -and $first.expected.verified -eq $false -and $first.observed.lease -like 'KingmakerWakingRune run run-f35') "inaccurate incident: $($first | ConvertTo-Json -Compress)"
        $j.runId = '..\..\escape'; [IO.File]::WriteAllText((Get-LabState 'lab-a'), ($j | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
        Assert-Refused (Invoke-Op 'Close' 'f35-path' 'lab-a') 'names no valid run' 'a journal with an invalid path component'
        Assert-That ((Get-TreeDigest (Join-Path (Get-Lab 'lab-a') 'runtime-evidence')) -eq $evidence -and -not (Test-Path -LiteralPath (Join-Path $root 'escape'))) 'evidence changed or a journal-named directory was created'
        Assert-That ((Get-Content -LiteralPath (Join-Path $incidents 'reservation-incident.json') -Raw | ConvertFrom-Json).expected.claimedRunId -ceq 'another-run') 'the first incident was replaced'
    }

    Test-Case 'R3-F6 a non-ASCII package path round-trips through Begin, Close and Status' {
        $name = "kandid$([char] 0xE1)t-$([char] 0x00FC)-$([char] 0x4E2D).zip"
        $expected = Join-Path (Get-Lab 'lab-a') $name
        Assert-Done (Invoke-Op 'Begin' 'f36-begin' 'lab-a' 'run-f36' -Extra @('-PackageName', $name)) 'Begin'
        Assert-Done (Invoke-Op 'Close' 'f36-close' 'lab-a') 'Close'
        Assert-Done (Invoke-Op 'Status' 'f36-status' 'lab-a') 'Status'
        $journal = [IO.File]::ReadAllText((Get-LabState 'lab-a'), [Text.UTF8Encoding]::new($false, $true)) | ConvertFrom-Json
        $status = [IO.File]::ReadAllText((Join-Path (Get-Signals) 'f36-status.out'), [Text.UTF8Encoding]::new($false, $true)) | ConvertFrom-Json
        Assert-That ([string]::Equals($journal.package, $expected, [StringComparison]::Ordinal) -and [string]::Equals($status.package, $expected, [StringComparison]::Ordinal)) "package recorded as '$($journal.package)' / Status '$($status.package)'"
        Assert-Done (Invoke-Op 'End' 'f36-end' 'lab-a' 'run-f36') 'End'
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
        Assert-That ((Get-Record).token -ceq $record.Token) 'not acquired after an abandoned mutex'
        Exit-KwrReservation $c $record
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
