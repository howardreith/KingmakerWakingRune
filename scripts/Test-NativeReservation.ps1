<#
.SYNOPSIS
  Self-tests of Waking Rune's Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md, scripts/KwrReservation.ps1).
.DESCRIPTION
  Runs only against stand-ins under a temporary folder, with a mutex name and record path private to this run: never the
  production reservation, the live game, Steam, the owner's profile or any other lab. Separate powershell.exe contender
  processes (scripts/Test-NativeReservation.Contender.ps1) run the admission functions KwrRuntime.ps1 uses, in its order,
  for two stand-in labs that share nothing but the reservation. Also checks KwrRuntime.ps1's wiring statically. Emits
  PASS/FAIL lines and a RESULT line; exits 1 on any failure.
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

function Get-Convention { [pscustomobject]@{ MutexName = $mutexName; RecordPath = (Join-Path $root 'programdata\KingmakerNativeTest\reservation.v1.json') } }
function Get-LabState([string] $Lab) { Join-Path $root "$Lab\runtime-state\transaction.json" }
function Get-Signals { Join-Path $root 'signals' }

function Reset-Root {
    foreach ($p in $processes) { if (-not $p.HasExited) { $p.Kill(); [void] $p.WaitForExit(5000) } }
    $processes.Clear()
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Get-Signals) | Out-Null
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
function Get-Sha256([string] $Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }

function Start-Step([string] $Step, [string] $Name, [string] $Lab, [switch] $Hold) {
    $convention = Get-Convention
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $contender + '"'), '-Step', $Step, '-Name', $Name,
        '-LabRoot', ('"' + (Join-Path $root $Lab) + '"'), '-Signals', ('"' + (Get-Signals) + '"'), '-MutexName', $convention.MutexName,
        '-RecordPath', ('"' + $convention.RecordPath + '"'))
    if ($Hold) { $arguments += '-Hold' }
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $processes.Add($process)
    return $process
}

function Wait-Signal([string] $Name, [string[]] $What, [int] $Seconds = 60) {
    $until = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $until) {
        foreach ($candidate in $What) {
            $path = Join-Path (Get-Signals) "$Name.$candidate"
            if (Test-Path -LiteralPath $path) { return [pscustomobject]@{ What = $candidate; Text = [string] (Get-Content -LiteralPath $path -Raw) } }
        }
        Start-Sleep -Milliseconds 50
    }
    throw "contender $Name reached none of: $($What -join ', ')"
}
function Send-Go([string] $Name, [string] $Step) { Set-Content -LiteralPath (Join-Path (Get-Signals) "$Name.$Step.go") -Value 'go' }

# A complete stand-in transaction in three separate processes, each held in its section until released.
function Invoke-Step([string] $Step, [string] $Name, [string] $Lab, [string] $Section, [string] $Done) {
    [void] (Start-Step $Step $Name $Lab)
    $outcome = Wait-Signal $Name @($Done, 'refused')
    if ($outcome.What -eq 'refused') { throw "$Name $Step was refused: $($outcome.Text)" }
}

# A peer that tries to begin while the winner holds the reservation: refused, and nothing of its own written.
function Assert-LoserRefused([string] $Loser, [string] $LoserLab, [string] $WinnerRunId) {
    $before = Get-Sha256 (Get-Convention).RecordPath
    [void] (Start-Step 'Begin' $Loser $LoserLab)
    $outcome = Wait-Signal $Loser @('refused', 'begin')
    if ($outcome.What -ne 'refused') { throw "loser $Loser began while $WinnerRunId held the reservation" }
    Assert-That ($outcome.Text -like "*reserved by KingmakerWakingRune run $WinnerRunId*") "loser $Loser refused for another reason: $($outcome.Text)"
    Assert-That (-not (Test-Path -LiteralPath (Get-LabState $LoserLab))) "loser $Loser wrote its transaction state"
    Assert-That (@(Get-ChildItem -LiteralPath (Join-Path $root "$LoserLab\shared") -Filter "$Loser.*" -ErrorAction SilentlyContinue).Count -eq 0) "loser $Loser mutated shared state"
    Assert-That ((Get-Sha256 (Get-Convention).RecordPath) -eq $before) "the winner's record changed while $Loser was refused"
}

