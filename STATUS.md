# Status â€” The Waking Rune

**Milestone:** WR-03A (Sanctum refinement), on top of WR-03. **Candidate under review.**
**Branch:** `feature/wr-03-sanctum` (draft PR #3; WR-03 and WR-03A are reviewed together).
Results and evidence: `docs/ACCEPTANCE-WR-03A.md` and `docs/ACCEPTANCE-WR-03.md`; source-to-built notes: `docs/WR-03-SANCTUM.md`; progress record: `docs/WR-03-PROGRESS.md`.

## What exists
- WR-02: 11 authored lodge NPCs, native briefing, quartermaster shop, free recruitment, level-11 XP, 450,000 gp; save-bound `KWR_*` flags.
- One Refuge Token per save, in the main character's belt (fresh games and WR-02 saves, a full belt displaces one stack to the stash), usable from the belt or by inventory Use.
- Transit: pre-departure checkpoint save, fade, native area load to the printed A1 arrival, token consumed on arrival, fade in.
- The Sanctum of Sloth, A1â€“A5 with the seven-sided A3 circuit, built at runtime from the page-6 plan: stair channels through the ziggurat, wall-built rune fixtures, refined A4 and A5.
- Two working native doors: A2 and the east approach to A4. Closed by default, unlocked, untrapped, blocking movement and sight, state saved natively.
- No enemies, traps, loot, timers, functional runes or return portal.

## Environment state
The owner asked to keep the candidate installed: `Mods/KingmakerWakingRune` is WR-03A 0.3.1, DLL `344843a7â€¦`. The previous 0.3.0 install is backed up with a receipt at `runtime-backups\wr03-install-before-wr03a-candidate`. Profile, Steam mirror, registry and UMM params were restored after every run.

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.3.1.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Click / DoubleClick / Key / Type / Hover, Close
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```
Preconditions: Kingmaker not running; Steam running with Kingmaker cloud sync disabled. `End -KeepInstalled` only works when no earlier install existed.

## Open items for the owner's review
- `WR03A_Claude_Execution_Prompt.md` was not found; the work follows the brief pasted with the mission.
- The upper tiers hide part of the south terrace from the default camera.
- The east and west ziggurat flights remain steep (about 59Â°).
- The east door is edge-on from the default camera.
- Doors are silent. Camping stays disabled in the sanctum. The local map is rotated 180Â° from page 6.

## Next exact action
Owner review of the draft PR. Do not merge, tag or release from this branch without that review.
