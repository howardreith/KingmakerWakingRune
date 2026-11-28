<#
.SYNOPSIS
  Guarded runtime transactions for testing KingmakerWakingRune against the real game.

.DESCRIPTION
  Begin    Preflight (no game running, Steam running with Kingmaker cloud sync disabled, no open
           transaction), snapshot the Kingmaker profile (saves, portraits, achievements...), the
           Kingmaker registry key, UMM Params.xml and the Mods inventory; then install the package.
  Launch   Start the game through Steam (App 640820) and record the owned process.
  Shot     Screenshot the game window into the run's evidence folder.
  Click / Key / Type / Scroll   Native mouse/keyboard input to the owned game window only.
  Close    Gracefully close the owned game process (bounded); never touches other processes.
  Collect  Copy UMM log, output_log, recon files and KWR_AUTOMATION saves into a new evidence folder.
  End      Restore every snapshot exactly and verify; uninstall unless -KeepInstalled. Requires -RunId of the open
           transaction.
  Status   Print the open transaction (reads only the journal).

  All live state lives under the lab root (two levels above the repository).

  Every transaction holds the Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md, scripts/KwrReservation.ps1)
  from Begin until End has verified its restoration. Begin acquires it before reading or writing anything (Steam
  discovery included), then takes this lab's operation lock and journal. Every other operation except Status (which reads
  only the journal) needs an open transaction whose journal and lease are still the same run, token and journal path,
  and holds the operation lock for its whole duration. The admitted identity is rechecked immediately before every shared
  change and journal update. End records its close, releases the operation lock and releases the lease last. Other Kingmaker labs implementing the same protocol are excluded meanwhile;
  none of them needs to be installed.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('Begin', 'Reinstall', 'Uninstall', 'Launch', 'Shot', 'State', 'Click', 'DoubleClick', 'Drag', 'Hover', 'Key', 'Type', 'Scroll', 'Close', 'Collect', 'End', 'Status', 'Record', 'Fault', 'Command')]
    [string] $Operation,
    [string] $RunId,
    [string] $PackagePath,
    [switch] $Recon,
    # Begin/Reinstall: stage a receipt-backed diagnostic build without the frozen-candidate provenance requirement.
    [switch] $Diagnostic,
    [string[]] $LaunchArgs = @(),
    [string] $Name,
    [int] $X,
    [int] $Y,
    [int] $X2,
    [int] $Y2,
    [string] $Button = 'Left',
    [string] $Keys,
    [string] $Text,
    [int] $Delta = -120,
    [switch] $KeepInstalled,
    [int] $TimeoutSeconds = 180,
    [int] $Seconds = 15,
    [string] $FaultName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$LabRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot '..\..'))
$StateRoot = Join-Path $LabRoot 'runtime-state'
$BackupRoot = Join-Path $LabRoot 'runtime-backups'
$EvidenceRoot = Join-Path $LabRoot 'runtime-evidence'
$StatePath = Join-Path $StateRoot 'transaction.json'
. (Join-Path $PSScriptRoot 'KwrReservation.ps1')
$ReservationConvention = Get-KwrReservationConvention

$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker'
$ModsRoot = Join-Path $GameDir 'Mods'
$ModDir = Join-Path $ModsRoot 'KingmakerWakingRune'
$UmmParams = Join-Path $GameDir 'Kingmaker_Data\Managed\UnityModManager\Params.xml'
$UmmLog = Join-Path $GameDir 'Kingmaker_Data\Managed\UnityModManager\Log.txt'
$ProfileDir = Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker'
$RegKey = 'HKCU\Software\Owlcat Games\Pathfinder Kingmaker'
$SteamExe = 'C:\Program Files (x86)\Steam\steam.exe'
$SteamCloudLog = 'C:\Program Files (x86)\Steam\logs\cloud_log.txt'
$AppId = '640820'
$FfmpegExe = 'ffmpeg'
# How long a launch or recording whose process cannot be confirmed stays pending (Steam may start the game late).
$KwrPendingWindowSeconds = 180
$SteamUserDataRoot = 'C:\Program Files (x86)\Steam\userdata'

Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class KwrInput {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, int data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint f, UIntPtr extra);
    [DllImport("user32.dll")] public static extern short VkKeyScan(char c);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint flags);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
'@
[void][KwrInput]::SetProcessDPIAware()

function Write-Step([string] $m) { Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $m) }

function Get-State {
    if (-not (Test-Path -LiteralPath $StatePath)) { return $null }
    return [IO.File]::ReadAllText($StatePath, [Text.UTF8Encoding]::new($false, $true)).TrimStart([char] 0xFEFF) | ConvertFrom-Json
}