# The winner's transaction: Begin, Run and End in separate processes, each held in its section, plus the gaps between
# them when no winner process exists at all. A loser is refused at every point; after End it begins.
function Invoke-ArrivalOrder([string] $Winner, [string] $WinnerLab, [string] $Loser, [string] $LoserLab) {
    $attempt = 0
    foreach ($step in @(@('Begin', 'begin', 'begun'), @('Run', 'run', 'ran'), @('End', 'end', 'ended'))) {
        [void] (Start-Step $step[0] $Winner $WinnerLab -Hold)
        $outcome = Wait-Signal $Winner @($step[1], 'refused')
        if ($outcome.What -eq 'refused') { throw "winner $Winner $($step[0]) was refused: $($outcome.Text)" }
        $attempt++; Assert-LoserRefused "$Loser-in-$($step[1])-$attempt" $LoserLab $Winner
        Send-Go $Winner $step[0]
        [void] (Wait-Signal $Winner @($step[2]))
        Start-Sleep -Milliseconds 300   # the winner's process has exited; its transaction is still open (until End)
        if ($step[0] -ne 'End') { $attempt++; Assert-LoserRefused "$Loser-between-$attempt" $LoserLab $Winner }
    }
    Assert-That (-not (Test-Path -LiteralPath (Get-Convention).RecordPath)) "winner $Winner left its reservation"
    Assert-That (-not (Test-Path -LiteralPath (Get-LabState $WinnerLab))) "winner $Winner left its state"
    $after = "$Loser-after"
    Invoke-Step 'Begin' $after $LoserLab 'begin' 'begun'
    Invoke-Step 'Run' $after $LoserLab 'run' 'ran'
    Invoke-Step 'End' $after $LoserLab 'end' 'ended'
    Assert-That (-not (Test-Path -LiteralPath (Get-Convention).RecordPath)) "$after left its reservation"
}

