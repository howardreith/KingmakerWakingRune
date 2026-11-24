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
1. Use the ZIP produced by the reviewed build of this branch, `artifacts/KingmakerWakingRune-<version>.zip`
   (`<version>` is the `Version` in `Info.json`; `Build.ps1` writes its receipt beside it as `<zip>.build.json`).
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

Static loader API audit (static evidence only; see [UMM compatibility](docs/UMM-COMPATIBILITY.md)). The tool's source is
committed as `docs/evidence/umm-0311/loader-api-audit-Program.cs.txt` and `loader-api-audit-csproj.txt`; each profile is
assembled from the official NuGet packages listed in `docs/evidence/umm-0311/loader-profiles.md`
(`unitymodmanager.<v>.nupkg` `lib/net35/UnityModManager.dll`, `lib.harmony.<v>.nupkg` `lib/net48/0Harmony.dll`, and the
pinned `0Harmony12.dll`):
```powershell
dotnet build -c Release <copy of the LoaderApiAudit project>
LoaderApiAudit.exe --mod bin\Release\KingmakerWakingRune.dll --json api-audit.json --assemblies UnityModManager,0Harmony12,0Harmony `
  --profile "0.31.1=<p>\0.31.1\UnityModManager.dll;<p>\0.31.1\0Harmony.dll;<p>\0.31.1\0Harmony12.dll" `
  --profile "0.32.4=<p>\0.32.4\UnityModManager.dll;<p>\0.32.4\0Harmony.dll;<p>\0.32.4\0Harmony12.dll" `
  --profile "owner-0.33.0=<p>\owner-0.33.0\UnityModManager.dll;<p>\owner-0.33.0\0Harmony.dll;<p>\owner-0.33.0\0Harmony12.dll" `
  --profile "0.33.4=<p>\0.33.4\UnityModManager.dll;<p>\0.33.4\0Harmony.dll;<p>\0.33.4\0Harmony12.dll"
```

## Repository map
- `src/KingmakerWakingRune` — the mod (blueprints, selection, lodge, thin Harmony patches).
- `tests` — framework-free tests for extracted decision logic.
- `scripts/Build.ps1` — reproducible build/package; `scripts/KwrRuntime.ps1` — guarded test harness.
- `docs/RECON.md` — verified engine integration points and donor choices.
- `docs/ACCEPTANCE-WR-01.md` — acceptance results; `STATUS.md` — current state and next action.
