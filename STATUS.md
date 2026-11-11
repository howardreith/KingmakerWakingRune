# Status: The Waking Rune

**Milestone:** WR-05, Kurshu the Undying in A1. Candidate 0.6.7 is ready for owner review.

**Branch:** `feature/wr-05-kurshu`, a child of `feature/wr-04-sanctum-wards`. PR #4 (WR-04) is still open and unmerged, so the WR-05 draft PR targets the WR-04 branch and shows only the encounter. Do not merge, tag or release without owner review, and merge PR #4 first.

Results:
- `docs/ACCEPTANCE-WR-05.md`: this cycle, PASS / PARTIAL / NOT RUN by check.
- `docs/WR-05-KURSHU.md`: the source-to-runtime stat sheet and every adaptation.
- `docs/WR-05-PROGRESS.md`: checkpoint A (input) and the run record.
- `docs/WR-04-LEDGER.md`: the cumulative ledger, now with a WR-05 section.
- The WR-04 documents are unchanged.

## Candidate
- Version 0.6.7, commit `15f1c71`.
- DLL SHA-256 `07a6823c490b57418d775a2852f26d3af4377e8b09dcb9807b581da71e9baa06`.
- Zip `artifacts\KingmakerWakingRune-0.6.7.zip`, SHA-256 `46396df1ca1582b0e548119600eecfd447fadc1b40c56c6baf2fbe93e1add272`. This is the package tested last and now installed.
- 163 unit tests pass (12 new).
- Earlier test builds 0.6.0–0.6.6 have their own versions and zips; none were replaced.

## What WR-05 adds
- **Kurshu as a real hostile combat unit.** Large outsider; the Erinyes model drawn at Large; native class Hit Dice; stats from the printed Subtier 10–11 block with the extra challenge (Weakness and one negative level removed); the player's preset on top.
- **Native defenses:** regeneration 5 (acid), DR 10/cold iron and magic, resistance 30, SR 26, trip immunity.
- **Attacks:** two slams with 1d4 Intelligence drain, and a tail grab with constrict.
- **Spell-like abilities** with saved daily uses, including bounded limited wishes, one already spent.
- **Power Surge, Divine Separation and Feed.**
- **Her tactics** run through the native brain, with considerations for her printed priorities, plus a bounded director for feeding and a self-only teleport.
- **Narrative:** the wound speech, the blood message, and the existing 90-minute deadline started once on her defeat.
- **Saves:** version 4. Pre-departure saves get the fight; saves already inside the sanctum keep their state, with a notice.

## Proven this cycle
- Ordinary input, the fresh shop, the arrival gate, the hidden check, spell-like abilities and finite uses, and Power Surge.
- Drain, grab, DR, resistance and SR, regeneration and acid, the wound, the defeat and deadline, and death.
- Feed valid and invalid in both modes, turn-based turns, the reload after the fight, the A2 Alarm afterwards, and the legacy policy.

Details and labels are in the acceptance report.

## Not done or not proven
- **A legitimate victory.** The one fight run to its end was a native defeat with the printed values intact. A winning party needs the printed counters (+3 weapons, acid, Freedom of Movement).
- **A stock-campaign run.**
- **A reload with Kurshu alive mid-fight.** The game refuses saves in combat.
- **CMB and CMD:** 23/27/42 against the authored 21/25/39. Calibration is open.
- **Not implemented, deliberately:** plane shift and the non-combat at-will abilities.
- Earlier ledger rows that are still open.

## Owner decisions
- **Merge order:** PR #4, then the WR-05 PR.
- **Victory test:** whether to authorize a victory test with a party re-equipped from the Quartermaster.
- **Python 3.15 removal** remains yours to authorize. None was used.

## Environment state
- **Installed:** candidate 0.6.7, per the owner's standing preference.
- **Backed up:** the 0.5.15 install it replaced, at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr05-a\mod-existing` (DLL `462a2ab2…`). Earlier installs are as listed in previous reports.
- **Restored and verified:** transactions `wr05-input-a` and `wr05-a`, covering the profile, Steam mirror, registry (turn-based setting reset), UMM Params and Mods.
- **Saves:** no owner save was opened.

## How to test again
```powershell
.\scripts\Build.ps1                  # refuses to replace an existing clean package
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.6.7.zip
.\scripts\KwrRuntime.ps1 Launch      # then Shot / Click / Key / Command ..., Close, Collect
.\scripts\KwrRuntime.ps1 End -KeepInstalled
```

**Seeds:**
- `runtime-evidence\wr05-a\saves\Manual_327_Before_the_Sanctum_of_Sloth_1.zks`: rested, about 74 prepared slots, pre-departure.
- `Manual_332_KWR_AUTOMATION_WR05_KURSHU_SLAIN.zks`: after her death.
- The earlier seeds remain under `wr04r*`.

**WR-05 harness additions:**
- Read-only: `kurshu`, `known <member>`, `paused`.
- INJECTED: `hurt <member> acid|fire|cold|physical|magic|coldironmagic <n>`, `corpse deva|axiomite|hellhound|wolf [m]`, `kurshudefeat` (the old fixture).

## Next exact action
Owner review of PR #4, then of the WR-05 draft PR.
