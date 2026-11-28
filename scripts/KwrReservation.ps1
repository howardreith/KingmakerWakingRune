<#
.SYNOPSIS
  Kingmaker native-test reservation, protocol 1 (docs/NATIVE-RESERVATION.md), and Waking Rune's transaction admission.
.DESCRIPTION
  A machine-wide convention shared by the Kingmaker labs that run native game tests: a named mutex in the Global
  namespace guards a lease record under %ProgramData%. Waking Rune implements it here on its own, from Windows and .NET
  facilities only; it needs no other lab to exist. KwrRuntime.ps1 transactions span separate processes (Begin ... End),
  so the reservation is the lease record, held from Begin until End has verified its restoration; the mutex only makes
  each acquire and release step atomic.

  Waking Rune's own locks, taken after the reservation: the transaction journal (runtime-state\transaction.json, which
  carries the lease token) and, for the duration of each operation, runtime-state\operation.lock held open unshared, so
  two Waking Rune operations never overlap.

  Every function takes the convention explicitly. KwrRuntime.ps1 always passes Get-KwrReservationConvention (the
  production identity); scripts/Test-NativeReservation.ps1 passes a private one.
#>
Set-StrictMode -Version Latest

$script:KwrReservationProtocol = 'kingmaker-native-test-reservation/1'
$script:KwrReservationLab = 'KingmakerWakingRune'
$script:KwrOperationLock = $null

function Get-KwrReservationConvention {
    return [pscustomobject]@{
        MutexName = 'Global\KingmakerNativeTest.Reservation.v1'
        RecordPath = Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json'
    }
}

# Runs $Body holding the reservation mutex (a few steps around the record, never a whole transaction). A mutex abandoned
# by a dead holder is owned all the same; the atomically written record stays the authority. Busy or inaccessible refuses.
function Invoke-KwrUnderReservationMutex($Convention, [scriptblock] $Body) {
    $mutex = New-Object System.Threading.Mutex($false, $Convention.MutexName)
    $owned = $false
    try {
        try { $owned = $mutex.WaitOne([TimeSpan]::FromSeconds(10)) }
        catch [System.Threading.AbandonedMutexException] { $owned = $true }
        if (-not $owned) { throw "The Kingmaker native-test reservation mutex stayed busy: $($Convention.MutexName)" }
        return (& $Body)
    }
    finally {
        if ($owned) { $mutex.ReleaseMutex() }
        $mutex.Dispose()
    }
}

function Test-KwrRecordString($Record, [string] $Name, [string] $Pattern) {
    $property = $Record.PSObject.Properties[$Name]
    return ($null -ne $property -and $property.Value -is [string] -and $property.Value -cmatch $Pattern)
}

# The record exactly as written (UTF-8), validated: the exact protocol, a lab name, run, purpose, a 32-hex token, a
# positive integer holder PID and ISO-8601 holder start and creation times; statePath, when present, a string. Anything
# else is ambiguous: refuse, never remove. $null when the resources are free.
function Read-KwrReservationRecord($Convention) {
    if (-not (Test-Path -LiteralPath $Convention.RecordPath)) { return $null }
    $record = $null
    try { $record = [IO.File]::ReadAllText($Convention.RecordPath, [Text.UTF8Encoding]::new($false, $true)) | ConvertFrom-Json } catch { }
    $valid = $record -is [pscustomobject] -and
        (Test-KwrRecordString $record 'protocol' ('^' + [regex]::Escape($script:KwrReservationProtocol) + '$')) -and
        (Test-KwrRecordString $record 'lab' '^[A-Za-z0-9._-]{1,100}$') -and (Test-KwrRecordString $record 'runId' '^.{1,200}$') -and
        (Test-KwrRecordString $record 'purpose' '^.{1,200}$') -and (Test-KwrRecordString $record 'token' '^[0-9a-f]{32}$') -and
        (Test-KwrRecordString $record 'holderStartUtc' '^\d{4}-\d\d-\d\dT') -and (Test-KwrRecordString $record 'createdUtc' '^\d{4}-\d\d-\d\dT')
    if ($valid) {
        $holder = $record.PSObject.Properties['holderProcessId']
        $valid = $null -ne $holder -and ($holder.Value -is [int] -or $holder.Value -is [long]) -and $holder.Value -gt 0
        $parsed = [DateTime]::MinValue
        $valid = $valid -and [DateTime]::TryParse($record.holderStartUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind, [ref] $parsed)
        $statePath = $record.PSObject.Properties['statePath']
        $valid = $valid -and ($null -eq $statePath -or $statePath.Value -is [string])
    }
    if (-not $valid) {
        throw "The Kingmaker native-test reservation record is unreadable, malformed or of an unknown protocol; it is left for the owner: $($Convention.RecordPath)"
    }
    return $record
}

