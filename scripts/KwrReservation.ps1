<#
.SYNOPSIS
  Kingmaker native-test reservation, protocol 1 (docs/NATIVE-RESERVATION.md), and Waking Rune's transaction admission.
.DESCRIPTION
  A machine-wide convention shared by the Kingmaker labs that run native game tests: a named mutex in the Global
  namespace guards a lease record under %ProgramData%. Waking Rune implements it here on its own, from Windows and .NET
  facilities only; it needs no other lab to exist. KwrRuntime.ps1 transactions span separate processes (Begin ... End),
  so the reservation is the lease record, held from Begin until End has verified its restoration; the mutex only makes
  each acquire and release step atomic.

  Waking Rune's own locks, always taken after the reservation: the transaction journal (runtime-state\transaction.json,
  which records the run, the lease token and its own path in the lease) and, for the whole of each operation,
  runtime-state\operation.lock held open unshared, so two Waking Rune operations never overlap.

  No path adopts or removes a lease this process did not acquire. A lease left without its journal (an interrupted
  Begin or End) is left for the owner (README, "Native test runs").

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

# An ISO-8601 instant with an explicit offset (Z or +hh:mm), or $null.
function ConvertTo-KwrInstant($Value) {
    if (-not ($Value -is [string]) -or $Value -cnotmatch '^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(\.\d{1,7})?(Z|[+-]\d\d:\d\d)\z') { return $null }
    $instant = [DateTimeOffset]::MinValue
    if ([DateTimeOffset]::TryParse($Value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref] $instant)) { return $instant }
    return $null
}

# The record exactly as written (strict UTF-8), validated before anything relies on it: protocol exactly equal; lab,
# run, purpose and host strings; a 32-hex token; a positive integer holder PID; holder start and creation instants with an
# explicit offset; the holder's journal path. Anything else is ambiguous: refuse, never remove. $null when free.
function Read-KwrReservationRecord($Convention) {
    if (-not (Test-Path -LiteralPath $Convention.RecordPath)) { return $null }
    $record = $null
    try { $record = [IO.File]::ReadAllText($Convention.RecordPath, [Text.UTF8Encoding]::new($false, $true)) | ConvertFrom-Json } catch { }
    $valid = $record -is [pscustomobject] -and $record.PSObject.Properties['protocol'] -and $record.protocol -is [string] -and
        $record.protocol -ceq $script:KwrReservationProtocol -and
        (Test-KwrRecordString $record 'lab' '^[A-Za-z0-9._-]{1,100}\z') -and (Test-KwrRecordString $record 'runId' '^[^\r\n]{1,200}\z') -and
        (Test-KwrRecordString $record 'purpose' '^[^\r\n]{1,200}\z') -and (Test-KwrRecordString $record 'host' '^[^\r\n]{1,256}\z') -and
        (Test-KwrRecordString $record 'token' '^[0-9a-f]{32}\z') -and (Test-KwrRecordString $record 'statePath' '^[^\r\n]{1,1024}\z')
    if ($valid) {
        $holder = $record.PSObject.Properties['holderProcessId']
        $valid = $null -ne $holder -and ($holder.Value -is [int] -or $holder.Value -is [long]) -and $holder.Value -gt 0 -and
            $null -ne (ConvertTo-KwrInstant $record.PSObject.Properties['holderStartUtc'].Value) -and
            $null -ne (ConvertTo-KwrInstant $record.PSObject.Properties['createdUtc'].Value)
    }
    if (-not $valid) {
        throw "The Kingmaker native-test reservation record is unreadable, malformed or of an unknown protocol; it is left for the owner: $($Convention.RecordPath)"
    }
    return $record
}

# The identity a transaction was admitted with: run, token and the absolute journal path.
function New-KwrAdmitted([string] $RunId, [string] $Token, [string] $StatePath) {
    return [pscustomobject]@{ RunId = $RunId; Token = $Token; StatePath = [IO.Path]::GetFullPath($StatePath) }
}

