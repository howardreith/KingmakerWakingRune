<#
.SYNOPSIS
  One step of a stand-in Waking Rune transaction, run as its own process by scripts/Test-NativeReservation.ps1.
.DESCRIPTION
  Uses the admission functions KwrRuntime.ps1 uses (scripts/KwrReservation.ps1), in the same order, against a stand-in
  lab: Begin = Enter-KwrTransactionReservation, refuse an open state (releasing), save the state with its token, then
  the transaction's first shared mutation; Run = the operation guard (Assert-KwrTransactionReservation) then a mutation;
  End = the guard, the restoration, then Complete-KwrTransactionReservation. KwrRuntime.ps1 itself is not run: it needs
  Steam, a package and the live game. With -Hold, a step writes its section marker and waits for <Name>.<Step>.go before
  leaving the section. Outcomes are written to the signals folder as <Name>.<outcome>.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('Begin', 'Run', 'End')][string] $Step,
    [Parameter(Mandatory = $true)][string] $Name,
    [Parameter(Mandatory = $true)][string] $LabRoot,
    [Parameter(Mandatory = $true)][string] $Signals,
    [Parameter(Mandatory = $true)][string] $MutexName,
    [Parameter(Mandatory = $true)][string] $RecordPath,
    [switch] $Hold
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'KwrReservation.ps1')

$convention = [pscustomobject]@{ MutexName = $MutexName; RecordPath = $RecordPath }
$statePath = Join-Path $LabRoot 'runtime-state\transaction.json'
$shared = Join-Path $LabRoot 'shared'
New-Item -ItemType Directory -Force -Path (Split-Path $statePath), $shared | Out-Null

function Write-Signal([string] $What, [string] $Text = '') { Set-Content -LiteralPath (Join-Path $Signals "$Name.$What") -Value $Text }
function Wait-Go {
    if (-not $Hold) { return }
    $go = Join-Path $Signals "$Name.$Step.go"
    $until = (Get-Date).AddSeconds(90)
    while (-not (Test-Path -LiteralPath $go)) {
        if ((Get-Date) -gt $until) { throw "no signal to leave $Step" }
        Start-Sleep -Milliseconds 50
    }
}

try {
    switch ($Step) {
        'Begin' {
            $reservation = Enter-KwrTransactionReservation $convention $statePath $Name
            if (Read-KwrTransactionState $statePath) {
                Exit-KwrReservation $convention $reservation.token
                throw 'A transaction is already open.'
            }
            [ordered]@{ runId = $Name; phase = 'installed'; reservationToken = $reservation.token } | ConvertTo-Json | Set-Content -LiteralPath $statePath -Encoding UTF8
            Set-Content -LiteralPath (Join-Path $shared "$Name.begin") -Value 'shared mutation'
            Write-Signal 'begin'
            Wait-Go
            Write-Signal 'begun'
        }
        'Run' {
            [void] (Assert-KwrTransactionReservation $convention $statePath)
            Set-Content -LiteralPath (Join-Path $shared "$Name.run") -Value 'shared mutation'
            Write-Signal 'run'
            Wait-Go
            Write-Signal 'ran'
        }
        'End' {
            $token = Assert-KwrTransactionReservation $convention $statePath
            Get-ChildItem -LiteralPath $shared -Filter "$Name.*" | Remove-Item -Force
            Write-Signal 'end'
            Wait-Go
            Complete-KwrTransactionReservation $convention $statePath $token
            Write-Signal 'ended'
        }
    }
}
catch {
    Write-Signal 'refused' $_.Exception.Message
    exit 3
}
exit 0
