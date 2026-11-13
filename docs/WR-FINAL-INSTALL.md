# WR-FINAL: installation and restoration report

## Installed now
- **Mod:** The Waking Rune 0.9.19, from `artifacts\KingmakerWakingRune-0.9.19.zip`
  (`a4feed246d68042156e510f042b01f0bd0edec4af81078711678b61148abcbbd`), commit `35c54e1` on `feature/wr-final-draft`.
  Installed DLL `377d7ec5bc3b4498a544ce6f325583aa58182d4564e5246c33c14f430950bc7b`.
- **Location:** `<Kingmaker>\Mods\KingmakerWakingRune` (Unity Mod Manager 0.28.2, game 2.1.7).
- **Prior version backed up:** 0.8.13 (WR-07A candidate, DLL `7ac49088…`) in
  `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wrfinal-p3\mod-existing`.

## The test transactions
Every runtime test ran inside the harness's guarded transaction (`scripts\KwrRuntime.ps1`), which snapshots and then
restores the save profile, the Steam remote-storage mirror, the game's registry key, UMM's `Params.xml` and the Mods
folder, and runs only its own game process.

| Run | Builds | Ended | Result |
|---|---|---|---|
| `wrfinal-p1` | 0.9.0 | yes | restored |
| `wrfinal-p2` | 0.9.1–0.9.6 | yes | restored |
| `wrfinal-p3` | 0.9.8–0.9.19 | 2026-11-13 02:21 | profile: 28 differences reverted; Steam mirror: 16 reverted (test saves quarantined in `runtime-backups\wrfinal-p3\quarantine`); registry restored and verified; `Params.xml` and Mods verified; candidate kept installed (`-KeepInstalled`) |

No owner save was opened or changed: tests used disposable seeds only. Diagnostic output went to
`C:\Dev\KingmakerWakingRuneLab\runtime-evidence\<run>` (one UMM log per launch, collected before each restart), never
into the game folder.

## To restore the previous version
Close the game, delete `<Kingmaker>\Mods\KingmakerWakingRune`, and copy
`runtime-backups\wrfinal-p3\mod-existing` back to that path. Waking Rune saves made with 0.9.x need the 0.9.x mod.

## To install a build by hand
Extract the zip into `<Kingmaker>\Mods\` (giving `Mods\KingmakerWakingRune\Info.json` and the DLL), or install the zip
through the UMM installer; enable it in UMM (Ctrl+F10 in game shows the version).
