# Status — The Waking Rune

**Milestone:** WR-02 (lodge briefing, cast, quartermaster, free recruitment, level-11 XP, 450,000 gp). **Candidate under review.**
**Branch:** `feature/wr-02-lodge-briefing` (from accepted `main`).
Results and evidence: `docs/ACCEPTANCE-WR-02.md`; design notes: `docs/WR-02-NOTES.md`; stock: `docs/QUARTERMASTER.md`.

## What exists
- 11 authored NPCs in the lodge (Sheila, Ambrus, the Masked Figure, Quartermaster, Society Coordinator, 6 warding agents), spawned once per save.
- Native briefing dialogue with the question hub; follow-up questions via Sheila or Ambrus after it.
- Native shared-vendor shop, native custom-companion recruitment (free, level-11 XP), native party selection.
- 450,000 gp and protagonist XP granted once per save (save-bound flags `KWR_*`); WR-01 saves upgrade once, automatically.

## Environment state
The owner's install at `Mods/KingmakerWakingRune` (WR-01, DLL `72a011dd…`) is preserved by the harness: every run snapshots and restores it,
the profile, Steam save mirror, registry and UMM params. The WR-02 candidate is not installed unless the owner installs it.

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.2.0.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Click / Key / Type / Hover, Close
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```
Preconditions: Kingmaker not running; Steam running with Kingmaker cloud sync disabled. `Shot` is an in-engine capture (harness flag in the mod folder) because the game presents directly to the display; screen capture would read the desktop instead.

## Next exact action
Owner review of the draft PR; then WR-03 (refuge-token activation and departure).
