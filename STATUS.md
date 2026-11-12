# Status: The Waking Rune

**Milestone:** WR-06, the Lashmistress encounter in A4. Ready for owner review, together with the WR-05 closeout (Phase A, 0.6.16).

**Branches** (do not merge, tag or release without owner review):
- `feature/wr-04-sanctum-wards`: draft PR #4, base `main`.
- `feature/wr-05-kurshu`: draft PR #5, base the WR-04 branch. Its head `caa2e09` carries the Phase A repair (`b630849`, 0.6.16) and its acceptance notes.
- `feature/wr-06-lashmistress`: the WR-06 draft PR, base the WR-05 branch.

Merge order: PR #4, then #5, then the WR-06 PR.

Results:
- `docs/WR-06-LASHMISTRESS.md`: source-to-runtime sheets, ability coverage, adaptations, countdown and saves.
- `docs/ACCEPTANCE-WR-06.md`: PASS / PARTIAL / NOT RUN by check, the defects found, and the victory.
- `docs/ACCEPTANCE-WR-05.md`: the Phase A cases A–C.
- `docs/WR-04-LEDGER.md`: the cumulative ledger, with WR-05 closeout and WR-06 sections.

## Candidate
- Version 0.7.10, commit `4630671`.
- DLL SHA-256 `4733827d4a050b31bf5165466af5e75974808cfc42f07716f9a0ce9288999776`.
- Zip `artifacts\KingmakerWakingRune-0.7.10.zip`, SHA-256 `95517866a8b2128c195cd26d7dc791722ff502594505f4ea3a006cff6a3d9abb`. Tested last and now installed.
- 187 unit tests pass.
- WR-06 test builds 0.7.0–0.7.9 and WR-05's 0.6.16 have their own zips; none were replaced.

## What WR-06 adds
- **Vandiana**, cleric 3 / conjurer 4 / mystic theurge 5:
  - two native prepared books, filled entry for entry;
  - what she cast before arrival spent from them once;
  - Order's Wrath, Unholy Blight, Command, Darkness and Darkvision as documented encounter-local spells;
  - native channel energy, acid dart and blast rune.
- **Agarik and Jorgas:** fighter 7 plus two shadowdancer Hit Dice, advanced. Native feats, evasion, uncanny dodge, hide in plain sight, ambush stealth, and their own potions.
- **The dark naga:** aberration 10, advanced. Spontaneous slots, sleep poison, poison immunity, guarded thoughts, no trip.
- **Preparation:**
  - Warnings and the east door prepare the defenders through their own actions and resources, once each.
  - Engagement, and only engagement, commits the existing 1d6+6 countdown.
  - The awakening coming due mid-fight leaves A4 running.
- **The support Vandiana fixture is retired.** One canonical Vandiana drives the glyph context, the hound tether and the reporter.
- **Saves:** version 5. An older save already inside gets the defenders only when every fact says A4 is untouched; otherwise it is left as it was, with one notice.

## Proven
Native play unless labelled:
- door preparation without the clock;
- warning-to-Darkvision;
- the engagement commit and its stored roll;
- the awakening during A4;
- a legitimate real-time victory (party AI, setup placement);
- persistence through full restarts;
- stock isolation;
- the fixture's retirement.

Details are in the acceptance report.

## Not done or not proven
- **Partial:** turn-based combat (the defenders act; the party's AI does not in turn-based mode).
- **Not shown in surviving evidence:** an undetected peek, Darkness concealment, Blur and hide in combat, the sting's save, a dispelled Mage Armor, a live mid-combat save, a messenger warning, the fresh pre-departure path.
- **Deferred abilities:** listed with reasons (summoner's charm, arcane bond, Magic Circle, Wind Wall, Sanctuary, Chill Touch and others).
- **Not part of WR-06:** Krune (A5), the return portal, the finale, Society rewards. Plane Shift (WR-05) stays deferred.

## Environment state
- **Installed:** candidate 0.7.10.
- **Backed up:** 0.6.16, at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr06-recon\mod-existing`.
- **Restored and verified:** transaction `wr06-recon` (profile, Steam mirror, registry, UMM Params and Mods).
- **Saves:** no owner save was opened. No Python was used and nothing was installed.

## How to test again
```powershell
.\scripts\Build.ps1                  # refuses to replace an existing clean package
.\scripts\KwrRuntime.ps1 Begin -RunId <new-id> -PackagePath .\artifacts\KingmakerWakingRune-0.7.10.zip
.\scripts\KwrRuntime.ps1 Launch      # then Command "load Manual_97x", Shot / Click / Key ..., Collect BEFORE Close
.\scripts\KwrRuntime.ps1 End -KeepInstalled
```

**Seeds** (in `runtime-evidence\wr06-recon\saves`):

| Save | Contents |
|---|---|
| `974` | Kurshu slain, state version 4: the upgrade path |
| `975` | Departure checkpoint |
| `976` | A4 placed |
| `977` | East door opened, defenders prepared |
| `978` | After the A4 victory |
| `979` | Stock |

**Harness additions:**
- **Read-only:** `catalog <path>`, `a4 [name]`, `a4ai [name]`.
- **INJECTED:** `armslam <member> <n>` (Phase A).

## Next exact action
Owner review of PR #4, #5 and the WR-06 draft PR.
