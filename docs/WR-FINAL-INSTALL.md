# WR-FINAL: installation and restoration report

## Installed now
- **Mod:** The Waking Rune 0.9.25, from `artifacts\KingmakerWakingRune-0.9.25.zip`
  (`e750c929e78fc4843ea8d263758110c6d2666e16c96e234111ef9e643d052a56`), commit `227c3d8` on `feature/wr-final-draft`.
  Installed DLL `abc76875e1074b403dee5d5947464a8b88a4c6df76be4641d0d5cb57afa839d1` (verified after the transaction).
- **Location:** `<Kingmaker>\Mods\KingmakerWakingRune` (Unity Mod Manager 0.28.2, game 2.1.7).
- **Prior version backed up:** 0.9.19 in `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wrfinal-p4\mod-existing`
  (and 0.8.13 before it in `runtime-backups\wrfinal-p3\mod-existing`).

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

No owner save was opened or changed: tests used disposable seeds only. Diagnostic output went to
`C:\Dev\KingmakerWakingRuneLab\runtime-evidence\<run>` (one UMM log per launch, collected before each restart), never
into the game folder.

## To restore the previous version
Close the game, delete `<Kingmaker>\Mods\KingmakerWakingRune`, and copy
`runtime-backups\wrfinal-p3\mod-existing` back to that path. Waking Rune saves made with 0.9.x need the 0.9.x mod.

## To install a build by hand
Extract the zip into `<Kingmaker>\Mods\` (giving `Mods\KingmakerWakingRune\Info.json` and the DLL), or install the zip
through the UMM installer; enable it in UMM (Ctrl+F10 in game shows the version).