# Whether the record's writer still runs. Only a lookup that finds no process, or a process with a different start time
# (a reused PID), counts as gone; anything inconclusive counts as alive.
function Test-KwrReservationHolderAlive($Record) {
    $process = $null
    try { $process = Get-Process -Id ([int] $Record.holderProcessId) -ErrorAction Stop }
    catch [Microsoft.PowerShell.Commands.ProcessCommandException] { return $false }
    catch { return $true }
    try { return ($process.StartTime.ToUniversalTime().ToString('o') -ceq [string] $Record.holderStartUtc) } catch { return $true }
}

function Test-KwrOwnRecord($Record, [string] $Token) {
    return ($null -ne $Record -and $Record.lab -ceq $script:KwrReservationLab -and $Record.token -ceq $Token)
}

function Write-KwrRecord($Convention, $Record) {
    $temporary = $Convention.RecordPath + '.' + $Record.token + '.tmp'
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Record | ConvertTo-Json -Depth 4))
    $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    [IO.File]::Move($temporary, $Convention.RecordPath)
}

# The closed journal: End renames the transaction journal to it after verified restoration, then releases the lease,
# then deletes it. A closed journal whose run and token match a Waking Rune lease is durable proof that the lease's
# transaction restored; it is the only evidence on which Waking Rune releases a lease it does not hold in this process.
function Get-KwrClosedJournalPath([string] $StatePath) { return [IO.Path]::Combine([IO.Path]::GetDirectoryName($StatePath), 'transaction.closed.json') }

