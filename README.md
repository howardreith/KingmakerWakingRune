# The Waking Rune for Pathfinder: Kingmaker (opening prototype)

A standalone adventure mod that will eventually make Pathfinder Society Scenario #4–26, *The Waking Rune*,
playable in Pathfinder: Kingmaker. It adds **The Waking Rune** as a fourth choice on the New Game screen, runs the
native difficulty and character-creation screens, and starts the created hero in a populated "Magnimar Lodge —
Sheila's Study" for a scripted briefing. The party can then level to 11, recruit custom adventurers for free through
the Society Coordinator, and spend a shared 450,000 gp at the Quartermaster's shop. When ready, break the Refuge Token
(belt or inventory Use) to carry the party to the Sanctum of Sloth, an explorable, empty environment built from the
scenario's map. It has no enemies or return route yet.

## Requirements
- Pathfinder: Kingmaker 2.1.7b (Steam), Unity Mod Manager 0.33.x (any 0.28+ with Harmony 1.2 support).
- No other mods are required.

## Install
1. Build (or take) `artifacts/KingmakerWakingRune-0.3.0.zip`.
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

## Repository map
- `src/KingmakerWakingRune` — the mod (blueprints, selection, lodge, thin Harmony patches).
- `tests` — framework-free tests for extracted decision logic.
- `scripts/Build.ps1` — reproducible build/package; `scripts/KwrRuntime.ps1` — guarded test harness.
- `docs/RECON.md` — verified engine integration points and donor choices.
- `docs/ACCEPTANCE-WR-01.md` — acceptance results; `STATUS.md` — current state and next action.
