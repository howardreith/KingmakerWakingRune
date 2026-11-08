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
  Collect  Copy UMM log, output_log, recon files and KWR_AUTOMATION saves into evidence.
  End      Restore every snapshot exactly and verify; uninstall unless -KeepInstalled.
  Status   Print the open transaction.

  All live state lives under the lab root (two levels above the repository).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('Begin', 'Reinstall', 'Launch', 'Shot', 'Click', 'Hover', 'Key', 'Type', 'Scroll', 'Close', 'Collect', 'End', 'Status')]
    [string] $Operation,
    [string] $RunId,
    [string] $PackagePath,
    [switch] $Recon,
    [string[]] $LaunchArgs = @(),
    [string] $Name,
    [int] $X,
    [int] $Y,
    [string] $Button = 'Left',
    [string] $Keys,
    [string] $Text,
    [int] $Delta = -120,
    [switch] $KeepInstalled,
    [int] $TimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$LabRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot '..\..'))
$StateRoot = Join-Path $LabRoot 'runtime-state'
$BackupRoot = Join-Path $LabRoot 'runtime-backups'
$EvidenceRoot = Join-Path $LabRoot 'runtime-evidence'
$StatePath = Join-Path $StateRoot 'transaction.json'

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
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@
[void][KwrInput]::SetProcessDPIAware()

function Write-Step([string] $m) { Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $m) }

function Get-State {
    if (-not (Test-Path -LiteralPath $StatePath)) { return $null }
    return Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
}

function Save-State($s) {
    New-Item -ItemType Directory -Force -Path $StateRoot | Out-Null
    $s | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $StatePath -Encoding UTF8
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

function Get-RunDir($s) { Join-Path $EvidenceRoot $s.runId }

function Invoke-Begin {
    if (Get-State) { throw "A transaction is already open: $StatePath. Run End (or inspect) first." }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'Kingmaker is running; it may be an owner session. Refusing to start.' }
    if (-not $RunId -or $RunId -notmatch '^[A-Za-z0-9._-]{3,80}$') { throw 'A -RunId of 3-80 safe characters is required.' }
    if (-not $PackagePath -or -not (Test-Path -LiteralPath $PackagePath)) { throw 'A -PackagePath to the built zip is required.' }
    if (Test-Path -LiteralPath $ModDir) { throw "Mod already installed at $ModDir; refusing to overwrite an unknown install." }
    Assert-SteamCloudOffline

    $tx = Join-Path $BackupRoot $RunId
    $run = Join-Path $EvidenceRoot $RunId
    if ((Test-Path $tx) -or (Test-Path $run)) { throw "RunId $RunId was already used." }
    New-Item -ItemType Directory -Force -Path $tx, $run | Out-Null
    $s = [ordered]@{ runId = $RunId; createdUtc = (Get-Date).ToUniversalTime().ToString('o'); phase = 'snapshotting'; gamePid = $null; gameStart = $null; package = (Resolve-Path $PackagePath).Path; packageSha256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash.ToLowerInvariant(); recon = [bool]$Recon }
    Save-State $s

    Write-Step 'Snapshotting Kingmaker profile (saves etc.)...'
    & robocopy.exe $ProfileDir (Join-Path $tx 'profile') /MIR /COPY:DAT /DCOPY:DAT /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy snapshot failed ($LASTEXITCODE)" }
    $inv = Get-Inventory $ProfileDir
    $bak = Get-Inventory (Join-Path $tx 'profile')
    $d = Compare-Inventory $inv $bak
    if ($d.Count -gt 0) { throw "Profile snapshot does not match source: $($d[0..([Math]::Min(5,$d.Count)-1)] -join '; ')" }
    $inv | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $tx 'profile-inventory.json') -Encoding UTF8
    Export-Registry (Join-Path $tx 'registry.reg')
    Copy-Item -LiteralPath $UmmParams -Destination (Join-Path $tx 'Params.xml')
    (Get-Inventory $ModsRoot) | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $tx 'mods-inventory.json') -Encoding UTF8
    $s.ummLogLength = (Get-Item -LiteralPath $UmmLog).Length
    Write-Step ("Snapshot verified: {0} profile files." -f $inv.Count)

    # Temporary test profile: suppress the UMM window at start (restored by End).
    [xml] $params = Get-Content -LiteralPath $UmmParams -Raw
    $params.Param.ShowOnStart = '0'
    $params.Save($UmmParams)

    Expand-Archive -LiteralPath $PackagePath -DestinationPath $ModsRoot
    if (-not (Test-Path -LiteralPath (Join-Path $ModDir 'KingmakerWakingRune.dll'))) { throw 'Package did not install the expected mod folder.' }
    if ($Recon) { Set-Content -LiteralPath (Join-Path $ModDir 'recon.flag') -Value 'recon' }
    $s.phase = 'installed'
    Save-State $s
    Write-Step "Installed package $($s.packageSha256) into $ModDir (recon=$([bool]$Recon))."
}