# Published atomically: a temporary file replaces the journal, so a reader sees the previous or the new state.
function Save-State($s) {
    if ($script:KwrAdmitted) { Assert-KwrSharedMutation 'before publishing the journal' }
    New-Item -ItemType Directory -Force -Path $StateRoot | Out-Null
    $temporary = $StatePath + '.tmp'
    [IO.File]::WriteAllText($temporary, ($s | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
    if (Test-Path -LiteralPath $StatePath) { [IO.File]::Replace($temporary, $StatePath, [NullString]::Value) } else { [IO.File]::Move($temporary, $StatePath) }
}

function Get-GameProcesses { @(Get-Process -Name 'Kingmaker' -ErrorAction SilentlyContinue) }

function Get-OwnedProcess($s) {
    if (-not $s -or -not $s.gamePid) { return $null }
    $p = Get-Process -Id $s.gamePid -ErrorAction SilentlyContinue
    if ($p -and $p.ProcessName -eq 'Kingmaker' -and $p.StartTime.ToString('o') -eq $s.gameStart) { return $p }
    return $null
}

function Get-Inventory([string] $root) {
    $result = @{}
    if (-not (Test-Path -LiteralPath $root)) { return $result }
    $full = [IO.Path]::GetFullPath($root).TrimEnd('\')
    foreach ($f in Get-ChildItem -LiteralPath $full -Recurse -File -Force) {
        $rel = $f.FullName.Substring($full.Length + 1)
        $result[$rel] = '{0}|{1}' -f $f.Length, $f.LastWriteTimeUtc.Ticks
    }
    return $result
}

function Compare-Inventory($before, $after) {
    $diff = New-Object System.Collections.Generic.List[string]
    foreach ($k in $before.Keys) {
        if (-not $after.ContainsKey($k)) { $diff.Add("missing:$k") }
        elseif ($after[$k] -ne $before[$k]) { $diff.Add("changed:$k") }
    }
    foreach ($k in $after.Keys) { if (-not $before.ContainsKey($k)) { $diff.Add("added:$k") } }
    return , $diff
}

function Save-Tree([string] $name, [string] $root, [string] $tx) {
    & robocopy.exe $root (Join-Path $tx $name) /MIR /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy snapshot of $name failed ($LASTEXITCODE)" }
    $inv = Get-Inventory $root
    $d = Compare-Inventory $inv (Get-Inventory (Join-Path $tx $name))
    if ($d.Count -gt 0) { throw "$name snapshot does not match source: $($d[0..([Math]::Min(5,$d.Count)-1)] -join '; ')" }
    $inv | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $tx "$name-inventory.json") -Encoding UTF8
    Write-Step ("Snapshot verified: {0} {1} files." -f $inv.Count, $name)
}

# Quarantines files the test added and restores anything changed or removed, then verifies.
function Restore-Tree([string] $name, [string] $root, [string] $tx) {
    $baseline = @{}
    (Get-Content -LiteralPath (Join-Path $tx "$name-inventory.json") -Raw | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $baseline[$_.Name] = $_.Value }
    $quarantine = Join-Path (Join-Path $tx 'quarantine') $name
    $diff = Compare-Inventory $baseline (Get-Inventory $root)
    foreach ($entry in $diff) {
        $kind, $rel = $entry.Split(':', 2)
        $live = Join-Path $root $rel
        $saved = Join-Path (Join-Path $tx $name) $rel
        if ($kind -eq 'added') {
            $dest = Join-Path $quarantine $rel
            New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
            Move-Item -LiteralPath $live -Destination $dest -Force
        }
        else {
            New-Item -ItemType Directory -Force -Path (Split-Path $live) | Out-Null
            Copy-Item -LiteralPath $saved -Destination $live -Force
            (Get-Item -LiteralPath $live).LastWriteTimeUtc = (Get-Item -LiteralPath $saved).LastWriteTimeUtc
        }
    }
    $after = Compare-Inventory $baseline (Get-Inventory $root)
    if ($after.Count -gt 0) { throw "$name restore incomplete: $($after -join '; ')" }
    Write-Step ("{0} restored: {1} differences reverted (added files quarantined in {2})." -f $name, $diff.Count, $quarantine)
}

function Export-Registry([string] $path) {
    & reg.exe export $RegKey $path /y | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "reg export failed ($LASTEXITCODE)" }
}

function Assert-SteamCloudOffline {
    $steam = @(Get-Process -Name 'steam' -ErrorAction SilentlyContinue)
    if ($steam.Count -ne 1) { throw "Expected exactly one running Steam client; found $($steam.Count)." }
    $last = Get-Content -LiteralPath $SteamCloudLog -Tail 4000 | Where-Object { $_ -match "\[AppID $AppId\].*(Starting sync|Failed sync|Upload|Download|Sync)" } | Select-Object -Last 1
    if (-not $last -or $last -notmatch 'Sync Disabled') { throw "Steam Cloud for $AppId is not proven disabled. Last entry: $last" }
    Write-Step "Steam cloud preflight: $last"
}

# Every install path resolves the caller's package once, validates a private copy (scripts\KwrPackage.ps1: files,
# manifest, assembly definition and the DLL's actual AssemblyRefs; with a build receipt beside the zip, also the clean
# producing commit and current build inputs) and installs that same copy. The state records the resolved source path and
# the validated digest captured before installing; the private copy is removed in every outcome.
. (Join-Path $PSScriptRoot 'KwrPackage.ps1')

function Get-RunDir($s) { Join-Path $EvidenceRoot $s.runId }

# The single Steam account's Kingmaker remote-storage mirror. Called only by an admitted Begin or End, never at load.
function Get-SteamMirror {
    $found = @(Get-ChildItem -LiteralPath $SteamUserDataRoot -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "$AppId\remote") })
    if ($found.Count -ne 1) { throw "Expected exactly one Steam user with a Kingmaker save mirror; found $($found.Count)." }
    return [IO.Path]::GetFullPath((Join-Path $found[0].FullName "$AppId\remote"))
}

# The trees a transaction snapshots: the profile and the Steam mirror Begin selected (recorded in the journal).
# The game mirrors saves via Steam remote storage and restores missing local saves from it.
# remotecache.vdf beside it is Steam-maintained and rewritten on every launch; it is not restored.
function Get-SnapshotTrees($s) {
    return @(@{ Name = 'profile'; Path = $ProfileDir }, @{ Name = 'steam-remote'; Path = [string] $s.steamMirror })
}

# Every baseline file End relies on.
function Get-KwrBaselineFiles($s) {
    $files = @('profile-inventory.json', 'profile-sha256.json', 'steam-remote-inventory.json', 'steam-remote-sha256.json', 'registry.reg', 'Params.xml', 'mods-inventory.json')
    if ($s.existingMod) { $files += @('mod-existing-inventory.json', 'mod-existing-sha256.json') }
    return $files
}

function Get-Sha256([string] $Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }

# Immediately before every shared change: the journal and lease must still be the admitted run, token and journal path.
# A loss of either is recorded as the run's ownership incident (the run was verified at admission, so its evidence path is
# contained) and stops the operation.
function Assert-KwrSharedMutation([string] $Stage) {
    $loss = Get-KwrOwnershipLoss $ReservationConvention $script:KwrAdmitted
    if (-not $loss) { return }
    $expected = [ordered]@{ lab = 'KingmakerWakingRune'; runId = $script:KwrAdmitted.RunId; token = $script:KwrAdmitted.Token; statePath = $script:KwrAdmitted.StatePath }
    $incident = Write-KwrReservationIncident $ReservationConvention (Join-Path $EvidenceRoot $script:KwrAdmitted.RunId) $expected $loss $script:KwrOperation $Stage $script:KwrProgress.ToArray() $script:KwrDifferences.ToArray()
    throw "This Waking Rune transaction ($($script:KwrAdmitted.RunId)) no longer holds the Kingmaker native-test reservation or its journal (lease: $($loss.Lease); journal: $($loss.Journal)) $Stage; nothing more was changed. Incident: $incident"
}

# Work the transaction starts (a launch, a recording) is journaled before it is dispatched, then its process identity.
# It stays outstanding until an affirmative outcome: a launch until the game was observed, or its launcher has exited and
# the pending window has passed; a recording until its recorder process has exited (or, with no process recorded, the
# window has passed).
function Get-KwrOutstanding($s) { return @(Get-KwrProp $s 'outstanding' | Where-Object { $null -ne $_ }) }

function Set-KwrOutstanding($s, [object[]] $Entries) {
    if ($s -is [System.Collections.IDictionary]) { $s['outstanding'] = @($Entries) }
    else { $s | Add-Member -NotePropertyName outstanding -NotePropertyValue @($Entries) -Force }
    Save-State $s
}

function Add-KwrOutstandingWork($s, [string] $Kind) {
    $entry = [pscustomobject]@{ id = [guid]::NewGuid().ToString('N').Substring(0, 12); kind = $Kind; requestedUtc = (Get-Date).ToUniversalTime().ToString('o'); pid = $null; startUtc = $null; resolved = $false }
    Set-KwrOutstanding $s (@(Get-KwrOutstanding $s) + @($entry))
    return $entry.id
}

function Update-KwrOutstanding($s, [string] $Id, [scriptblock] $Change) {
    $entries = @(Get-KwrOutstanding $s)
    foreach ($entry in $entries) { if (Test-KwrSame ([string] $entry.id) $Id) { & $Change $entry } }
    Set-KwrOutstanding $s $entries
}

function Get-KwrOutstandingWork($s) {
    $now = [DateTimeOffset]::UtcNow
    return @(Get-KwrOutstanding $s | Where-Object {
        $entry = $_
        if ($entry.resolved -eq $true) { return $false }
        $alive = $null -ne $entry.pid -and (Test-KwrWriterAlive $entry.pid ([string] $entry.startUtc))
        $requested = ConvertTo-KwrInstant ([string] $entry.requestedUtc)
        $windowOpen = $null -eq $requested -or $now -lt $requested.AddSeconds($KwrPendingWindowSeconds)
        if ($entry.kind -eq 'recorder' -and $null -ne $entry.pid) { return $alive }
        return ($alive -or $windowOpen)
    })
}

# SHA-256 of every file of a tree, by relative path.
function Get-KwrContentManifest([string] $Root) {
    $manifest = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return $manifest }
    $full = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    foreach ($f in @(Get-ChildItem -LiteralPath $full -Recurse -File -Force | Sort-Object FullName)) { $manifest[$f.FullName.Substring($full.Length + 1)] = Get-Sha256 $f.FullName }
    return $manifest
}

