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
- Newer UMM versions are separate compatibility targets. The baseline change still needs an in-game qualification run; see [UMM compatibility](docs/UMM-COMPATIBILITY.md) for evidence and remaining checks.
- No other mods are required.

## Install
1. Build (or take) `artifacts/KingmakerWakingRune-0.4.0.zip`.
2. Extract it into `<Kingmaker>/Mods/` so you get `Mods/KingmakerWakingRune/{Info.json, KingmakerWakingRune.dll, Icons/..., Portraits/...}`
   (or install the zip through the UMM installer).

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

## Repository map
- `src/KingmakerWakingRune` — the mod (blueprints, selection, lodge, thin Harmony patches).
- `tests` — framework-free tests for extracted decision logic.
- `scripts/Build.ps1` — reproducible build/package; `scripts/KwrRuntime.ps1` — guarded test harness.
- `docs/RECON.md` — verified engine integration points and donor choices.
- `docs/ACCEPTANCE-WR-01.md` — acceptance results; `STATUS.md` — current state and next action.