function Invoke-Reinstall {
    $s = Get-State
    if (-not $s -or $s.phase -notin @('installed', 'closed')) { throw 'Reinstall needs an open transaction with the game closed.' }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'Kingmaker is running; never hot-replace the DLL.' }
    if (-not $PackagePath -or -not (Test-Path -LiteralPath $PackagePath)) { throw 'A -PackagePath is required.' }
    $recon = Test-Path -LiteralPath (Join-Path $ModDir 'recon.flag')
    Remove-Item -LiteralPath $ModDir -Recurse -Force
    Expand-Archive -LiteralPath $PackagePath -DestinationPath $ModsRoot
    if ($recon) { Set-Content -LiteralPath (Join-Path $ModDir 'recon.flag') -Value 'recon' }
    $s.package = (Resolve-Path $PackagePath).Path
    $s.packageSha256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    Save-State $s
    Write-Step "Reinstalled package $($s.packageSha256) (recon=$recon)."
}

function Invoke-Launch {
    $s = Get-State
    if (-not $s -or $s.phase -notin @('installed', 'closed')) { throw 'No installed transaction ready to launch.' }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'Kingmaker is already running.' }
    Assert-SteamCloudOffline
    $since = Get-Date
    $argList = @('-applaunch', $AppId) + $LaunchArgs
    Start-Process -FilePath $SteamExe -ArgumentList $argList | Out-Null
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $p = $null
    while ((Get-Date) -lt $deadline -and -not $p) {
        Start-Sleep -Seconds 2
        $p = Get-GameProcesses | Where-Object { $_.StartTime -ge $since.AddSeconds(-5) } | Select-Object -First 1
    }
    if (-not $p) { throw 'Kingmaker did not start within the timeout.' }
    while ((Get-Date) -lt $deadline -and $p.MainWindowHandle -eq [IntPtr]::Zero) { Start-Sleep -Seconds 1; $p.Refresh() }
    $s.gamePid = $p.Id
    $s.gameStart = $p.StartTime.ToString('o')
    $s.phase = 'running'
    Save-State $s
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

function Invoke-Shot {
    # The DirectX surface cannot be read while hidden (PrintWindow returns black), so capture the
    # screen only after verifying the game is the foreground window; otherwise abort.
    $w = Get-GameWindow
    Assert-GameForeground $w
    Start-Sleep -Milliseconds 400
    $r = $w.Rect
    $width = $r.Right - $r.Left; $height = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    if ([KwrInput]::GetForegroundWindow() -ne $w.Process.MainWindowHandle) { $g.Dispose(); $bmp.Dispose(); throw 'Game lost foreground during capture; discarded.' }
    $label = if ($Name) { $Name } else { 'shot' }
    $file = Join-Path (Get-RunDir $w.State) ('{0:HHmmss}-{1}.png' -f (Get-Date), $label)
    $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output $file
    Write-Step "Window $($r.Left),$($r.Top) ${width}x${height} -> $file"
}

