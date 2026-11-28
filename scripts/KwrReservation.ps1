<#
.SYNOPSIS
  Kingmaker native-test reservation, protocol 1 (docs/NATIVE-RESERVATION.md), and Waking Rune's transaction admission.
.DESCRIPTION
  A machine-wide convention shared by the Kingmaker labs that run native game tests: a named mutex in the Global
  namespace guards a lease record under %ProgramData%. Waking Rune implements it here on its own, from Windows and .NET
  facilities only; it needs no other lab to exist. KwrRuntime.ps1 transactions span separate processes (Begin ... End),
  so the reservation is the lease record, held from Begin until End has verified its restoration; the mutex only makes
  each acquire and release step atomic.

  Waking Rune's own locks, always taken after the reservation and released before it: the transaction journal
  (runtime-state\transaction.json, which records the run and the lease token; the lease records the journal's path) and,
  for the whole of each operation, runtime-state\operation.lock held open unshared.

  Waking Rune releases only its own transaction's lease: a refused Begin before its journal exists, End after verified
  restoration, and Begin finishing a close that End interrupted after retiring its journal (proved by the closed journal,
  its lease token and a closer that is no longer running). Any other lease is never removed.

  Comparisons of protocol, lab, run and token are ordinal; journal paths are compared as canonical full paths, ordinal
  ignoring case. Records and journals are decoded as strict UTF-8.

  Every function takes the convention explicitly. KwrRuntime.ps1 always passes Get-KwrReservationConvention (the
  production identity); scripts/Test-NativeReservation.ps1 passes a private one.
#>
Set-StrictMode -Version Latest

$script:KwrReservationProtocol = 'kingmaker-native-test-reservation/1'
$script:KwrReservationLab = 'KingmakerWakingRune'
$script:KwrOperationLock = $null
$script:KwrRunIdPattern = '^[A-Za-z0-9._-]{3,80}\z'

function Get-KwrReservationConvention {
    return [pscustomobject]@{
        MutexName = 'Global\KingmakerNativeTest.Reservation.v1'
        RecordPath = Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json'
    }
}

function Test-KwrSame($A, $B) { return ($A -is [string] -and $B -is [string] -and [string]::Equals($A, $B, [StringComparison]::Ordinal)) }
function Test-KwrSamePath($A, $B) {
    if (-not ($A -is [string]) -or -not ($B -is [string])) { return $false }
    try { return [string]::Equals([IO.Path]::GetFullPath($A), [IO.Path]::GetFullPath($B), [StringComparison]::OrdinalIgnoreCase) } catch { return $false }
}
function Get-KwrProp($Object, [string] $Name) { if ($null -eq $Object) { return $null }; $p = $Object.PSObject.Properties[$Name]; if ($null -eq $p) { return $null }; return $p.Value }
function Test-KwrRunId($Value) { return ($Value -is [string] -and $Value -cmatch $script:KwrRunIdPattern) }

# Strict UTF-8 (a leading UTF-8 byte-order mark, written by earlier journals, is accepted); anything else throws.
function Read-KwrUtf8Json([string] $Path) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $offset = if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { 3 } else { 0 }
    return [Text.UTF8Encoding]::new($false, $true).GetString($bytes, $offset, $bytes.Length - $offset) | ConvertFrom-Json
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

function Test-KwrString($Object, [string] $Name, [string] $Pattern = '^[^\r\n]{1,1024}\z') {
    $property = $Object.PSObject.Properties[$Name]
    return ($null -ne $property -and $property.Value -is [string] -and $property.Value -cmatch $Pattern)
}

# An ISO-8601 instant with an explicit offset (Z or +hh:mm), or $null.
function ConvertTo-KwrInstant($Value) {
    if (-not ($Value -is [string]) -or $Value -cnotmatch '^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(\.\d{1,7})?(Z|[+-]\d\d:\d\d)\z') { return $null }
    $instant = [DateTimeOffset]::MinValue
    if ([DateTimeOffset]::TryParse($Value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref] $instant)) { return $instant }
    return $null
}