function New-Record([string] $Lab, [int] $HolderProcessId, [string] $HolderStartUtc, [string] $RunId, [string] $StatePath = '') {
    $convention = Get-Convention
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $convention.RecordPath) | Out-Null
    $record = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Lab; runId = $RunId; purpose = 'stand-in'; token = [guid]::NewGuid().ToString('N')
        holderProcessId = $HolderProcessId; holderStartUtc = $HolderStartUtc; host = $env:COMPUTERNAME; createdUtc = (Get-Date).ToUniversalTime().ToString('o') }
    if ($StatePath) { $record.statePath = [IO.Path]::GetFullPath($StatePath) }
    $record | ConvertTo-Json | Set-Content -LiteralPath $convention.RecordPath -Encoding UTF8
}
function Get-DeadProcessId {
    $p = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', '1', '127.0.0.1') -WindowStyle Hidden -PassThru
    [void] $p.WaitForExit(10000); return $p.Id
}
function New-State([string] $Lab, [string] $RunId, [string] $Token) {
    $path = Get-LabState $Lab
    New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
    $state = [ordered]@{ runId = $RunId; phase = 'installed' }
    if ($Token) { $state.reservationToken = $Token }
    $state | ConvertTo-Json | Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

try {
    Test-Case 'the production convention is the neutral protocol identity' {
        $c = Get-KwrReservationConvention
        Assert-That ($c.MutexName -ceq 'Global\KingmakerNativeTest.Reservation.v1') "mutex $($c.MutexName)"
        Assert-That ($c.RecordPath -eq (Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json')) "record $($c.RecordPath)"
        foreach ($file in @('KwrReservation.ps1', 'KwrRuntime.ps1')) {
            $text = Get-Content -LiteralPath (Join-Path $PSScriptRoot $file) -Raw
            foreach ($foreign in @('KingmakerFlight', 'MountedCombat', 'BuffPlanner', 'C:\Dev\')) { Assert-That ($text.IndexOf($foreign, [StringComparison]::OrdinalIgnoreCase) -lt 0) "$file names $foreign" }
        }
    }

    Test-Case 'KwrRuntime acquires first, guards every acting operation and releases last' {
        $text = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'KwrRuntime.ps1') -Raw
        $begin = $text.Substring($text.IndexOf('function Invoke-Begin {'), $text.IndexOf('function Invoke-Reinstall {') - $text.IndexOf('function Invoke-Begin {'))
        $order = @('Enter-KwrTransactionReservation $Reservation $StatePath $RunId', 'if (Get-State)', 'Get-GameProcesses', 'New-KwrValidatedPackageCopy', 'Assert-SteamCloudOffline', 'reservationToken = $reservation.token', 'Save-State $s', '$stateSaved = $true', 'Save-Tree')
        $last = -1
        foreach ($marker in $order) { $at = $begin.IndexOf($marker); Assert-That ($at -gt $last) "Begin: '$marker' is missing or out of order"; $last = $at }
        Assert-That ($begin -match 'if \(-not \$stateSaved\) \{\s*try \{ Exit-KwrReservation \$Reservation \$reservation\.token \}') 'Begin does not release on a refusal before its state'
        Assert-That ($text.Contains("if (`$Operation -notin @('Begin', 'Status', 'Collect')) { [void] (Assert-KwrTransactionReservation `$Reservation `$StatePath) }")) 'the operation guard is missing'
        Assert-That ($text.IndexOf('Assert-KwrTransactionReservation $Reservation $StatePath') -lt $text.IndexOf('switch ($Operation) {')) 'the guard does not precede dispatch'
        $end = $text.Substring($text.IndexOf('function Invoke-End {'))
        Assert-That ($end.IndexOf('Mods inventory differs after restore') -lt $end.IndexOf('Complete-KwrTransactionReservation $Reservation $StatePath')) 'End releases before its restoration is verified'
        Assert-That ($text.IndexOf('Remove-Item -LiteralPath $StatePath') -lt 0) 'the state is removed outside Complete-KwrTransactionReservation'
    }

    Test-Case 'a lab alone (no peer, no reservation folder yet) begins, runs and ends across processes' {
        Assert-That (-not (Test-Path -LiteralPath (Split-Path -Parent (Get-Convention).RecordPath))) 'stand-in folder pre-exists'
        Invoke-Step 'Begin' 'solo' 'lab-a' 'begin' 'begun'
        $record = Read-KwrReservationRecord (Get-Convention)
        $state = Get-Content -LiteralPath (Get-LabState 'lab-a') -Raw | ConvertFrom-Json
        Assert-That ($record.lab -eq 'KingmakerWakingRune' -and $record.runId -eq 'solo' -and $record.token -eq $state.reservationToken) 'record and state disagree'
        Invoke-Step 'Run' 'solo' 'lab-a' 'run' 'ran'
        Invoke-Step 'End' 'solo' 'lab-a' 'end' 'ended'
        Assert-That (-not (Test-Path -LiteralPath (Get-Convention).RecordPath)) 'record left behind'
        Assert-That (-not (Test-Path -LiteralPath (Get-LabState 'lab-a'))) 'state left behind'
    }

    Test-Case 'two labs exclude each other through the reservation alone, in either arrival order' {
        Invoke-ArrivalOrder 'A' 'lab-a' 'B' 'lab-b'
        Invoke-ArrivalOrder 'B2' 'lab-b' 'A2' 'lab-a'
    }

    Test-Case 'simultaneous arrivals admit exactly one' {
        foreach ($round in 1..3) {
            [void] (Start-Step 'Begin' "X$round" 'lab-a')
            [void] (Start-Step 'Begin' "Y$round" 'lab-b')
            $x = Wait-Signal "X$round" @('begun', 'refused')
            $y = Wait-Signal "Y$round" @('begun', 'refused')
            Assert-That (@(@($x.What, $y.What) | Where-Object { $_ -eq 'begun' }).Count -eq 1) "round ${round}: $($x.What) / $($y.What)"
            if ($x.What -eq 'begun') { Invoke-Step 'End' "X$round" 'lab-a' 'end' 'ended' } else { Invoke-Step 'End' "Y$round" 'lab-b' 'end' 'ended' }
            Assert-That (-not (Test-Path -LiteralPath (Get-LabState 'lab-a')) -and -not (Test-Path -LiteralPath (Get-LabState 'lab-b'))) "round ${round}: a state remained"
        }
    }

    Test-Case 'a refused Begin (open state) releases what it took and writes nothing' {
        $path = New-State 'lab-a' 'legacy-open' $null
        $before = Get-Sha256 $path
        [void] (Start-Step 'Begin' 'late' 'lab-a')
        $outcome = Wait-Signal 'late' @('refused', 'begun')
        Assert-That ($outcome.What -eq 'refused' -and $outcome.Text -like '*already open*') "unexpected: $($outcome.What) $($outcome.Text)"
        Assert-That (-not (Test-Path -LiteralPath (Get-Convention).RecordPath)) 'the refused Begin kept the reservation'
        Assert-That ((Get-Sha256 $path) -eq $before) 'the open state changed'
    }

    Test-Case 'only Waking Rune''s own orphan is adopted, and only with no transaction state' {
        $c = Get-Convention
        $statePath = Get-LabState 'lab-a'
        New-Record 'KingmakerWakingRune' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'other-root' (Get-LabState 'lab-b')
        Assert-Throws { Enter-KwrTransactionReservation $c $statePath 'next' } 'reserved by KingmakerWakingRune run other-root' 'adoption of another lab root''s orphan'
        New-Record 'KingmakerWakingRune' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'no-state-path'
        Assert-Throws { Enter-KwrTransactionReservation $c $statePath 'next' } 'reserved by KingmakerWakingRune run no-state-path' 'adoption of a record without its state path'
        New-Record 'KingmakerWakingRune' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'crashed' $statePath
        $adopted = Enter-KwrTransactionReservation $c $statePath 'next'
        Assert-That ($adopted.adopted.runId -eq 'crashed' -and (Read-KwrReservationRecord $c).token -eq $adopted.token) 'the orphan was not adopted'
        Exit-KwrReservation $c $adopted.token
        New-Record 'KingmakerWakingRune' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'open-tx' $statePath
        [void] (New-State 'lab-a' 'open-tx' 'some-token')
        Assert-Throws { Enter-KwrTransactionReservation $c $statePath 'next' } 'reserved by KingmakerWakingRune run open-tx' 'adoption over an open transaction'
        Remove-Item -LiteralPath $statePath
        New-Record 'KingmakerWakingRune' $PID ((Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o')) 'live' $statePath
        Assert-Throws { Enter-KwrTransactionReservation $c $statePath 'next' } 'reserved by KingmakerWakingRune run live' 'adoption from a live holder'
    }

    Test-Case 'another lab''s record (abandoned or not) is refused and left byte-identical' {
        $c = Get-Convention
        New-Record 'KingmakerMountedCombat' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'kmc-run'
        $before = Get-Sha256 $c.RecordPath
        Assert-Throws { Enter-KwrTransactionReservation $c (Get-LabState 'lab-a') 'mine' } 'reserved by KingmakerMountedCombat run kmc-run' 'Begin under a foreign record'
        [void] (New-State 'lab-a' 'legacy' $null)
        Assert-Throws { Assert-KwrTransactionReservation $c (Get-LabState 'lab-a') } 'reserved by KingmakerMountedCombat' 'a legacy transaction joining under a foreign record'
        Assert-That ((Get-Sha256 $c.RecordPath) -eq $before) 'the foreign record changed'
        Assert-That (-not ((Get-Content -LiteralPath (Get-LabState 'lab-a') -Raw | ConvertFrom-Json).PSObject.Properties.Name -contains 'reservationToken')) 'the legacy state was changed'
    }

    Test-Case 'an unreadable record refuses every step and is left for the owner' {
        $c = Get-Convention
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $c.RecordPath) | Out-Null
        Set-Content -LiteralPath $c.RecordPath -Value 'not json'
        $before = Get-Sha256 $c.RecordPath
        Assert-Throws { Enter-KwrTransactionReservation $c (Get-LabState 'lab-a') 'mine' } 'unreadable or of an unknown protocol' 'Begin over an unreadable record'
        [void] (New-State 'lab-a' 'tx' 'tok')
        Assert-Throws { Assert-KwrTransactionReservation $c (Get-LabState 'lab-a') } 'unreadable record' 'an operation over an unreadable record'
        Assert-That ((Get-Sha256 $c.RecordPath) -eq $before) 'the unreadable record changed'
    }

    Test-Case 'a lost reservation stops End and leaves the new record and the state' {
        $c = Get-Convention
        Invoke-Step 'Begin' 'lost' 'lab-a' 'begin' 'begun'
        New-Record 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-run'
        $before = Get-Sha256 $c.RecordPath
        [void] (Start-Step 'End' 'lost' 'lab-a')
        $outcome = Wait-Signal 'lost' @('refused', 'ended')
        Assert-That ($outcome.What -eq 'refused' -and $outcome.Text -like '*no longer holds*KingmakerBuffPlanner run kbp-run*') "unexpected: $($outcome.What) $($outcome.Text)"
        Assert-That (Test-Path -LiteralPath (Get-LabState 'lab-a')) 'the state was removed'
        Assert-That ((Get-Sha256 $c.RecordPath) -eq $before) 'the new holder''s record changed'
        Assert-Throws { Complete-KwrTransactionReservation $c (Get-LabState 'lab-a') ((Get-Content -LiteralPath (Get-LabState 'lab-a') -Raw | ConvertFrom-Json).reservationToken) } 'no longer holds' 'Complete over a lost reservation'
    }

    Test-Case 'a transaction opened before the protocol joins it only when it is free' {
        $c = Get-Convention
        $path = New-State 'lab-a' 'legacy' $null
        $token = Assert-KwrTransactionReservation $c $path
        Assert-That ((Read-KwrReservationRecord $c).token -eq $token -and (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json).reservationToken -eq $token) 'the legacy transaction did not join'
        Assert-That ((Assert-KwrTransactionReservation $c $path) -eq $token) 'the joined transaction is not recognised'
        Complete-KwrTransactionReservation $c $path $token
        Assert-That (-not (Test-Path -LiteralPath $c.RecordPath) -and -not (Test-Path -LiteralPath $path)) 'End left the record or the state'
        Assert-That ($null -eq (Assert-KwrTransactionReservation $c $path)) 'no open transaction should need nothing'
    }

    Test-Case 'an abandoned mutex does not block the next acquisition' {
        $c = Get-Convention
        $script = "`$m = New-Object System.Threading.Mutex(`$false, '$($c.MutexName)'); [void] `$m.WaitOne(); exit 0"
        $p = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-Command', $script) -WindowStyle Hidden -PassThru
        [void] $p.WaitForExit(30000)
        $record = Enter-KwrTransactionReservation $c (Get-LabState 'lab-a') 'after-abandon'
        Assert-That ((Read-KwrReservationRecord $c).token -eq $record.token) 'not acquired after an abandoned mutex'
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