function Invoke-Click {
    $w = Get-GameWindow
    Assert-GameForeground $w
    Start-Sleep -Milliseconds 150
    [void][KwrInput]::SetCursorPos($w.Rect.Left + $X, $w.Rect.Top + $Y)
    Start-Sleep -Milliseconds 150
    $down, $up = if ($Button -eq 'Right') { 0x0008, 0x0010 } else { 0x0002, 0x0004 }
    [KwrInput]::mouse_event($down, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [KwrInput]::mouse_event($up, 0, 0, 0, [UIntPtr]::Zero)
    Write-Step "$Button click at window ($X,$Y)"
}

function Invoke-Scroll {
    $w = Get-GameWindow
    Assert-GameForeground $w
    [void][KwrInput]::SetCursorPos($w.Rect.Left + $X, $w.Rect.Top + $Y)
    Start-Sleep -Milliseconds 100
    [KwrInput]::mouse_event(0x0800, 0, 0, $Delta, [UIntPtr]::Zero)
    Write-Step "Scroll $Delta at window ($X,$Y)"
}

$VirtualKeys = @{ ESC = 0x1B; ENTER = 0x0D; SPACE = 0x20; TAB = 0x09; BACK = 0x08; DEL = 0x2E; I = 0x49; C = 0x43; M = 0x4D; W = 0x57; A = 0x41; S = 0x53; D = 0x44; F5 = 0x74; F9 = 0x78; CTRL = 0x11; SHIFT = 0x10; ALT = 0x12; LEFT = 0x25; UP = 0x26; RIGHT = 0x27; DOWN = 0x28 }

function Send-Vk([byte] $vk, [bool] $up) { [KwrInput]::keybd_event($vk, 0, $(if ($up) { 2 } else { 0 }), [UIntPtr]::Zero) }

function Invoke-Key {
    $w = Get-GameWindow
    Assert-GameForeground $w
    Start-Sleep -Milliseconds 150
    foreach ($chord in $Keys.Split(' ')) {
        $parts = $chord.Split('+') | ForEach-Object { $_.ToUpperInvariant() }
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
        [void]$p.CloseMainWindow()
        if (-not $p.WaitForExit(60000)) {
            Write-Step 'Game did not exit after a close request; terminating the owned process only.'
            Stop-Process -Id $p.Id -Force
            $p.WaitForExit(30000) | Out-Null
        }
        Write-Step "Owned game process $($s.gamePid) exited."
    }
    $s.phase = 'closed'; $s.gamePid = $null; $s.gameStart = $null
    Save-State $s
}

function Invoke-Collect {
    $s = Get-State
    if (-not $s) { throw 'No open transaction.' }
    $run = Get-RunDir $s
    $logBytes = [IO.File]::ReadAllBytes($UmmLog)
    $start = [Math]::Min([long]$s.ummLogLength, $logBytes.Length)
    if ($logBytes.Length -lt [long]$s.ummLogLength) { $start = 0 }
    [IO.File]::WriteAllBytes((Join-Path $run 'umm-log.txt'), $logBytes[$start..($logBytes.Length - 1)])
    $out = Join-Path $ProfileDir 'output_log.txt'
    if (Test-Path -LiteralPath $out) { Copy-Item -LiteralPath $out -Destination (Join-Path $run ('output_log-{0:HHmmss}.txt' -f (Get-Date))) }
    $recon = Join-Path $ModDir 'recon'
    if (Test-Path -LiteralPath $recon) { Copy-Item -LiteralPath $recon -Destination (Join-Path $run 'recon') -Recurse -Force }
    $saves = Join-Path $run 'saves'
    New-Item -ItemType Directory -Force -Path $saves | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $ProfileDir 'Saved Games') -File | Where-Object Name -like '*KWR_AUTOMATION*' | Copy-Item -Destination $saves -Force
    Write-Step "Collected evidence into $run"
    Get-Content (Join-Path $run 'umm-log.txt') | Where-Object { $_ -match 'KingmakerWakingRune|Exception|\[Manager\].*(Error|error)' } | Select-Object -Last 60
}