function Test-KwrContentManifest([string] $Root, [string] $ManifestPath) {
    $expected = [ordered]@{}
    (Read-KwrUtf8Json $ManifestPath).PSObject.Properties | ForEach-Object { $expected[$_.Name] = [string] $_.Value }
    $actual = Get-KwrContentManifest $Root
    if ($expected.Count -ne $actual.Count) { return $false }
    foreach ($key in $expected.Keys) { if (-not $actual.Contains($key) -or -not (Test-KwrSame $actual[$key] $expected[$key])) { return $false } }
    return $true
}

function Write-KwrJsonFile([string] $Path, $Value) { [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false)) }

function Invoke-Begin {
    if (-not (Test-KwrRunId $RunId)) { throw 'A -RunId of 3-80 safe characters is required.' }
    if (-not $PackagePath) { throw 'A -PackagePath to the built zip is required.' }
    # The neutral reservation comes first, then this lab's operation lock and journal; nothing shared is read or written
    # before. Until the journal is saved nothing shared has changed, so a refusal or failure releases in reverse order
    # (operation lock, then lease); afterwards the lease stays with the open transaction for End. Nothing shared changes
    # before the journal says 'snapshotted'.
    $lease = Enter-KwrTransactionReservation $ReservationConvention $StatePath $RunId
    $stateSaved = $false
    try {
    Enter-KwrOperationLock $StateRoot
    if (Get-State) { throw "A transaction is already open: $StatePath. Run End (or inspect) first." }
    $closedJournal = Get-KwrClosedJournalPath $StatePath
    if (Test-Path -LiteralPath $closedJournal) { throw "A closed journal from an interrupted close exists ($closedJournal); it is left for the owner." }
    if ((Test-Path -LiteralPath $ModDir) -and -not (Test-Path -LiteralPath $ModDir -PathType Container)) { throw "$ModDir exists but is not a folder; refusing before any change." }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'Kingmaker is running; it may be an owner session. Refusing to start.' }
    $validated = New-KwrValidatedPackageCopy $PackagePath -Diagnostic:$Diagnostic
    try {
    Assert-SteamCloudOffline
    $mirror = Get-SteamMirror

    $tx = Join-Path $BackupRoot $RunId
    $run = Join-Path $EvidenceRoot $RunId
    if ((Test-Path $tx) -or (Test-Path $run)) { throw "RunId $RunId was already used." }
    New-Item -ItemType Directory -Force -Path $tx, $run | Out-Null
    $s = [ordered]@{ runId = $RunId; createdUtc = (Get-Date).ToUniversalTime().ToString('o'); phase = 'snapshotting'; gamePid = $null; gameStart = $null; package = $validated.Source; packageSha256 = $validated.Sha256; dllSha256 = $validated.DllSha256; sourceProvenance = $validated.SourceProvenance; installMode = $validated.InstallMode; recon = [bool]$Recon; reservationToken = $lease.Token; steamMirror = $mirror }
    Save-State $s
    $stateSaved = $true
    $script:KwrAdmitted = $lease

    Write-Step 'Snapshotting Kingmaker profile and Steam save mirror...'
    foreach ($t in (Get-SnapshotTrees $s)) {
        Save-Tree $t.Name $t.Path $tx
        Write-KwrJsonFile (Join-Path $tx "$($t.Name)-sha256.json") (Get-KwrContentManifest (Join-Path $tx $t.Name))
    }
    Export-Registry (Join-Path $tx 'registry.reg')
    Copy-Item -LiteralPath $UmmParams -Destination (Join-Path $tx 'Params.xml')
    (Get-Inventory $ModsRoot) | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $tx 'mods-inventory.json') -Encoding UTF8
    $s.ummLogLength = (Get-Item -LiteralPath $UmmLog).Length

    # An existing install (the owner's accepted build, possibly an empty folder) is preserved exactly and put back by End.
    # Its snapshot and inventory are complete before the journal says 'snapshotted' and anything shared is changed.
    $s.existingMod = Test-Path -LiteralPath $ModDir -PathType Container
    if ($s.existingMod) {
        & robocopy.exe $ModDir (Join-Path $tx 'mod-existing') /MIR /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy snapshot of the existing mod failed ($LASTEXITCODE)" }
        if (-not (Test-Path -LiteralPath (Join-Path $tx 'mod-existing') -PathType Container)) { throw 'The existing mod snapshot folder was not created.' }
        $existingInventory = Get-Inventory $ModDir
        $d = Compare-Inventory $existingInventory (Get-Inventory (Join-Path $tx 'mod-existing'))
        if ($d.Count -gt 0) { throw "Existing mod snapshot does not match: $($d -join '; ')" }
        $existingInventory | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $tx 'mod-existing-inventory.json') -Encoding UTF8
        Write-KwrJsonFile (Join-Path $tx 'mod-existing-sha256.json') (Get-KwrContentManifest (Join-Path $tx 'mod-existing'))
    }
    # The complete baseline set, with independent integrity evidence for each file, before anything shared changes.
    $digests = [ordered]@{}
    foreach ($file in (Get-KwrBaselineFiles $s)) {
        if (-not (Test-Path -LiteralPath (Join-Path $tx $file) -PathType Leaf)) { throw "Baseline $file was not captured; nothing shared was changed (End closes this transaction)." }
        $digests[$file] = Get-Sha256 (Join-Path $tx $file)
    }
    $s.baselineDigests = $digests
    $s.phase = 'snapshotted'
    Save-State $s

    if ($s.existingMod) {
        Assert-KwrSharedMutation 'before setting the existing mod aside'
        Remove-Item -LiteralPath $ModDir -Recurse -Force
        Write-Step 'Existing mod install snapshotted and set aside.'
    }

    # Temporary test profile: suppress the UMM window at start (restored by End).
    Assert-KwrSharedMutation 'before changing UMM Params.xml'
    [xml] $params = Get-Content -LiteralPath $UmmParams -Raw
    $params.Param.ShowOnStart = '0'
    $params.Save($UmmParams)

    Assert-KwrSharedMutation 'before installing the candidate'
    Install-KwrValidatedPackageCopy $validated $ModsRoot
    $s.candidateFiles = [ordered]@{ 'Info.json' = Get-Sha256 (Join-Path $ModDir 'Info.json'); 'KingmakerWakingRune.dll' = Get-Sha256 (Join-Path $ModDir 'KingmakerWakingRune.dll') }
    if ($Recon) { Set-Content -LiteralPath (Join-Path $ModDir 'recon.flag') -Value 'recon' }
    Set-Content -LiteralPath (Join-Path $ModDir 'capture.flag') -Value 'harness'
    $s.phase = 'installed'
    Save-State $s
    Write-Step "Installed package $($s.packageSha256) into $ModDir (recon=$([bool]$Recon))."
    }
    finally { Remove-KwrValidatedPackageCopy $validated }
    }
    catch {
        if (-not $stateSaved) {
            Exit-KwrOperationLock
            try { Exit-KwrReservation $ReservationConvention $lease }
            catch { Write-Step "WARNING: releasing the reservation after a refused Begin failed: $($_.Exception.Message)" }
        }
        throw
    }
}

