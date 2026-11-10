# Status — The Waking Rune

**Milestone:** WR-04 (sanctum wards, investigation and rune sabotage), candidate 0.4.0 ready for owner review.
**Branch:** `feature/wr-04-sanctum-wards` (draft PR). Do not merge, tag or release without owner review.
Results and evidence: `docs/ACCEPTANCE-WR-04.md`, which labels what ran natively and what did not. Rules and adaptations: `docs/WR-04-RULES.md`. Progress record: `docs/WR-04-PROGRESS.md`.

## What exists
- WR-02 and WR-03 unchanged: lodge, briefing, shop, free recruitment, refuge token, checkpoint transit, and the sanctum environment with 31 statues and two native doors.
- A save-bound sanctum state carried on the main character, frozen at Subtier 10-11, six PCs, extra challenge.
- A2's conduit door as a real barrier, with door audio, latch, Krune feeder and separate look, study, touch, force, disarm and attack answers.
- Native-trap Alarm, Glyph and feeder discovery and disarm; a scripted Faithful Hound; shared Vandiana awareness and preparation.
- All seven runes with real spell, attack and skill routes, the aggregate Rune of Sloth curse, and Krune's rune projection and real temporary HP.
- The A5 mist hazard, Stone to Flesh outcomes for all 31 figures, restored cultists, the +3 flail, the soul emerald, the two printed clocks and the temporary A4 commitment.
- Investigation dialogues for every fixture in section 3 of the mission.

## Not done or not proven
Boss encounters and AI are deferred. Turn-based timing, captured door audio, and several rune and door routes were not proven in a running game; see the NOT RUN and PARTIAL rows of the acceptance report. No native Sihedron or Knock/Stone Shape exists, so the marked door bypass is unreachable in normal play.

## Environment state
The candidate is installed in `Mods/KingmakerWakingRune` (DLL `83e75cc7…`, zip `2bcab974…`), per the owner's standing preference. The previous 0.3.2 install is at `runtime-backups\wr04-dev3\mod-existing`. Profile, Steam mirror, registry and UMM params were restored after every run.

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.4.0.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Command / Record / Fault / Click / DoubleClick / Key / Type / Hover, Close
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```
Preconditions: Kingmaker not running; Steam running with Kingmaker cloud sync disabled. `End -KeepInstalled` leaves the candidate in place. `Command` sends a labelled test command through the mod's harness probe; it exists only under `capture.flag`.

## Open items for the owner's review
- The NOT RUN and PARTIAL rows of the acceptance report, especially turn-based timing and door audio by ear.
- Restored cultist bodies use a native level-3 cleric: statistics differ from the GameMastery Guide block.
- Carried over from WR-03B: upper tiers hide part of the south terrace, the steep east and west flights, the east door seen edge-on, and an in-session reload's `TryConnectUnits` exception, which the stock game also throws.

## Next exact action
Owner review of the draft PR. Do not merge, tag or release from this branch without that review.
