# Status — The Waking Rune

**Milestone:** WR-04-R1 (reachable A2 puzzle and ward integration repairs) on top of WR-04; candidate 0.4.1 ready for owner review.
**Branch:** `feature/wr-04-sanctum-wards` (draft PR #4). Do not merge, tag or release without owner review.
Results: `docs/ACCEPTANCE-WR-04-R1.md` (continuation matrix, which also restates every original WR-04 row) and `docs/ACCEPTANCE-WR-04.md` (the 0.4.0 report). Rules, adaptations and migration: `docs/WR-04-RULES.md`. Progress record: `docs/WR-04-PROGRESS.md`.

## What exists
- Everything listed for WR-04: lodge, token, sanctum, wards, runes, curse, mist, statues, clocks.
- **A2 local solution (CRPG adaptation, documented as such):** two A1 cultists (Support-N1 reporter, Support-N4 attacker) bear an authentic Sihedron palm mark. A one-minute impression yields a `Sihedron Palm Impression`; a marked corpse yields a `Sihedron-Branded Hand`; a living marked reporter uses its own palm. Presenting either item at A2 opens the real door for that contact only.
- Every touch at a fixture is one character's native action: walk, act, resolve against that same body.
- Door hardness once per hit, with restrained feedback; destruction removes the leaf and stops attacks.
- Silent knowledge checks with descriptive results.
- Repairs: feeder ledger consumption, glyph SR in Vandiana's context, typed mist bonus, native turn ends, bounded long work, hound barriers and tether, scoped fixture saves and proxy protection, per-figure cultists.
- Two inherited defects fixed: every reload re-ran legacy initialization, and every reload opened A2 because a fixture stood in its doorway.

## Not done or not proven
See the NOT RUN and PARTIAL rows in `docs/ACCEPTANCE-WR-04-R1.md`, chiefly:
- the integrated six-character walkthrough
- turn-based sessions
- native feeder depletion
- interception of the runner
- the outstanding rune routes (Defense dispel success, Armor success, an obtainable Control route, poison summon, entangle, cold, energy)
- the mist sleep anomaly
- door audio by ear

Open defect: restored cultists show ability scores below and varying around the GameMastery Guide block on the owner's difficulty preset; cause not identified.

## Environment state
See the delivery note in the PR. The candidate is installed per the owner's standing preference; the 0.4.0 install that preceded it is backed up under `runtime-backups\wr04r1-final\mod-existing`. Profile, Steam mirror, registry and UMM params were restored after every run.

## How to test again
```powershell
.\scripts\Build.ps1
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.4.1.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / State / Command / Click / DoubleClick / Key / Hover / Scroll, Close, Collect
.\scripts\KwrRuntime.ps1 End         # restores profile, Steam mirror, registry, Params, and the previous Mods install
```
New harness commands: `items`, `units`, `hitdoor <n...>`, `damagekrune <n>`, `grantfeeder <n>`, `shield <n>`, `move <i> <x> <y>`. Test saves from these runs are under `runtime-evidence\wr04r1-*\saves`. Copy one into the profile after `Begin`; `End` removes it.

## Next exact action
Owner review of draft PR #4. Do not merge, tag or release from this branch without that review.