function Invoke-Reinstall {
    $s = Get-State
    # 'reinstalling' (an interrupted reinstall) is resumed here; End restores the transaction in every phase.
    if (-not $s -or $s.phase -notin @('installed', 'closed', 'reinstalling')) { throw 'Reinstall needs an open transaction with the game closed.' }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'Kingmaker is running; never hot-replace the DLL.' }
    if (-not $PackagePath) { throw 'A -PackagePath is required.' }
    $validated = New-KwrValidatedPackageCopy $PackagePath -Diagnostic:$Diagnostic
    try {
        # An interrupted reinstall resumes with the recon setting its journal recorded: reconEnabled, or, in a journal
        # written before reconEnabled existed, Begin's recon field. The folder found then may be gone or may be a placed
        # replacement without recon.flag, so it is not consulted. An ordinary reinstall reads recon.flag of the current
        # installation.
        $recon = if ($s.phase -eq 'reinstalling' -and $s.PSObject.Properties['reconEnabled']) { [bool]$s.reconEnabled }
            elseif ($s.phase -eq 'reinstalling' -and $s.PSObject.Properties['recon']) { [bool]$s.recon }
            else { Test-Path -LiteralPath (Join-Path $ModDir 'recon.flag') }
        # Stage and verify the new folder first; then mark the transaction non-launchable before touching the installed
        # mod, so a failure leaves a 'reinstalling' transaction that Launch refuses and End restores.
        $stagedMod = Expand-KwrValidatedPackageCopy $validated
        $previousPhase = if ($s.phase -eq 'reinstalling') { $(if ($s.PSObject.Properties['resumePhase'] -and [string]$s.resumePhase -in @('installed', 'closed')) { [string]$s.resumePhase } else { 'closed' }) } else { [string]$s.phase }
        $s | Add-Member -NotePropertyName resumePhase -NotePropertyValue $previousPhase -Force
        $s | Add-Member -NotePropertyName reconEnabled -NotePropertyValue ([bool]$recon) -Force
        $s.phase = 'reinstalling'
        Save-State $s
        Assert-KwrSharedMutation 'before replacing the installed mod'
        if (Test-Path -LiteralPath $ModDir) { Remove-Item -LiteralPath $ModDir -Recurse -Force }
        Assert-KwrSharedMutation 'before placing the new mod'
        Move-Item -LiteralPath $stagedMod -Destination $ModDir
        if ((Get-FileHash -LiteralPath (Join-Path $ModDir 'KingmakerWakingRune.dll') -Algorithm SHA256).Hash.ToLowerInvariant() -cne $validated.DllSha256) { throw 'Installed DLL differs from the validated package.' }
        if ($recon) { Set-Content -LiteralPath (Join-Path $ModDir 'recon.flag') -Value 'recon' }
        Set-Content -LiteralPath (Join-Path $ModDir 'capture.flag') -Value 'harness'
        # Recorded from the identities captured before installing (never re-resolved from the caller's path).
        $s.package = $validated.Source
        $s.packageSha256 = $validated.Sha256
        $s | Add-Member -NotePropertyName dllSha256 -NotePropertyValue $validated.DllSha256 -Force
        $s | Add-Member -NotePropertyName candidateFiles -NotePropertyValue ([ordered]@{ 'Info.json' = (Get-FileHash -LiteralPath (Join-Path $ModDir 'Info.json') -Algorithm SHA256).Hash.ToLowerInvariant(); 'KingmakerWakingRune.dll' = (Get-FileHash -LiteralPath (Join-Path $ModDir 'KingmakerWakingRune.dll') -Algorithm SHA256).Hash.ToLowerInvariant() }) -Force
        $s | Add-Member -NotePropertyName sourceProvenance -NotePropertyValue $validated.SourceProvenance -Force
        $s | Add-Member -NotePropertyName installMode -NotePropertyValue $validated.InstallMode -Force
        $s.phase = $previousPhase
        $s.PSObject.Properties.Remove('resumePhase')
        Save-State $s
        Write-Step "Reinstalled package $($s.packageSha256) (recon=$recon)."
    }
    finally { Remove-KwrValidatedPackageCopy $validated }
}

function Invoke-Uninstall {
    $s = Get-State
    if (-not $s -or $s.phase -notin @('installed', 'closed')) { throw 'Uninstall needs an open transaction with the game closed.' }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'Kingmaker is running.' }
    Assert-KwrSharedMutation 'before removing the installed mod'
    if (Test-Path -LiteralPath $ModDir) { Remove-Item -LiteralPath $ModDir -Recurse -Force }
    Write-Step 'Mod folder removed for a mod-absent run (End still restores everything).'
}

function Invoke-Launch {
    $s = Get-State
    if (-not $s -or $s.phase -notin @('installed', 'closed')) { throw 'No installed transaction ready to launch.' }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'Kingmaker is already running.' }
    Assert-SteamCloudOffline
    $since = Get-Date
    $argList = @('-applaunch', $AppId) + $LaunchArgs
    $work = Add-KwrOutstandingWork $s 'launch'
    Assert-KwrSharedMutation 'before launching the game'
    $launcher = Start-Process -FilePath $SteamExe -ArgumentList $argList -PassThru
    Update-KwrOutstanding $s $work { param($e) $e.pid = $launcher.Id; $e.startUtc = $launcher.StartTime.ToUniversalTime().ToString('o') }
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $p = $null
    while ((Get-Date) -lt $deadline -and -not $p) {
        Start-Sleep -Seconds 2
        $p = Get-GameProcesses | Where-Object { $_.StartTime -ge $since.AddSeconds(-5) } | Select-Object -First 1
    }
    if (-not $p) { throw 'Kingmaker did not start within the timeout; the launch stays pending until its launcher has exited and its window has passed.' }
    while ((Get-Date) -lt $deadline -and $p.MainWindowHandle -eq [IntPtr]::Zero) { Start-Sleep -Seconds 1; $p.Refresh() }
    $s.gamePid = $p.Id
    $s.gameStart = $p.StartTime.ToString('o')
    $s.phase = 'running'
    Update-KwrOutstanding $s $work { param($e) $e.resolved = $true }
    Write-Step "Owned game process pid=$($p.Id) args='$($LaunchArgs -join ' ')'."
}

function Get-GameWindow {
    $s = Get-State
    $p = Get-OwnedProcess $s
    if (-not $p) { throw 'No owned game process is running.' }
    $p.Refresh()
    $r = New-Object KwrInput+RECT
    [void][KwrInput]::GetWindowRect($p.MainWindowHandle, [ref] $r)
    return @{ Process = $p; Rect = $r; State = $s }
}

# Input is only ever sent while the owned game window is verifiably in the foreground;
# otherwise the operation aborts so nothing lands in another application.
function Assert-GameForeground($w) {
    $h = $w.Process.MainWindowHandle
    if ([KwrInput]::IsIconic($h)) { [void][KwrInput]::ShowWindow($h, 9) }
    for ($i = 0; $i -lt 5 -and [KwrInput]::GetForegroundWindow() -ne $h; $i++) {
        # A synthetic ALT tap lets this process take foreground rights.
        Send-Vk 0x12 $false; Send-Vk 0x12 $true
        [void][KwrInput]::SetForegroundWindow($h)
        Start-Sleep -Milliseconds 300
    }
    if ([KwrInput]::GetForegroundWindow() -ne $h) { throw 'The game window is not in the foreground; no input was sent.' }
    $r = New-Object KwrInput+RECT
    [void][KwrInput]::GetWindowRect($h, [ref] $r)
    $w.Rect = $r
}

