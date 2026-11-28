# The Waking Rune for Pathfinder: Kingmaker (opening prototype)

A standalone adventure mod that will eventually make Pathfinder Society Scenario #4–26, *The Waking Rune*,
playable in Pathfinder: Kingmaker. It adds **The Waking Rune** as a fourth choice on the New Game screen, runs the
native difficulty and character-creation screens, and starts the created hero in a populated "Magnimar Lodge —
Sheila's Study" for a scripted briefing. The party can then level to 11, recruit custom adventurers for free through
the Society Coordinator, and spend a shared 450,000 gp at the Quartermaster's shop. When ready, break the Refuge Token
(belt or inventory Use) to carry the party to the Sanctum of Sloth, an explorable sanctum with working wards,
investigation and rune sabotage. Boss combat is the next milestone, and there is no return route yet.

## Requirements
- Pathfinder: Kingmaker 2.1.7b (Steam). The common UMM build and support target is **0.31.1**, using its Harmony 1.2 adapter and Harmony 2.3.1.1.
- Newer UMM versions are separate compatibility targets. The baseline build, tests and package checks pass; in-game validation on 0.31.1 and newer loaders has not been run. See [UMM compatibility](docs/UMM-COMPATIBILITY.md) for evidence and remaining checks.
- No other mods are required.

