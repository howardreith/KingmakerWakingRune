<#
.SYNOPSIS
  Kingmaker native-test reservation, protocol 1 (docs/NATIVE-RESERVATION.md), as Waking Rune implements it.
.DESCRIPTION
  A machine-wide convention shared by the Kingmaker labs that run native game tests: a named mutex in the Global
  namespace guards a lease record under %ProgramData%. Waking Rune implements it here on its own, from Windows and .NET
  facilities only; it needs no other lab to exist. KwrRuntime.ps1 transactions span separate processes (Begin ... End),
  so the reservation is the lease record, held from Begin until End has verified its restoration; the mutex only makes
  each acquire, adopt and release step atomic.

  Every function takes the convention explicitly. KwrRuntime.ps1 always passes Get-KwrReservationConvention (the
  production identity); scripts/Test-NativeReservation.ps1 passes a private one.
#>
Set-StrictMode -Version Latest

$script:KwrReservationProtocol = 'kingmaker-native-test-reservation/1'
$script:KwrReservationLab = 'KingmakerWakingRune'

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

# The record, or $null when the resources are free. Unreadable or another protocol is ambiguous: refuse, never remove.
function Read-KwrReservationRecord($Convention) {
    if (-not (Test-Path -LiteralPath $Convention.RecordPath)) { return $null }
    $record = $null
    try { $record = Get-Content -LiteralPath $Convention.RecordPath -Raw | ConvertFrom-Json } catch { }
    if (-not $record -or -not ($record.PSObject.Properties.Name -contains 'protocol') -or $record.protocol -ne $script:KwrReservationProtocol) {
        throw "The Kingmaker native-test reservation record is unreadable or of an unknown protocol; it is left for the owner: $($Convention.RecordPath)"
    }
    return $record
}

# Whether the process that wrote a record still runs (same PID and start time, so a reused PID is not it).
function Test-KwrReservationHolderAlive($Record) {
    $process = Get-Process -Id ([int] $Record.holderProcessId) -ErrorAction SilentlyContinue
    if (-not $process) { return $false }
    try { return ($process.StartTime.ToUniversalTime().ToString('o') -eq [string] $Record.holderStartUtc) } catch { return $true }
}