function Test-KwrRecordIs($Record, $Admitted) {
    return ($null -ne $Record -and $Record.lab -ceq $script:KwrReservationLab -and $Record.token -ceq $Admitted.Token -and
        $Record.runId -ceq $Admitted.RunId -and $Record.statePath -ceq $Admitted.StatePath)
}

function Write-KwrRecord($Convention, $Record) {
    $temporary = $Convention.RecordPath + '.' + $Record.token + '.tmp'
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Record | ConvertTo-Json -Depth 4))
    $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    [IO.File]::Move($temporary, $Convention.RecordPath)
}

# Reserves the shared resources for a Waking Rune transaction, or refuses naming the holder. Any present record is a
# reservation, whoever wrote it and whether or not its writer still runs.
function Enter-KwrReservation($Convention, [string] $RunId, [string] $Purpose, [string] $StatePath) {
    $fullStatePath = [IO.Path]::GetFullPath($StatePath)
    return Invoke-KwrUnderReservationMutex $Convention {
        $directory = Split-Path -Parent $Convention.RecordPath
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        if (([IO.File]::GetAttributes($directory) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "The reservation directory is a link: $directory" }
        $existing = Read-KwrReservationRecord $Convention
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
        New-KwrAdmitted $RunId $record.token $fullStatePath
    }
}

# Begin, before this lab's operation lock and journal.
function Enter-KwrTransactionReservation($Convention, [string] $StatePath, [string] $RunId) {
    return Enter-KwrReservation $Convention $RunId 'transaction' $StatePath
}

function Get-KwrObservedHolder($Convention) {
    try { $record = Read-KwrReservationRecord $Convention; if ($record) { return "$($record.lab) run $($record.runId)" } else { return 'nobody' } }
    catch { return 'an unreadable record' }
}

# Releases the reservation this transaction holds; a record that is no longer its own is left alone and reported.
function Exit-KwrReservation($Convention, $Admitted) {
    Invoke-KwrUnderReservationMutex $Convention {
        $record = Read-KwrReservationRecord $Convention
        if (-not (Test-KwrRecordIs $record $Admitted)) {
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

function Get-KwrClosedJournalPath([string] $StatePath) { return [IO.Path]::Combine([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($StatePath)), 'transaction.closed.json') }

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
    if ($null -eq $token -or -not ($token.Value -is [string]) -or $token.Value -cnotmatch '^[0-9a-f]{32}\z') {
        throw "The Waking Rune transaction $StatePath carries no reservation token (written before the Kingmaker native-test reservation); it is refused. Restore it under the owner's supervision with the KwrRuntime version that opened it."
    }
    return $token.Value
}

# The admitted identity, rechecked: the journal at the admitted path is the admitted run and token, and the lease is
# Waking Rune's for that run, token and journal path. Returns what was observed when it is not.
function Get-KwrOwnershipLoss($Convention, $Admitted) {
    $journal = 'unknown'
    try {
        $state = Read-KwrTransactionState $Admitted.StatePath
        if (-not $state) { $journal = 'absent' }
        elseif ([string] $state.runId -ceq $Admitted.RunId -and $state.PSObject.Properties['reservationToken'] -and [string] $state.reservationToken -ceq $Admitted.Token) { $journal = $null }
        else { $journal = "run $($state.runId)" }
    }
    catch { $journal = "unknown (unreadable: $($_.Exception.Message))" }
    $lease = 'unknown'
    try {
        $record = Read-KwrReservationRecord $Convention
        if (Test-KwrRecordIs $record $Admitted) { $lease = $null }
        elseif ($record) { $lease = "$($record.lab) run $($record.runId)" }
        else { $lease = 'absent' }
    }
    catch { $lease = "unknown (unreadable: $($_.Exception.Message))" }
    if ($null -eq $journal -and $null -eq $lease) { return $null }
    return [pscustomobject]@{ Journal = $(if ($null -eq $journal) { 'as admitted' } else { $journal }); Lease = $(if ($null -eq $lease) { 'as admitted' } else { $lease }) }
}

# The open transaction's admitted identity for one operation. With no journal there is nothing to admit: refuse before any
# local lock or probe. Otherwise the journal must carry a token (a journal without one was written by an older
# KwrRuntime and is refused) and the lease must be this run, token and journal path; then the operation lock is taken and
# the same identity rechecked.
function Enter-KwrTransactionOperation($Convention, [string] $StatePath, [string] $StateRoot) {
    $before = Read-KwrTransactionState $StatePath
    if (-not $before) { throw "No open Waking Rune transaction ($StatePath)." }
    $admitted = New-KwrAdmitted ([string] $before.runId) (Assert-KwrJournalToken $before $StatePath) $StatePath
    $loss = Get-KwrOwnershipLoss $Convention $admitted
    if ($loss) { throw "This Waking Rune transaction ($($admitted.RunId)) no longer holds the Kingmaker native-test reservation or its journal (lease: $($loss.Lease); journal: $($loss.Journal)); stopping without touching the game or the shared state." }
    Enter-KwrOperationLock $StateRoot
    $loss = Get-KwrOwnershipLoss $Convention $admitted
    if ($loss) { throw "This Waking Rune transaction ($($admitted.RunId)) no longer holds the Kingmaker native-test reservation or its journal (lease: $($loss.Lease); journal: $($loss.Journal)); stopping without touching the game or the shared state." }
    return $admitted
}

# The first ownership incident of a run, written once (later ones are kept beside it): expected and observed ownership
# (unknown when it could not be read), the stage, what was done so far and that nothing was released.
function Write-KwrReservationIncident($Convention, [string] $RunDirectory, $Admitted, $Loss, [string] $Operation, [string] $Stage, [string[]] $Progress) {
    [IO.Directory]::CreateDirectory($RunDirectory) | Out-Null
    $observedRecord = 'absent'
    try { if (Test-Path -LiteralPath $Convention.RecordPath) { $observedRecord = [IO.File]::ReadAllText($Convention.RecordPath) } }
    catch { $observedRecord = "unknown (unreadable: $($_.Exception.Message))" }
    $incident = [ordered]@{
        kind = 'Kingmaker native-test reservation or journal ownership lost'; recordedUtc = (Get-Date).ToUniversalTime().ToString('o')
        operation = $Operation; stage = $Stage
        expected = [ordered]@{ lab = $script:KwrReservationLab; runId = $Admitted.RunId; token = $Admitted.Token; statePath = $Admitted.StatePath }
        observed = [ordered]@{ lease = $Loss.Lease; journal = $Loss.Journal; record = $observedRecord }
        progress = @($Progress)
        release = 'not released: the observed record and journal were left alone; shared state after this point is unknown to this transaction'
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

# End, after verified restoration, while the caller holds the operation lock for the whole close: the journal is retired
# (renamed to the closed journal), then the lease is released, then the closed journal is deleted. Each step first
# confirms the admitted identity. An interruption leaves the lease and the closed journal for the owner.
function Complete-KwrTransactionReservation($Convention, $Admitted) {
    if (Get-KwrOwnershipLoss $Convention $Admitted) { throw "This Waking Rune transaction ($($Admitted.RunId)) no longer holds its reservation or journal; it is not closed." }
    $closedPath = Get-KwrClosedJournalPath $Admitted.StatePath
    if (Test-Path -LiteralPath $closedPath) { throw "A closed journal from an interrupted close exists ($closedPath); it is left for the owner." }
    [IO.File]::Move($Admitted.StatePath, $closedPath)
    Exit-KwrReservation $Convention $Admitted
    Remove-Item -LiteralPath $closedPath -Force
}