# Whether a writer recorded as (PID, start instant) still runs. Only "no such process" or a process that started at a
# different instant (a reused PID) counts as gone; an unparsable instant or an inconclusive lookup counts as alive.
function Test-KwrWriterAlive($ProcessId, $StartUtc) {
    $instant = ConvertTo-KwrInstant $StartUtc
    if (-not ($ProcessId -is [int] -or $ProcessId -is [long]) -or $ProcessId -le 0 -or $null -eq $instant) { return $true }
    try { $process = Get-Process -Id ([int] $ProcessId) -ErrorAction Stop }
    catch [Microsoft.PowerShell.Commands.ProcessCommandException] { return $false }
    catch { return $true }
    try { return ($process.StartTime.ToUniversalTime().Ticks -eq $instant.UtcTicks) } catch { return $true }
}

# The record exactly as written (strict UTF-8), validated against the protocol's common fields only: protocol exactly
# equal; lab, run, purpose, token and host strings; a positive integer holder PID; holder start and creation strings.
# Anything else is ambiguous: refuse, never remove. $null when free. Fields a lab adds for itself are not required here.
function Read-KwrReservationRecord($Convention) {
    if (-not (Test-Path -LiteralPath $Convention.RecordPath)) { return $null }
    $record = $null
    try { $record = Read-KwrUtf8Json $Convention.RecordPath } catch { }
    $valid = $record -is [pscustomobject] -and $null -ne $record.PSObject.Properties['protocol'] -and (Test-KwrSame $record.protocol $script:KwrReservationProtocol) -and
        (Test-KwrString $record 'lab') -and (Test-KwrString $record 'runId') -and (Test-KwrString $record 'purpose') -and (Test-KwrString $record 'token') -and
        (Test-KwrString $record 'host') -and (Test-KwrString $record 'holderStartUtc') -and (Test-KwrString $record 'createdUtc')
    if ($valid) {
        $holder = $record.PSObject.Properties['holderProcessId']
        $valid = $null -ne $holder -and ($holder.Value -is [int] -or $holder.Value -is [long]) -and $holder.Value -gt 0
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

# Whether a record is this Waking Rune transaction's own lease (Waking Rune's extension field statePath included).
function Test-KwrRecordIs($Record, $Admitted) {
    return ($null -ne $Record -and (Test-KwrSame $Record.lab $script:KwrReservationLab) -and (Test-KwrSame $Record.token $Admitted.Token) -and
        (Test-KwrSame $Record.runId $Admitted.RunId) -and (Test-KwrSamePath (Get-KwrProp $Record 'statePath') $Admitted.StatePath))
}

function Write-KwrRecord($Convention, $Record) {
    $temporary = $Convention.RecordPath + '.' + $Record.token + '.tmp'
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($Record | ConvertTo-Json -Depth 4))
    $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    [IO.File]::Move($temporary, $Convention.RecordPath)
}

function Get-KwrClosedJournalPath([string] $StatePath) { return [IO.Path]::Combine([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($StatePath)), 'transaction.closed.json') }

# Reserves the shared resources for a Waking Rune transaction, or refuses naming the holder. Any present record is a
# reservation, whoever wrote it and whether or not its writer still runs, with one exception finished first: this lab's
# own close that End interrupted after retiring its journal. That is proved only by the closed journal of this journal
# path naming the same run and token as a Waking Rune lease for this path, no journal, and a closer (recorded PID and
# start instant) that is no longer running. A closed journal whose lease is already gone is the leftover of a completed
# close and is deleted. Anything else is left as it is.
function Enter-KwrReservation($Convention, [string] $RunId, [string] $Purpose, [string] $StatePath) {
    if (-not (Test-KwrRunId $RunId)) { throw 'A -RunId of 3-80 safe characters is required.' }
    $fullStatePath = [IO.Path]::GetFullPath($StatePath)
    $closedPath = Get-KwrClosedJournalPath $fullStatePath
    return Invoke-KwrUnderReservationMutex $Convention {
        $directory = Split-Path -Parent $Convention.RecordPath
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        if (([IO.File]::GetAttributes($directory) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "The reservation directory is a link: $directory" }
        $existing = Read-KwrReservationRecord $Convention
        if ((Test-Path -LiteralPath $closedPath) -and -not (Test-Path -LiteralPath $fullStatePath)) {
            $closed = $null
            try { $closed = Read-KwrUtf8Json $closedPath } catch { }
            $valid = $closed -is [pscustomobject] -and (Test-KwrRunId (Get-KwrProp $closed 'runId')) -and (Test-KwrString $closed 'reservationToken' '^[0-9a-f]{32}\z')
            if ($valid -and -not $existing) {
                Remove-Item -LiteralPath $closedPath -Force   # its lease is gone: the close completed except for this file
            }
            elseif ($valid -and (Test-KwrRecordIs $existing (New-KwrAdmitted $closed.runId $closed.reservationToken $fullStatePath)) -and
                $null -ne (Get-KwrProp $closed 'closerProcessId') -and -not (Test-KwrWriterAlive (Get-KwrProp $closed 'closerProcessId') (Get-KwrProp $closed 'closerStartUtc'))) {
                Remove-Item -LiteralPath $Convention.RecordPath -Force
                Remove-Item -LiteralPath $closedPath -Force
                $existing = $null
            }
        }
        if ($existing) {
            throw "Kingmaker native-test resources are reserved by $($existing.lab) run $($existing.runId) ($($existing.purpose); process $($existing.holderProcessId); since $($existing.createdUtc)); $($Convention.RecordPath)"
        }
        if (Test-Path -LiteralPath $closedPath) { throw "A closed journal that cannot be attributed exists ($closedPath); it is left for the owner." }
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
    return Read-KwrUtf8Json $StatePath
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

function Test-KwrJournalToken($State) { return ($null -ne $State -and (Test-KwrString $State 'reservationToken' '^[0-9a-f]{32}\z')) }

# The admitted identity, rechecked: the journal at the admitted path is the admitted run and token, and the lease is
# Waking Rune's for that run, token and journal path. Returns what was observed when it is not.
function Get-KwrOwnershipLoss($Convention, $Admitted) {
    $journal = 'unknown'
    try {
        $state = Read-KwrTransactionState $Admitted.StatePath
        if (-not $state) { $journal = 'absent' }
        elseif ((Test-KwrSame (Get-KwrProp $state 'runId') $Admitted.RunId) -and (Test-KwrJournalToken $state) -and (Test-KwrSame $state.reservationToken $Admitted.Token)) { $journal = $null }
        else { $journal = "run $(Get-KwrProp $state 'runId')" }
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
# local lock or probe. Otherwise the journal must name a valid run and carry a token, and the lease must be this run,
# token and journal path; then the operation lock is taken and the same identity rechecked.
function Enter-KwrTransactionOperation($Convention, [string] $StatePath, [string] $StateRoot) {
    $before = Read-KwrTransactionState $StatePath
    if (-not $before) { throw "No open Waking Rune transaction ($StatePath)." }
    $runId = $before.PSObject.Properties['runId']
    if ($null -eq $runId -or -not (Test-KwrRunId $runId.Value)) { throw "The Waking Rune journal $StatePath names no valid run; it is refused." }
    if (-not (Test-KwrJournalToken $before)) {
        throw "The Waking Rune transaction $StatePath carries no reservation token (written before the Kingmaker native-test reservation); only End -RunId $($runId.Value) recovers it, under a free reservation."
    }
    $admitted = New-KwrAdmitted $runId.Value $before.reservationToken $StatePath
    $loss = Get-KwrOwnershipLoss $Convention $admitted
    if ($loss) { throw "This Waking Rune transaction ($($admitted.RunId)) no longer holds the Kingmaker native-test reservation or its journal (lease: $($loss.Lease); journal: $($loss.Journal)); stopping without touching the game or the shared state." }
    Enter-KwrOperationLock $StateRoot
    $loss = Get-KwrOwnershipLoss $Convention $admitted
    if ($loss) { throw "This Waking Rune transaction ($($admitted.RunId)) no longer holds the Kingmaker native-test reservation or its journal (lease: $($loss.Lease); journal: $($loss.Journal)); stopping without touching the game or the shared state." }
    return $admitted
}

# An ownership incident, written once per destination (later ones are kept beside it): expected and observed identity
# (unknown when unreadable), the stage, known differences, what was done and where each resource was left.
function Write-KwrReservationIncident($Convention, [string] $Directory, $Expected, $Loss, [string] $Operation, [string] $Stage, [string[]] $Progress, [string[]] $Differences) {
    [IO.Directory]::CreateDirectory($Directory) | Out-Null
    $observedRecord = 'absent'
    try { if (Test-Path -LiteralPath $Convention.RecordPath) { $observedRecord = [IO.File]::ReadAllText($Convention.RecordPath) } }
    catch { $observedRecord = "unknown (unreadable: $($_.Exception.Message))" }
    $incident = [ordered]@{
        kind = 'Kingmaker native-test reservation or journal ownership lost'; recordedUtc = (Get-Date).ToUniversalTime().ToString('o')
        operation = $Operation; stage = $Stage
        expected = $Expected
        observed = [ordered]@{ lease = $Loss.Lease; journal = $Loss.Journal; record = $observedRecord }
        knownDifferences = @($Differences)
        restorationDone = @($Progress)
        disposition = [ordered]@{
            lease = 'left as observed (not released)'; journal = 'left as observed (not rewritten)'; operationLock = 'released when the operation exits'
            sharedState = "unknown to this transaction after: $Stage"
        }
    }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($incident | ConvertTo-Json -Depth 6))
    $path = Join-Path $Directory 'reservation-incident.json'
    try { $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None) }
    catch [IO.IOException] {
        $path = Join-Path $Directory ('reservation-incident-' + (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8) + '.json')
        $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    }
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    return $path
}

# End, after verified restoration, holding the operation lock: the local close is recorded durably first (the closed
# journal: the journal's run and token plus this closer's PID and start instant, then the journal is deleted), then the
# operation lock is released, then the matching lease is released as the last step; the closed journal is deleted after.
# An interruption leaves the closed journal and the lease; Begin finishes them only once this closer is gone.
function Complete-KwrTransactionReservation($Convention, $Admitted) {
    if (Get-KwrOwnershipLoss $Convention $Admitted) { throw "This Waking Rune transaction ($($Admitted.RunId)) no longer holds its reservation or journal; it is not closed." }
    $closedPath = Get-KwrClosedJournalPath $Admitted.StatePath
    if (Test-Path -LiteralPath $closedPath) { throw "A closed journal from an interrupted close exists ($closedPath); it is left for the owner." }
    $closed = [ordered]@{ runId = $Admitted.RunId; reservationToken = $Admitted.Token; closerProcessId = $PID; closerStartUtc = (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o'); closedUtc = (Get-Date).ToUniversalTime().ToString('o') }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($closed | ConvertTo-Json))
    $stream = [IO.File]::Open($closedPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    Remove-Item -LiteralPath $Admitted.StatePath -Force
    Exit-KwrOperationLock
    Exit-KwrReservation $Convention $Admitted
    # Housekeeping: only this close's own closed journal (a successor may already have removed it, or written its own).
    try { $own = Read-KwrUtf8Json $closedPath; if ((Test-KwrSame (Get-KwrProp $own 'reservationToken') $Admitted.Token)) { Remove-Item -LiteralPath $closedPath -Force } } catch { }
}