# The game presents directly to the display, so screen capture sees the desktop instead of the game.
# The mod's harness-only probe (capture.flag) writes Unity's own frame; nothing else can be captured.
function Request-Probe([string] $requestFile, [string] $target, [int] $timeoutSeconds = 30) {
    Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue
    [IO.File]::WriteAllText((Join-Path $ModDir $requestFile), $target)
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path -LiteralPath $target) -and (Get-Item -LiteralPath $target).Length -gt 0) {
            Start-Sleep -Milliseconds 200
            return
        }
        Start-Sleep -Milliseconds 100
    }
    throw "The game did not answer $requestFile within $timeoutSeconds seconds."
}

function Invoke-Shot {
    $w = Get-GameWindow
    Assert-GameForeground $w
    $label = if ($Name) { $Name } else { 'shot' }
    $file = Join-Path (Get-RunDir $w.State) ('{0:HHmmss}-{1}.png' -f (Get-Date), $label)
    Request-Probe 'capture.request' $file
    Write-Output $file
}

function Invoke-State {
    $w = Get-GameWindow
    Assert-GameForeground $w
    $label = if ($Name) { $Name } else { 'state' }
    $file = Join-Path (Get-RunDir $w.State) ('{0:HHmmss}-{1}.txt' -f (Get-Date), $label)
    Request-Probe 'state.request' $file
    Get-Content -LiteralPath $file
}

# Input lands on whatever window is under the cursor, so that window must be the game.
function Assert-GameAtPoint($w, [int] $px, [int] $py) {
    $point = New-Object KwrInput+POINT
    $point.X = $w.Rect.Left + $px; $point.Y = $w.Rect.Top + $py
    if ([KwrInput]::GetAncestor([KwrInput]::WindowFromPoint($point), 2) -ne $w.Process.MainWindowHandle) { throw "The game window is not under ($px,$py); no input was sent." }
}

# Desktop Duplication sees what the game presents, which GDI capture does not. Only the game window's rectangle is grabbed.
function Invoke-Record {
    $w = Get-GameWindow
    Assert-GameForeground $w
    $label = if ($Name) { $Name } else { 'recording' }
    $file = Join-Path (Get-RunDir $w.State) ('{0:HHmmss}-{1}.mp4' -f (Get-Date), $label)
    $r = $w.Rect
    $grab = 'ddagrab=framerate=30:offset_x={0}:offset_y={1}:video_size={2}x{3},hwdownload,format=bgra,scale=1280:720,format=yuv420p' -f $r.Left, $r.Top, ($r.Right - $r.Left), ($r.Bottom - $r.Top)
    $arguments = @('-hide_banner', '-loglevel', 'error', '-y', '-init_hw_device', 'd3d11va', '-filter_complex', $grab, '-t', "$Seconds", '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '18', "`"$file`"")
    $s = Get-State
    $work = Add-KwrOutstandingWork $s 'recorder'
    $recorder = Start-Process -FilePath $FfmpegExe -ArgumentList $arguments -WindowStyle Hidden -PassThru
    Update-KwrOutstanding $s $work { param($e) $e.pid = $recorder.Id; $e.startUtc = $recorder.StartTime.ToUniversalTime().ToString('o') }
    Write-Output $file
}

# Arms one labelled test fault; the mod consumes it only while capture.flag exists and logs it as injected.
function Invoke-Fault {
    $s = Get-State
    if (-not $s) { throw 'No open transaction.' }
    if ($FaultName -notin @('save-refused', 'save-write-error', 'save-readback', 'save-stale', 'save-delay', 'save-delay-write-error', 'build-failed', 'arrival-invalid', 'position-invalid')) { throw "Unknown fault '$FaultName'." }
    # Only the transaction's own installed test deployment takes a fault.
    if ($s.phase -notin @('installed', 'closed', 'running') -or -not (Test-Path -LiteralPath (Join-Path $ModDir 'capture.flag') -PathType Leaf)) { throw "Fault needs this transaction's installed test deployment (phase $($s.phase))." }
    Assert-KwrSharedMutation 'before arming a fault'
    [IO.File]::WriteAllText((Join-Path $ModDir 'fault.request'), $FaultName)
    Write-Step "Fault armed: $FaultName"
}

# Sends one labelled test command through the mod's harness probe and returns its text response.
function Invoke-Command {
    $s = Get-State
    if (-not (Get-OwnedProcess $s)) { throw 'No owned game process is running.' }
    $file = Join-Path (Get-RunDir $s) ('{0:HHmmss_fff}-cmd.txt' -f (Get-Date))
    Assert-KwrSharedMutation 'before sending a harness command'
    [IO.File]::WriteAllText((Join-Path $ModDir 'command.request'), "$file`n$Text")
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path -LiteralPath $file) -and (Get-Item -LiteralPath $file).Length -gt 0) {
            Start-Sleep -Milliseconds 100
            Get-Content -LiteralPath $file
            return
        }
        Start-Sleep -Milliseconds 100
    }
    throw "The game did not answer command: $Text"
}

