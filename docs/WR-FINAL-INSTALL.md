# WR-FINAL: installation and restoration report

## Installed now
- **Mod:** The Waking Rune 0.11.1 (WR-PLAYTEST-02), from `artifacts\KingmakerWakingRune-0.11.1.zip`
  (`0446807b2a9161120d83640c87e7c0c2695d1ee31c2ba2f50a253e295e69f535`), commit `80587b7` on `feature/wr-playtest-02`. Installed
  DLL `39ad4570ea7c9ad0b27975eb1cfce8d2d1b61233d6d17b042f16006cb9e9a929` (verified after the transaction). The install is the DLL
  and `Info.json` only.
- **Location:** `<Kingmaker>\Mods\KingmakerWakingRune` (Unity Mod Manager 0.28.2, game 2.1.7). No other mod is installed
  in this lab's game folder: Mounted Combat, the Tabletop Rules Expansion and Dice Roller were installed only inside
  the runs and removed before each ended.
- **Prior versions backed up:** the released 0.10.14 in `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wrplay2-r1\mod-existing`,
  0.11.0 in `runtime-backups\wrplay2-r2\mod-existing` (0.10.13 in `runtime-backups\wrplay-p4\mod-existing`, earlier
  builds in the earlier runs' folders).

## The test transactions
Every runtime test ran inside the harness's guarded transaction (`scripts\KwrRuntime.ps1`), which snapshots and then
restores the save profile, the Steam remote-storage mirror, the game's registry key, UMM's `Params.xml` and the Mods
folder, and runs only its own game process.

| Run | Builds | Ended | Result |
|---|---|---|---|
| `wrfinal-p1` | 0.9.0 | yes | restored |
| `wrfinal-p2` | 0.9.1–0.9.6 | yes | restored |
| `wrfinal-p3` | 0.9.8–0.9.19 | 2026-11-13 02:21 | profile: 28 differences reverted; Steam mirror: 16 reverted (test saves quarantined in `runtime-backups\wrfinal-p3\quarantine`); registry restored and verified; `Params.xml` and Mods verified; candidate kept installed (`-KeepInstalled`) |
| `wrfinal-p4` | 0.9.20–0.9.25 | 2026-11-13 12:00 | profile: 29 differences reverted; Steam mirror: 12 reverted (test saves quarantined in `runtime-backups\wrfinal-p4\quarantine`); registry restored; `Params.xml` and Mods verified; 0.9.25 kept installed |
| `wrfinal-p5` | 0.9.26–0.9.28 | 2026-11-13 15:03 | profile: 29 differences reverted (including the owner's `Quick_432`, overwritten by an in-game quicksave and restored byte for byte); Steam mirror: 14 reverted; registry restored; `Params.xml` and Mods verified; 0.9.28 kept installed |
| `wrfinal-p6` | 0.9.29–0.9.30 | 2026-11-13 18:50 | profile: 20 differences reverted (test saves and area caches quarantined in `runtime-backups\wrfinal-p6\quarantine`); Steam mirror: 5 reverted; registry restored; `Params.xml` and Mods verified; 0.9.30 kept installed. No quicksave or autosave was written |
| `wrplay-p1` | 0.10.0 diagnostic builds and the candidate | 2026-11-14 02:45 | profile: 23 differences reverted (test saves quarantined in `runtime-backups\wrplay-p1\quarantine`); Steam mirror: 6 reverted; registry restored; `Params.xml` and Mods verified; the optional provider (Tabletop Rules Expansion 0.0.188) was installed only inside the run and removed before it ended; 0.10.0 kept installed. No quicksave or quickload |
| `wrplay-p2` | 0.10.1-0.10.11 (and diagnostic builds) | 2026-11-14 12:47 | profile: 28 differences reverted (test saves quarantined in `runtime-backups\wrplay-p2\quarantine`); Steam mirror: 11 reverted; registry restored; `Params.xml` and Mods verified; the optional provider (Tabletop Rules Expansion 0.0.188, zip hash checked against its `SHA256SUMS.txt`) was installed only inside the run and removed before it ended; 0.10.11 kept installed. No quicksave or quickload; test saves were named harness saves and the game's own checkpoint and rest autosave |
| `wrplay-p3` | 0.10.12-0.10.13 | 2026-11-14 12:59 | profile: 13 differences reverted (quarantined in `runtime-backups\wrplay-p3\quarantine`); Steam mirror: 3 reverted; registry restored; `Params.xml` and Mods verified; 0.10.13 kept installed. No quicksave or quickload |
| `wrplay-p4` | 0.10.14 | 2026-11-15 13:13 | profile: 15 differences reverted (quarantined in `runtime-backups\wrplay-p4\quarantine`); Steam mirror: 3 reverted; registry restored (including the turn-based setting the run switched on); `Params.xml` and Mods verified; 0.10.14 kept installed. No quicksave or quickload |
| `wrplay2-r1` | 0.11.0 and its development builds; 0.10.14 for one shop seed | 2026-11-16 15:53 | profile: 32 differences reverted (test saves quarantined in `runtime-backups\wrplay2-r1\quarantine`); Steam mirror: 16 reverted; registry restored; `Params.xml` and Mods verified; Mounted Combat 0.2.11, Tabletop Rules Expansion 0.0.191 and Dice Roller 0.2.0 (release zips, hashes checked) installed only inside the run and removed before it ended; 0.11.0 kept installed. No quicksave or quickload; test saves were named harness saves and the scenario's own checkpoints |
| `wrplay2-r2` | 0.11.0, then 0.11.1 | 2026-11-16 16:23 | profile: 19 differences reverted (quarantined in `runtime-backups\wrplay2-r2\quarantine`); Steam mirror: 5 reverted; registry restored; `Params.xml` and Mods verified; the same three mods only inside the run; 0.11.1 kept installed. No quicksave or quickload |

No owner save was opened: tests used disposable seeds only. One owner save file was overwritten during `wrfinal-p5`
(an in-game quicksave reused the quick slot `Quick_432`); the transaction's end restored it from the snapshot, and its
hash matches the snapshot. Since then the harness refuses the quicksave and quickload keys. The game's quick slots
rotate across every campaign (decompiled `SaveManager.GetNextQuickslot`), so any test quicksave would overwrite the
owner's oldest one; test saves are named saves made through the harness. Diagnostic output went to
`C:\Dev\KingmakerWakingRuneLab\runtime-evidence\<run>` (one UMM log per launch, collected before each restart), never
into the game folder.

## To restore the previous version
Close the game, delete `<Kingmaker>\Mods\KingmakerWakingRune`, and copy
`runtime-backups\wrplay2-r1\mod-existing` (the released 0.10.14) back to that path. Loading saves made on 0.11.x with 0.10.14
was not tested.

## To install a build by hand
Extract the zip into `<Kingmaker>\Mods\` (giving `Mods\KingmakerWakingRune\Info.json` and the DLL), or install the zip
through the UMM installer; enable it in UMM (Ctrl+F10 in game shows the version).