function Invoke-End {
    $s = Get-State
    if (-not $s) { throw 'No open transaction.' }
    if (Get-OwnedProcess $s) { throw 'The owned game is still running; Close it first.' }
    if (@(Get-GameProcesses).Count -gt 0) { throw 'A Kingmaker process is running; refusing to restore underneath it.' }
    $tx = Join-Path $BackupRoot $s.runId
    $run = Get-RunDir $s
    try { Invoke-Collect | Out-Null } catch { Write-Step "Collect before restore failed: $_" }

    # Profile: quarantine files the test added, restore anything changed or removed.
    $baseline = @{}
    (Get-Content -LiteralPath (Join-Path $tx 'profile-inventory.json') -Raw | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $baseline[$_.Name] = $_.Value }
    $current = Get-Inventory $ProfileDir
    $quarantine = Join-Path $tx 'quarantine'
    $diff = Compare-Inventory $baseline $current
    foreach ($entry in $diff) {
        $kind, $rel = $entry.Split(':', 2)
        $live = Join-Path $ProfileDir $rel
        $saved = Join-Path (Join-Path $tx 'profile') $rel
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
    $after = Compare-Inventory $baseline (Get-Inventory $ProfileDir)
    if ($after.Count -gt 0) { throw "Profile restore incomplete: $($after -join '; ')" }
    Write-Step ("Profile restored: {0} differences reverted (added files quarantined in {1})." -f $diff.Count, $quarantine)

    # Registry: restore exactly if anything differs.
    $now = Join-Path $tx 'registry-after.reg'
    Export-Registry $now
    if ((Get-FileHash $now).Hash -ne (Get-FileHash (Join-Path $tx 'registry.reg')).Hash) {
        & cmd.exe /c "reg delete `"$RegKey`" /f >nul 2>&1"
        & cmd.exe /c "reg import `"$(Join-Path $tx 'registry.reg')`" >nul 2>&1"
        Export-Registry $now
        if ((Get-FileHash $now).Hash -ne (Get-FileHash (Join-Path $tx 'registry.reg')).Hash) { throw 'Registry restore did not verify.' }
        Write-Step 'Registry key restored.'
    }
    else { Write-Step 'Registry key unchanged.' }

    Copy-Item -LiteralPath (Join-Path $tx 'Params.xml') -Destination $UmmParams -Force
    if ((Get-FileHash $UmmParams).Hash -ne (Get-FileHash (Join-Path $tx 'Params.xml')).Hash) { throw 'UMM Params.xml restore did not verify.' }

    if (-not $KeepInstalled -and (Test-Path -LiteralPath $ModDir)) { Remove-Item -LiteralPath $ModDir -Recurse -Force }
    if ($KeepInstalled -and (Test-Path -LiteralPath (Join-Path $ModDir 'recon.flag'))) { Remove-Item -LiteralPath (Join-Path $ModDir 'recon.flag'), (Join-Path $ModDir 'recon') -Recurse -Force -ErrorAction SilentlyContinue }
    $modsBefore = @{}
    (Get-Content -LiteralPath (Join-Path $tx 'mods-inventory.json') -Raw | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $modsBefore[$_.Name] = $_.Value }
    $modsNow = Get-Inventory $ModsRoot
    $modsDiff = @((Compare-Inventory $modsBefore $modsNow) | Where-Object { -not ($KeepInstalled -and $_ -match '^added:KingmakerWakingRune\\') })
    if ($modsDiff.Count -gt 0) { throw "Mods inventory differs after restore: $($modsDiff -join '; ')" }
    Write-Step "Params.xml and Mods verified (candidate installed: $([bool]$KeepInstalled))."
    Remove-Item -LiteralPath $StatePath -Force
    Write-Step "Transaction $($s.runId) closed. Evidence: $run"
}

switch ($Operation) {
    'Begin' { Invoke-Begin }
    'Reinstall' { Invoke-Reinstall }
    'Launch' { Invoke-Launch }
    'Shot' { Invoke-Shot }
    'Click' { Invoke-Click }
    'Hover' { $w = Get-GameWindow; Assert-GameForeground $w; [void][KwrInput]::SetCursorPos($w.Rect.Left + $X, $w.Rect.Top + $Y); Write-Step "Hover at window ($X,$Y)" }
    'Key' { Invoke-Key }
    'Type' { Invoke-Type }
    'Scroll' { Invoke-Scroll }
    'Close' { Invoke-Close }
    'Collect' { Invoke-Collect }
    'End' { Invoke-End }
    'Status' { $s = Get-State; if ($s) { $s | ConvertTo-Json } else { 'No open transaction.' } }
}