function Invoke-Click {
    $w = Get-GameWindow
    Assert-GameForeground $w
    Assert-GameAtPoint $w $X $Y
    Start-Sleep -Milliseconds 150
    [void][KwrInput]::SetCursorPos($w.Rect.Left + $X, $w.Rect.Top + $Y)
    Start-Sleep -Milliseconds 150
    $down, $up = if ($Button -eq 'Right') { 0x0008, 0x0010 } else { 0x0002, 0x0004 }
    [KwrInput]::mouse_event($down, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [KwrInput]::mouse_event($up, 0, 0, 0, [UIntPtr]::Zero)
    Write-Step "$Button click at window ($X,$Y)"
}

function Invoke-DoubleClick {
    $w = Get-GameWindow
    Assert-GameForeground $w
    Assert-GameAtPoint $w $X $Y
    [void][KwrInput]::SetCursorPos($w.Rect.Left + $X, $w.Rect.Top + $Y)
    Start-Sleep -Milliseconds 150
    for ($i = 0; $i -lt 2; $i++) {
        [KwrInput]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 50
        [KwrInput]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 70
    }
    Write-Step "Double click at window ($X,$Y)"
}

function Invoke-Drag {
    $w = Get-GameWindow
    Assert-GameForeground $w
    Assert-GameAtPoint $w $X $Y
    [void][KwrInput]::SetCursorPos($w.Rect.Left + $X, $w.Rect.Top + $Y)
    Start-Sleep -Milliseconds 200
    [KwrInput]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
    for ($i = 1; $i -le 10; $i++) {
        [void][KwrInput]::SetCursorPos($w.Rect.Left + $X + ($X2 - $X) * $i / 10, $w.Rect.Top + $Y + ($Y2 - $Y) * $i / 10)
        Start-Sleep -Milliseconds 40
    }
    Assert-GameAtPoint $w $X2 $Y2
    Start-Sleep -Milliseconds 150
    [KwrInput]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Write-Step "Drag ($X,$Y) -> ($X2,$Y2)"
}

function Invoke-Scroll {
    $w = Get-GameWindow
    Assert-GameForeground $w
    Assert-GameAtPoint $w $X $Y
    [void][KwrInput]::SetCursorPos($w.Rect.Left + $X, $w.Rect.Top + $Y)
    Start-Sleep -Milliseconds 100
    [KwrInput]::mouse_event(0x0800, 0, 0, $Delta, [UIntPtr]::Zero)
    Write-Step "Scroll $Delta at window ($X,$Y)"
}

$VirtualKeys = @{ ESC = 0x1B; ENTER = 0x0D; SPACE = 0x20; TAB = 0x09; BACK = 0x08; DEL = 0x2E; I = 0x49; J = 0x4A; R = 0x52; T = 0x54; C = 0x43; M = 0x4D; W = 0x57; A = 0x41; S = 0x53; D = 0x44; CTRL = 0x11; SHIFT = 0x10; ALT = 0x12; LEFT = 0x25; UP = 0x26; RIGHT = 0x27; DOWN = 0x28 }
foreach ($letter in [char[]]"ABCDEFGHIJKLMNOPQRSTUVWXYZ") { $VirtualKeys["$letter"] = [int][char]$letter }

function Send-Vk([byte] $vk, [bool] $up) { [KwrInput]::keybd_event($vk, 0, $(if ($up) { 2 } else { 0 }), [UIntPtr]::Zero) }

function Invoke-Key {
    $w = Get-GameWindow
    Assert-GameForeground $w
    Start-Sleep -Milliseconds 150
    foreach ($chord in $Keys.Split(' ')) {
        $parts = $chord.Split('+') | ForEach-Object { $_.ToUpperInvariant() }
        # Quicksave and quickload are refused: the game's quick slots rotate across every campaign, so a test quicksave
        # overwrites the owner's oldest quicksave. Test saves are named saves through the harness's own save command.
        foreach ($k in $parts) { if ($k -eq 'F5' -or $k -eq 'F9') { throw "$k (quicksave/quickload) is refused: use the harness's named save and load" } }
        foreach ($k in $parts) { if (-not $VirtualKeys.ContainsKey($k)) { throw "Unknown key $k" }; Send-Vk $VirtualKeys[$k] $false; Start-Sleep -Milliseconds 40 }
        [array]::Reverse($parts)
        foreach ($k in $parts) { Send-Vk $VirtualKeys[$k] $true; Start-Sleep -Milliseconds 40 }
        Start-Sleep -Milliseconds 200
    }
    Write-Step "Keys: $Keys"
}

function Invoke-Type {
    $w = Get-GameWindow
    Assert-GameForeground $w
    Start-Sleep -Milliseconds 150
    foreach ($c in $Text.ToCharArray()) {
        $scan = [KwrInput]::VkKeyScan($c)
        $vk = [byte]($scan -band 0xFF); $shift = ($scan -band 0x100) -ne 0
        if ($shift) { Send-Vk 0x10 $false }
        Send-Vk $vk $false; Send-Vk $vk $true
        if ($shift) { Send-Vk 0x10 $true }
        Start-Sleep -Milliseconds 40
    }
    Write-Step "Typed $($Text.Length) characters"
}

function Invoke-Close {
    $s = Get-State
    $p = Get-OwnedProcess $s
    if (-not $p) {
        if (@(Get-GameProcesses).Count -gt 0) { throw 'A Kingmaker process is running that this transaction does not own; not touching it.' }
        Write-Step 'Owned game process already exited.'
    }
    else {
        Assert-KwrSharedMutation 'before closing the owned game'
        [void]$p.CloseMainWindow()
        if (-not $p.WaitForExit(60000)) {
            Write-Step 'Game did not exit after a close request; terminating the owned process only.'
            Stop-Process -Id $p.Id -Force
            $p.WaitForExit(30000) | Out-Null
        }
        Write-Step "Owned game process $($s.gamePid) exited."
    }
    # Only a running transaction becomes 'closed'; a recovery phase such as 'reinstalling' is kept, so Close never makes
    # an interrupted reinstall launchable.
    if ($s -and $s.phase -eq 'running') { $s.phase = 'closed' }
    if ($s) { $s.gamePid = $null; $s.gameStart = $null; Save-State $s }
}

# Each collection goes to a new folder of the run's evidence, so it never overwrites an earlier one.
function Invoke-Collect {
    $s = Get-State
    if (-not $s) { throw 'No open transaction.' }
    $run = Join-Path (Get-RunDir $s) ('collect-' + (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ'))
    if (Test-Path -LiteralPath $run) { throw "Evidence folder $run already exists; not overwriting it." }
    New-Item -ItemType Directory -Path $run | Out-Null
    $logBytes = [IO.File]::ReadAllBytes($UmmLog)
    $start = [Math]::Min([long]$s.ummLogLength, $logBytes.Length)
    if ($logBytes.Length -lt [long]$s.ummLogLength) { $start = 0 }
    $newLog = [IO.File]::Open((Join-Path $run 'umm-log.txt'), [IO.FileMode]::CreateNew)
    try { $newLog.Write($logBytes, $start, $logBytes.Length - $start) } finally { $newLog.Dispose() }
    $out = Join-Path $ProfileDir 'output_log.txt'
    if (Test-Path -LiteralPath $out) { Copy-Item -LiteralPath $out -Destination (Join-Path $run 'output_log.txt') }
    $recon = Join-Path $ModDir 'recon'
    if (Test-Path -LiteralPath $recon) { Copy-Item -LiteralPath $recon -Destination (Join-Path $run 'recon') -Recurse }
    $saves = Join-Path $run 'saves'
    New-Item -ItemType Directory -Path $saves | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $ProfileDir 'Saved Games') -File | Where-Object Name -like '*KWR_AUTOMATION*' | Copy-Item -Destination $saves
    Write-Step "Collected evidence into $run"
    Get-Content (Join-Path $run 'umm-log.txt') | Where-Object { $_ -match 'KingmakerWakingRune|Exception|\[Manager\].*(Error|error)' } | Select-Object -Last 60
}

# Registry: restore exactly if anything differs (the import itself is Import-KwrRegistry).
function Restore-KwrRegistry([string] $tx) {
    $now = Join-Path $tx 'registry-after.reg'
    Export-Registry $now
    if ((Get-FileHash $now).Hash -ne (Get-FileHash (Join-Path $tx 'registry.reg')).Hash) {
        Import-KwrRegistry (Join-Path $tx 'registry.reg')
        Export-Registry $now
        if ((Get-FileHash $now).Hash -ne (Get-FileHash (Join-Path $tx 'registry.reg')).Hash) { throw 'Registry restore did not verify.' }
        Write-Step 'Registry key restored.'
    }
    else { Write-Step 'Registry key unchanged.' }
}

function Import-KwrRegistry([string] $path) {
    & cmd.exe /c "reg delete `"$RegKey`" /f >nul 2>&1"
    & cmd.exe /c "reg import `"$path`" >nul 2>&1"
}

function Invoke-End {
    $s = Get-State
    if (-not $s) { throw 'No open transaction.' }
    if (-not (Test-KwrSame $RunId (Get-KwrProp $s 'runId'))) { throw "End needs -RunId of the open transaction ($(Get-KwrProp $s 'runId')); refusing." }
    $legacy = [bool] (Get-KwrProp $s 'legacyRecovery')
    if (Get-OwnedProcess $s) { throw 'The owned game is still running; Close it first.' }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'A Kingmaker process is running; refusing to restore underneath it.' }
    $outstanding = @(Get-KwrOutstandingWork $s)
    if ($outstanding.Count -gt 0) { throw "Work this transaction started is still outstanding ($(@($outstanding | ForEach-Object { "$($_.kind) $($_.pid)" }) -join ', ')); End waits for it. The journal and the reservation stay." }
    $tx = Join-Path $BackupRoot $s.runId
    $run = Get-RunDir $s
    try { Invoke-Collect | Out-Null } catch { Write-Step "Collect before restore failed: $_" }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'A Kingmaker process appeared during collection; refusing to restore underneath it.' }

    if ($s.phase -eq 'snapshotting' -and -not $legacy) {
        # Begin stopped before the journal said 'snapshotted': nothing shared was changed, so nothing is restored.
        Assert-KwrSharedMutation 'before closing an unmutated transaction'
        Complete-KwrTransactionReservation $ReservationConvention $script:KwrAdmitted
        Write-Step "Transaction $($s.runId) closed before any shared change; the Kingmaker native-test reservation released. Evidence: $run"
        return
    }

    # Every baseline End relies on is verified before the first restore write: present, equal to the digest Begin
    # recorded, and each snapshot tree equal in content (SHA-256 per file) to the manifest Begin wrote and to its
    # inventory. A previous-format transaction recorded no digests or manifests; its baseline files must be present and
    # its snapshot trees equal to their inventories. Otherwise nothing is restored and the journal and reservation stay.
    $problems = @()
    $required = if ($legacy) { @('profile-inventory.json', 'steam-remote-inventory.json', 'registry.reg', 'Params.xml', 'mods-inventory.json') } else { @(Get-KwrBaselineFiles $s) }
    $digests = Get-KwrProp $s 'baselineDigests'
    foreach ($file in $required) {
        $path = Join-Path $tx $file
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $problems += "$file missing"; continue }
        if (-not $legacy -and -not (Test-KwrSame (Get-Sha256 $path) ([string] (Get-KwrProp $digests $file)))) { $problems += "$file differs from its recorded digest" }
    }
    if ($problems.Count -eq 0) {
        $trees = @((Get-SnapshotTrees $s) | ForEach-Object { $_.Name })
        if ($s.existingMod) { $trees += 'mod-existing' }
        foreach ($name in $trees) {
            $saved = Join-Path $tx $name
            if (-not (Test-Path -LiteralPath $saved -PathType Container)) { $problems += "$name snapshot folder missing"; continue }
            if ($name -eq 'mod-existing' -and $legacy) { continue }
            $expected = @{}
            (Read-KwrUtf8Json (Join-Path $tx "$name-inventory.json")).PSObject.Properties | ForEach-Object { $expected[$_.Name] = $_.Value }
            $d = Compare-Inventory $expected (Get-Inventory $saved)
            if ($d.Count -gt 0) { $problems += "$name snapshot differs from its inventory ($($d[0..([Math]::Min(3, $d.Count) - 1)] -join '; '))" }
            elseif (-not $legacy -and -not (Test-KwrContentManifest $saved (Join-Path $tx "$name-sha256.json"))) { $problems += "$name snapshot content differs from its SHA-256 manifest" }
        }
    }
    if ($problems.Count -gt 0) { throw "Required baselines are missing or inconsistent: $($problems -join ', '). Nothing was restored; the journal and the reservation stay." }
    $selectedMirror = Get-SteamMirror
    if (-not (Test-KwrSamePath $selectedMirror ([string] $s.steamMirror))) { throw "The Steam save mirror is now $selectedMirror, not the $($s.steamMirror) this transaction snapshotted. Nothing was restored; the journal and the reservation stay." }
    if ((Test-Path -LiteralPath $ModDir) -and -not (Test-Path -LiteralPath $ModDir -PathType Container)) { throw "$ModDir is not a folder; nothing was restored." }
    $candidate = Get-KwrProp $s 'candidateFiles'
    $candidateIntact = {
        $null -ne $candidate -and @('Info.json', 'KingmakerWakingRune.dll' | Where-Object {
            $file = Join-Path $ModDir $_
            -not (Test-Path -LiteralPath $file -PathType Leaf) -or -not (Test-KwrSame (Get-Sha256 $file) ([string] (Get-KwrProp $candidate $_))) }).Count -eq 0
    }
    if ($KeepInstalled -and -not (& $candidateIntact)) {
        throw '-KeepInstalled needs the recorded candidate (Info.json and KingmakerWakingRune.dll) installed intact; it is not. Nothing was restored; run End without -KeepInstalled to restore the previous state.'
    }

    # Before any rollback write, the current state of everything End will restore is preserved in the run's evidence
    # (the run was verified at admission), and its differences from the snapshot are recorded, so no change made meanwhile
    # by anyone is destroyed. If that preservation fails, nothing is restored.
    $preserved = Join-Path $run ('pre-rollback-' + (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ'))
    New-Item -ItemType Directory -Path $preserved | Out-Null
    $differences = @()
    foreach ($t in (Get-SnapshotTrees $s)) {
        if (Test-Path -LiteralPath $t.Path) {
            & robocopy.exe $t.Path (Join-Path $preserved $t.Name) /E /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "Preserving the current $($t.Name) failed ($LASTEXITCODE); nothing was restored." }
            if ((Compare-Inventory (Get-Inventory $t.Path) (Get-Inventory (Join-Path $preserved $t.Name))).Count -gt 0) { throw "The preserved copy of $($t.Name) is incomplete; nothing was restored." }
        }
        $baseline = @{}
        (Read-KwrUtf8Json (Join-Path $tx "$($t.Name)-inventory.json")).PSObject.Properties | ForEach-Object { $baseline[$_.Name] = $_.Value }
        $differences += @((Compare-Inventory $baseline (Get-Inventory $t.Path)) | ForEach-Object { "$($t.Name): $_" })
    }
    Export-Registry (Join-Path $preserved 'registry.reg')
    Copy-Item -LiteralPath $UmmParams -Destination (Join-Path $preserved 'Params.xml')
    if (Test-Path -LiteralPath $ModDir -PathType Container) {
        & robocopy.exe $ModDir (Join-Path $preserved 'mod') /E /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Preserving the current mod folder failed ($LASTEXITCODE); nothing was restored." }
    }
    Write-KwrJsonFile (Join-Path $preserved 'differences.json') @($differences)
    foreach ($d in $differences) { $script:KwrDifferences.Add($d) }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'A Kingmaker process appeared while the current state was preserved; nothing was restored.' }

    # Saves live in two places: the profile and the game's Steam remote-storage mirror (it restores
    # missing local saves from the mirror at launch). Restore both, or test saves come back.
    foreach ($t in (Get-SnapshotTrees $s)) {
        Assert-KwrSharedMutation "before restoring $($t.Name)"
        Restore-Tree $t.Name $t.Path $tx
        $script:KwrProgress.Add("$($t.Name) restored and verified")
    }

    Assert-KwrSharedMutation 'before restoring the registry'
    Restore-KwrRegistry $tx
    $script:KwrProgress.Add('registry verified')

    Assert-KwrSharedMutation 'before restoring Params.xml'
    Copy-Item -LiteralPath (Join-Path $tx 'Params.xml') -Destination $UmmParams -Force
    if ((Get-FileHash $UmmParams).Hash -ne (Get-FileHash (Join-Path $tx 'Params.xml')).Hash) { throw 'UMM Params.xml restore did not verify.' }
    $script:KwrProgress.Add('Params.xml verified')

    Assert-KwrSharedMutation 'before restoring Mods'
    if (-not $KeepInstalled -and (Test-Path -LiteralPath $ModDir -PathType Container)) { Remove-Item -LiteralPath $ModDir -Recurse -Force }
    if ($KeepInstalled) {
        # The harness files this transaction placed (including an unconsumed command request) are removed; only the
        # two packaged files may remain, with the recorded content.
        foreach ($leftover in 'recon.flag', 'recon', 'capture.flag', 'capture.request', 'state.request', 'fault.request', 'command.request') {
            Remove-Item -LiteralPath (Join-Path $ModDir $leftover) -Recurse -Force -ErrorAction SilentlyContinue
        }
        $kept = @(Get-ChildItem -LiteralPath $ModDir -Force | ForEach-Object Name | Sort-Object)
        if (-not (Test-KwrSame ($kept -join '|') 'Info.json|KingmakerWakingRune.dll') -or -not (& $candidateIntact)) { throw "The kept candidate folder is not exactly the recorded packaged files after cleanup: $($kept -join ', ')" }
    }
    $existingSnapshot = Join-Path $tx 'mod-existing'
    if (-not $KeepInstalled -and $s.existingMod) {
        & robocopy.exe $existingSnapshot $ModDir /MIR /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy restore of the existing mod failed ($LASTEXITCODE)" }
        Write-Step 'Previously installed mod restored.'
    }
    $modsBefore = @{}
    (Read-KwrUtf8Json (Join-Path $tx 'mods-inventory.json')).PSObject.Properties | ForEach-Object { $modsBefore[$_.Name] = $_.Value }
    $modsNow = Get-Inventory $ModsRoot
    # With -KeepInstalled the verified candidate replaces whatever was there; the previous install stays in the
    # transaction's mod-existing snapshot.
    $modsDiff = @((Compare-Inventory $modsBefore $modsNow) | Where-Object { -not ($KeepInstalled -and $_ -match '^(added|changed|missing):KingmakerWakingRune\\') })
    if ($modsDiff.Count -gt 0) { throw "Mods inventory differs after restore: $($modsDiff -join '; ')" }
    # The folder itself (an empty one is a valid baseline) exists exactly when it did, or holds the kept candidate.
    if (-not $KeepInstalled -and (Test-Path -LiteralPath $ModDir -PathType Container) -ne [bool] $s.existingMod) { throw "The mod folder's existence was not restored (expected $([bool] $s.existingMod))." }
    $script:KwrProgress.Add("Mods verified (candidate installed: $([bool]$KeepInstalled))")
    Write-Step "Params.xml and Mods verified (candidate installed: $([bool]$KeepInstalled))."
    # Restoration verified: the close is recorded locally, the operation lock released, and the lease released last.
    Assert-KwrSharedMutation 'before completion'
    Complete-KwrTransactionReservation $ReservationConvention $script:KwrAdmitted
    Write-Step "Transaction $($s.runId) closed and the Kingmaker native-test reservation released. Evidence: $run"
}

