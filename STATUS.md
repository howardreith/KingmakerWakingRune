# Status — The Waking Rune

**Milestone:** WR-03 (Refuge Token and empty Sanctum of Sloth). **Candidate under review.**
**Branch:** `feature/wr-03-sanctum` (from accepted `main`).
Results and evidence: `docs/ACCEPTANCE-WR-03.md`; source-to-built notes: `docs/WR-03-SANCTUM.md`; progress record: `docs/WR-03-PROGRESS.md`.

## What exists
- WR-02: 11 authored lodge NPCs, native briefing, quartermaster shop, free recruitment, level-11 XP, 450,000 gp; save-bound `KWR_*` flags.
- One Refuge Token per save, in the main character's belt (fresh games and WR-02 saves, a full belt displaces one stack to the stash), usable from the belt or by inventory Use.
- Transit: pre-departure checkpoint save, fade, native area load to the printed A1 arrival, token consumed on arrival, fade in.
- The Sanctum of Sloth, A1–A5 with the seven-sided A3 circuit, built at runtime from the page-6 plan. Scenery only: no enemies, traps, loot, timers, functional runes or return portal.

## Environment state
The owner's install at `Mods/KingmakerWakingRune` is WR-02, DLL `280e0c70…e80b`; the harness restored it and the profile, Steam mirror, registry and UMM params after every run. The WR-03 candidate is not installed unless the owner installs it.

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.3.0.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Click / DoubleClick / Key / Type / Hover, Close
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```
Preconditions: Kingmaker not running; Steam running with Kingmaker cloud sync disabled.

## Open items for the owner's review
- The look of the primitive stone figures and the A4 dome rim, which the cutaway camera shows as an arc across the room.
- The local map is rotated 180° from page 6 because the camera faces south on arrival.
- Camping is disabled in the sanctum for this milestone.

## Next exact action
Owner review of the draft PR. Do not merge from this branch without that review.