# Reserves the shared resources for a Waking Rune transaction, or refuses naming the holder. Any present record is a
# reservation, whoever wrote it and whether or not its writer still runs. The single adoption Waking Rune allows is its
# own orphan from an interrupted Begin or End: a KingmakerWakingRune record written for this same transaction state
# file ($StatePath, recorded as statePath) whose writer has exited while that state does not exist. Begin writes the
# state before anything shared is touched, and End removes it only after verified restoration, so no state means
# nothing of that transaction is left to restore. A record for another state file (another Waking Rune lab root) is
# never adopted. Without -AdoptOrphan nothing is adopted.
function Enter-KwrReservation($Convention, [string] $RunId, [string] $Purpose, [string] $StatePath, [switch] $AdoptOrphan) {
    $fullStatePath = [IO.Path]::GetFullPath($StatePath)
    return Invoke-KwrUnderReservationMutex $Convention {
        $directory = Split-Path -Parent $Convention.RecordPath
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        if (([IO.File]::GetAttributes($directory) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "The reservation directory is a link: $directory" }
        $existing = Read-KwrReservationRecord $Convention
        if ($existing) {
            $orphan = $AdoptOrphan -and $existing.lab -eq $script:KwrReservationLab -and
                $existing.PSObject.Properties['statePath'] -and [string]::Equals([string] $existing.statePath, $fullStatePath, [StringComparison]::OrdinalIgnoreCase) -and
                -not (Test-KwrReservationHolderAlive $existing) -and -not (Test-Path -LiteralPath $fullStatePath)
            if (-not $orphan) {
                throw "Kingmaker native-test resources are reserved by $($existing.lab) run $($existing.runId) ($($existing.purpose); process $($existing.holderProcessId); since $($existing.createdUtc)); $($Convention.RecordPath)"
            }
        }
        $record = [ordered]@{
            protocol = $script:KwrReservationProtocol; lab = $script:KwrReservationLab; runId = $RunId; purpose = $Purpose
            token = [guid]::NewGuid().ToString('N'); holderProcessId = $PID
            holderStartUtc = (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o'); host = $env:COMPUTERNAME
            createdUtc = (Get-Date).ToUniversalTime().ToString('o'); statePath = $fullStatePath
            adopted = $(if ($existing) { [ordered]@{ runId = $existing.runId; token = $existing.token; holderProcessId = $existing.holderProcessId } } else { $null })
        }
        $temporary = $Convention.RecordPath + '.' + $record.token + '.tmp'
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($record | ConvertTo-Json -Depth 4))
        $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
        if ($existing) { [IO.File]::Replace($temporary, $Convention.RecordPath, [NullString]::Value) }
        else { [IO.File]::Move($temporary, $Convention.RecordPath) }
        [pscustomobject] $record
    }
}

function Test-KwrReservationHeld($Convention, [string] $Token) {
    if (-not $Token) { return $false }
    try { $record = Read-KwrReservationRecord $Convention } catch { return $false }
    return ($null -ne $record -and $record.lab -eq $script:KwrReservationLab -and $record.token -eq $Token)
}

# A lost or replaced reservation is an incident: the operation stops, the record and the transaction state stay.
function Assert-KwrReservationHeld($Convention, [string] $Token) {
    if (Test-KwrReservationHeld $Convention $Token) { return }
    $holder = 'nobody'
    try { $record = Read-KwrReservationRecord $Convention; if ($record) { $holder = "$($record.lab) run $($record.runId)" } } catch { $holder = 'an unreadable record' }
    throw "This Waking Rune transaction no longer holds the Kingmaker native-test reservation (now: $holder); stopping without touching the game or the shared state. Inspect $($Convention.RecordPath)."
}

# Releases a reservation this transaction holds; a record that is no longer its own is left alone and reported.
function Exit-KwrReservation($Convention, [string] $Token) {
    Invoke-KwrUnderReservationMutex $Convention {
        $record = Read-KwrReservationRecord $Convention
        if (-not $record -or $record.lab -ne $script:KwrReservationLab -or $record.token -ne $Token) {
            $holder = if ($record) { "$($record.lab) run $($record.runId)" } else { 'nobody' }
            throw "The Kingmaker native-test reservation is no longer this transaction's (now: $holder); it is left alone."
        }
        Remove-Item -LiteralPath $Convention.RecordPath -Force
    }
}

# --- KwrRuntime.ps1 transaction admission (the transaction state file is Waking Rune's own lock) --------------------

function Read-KwrTransactionState([string] $StatePath) {
    if (-not (Test-Path -LiteralPath $StatePath)) { return $null }
    return Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
}

# Begin: the reservation first (orphan adoption only), then the caller checks and writes its own state.
function Enter-KwrTransactionReservation($Convention, [string] $StatePath, [string] $RunId) {
    return Enter-KwrReservation $Convention $RunId 'transaction' $StatePath -AdoptOrphan
}

# Every later operation on an open transaction: the state's token must still own the record. A transaction opened
# before this protocol (no token in its state) joins it now, but only when nobody holds the reservation. Returns the
# token, or $null when no transaction is open (nothing to own).
function Assert-KwrTransactionReservation($Convention, [string] $StatePath) {
    $state = Read-KwrTransactionState $StatePath
    if (-not $state) { return $null }
    if ($state.PSObject.Properties['reservationToken'] -and $state.reservationToken) {
        Assert-KwrReservationHeld $Convention ([string] $state.reservationToken)
        return [string] $state.reservationToken
    }
    $record = Enter-KwrReservation $Convention ([string] $state.runId) 'transaction (joined)' $StatePath
    $state | Add-Member -NotePropertyName reservationToken -NotePropertyValue $record.token -Force
    $state | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $StatePath -Encoding UTF8
    return $record.token
}

# End, after its restoration verified: own lock first (the state), the reservation last.
function Complete-KwrTransactionReservation($Convention, [string] $StatePath, [string] $Token) {
    Assert-KwrReservationHeld $Convention $Token
    Remove-Item -LiteralPath $StatePath -Force
    Exit-KwrReservation $Convention $Token
}