# A previous-format journal (no reservation token) is recovered only by End -RunId <its run>: under a free reservation
# (any record refuses) and then the operation lock, the journal is re-read unchanged, the single Steam mirror is
# recorded, and the token is published atomically; End then verifies the previous-format baselines and restores. An
# interruption before the token is published leaves the lease and the unchanged journal; End then refuses.
function Enter-KwrLegacyRecovery {
    $s = Get-State
    $run = Get-KwrProp $s 'runId'
    if (-not (Test-KwrRunId $run) -or -not (Test-KwrSame $RunId $run)) { throw "End -RunId must name the previous-format transaction's run ($run); refusing." }
    $lease = Enter-KwrReservation $ReservationConvention $run 'previous-format recovery' $StatePath
    $published = $false
    try {
        Enter-KwrOperationLock $StateRoot
        $s = Get-State
        if (-not $s -or (Test-KwrJournalToken $s) -or -not (Test-KwrSame (Get-KwrProp $s 'runId') $run)) { throw 'The previous-format journal changed during recovery admission; refusing.' }
        $mirror = Get-SteamMirror
        foreach ($field in @(@('reservationToken', $lease.Token), @('steamMirror', $mirror), @('legacyRecovery', $true))) { $s | Add-Member -NotePropertyName $field[0] -NotePropertyValue $field[1] -Force }
        Save-State $s
        $published = $true
        return $lease
    }
    catch {
        if (-not $published) {
            Exit-KwrOperationLock
            try { Exit-KwrReservation $ReservationConvention $lease } catch { Write-Step "WARNING: releasing the recovery reservation failed: $($_.Exception.Message)" }
        }
        throw
    }
}