# Reserves the shared resources for a Waking Rune transaction, or refuses naming the holder. Any present record is a
# reservation, whoever wrote it and whether or not its writer still runs. One case is finished first: a Waking Rune lease
# for this journal whose closed journal (same run and token) shows End completed its verified restoration and was
# interrupted before releasing. Every other leftover (including a lease without any journal) needs the owner-supervised
# ReleaseOrphan step.
function Enter-KwrReservation($Convention, [string] $RunId, [string] $Purpose, [string] $StatePath) {
    $fullStatePath = [IO.Path]::GetFullPath($StatePath)
    $closedPath = Get-KwrClosedJournalPath $fullStatePath
    return Invoke-KwrUnderReservationMutex $Convention {
        $directory = Split-Path -Parent $Convention.RecordPath
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        if (([IO.File]::GetAttributes($directory) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "The reservation directory is a link: $directory" }
        $existing = Read-KwrReservationRecord $Convention
        $closed = $null
        if (Test-Path -LiteralPath $closedPath) { $closed = [IO.File]::ReadAllText($closedPath, [Text.Encoding]::UTF8) | ConvertFrom-Json }
        if ($existing -and $closed -and (Test-KwrOwnRecord $existing ([string] $closed.reservationToken)) -and $existing.runId -ceq [string] $closed.runId -and
            -not (Test-Path -LiteralPath $fullStatePath) -and $existing.PSObject.Properties['statePath'] -and $existing.statePath -ceq $fullStatePath) {
            Remove-Item -LiteralPath $Convention.RecordPath -Force
            $existing = $null
        }
        if ($closed -and -not (Test-Path -LiteralPath $fullStatePath) -and -not ($existing -and (Test-KwrOwnRecord $existing ([string] $closed.reservationToken)))) {
            Remove-Item -LiteralPath $closedPath -Force   # its lease is gone: the close completed except for this file
        }
        if ($existing) {
            throw "Kingmaker native-test resources are reserved by $($existing.lab) run $($existing.runId) ($($existing.purpose); process $($existing.holderProcessId); since $($existing.createdUtc)); $($Convention.RecordPath)"
        }
        $record = [ordered]@{
            protocol = $script:KwrReservationProtocol; lab = $script:KwrReservationLab; runId = $RunId; purpose = $Purpose
            token = [guid]::NewGuid().ToString('N'); holderProcessId = $PID
            holderStartUtc = (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o'); host = $env:COMPUTERNAME
            createdUtc = (Get-Date).ToUniversalTime().ToString('o'); statePath = $fullStatePath
        }
        Write-KwrRecord $Convention $record
        [pscustomobject] $record
    }
}

function Test-KwrReservationHeld($Convention, [string] $Token) {
    if (-not $Token) { return $false }
    try { $record = Read-KwrReservationRecord $Convention } catch { return $false }
    return (Test-KwrOwnRecord $record $Token)
}

function Get-KwrObservedHolder($Convention) {
    try { $record = Read-KwrReservationRecord $Convention; if ($record) { return "$($record.lab) run $($record.runId)" } else { return 'nobody' } }
    catch { return 'an unreadable record' }
}

# Releases a reservation this transaction holds; a record that is no longer its own is left alone and reported.
function Exit-KwrReservation($Convention, [string] $Token) {
    Invoke-KwrUnderReservationMutex $Convention {
        $record = Read-KwrReservationRecord $Convention
        if (-not (Test-KwrOwnRecord $record $Token)) {
            throw "The Kingmaker native-test reservation is no longer this transaction's (now: $(Get-KwrObservedHolder $Convention)); it is left alone."
        }
        Remove-Item -LiteralPath $Convention.RecordPath -Force
    }
}

# --- Waking Rune's own locks ----------------------------------------------------------------------------------------

function Read-KwrTransactionState([string] $StatePath) {
    if (-not (Test-Path -LiteralPath $StatePath)) { return $null }
    return [IO.File]::ReadAllText($StatePath, [Text.Encoding]::UTF8) | ConvertFrom-Json
}

# Held open, unshared, for one operation; refuses at once when another Waking Rune operation holds it.
function Enter-KwrOperationLock([string] $StateRoot) {
    if ($script:KwrOperationLock) { return }
    [IO.Directory]::CreateDirectory($StateRoot) | Out-Null
    $path = Join-Path $StateRoot 'operation.lock'
    try { $script:KwrOperationLock = [IO.File]::Open($path, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch [IO.IOException] { throw "Another Waking Rune operation is running (it holds $path); refusing." }
}

function Exit-KwrOperationLock {
    if ($script:KwrOperationLock) { $script:KwrOperationLock.Dispose(); $script:KwrOperationLock = $null }
}

function Assert-KwrJournalToken($State, [string] $StatePath) {
    $token = $State.PSObject.Properties['reservationToken']
    if ($null -eq $token -or -not ($token.Value -is [string]) -or $token.Value -cnotmatch '^[0-9a-f]{32}$') {
        throw "The Waking Rune transaction $StatePath carries no reservation token (written before the Kingmaker native-test reservation); it is refused. Restore it under the owner's supervision with the KwrRuntime version that opened it."
    }
    return $token.Value
}

# Rechecked at every destructive boundary: the journal is still the admitted run and token and the lease is still
# Waking Rune's with that token. On a mismatch nothing more is touched.
function Assert-KwrAdmitted($Convention, [string] $StatePath, $Admitted) {
    $state = Read-KwrTransactionState $StatePath
    $journalOk = $null -ne $state -and [string] $state.runId -ceq $Admitted.RunId -and [string] $state.reservationToken -ceq $Admitted.Token
    if (-not $journalOk -or -not (Test-KwrReservationHeld $Convention $Admitted.Token)) {
        throw "This Waking Rune transaction ($($Admitted.RunId)) no longer holds the Kingmaker native-test reservation or its journal (lease now: $(Get-KwrObservedHolder $Convention); journal: $(if ($state) { [string] $state.runId } else { 'none' })); stopping without touching the game or the shared state."
    }
}

# The open transaction's admitted identity for one operation: its journal must carry a token (a journal without one was
# written by an older KwrRuntime and is refused), the lease must be Waking Rune's with that token, and after the operation
# lock is taken the journal and lease must still be the same run and token. $null when no transaction is open.
function Enter-KwrTransactionOperation($Convention, [string] $StatePath, [string] $StateRoot) {
    $before = Read-KwrTransactionState $StatePath
    if ($before) {
        $token = Assert-KwrJournalToken $before $StatePath
        if (-not (Test-KwrReservationHeld $Convention $token)) {
            throw "This Waking Rune transaction ($($before.runId)) no longer holds the Kingmaker native-test reservation (now: $(Get-KwrObservedHolder $Convention)); stopping without touching the game or the shared state. Inspect $($Convention.RecordPath)."
        }
    }
    Enter-KwrOperationLock $StateRoot
    $state = Read-KwrTransactionState $StatePath
    if (-not $before -and -not $state) { return $null }
    if (-not $before -or -not $state -or [string] $state.runId -cne [string] $before.runId -or [string] $state.reservationToken -cne [string] $before.reservationToken) {
        throw 'The Waking Rune transaction changed while this operation was being admitted; refusing.'
    }
    $admitted = [pscustomobject]@{ RunId = [string] $state.runId; Token = [string] $state.reservationToken }
    Assert-KwrAdmitted $Convention $StatePath $admitted
    return $admitted
}

# The first ownership incident of a run, written once (later ones are kept beside it): expected and observed ownership,
# the stage, what was restored so far and that nothing was released.
function Write-KwrReservationIncident($Convention, [string] $RunDirectory, $Admitted, [string] $Operation, [string] $Stage, [string[]] $Progress, [string] $Failure) {
    [IO.Directory]::CreateDirectory($RunDirectory) | Out-Null
    $observed = if (Test-Path -LiteralPath $Convention.RecordPath) { [IO.File]::ReadAllText($Convention.RecordPath) } else { $null }
    $incident = [ordered]@{
        kind = 'Kingmaker native-test reservation ownership lost'; recordedUtc = (Get-Date).ToUniversalTime().ToString('o')
        operation = $Operation; stage = $Stage; failure = $Failure
        expected = [ordered]@{ lab = $script:KwrReservationLab; runId = $Admitted.RunId; token = $Admitted.Token }
        observedRecord = $observed; restorationProgress = @($Progress)
        release = 'not released: the observed record and the transaction journal were left alone'
    }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($incident | ConvertTo-Json -Depth 5))
    $path = Join-Path $RunDirectory 'reservation-incident.json'
    try { $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None) }
    catch [IO.IOException] {
        $path = Join-Path $RunDirectory ('reservation-incident-' + (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8) + '.json')
        $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    }
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    return $path
}

# Begin, before its own journal: the reservation (a durable closed journal finished first).
function Enter-KwrTransactionReservation($Convention, [string] $StatePath, [string] $RunId) {
    return Enter-KwrReservation $Convention $RunId 'transaction' $StatePath
}

# End, after verified restoration and with the operation lock held: the journal (this lab's lock) is renamed to the
# closed journal, then the lease is released, then the closed journal is deleted. Every step first confirms the journal
# and lease are still the admitted run and token, so a completion never closes a successor.
function Complete-KwrTransactionReservation($Convention, [string] $StatePath, $Admitted) {
    Assert-KwrAdmitted $Convention $StatePath $Admitted
    $closedPath = Get-KwrClosedJournalPath $StatePath
    if (Test-Path -LiteralPath $closedPath) { Remove-Item -LiteralPath $closedPath -Force }
    [IO.File]::Move($StatePath, $closedPath)
    Exit-KwrReservation $Convention $Admitted.Token
    Remove-Item -LiteralPath $closedPath -Force
}

# The owner-supervised release of a Waking Rune lease no transaction holds (for example a Begin interrupted between
# acquiring it and writing its journal): only Waking Rune's own record, for this journal, with the run the owner names,
# whose writer is gone, while no journal exists. Whether anything is left to restore is the owner's judgement, from the
# run's evidence; this step never decides it.
function Remove-KwrOrphanReservation($Convention, [string] $StatePath, [string] $RunId) {
    $fullStatePath = [IO.Path]::GetFullPath($StatePath)
    Invoke-KwrUnderReservationMutex $Convention {
        $record = Read-KwrReservationRecord $Convention
        if (-not $record) { throw 'No Kingmaker native-test reservation is recorded.' }
        if ($record.lab -cne $script:KwrReservationLab -or $record.runId -cne $RunId -or -not $record.PSObject.Properties['statePath'] -or $record.statePath -cne $fullStatePath) {
            throw "The reservation is $($record.lab) run $($record.runId), not Waking Rune run $RunId of this lab; it is left alone."
        }
        if (Test-Path -LiteralPath $fullStatePath) { throw "A Waking Rune transaction journal exists ($fullStatePath); End restores and releases it." }
        if (Test-KwrReservationHolderAlive $record) { throw "The reservation's writer (process $($record.holderProcessId)) may still be running; it is left alone." }
        Remove-Item -LiteralPath $Convention.RecordPath -Force
    }
}