## Install
1. Download `KingmakerWakingRune-0.12.4.zip` and its receipt `KingmakerWakingRune-0.12.4.zip.build.json` from
   [release v0.12.4](https://github.com/howardreith/KingmakerWakingRune/releases/tag/v0.12.4), the first published
   build on the UMM 0.31.1 baseline ([release notes](docs/RELEASE-NOTES-0.12.4.md)). To build your own, run
   `scripts/Build.ps1`: it writes `artifacts/KingmakerWakingRune-<version>.zip` (`<version>` is the `Version` in
   `Info.json`) and its receipt beside it as `<zip>.build.json`.
   The published prerelease `KingmakerWakingRune-0.12.3.zip` (GitHub release v0.12.3) predates the UMM 0.31.1 baseline:
   its DLL references UnityModManager 0.33.2.0 and Harmony 2.3.6.0 and it declares UMM 0.28.2.
2. Check it, then extract it into `<Kingmaker>/Mods/` (or install it through the UMM installer). The ZIP holds exactly
   two files (portraits and icons are embedded in the DLL):
   ```
   KingmakerWakingRune/Info.json
   KingmakerWakingRune/KingmakerWakingRune.dll
   ```
   ```powershell
   .\scripts\Validate-Package.ps1 -PackagePath artifacts\KingmakerWakingRune-<version>.zip `
     -ReceiptPath artifacts\KingmakerWakingRune-<version>.zip.build.json -RequireCurrentProvenance -ReportPath validation.json
   ```
   The report gives the ZIP SHA-256, DLL SHA-256, MVID and loader references; with the receipt and
   `-RequireCurrentProvenance` it also confirms that the ZIP was built from a clean commit with the current inputs.

## Play
Main Menu → **New Game** → **The Waking Rune** → Next → choose a difficulty → Next → **Create a new character**
(or a pregen) → complete character creation → **Complete**. The normal loading screen takes you to the lodge.

## Saves
- Waking Rune saves reference this mod's blueprints and **require the mod**; don't load them with it removed.
- Waking Rune saves are recorded as their own campaign, so the Varnhold's Lot / Beneath the Stolen Lands
  "import main character" lists never offer them.
- Removing the mod does not affect other campaigns or their saves.

## Build
```powershell
copy LocalGamePaths.props.example LocalGamePaths.props   # then point it at your install
.\scripts\Build.ps1                                       # builds, runs tests, writes artifacts\*.zip + hashes
```
Requires Visual Studio 2022 Build Tools (MSBuild) and the .NET Framework 4.7 targeting pack.
The build fetches the exact upstream loader references pinned in `scripts/loader-reference-pins.json`
into `obj/loader-references` and checks their SHA-256 and assembly identities. Direct MSBuild
also verifies those files; run `scripts/Get-PinnedLoaderReferences.ps1` first to populate the cache.
The installed UMM does not select build references. Loader/Harmony DLLs are not included in the mod ZIP.
Before installing any ZIP, `scripts/Validate-Package.ps1 -PackagePath <zip>` checks its files, manifest, the DLL's
assembly definition and its actual loader references (`KwrRuntime.ps1` runs it automatically, with current-provenance
checks when a receipt is beside the ZIP).

Static loader API audit (static evidence only; see [UMM compatibility](docs/UMM-COMPATIBILITY.md)). The tool's two
source files are committed as text under `docs/evidence/umm-0311/`; build it with the .NET 8 SDK:
```powershell
New-Item -ItemType Directory audit | Out-Null
Copy-Item docs\evidence\umm-0311\loader-api-audit-Program.cs.txt audit\Program.cs
Copy-Item docs\evidence\umm-0311\loader-api-audit-csproj.txt audit\LoaderApiAudit.csproj
dotnet build -c Release audit\LoaderApiAudit.csproj
$audit = 'audit\bin\Release\net8.0\LoaderApiAudit.exe'
# The audited DLL is the one in the package (scripts/Build.ps1 output); its digest equals the validator's dll_sha256.
Expand-Archive artifacts\KingmakerWakingRune-<version>.zip audit\package
```
Three profiles are assembled from official NuGet packages (`docs/evidence/umm-0311/loader-profiles.md` lists the
packages and hashes): `<p>\<v>\UnityModManager.dll` from `unitymodmanager.<v>.nupkg` (`lib/net35`), `0Harmony.dll` from
the matching `lib.harmony.<v>.nupkg` (`lib/net48`) and the pinned `0Harmony12.dll`. The fourth row is a read-only copy of
the owner's installed loader folder (`<Kingmaker>\Kingmaker_Data\Managed\UnityModManager`; measured, not assembled):
```powershell
& $audit --mod audit\package\KingmakerWakingRune\KingmakerWakingRune.dll --json api-audit.json --assemblies UnityModManager,0Harmony12,0Harmony `
  --profile "0.31.1=<p>\0.31.1\UnityModManager.dll;<p>\0.31.1\0Harmony.dll;<p>\0.31.1\0Harmony12.dll" `
  --profile "0.32.4=<p>\0.32.4\UnityModManager.dll;<p>\0.32.4\0Harmony.dll;<p>\0.32.4\0Harmony12.dll" `
  --profile "0.33.4=<p>\0.33.4\UnityModManager.dll;<p>\0.33.4\0Harmony.dll;<p>\0.33.4\0Harmony12.dll" `
  --profile "owner-0.33.0=<owner copy>\UnityModManager.dll;<owner copy>\0Harmony.dll;<owner copy>\0Harmony12.dll"
```

## Native test runs
`scripts/KwrRuntime.ps1` runs guarded transactions against the real game (Begin, operations, `End -RunId <run>`, each
its own process). Every transaction holds the machine-wide **Kingmaker native-test reservation**
([protocol](docs/NATIVE-RESERVATION.md), implemented in `scripts/KwrReservation.ps1`) from Begin until End has verified
its restoration:
- Begin acquires it before reading or writing anything (Steam discovery included), then this lab's operation lock
  (`runtime-state\operation.lock`) and journal (`transaction.json`, UTF-8, published atomically: the run, the lease
  token, the Steam mirror it snapshotted, digests of every baseline and SHA-256 manifests of the snapshot trees). The
  complete baseline set exists before the journal says `snapshotted`, and nothing shared changes before that; until
  then only Collect, Close and End run.
- Every other operation except Status (which only reads the journal) needs an open transaction whose journal and lease
  are the same run, token and journal path (compared ordinally), and holds the operation lock for its whole duration.
  The admitted identity is rechecked before every shared change and journal update; a lost or replaced journal or lease
  stops the operation and records an incident (in the run's evidence, or in `runtime-state\incidents` when the journal
  itself is rejected). Launches and recordings are journaled before they are dispatched and keep End and conflicting
  operations waiting until their outcome is known.
- End verifies every baseline's digest and every snapshot file's content before the first restore write, preserves the
  current state of everything it will restore (and its differences from the snapshot) in the run's evidence, then
  restores. With `-KeepInstalled` it requires the recorded candidate files intact before and after removing the harness
  files. It then records its close (`transaction.closed.json`), releases the operation lock, and releases the lease last.

Waking Rune releases only its own transaction's lease: a refused Begin before its journal exists, End after verified
restoration, and the next Begin finishing a close that End interrupted after recording it (the closed journal names the
lease's run and token, and the closer is no longer running). A journal written before the reservation (no token) is
recovered only by `End -RunId <run>`, under a free reservation, after the same baseline and Steam-mirror checks.

A lease without its journal or closed journal (for example a Begin or recovery interrupted before publishing its
journal) is an incident: the tool never clears it, and the owner investigates before anything else runs. Other Kingmaker
labs that implement the same protocol cannot start a native transaction while any lease exists. This lab needs none of
them to be installed, and none needs this lab. A record left by another lab, or an unreadable or malformed record, is
never removed. `scripts/Test-NativeReservation.ps1` runs KwrRuntime's own functions in separate processes against
disposable stand-in labs only.

## Repository map
- `src/KingmakerWakingRune` — the mod (blueprints, selection, lodge, thin Harmony patches).
- `tests` — framework-free tests for extracted decision logic.
- `scripts/Build.ps1` — reproducible build/package; `scripts/KwrRuntime.ps1` — guarded test harness.
- `docs/RECON.md` — verified engine integration points and donor choices.
- `docs/ACCEPTANCE-WR-01.md` — acceptance results; `STATUS.md` — current state and next action.