# Every operation except Status (which only reads the journal) runs admitted. Begin acquires the reservation, then the
# operation lock. Every other operation needs an open transaction: with none it stops before any lock or probe;
# otherwise the journal and lease must be the same run, token and journal path, and the operation lock is held for its
# whole duration. While the journal says 'snapshotting' or 'snapshotted' only Collect, Close and End may run.
function Invoke-KwrOperation([string] $Operation) {
    if ($Operation -eq 'Status') { $s = Get-State; if ($s) { $s | ConvertTo-Json } else { 'No open transaction.' }; return }
    $script:KwrOperation = $Operation
    $script:KwrAdmitted = $null
    $script:KwrProgress = New-Object System.Collections.Generic.List[string]
    $script:KwrDifferences = New-Object System.Collections.Generic.List[string]
    try {
        if ($Operation -eq 'Begin') { Invoke-Begin; return }
        $journal = $null; try { $journal = Get-State } catch { }
        if ($Operation -eq 'End' -and $journal -and -not (Test-KwrJournalToken $journal)) { $script:KwrAdmitted = Enter-KwrLegacyRecovery }
        else {
            try { $script:KwrAdmitted = Enter-KwrTransactionOperation $ReservationConvention $StatePath $StateRoot }
            catch {
                # A rejected journal never chooses where its incident goes: a fixed lab-owned folder, identities as data.
                $failure = $_
                if ($journal -and (Test-KwrJournalToken $journal)) {
                    $claimed = New-KwrAdmitted ([string] (Get-KwrProp $journal 'runId')) ([string] $journal.reservationToken) $StatePath
                    $loss = Get-KwrOwnershipLoss $ReservationConvention $claimed
                    if ($loss) {
                        $expected = [ordered]@{ lab = 'KingmakerWakingRune'; claimedRunId = $claimed.RunId; claimedToken = $claimed.Token; statePath = $claimed.StatePath; verified = $false }
                        [void] (Write-KwrReservationIncident $ReservationConvention (Join-Path $StateRoot 'incidents') $expected $loss $Operation 'admission' @() @())
                    }
                }
                throw $failure
            }
        }
        $current = Get-State
        $phase = [string] $current.phase
        if ($phase -in @('snapshotting', 'snapshotted') -and $Operation -notin @('Collect', 'Close', 'End')) { throw "$Operation is refused while Begin's snapshot is incomplete (phase $phase); run End." }
        $pending = @(Get-KwrOutstandingWork $current)
        if ($pending.Count -gt 0 -and $Operation -in @('Reinstall', 'Uninstall', 'Launch', 'Fault', 'End')) { throw "$Operation is refused while work this transaction started is outstanding ($(@($pending | ForEach-Object { "$($_.kind) $($_.pid)" }) -join ', ')); the journal and the reservation stay." }
        switch ($Operation) {
            'Reinstall' { Invoke-Reinstall }
            'Uninstall' { Invoke-Uninstall }
            'Launch' { Invoke-Launch }
            'Shot' { Invoke-Shot }
            'State' { Invoke-State }
            'Click' { Invoke-Click }
            'DoubleClick' { Invoke-DoubleClick }
            'Drag' { Invoke-Drag }
            'Hover' { $w = Get-GameWindow; Assert-GameForeground $w; Assert-GameAtPoint $w $X $Y; [void][KwrInput]::SetCursorPos($w.Rect.Left + $X, $w.Rect.Top + $Y); Write-Step "Hover at window ($X,$Y)" }
            'Key' { Invoke-Key }
            'Type' { Invoke-Type }
            'Scroll' { Invoke-Scroll }
            'Close' { Invoke-Close }
            'Collect' { Invoke-Collect }
            'End' { Invoke-End }
            'Record' { Invoke-Record }
            'Fault' { Invoke-Fault }
            'Command' { Invoke-Command }
        }
    }
    finally { Exit-KwrOperationLock }
}

Invoke-KwrOperation $Operation
