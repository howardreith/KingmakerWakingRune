# WR-FINAL: installation and restoration report

## Installed now
- **Mod:** The Waking Rune 0.9.30, from `artifacts\KingmakerWakingRune-0.9.30.zip`
  (`b6adbe38fbff3f56c8b3e8d3f3d954efb263a4e42c87d7a780c3d973c17160b1`), commit `6886bcb` on `feature/wr-final-draft`.
  Installed DLL `317dc363cc2426af65b903f58a306a00103f6959480840a2a5246a3289819688` (verified after the transaction).
- **Location:** `<Kingmaker>\Mods\KingmakerWakingRune` (Unity Mod Manager 0.28.2, game 2.1.7).
- **Prior version backed up:** 0.9.28 in `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wrfinal-p6\mod-existing`
  (0.9.25 in `runtime-backups\wrfinal-p5\mod-existing`, 0.9.19 in `runtime-backups\wrfinal-p4\mod-existing`,
  0.8.13 in `runtime-backups\wrfinal-p3\mod-existing`).

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

No owner save was opened: tests used disposable seeds only. One owner save file was overwritten during `wrfinal-p5`
(an in-game quicksave reused the quick slot `Quick_432`); the transaction's end restored it from the snapshot, and its
hash matches the snapshot. Since then the harness refuses the quicksave and quickload keys. The game's quick slots
rotate across every campaign (decompiled `SaveManager.GetNextQuickslot`), so any test quicksave would overwrite the
owner's oldest one; test saves are named saves made through the harness. Diagnostic output went to
`C:\Dev\KingmakerWakingRuneLab\runtime-evidence\<run>` (one UMM log per launch, collected before each restart), never
into the game folder.

## To restore the previous version
Close the game, delete `<Kingmaker>\Mods\KingmakerWakingRune`, and copy
`runtime-backups\wrfinal-p3\mod-existing` back to that path. Waking Rune saves made with 0.9.x need the 0.9.x mod.

## To install a build by hand
Extract the zip into `<Kingmaker>\Mods\` (giving `Mods\KingmakerWakingRune\Info.json` and the DLL), or install the zip
through the UMM installer; enable it in UMM (Ctrl+F10 in game shows the version).
