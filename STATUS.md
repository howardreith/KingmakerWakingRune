# Status — The Waking Rune

**Milestone:** WR-03B (code-reviewed sanctum repairs), candidate 0.3.2 ready for owner review.
**Branch:** `feature/wr-03-sanctum` (draft PR #3; WR-03, WR-03A and WR-03B are reviewed together).
Results and evidence: `docs/ACCEPTANCE-WR-03B.md`, which supersedes the screenshot-only visibility claims in `docs/ACCEPTANCE-WR-03A.md` and `docs/ACCEPTANCE-WR-03.md`. Statue manifest: `docs/WR-03B-STATUES.md`; source-to-built notes: `docs/WR-03-SANCTUM.md`; progress record: `docs/WR-03-PROGRESS.md`.

## What exists
- WR-02: 11 authored lodge NPCs, native briefing, quartermaster shop, free recruitment, level-11 XP, 450,000 gp; save-bound `KWR_*` flags.
- One Refuge Token per save, in the main character's belt (fresh games and WR-02 saves, a full belt displaces one stack to the stash), usable from the belt or by inventory Use.
- Transit: a verified pre-departure checkpoint save, fade, native area load to the printed A1 arrival, token consumed only once arrival is valid, fade in. A refused or failed checkpoint leaves the party in the lodge with the token; a failure after departure offers the checkpoint.
- The Sanctum of Sloth, A1–A5 with the seven-sided A3 circuit, built at runtime from the page-6 plan: stair channels through the ziggurat, wall-built rune fixtures, refined A4 and A5.
- 31 individually authored petrified figures baked from installed unit models: ten human bearers in A1 and twenty-one drained extraplanar beings at the seven A3 plates.
- Two working native doors: A2 and the east approach to A4. Closed by default, unlocked, untrapped, blocking movement and sight, state saved natively. They will not close on anyone whose body overlaps the doorway or the leaf's swing.
- No enemies, traps, loot, timers, functional runes or return portal.

## Environment state
The owner asked to keep the candidate installed: `Mods/KingmakerWakingRune` is WR-03B 0.3.2, DLL `6b402b7e…`. The previous 0.3.1 install is kept at `runtime-backups\wr03b-final1\mod-existing`, and 0.3.0 at `runtime-backups\wr03-install-before-wr03a-candidate`. Profile, Steam mirror, registry and UMM params were restored after every run.

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.3.2.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Record / Fault / Click / DoubleClick / Key / Type / Hover, Close
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```
Preconditions: Kingmaker not running; Steam running with Kingmaker cloud sync disabled. `End -KeepInstalled` leaves the candidate in place and keeps the earlier install in the transaction's `mod-existing`.

## Open items for the owner's review
- `WR03A_Claude_Execution_Prompt.md` was not found; the work follows the brief pasted with the mission.
- The upper tiers hide part of the south terrace from the default camera.
- The east and west ziggurat flights remain steep (about 59°).
- The east door is edge-on from the default camera.
- Doors are silent. Camping stays disabled in the sanctum. The local map is rotated 180° from page 6.
- The statues' art is for the owner to judge. Their robed lower halves hide the kneeling and seated legs.
- A save at a terrace edge can be nudged farther than expected, in one observed case down to the A1 floor. See `docs/ACCEPTANCE-WR-03B.md`.
- One native `TryConnectUnits` exception per in-session reload remains. It is in the stock game too and is not filtered.

## Next exact action
Owner review of the draft PR. Do not merge, tag or release from this branch without that review.
