# WR-03A acceptance report (Sanctum refinement)

**Candidate:** commit `3093e0347b903acbd9305a50d62be83c9d6878bd` · package `KingmakerWakingRune-0.3.1.zip` SHA-256 `d11846ff3f0c5a937cb326c2f11da8b800bc2b7b9e771717db37e89896423abb` · DLL SHA-256 `344843a7883a0a297ed8d4d99f54c0f0fea96804f70c1b936227cd05a1e857cf`. Later commits change only documents.
**Environment:** Kingmaker 2.1.7b (Steam, cloud sync disabled), UMM 0.33.2, 1280×720. **Tests:** 43/43.
**Method:** real mouse and keyboard input on the packaged candidate through the guarded harness, in one transaction started from this exact package. Captures were opened and inspected. State reports come from the harness probe, which is inert without `capture.flag`; that flag is written by the harness and is not in the package. No debug helper changes game state.
**Specification:** the mission named `WR03A_Claude_Execution_Prompt.md`. That file was not present in the lab folder, the WR-02/WR-03 package zips, or Downloads, Desktop and Documents, so this work follows the brief pasted with the mission. Requirements only in that file are not covered.

Evidence (outside the repo; contains game imagery): `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\wr03a-final2\`. Filenames are timestamp-tag. Review sheets: `before-after-1.png`, `before-after-2.png`, `fixtures-after.png`, `doors-after.png`. `operator-notes.txt` records operator mistakes, which are not evidence. `wr03a-dev1`, `wr03a-accept` and `wr03a-final` are superseded development runs. "Before" views are the WR-03 captures in `wr03-accept-a`: same locations and camera, not pixel-matched.

| Area | Result | Evidence | Caveat |
|---|---|---|---|
| Arrival | **PASS** | `v1-arrival`, `arrival.txt`: six characters and wolf on separate slots at height 0, token gone, belt slot empty, native fade and log `tokens left 0` | Stills and log, no video |
| Ziggurat traversal | **PASS** | `v2-north-stairs`, `v-terrace-west`, `v-apex`, `walk-tour`, `walk-apex`: north flight, terrace, west flight, 6.1 m apex | Apex is small; a full party spreads onto the top of both side flights |
| Clipping | **PASS** | `before-after-1`: characters stand in open channels and on treads, not inside courses | Side flights are still steep, about 59° (were 63.5°) |
| Tier faces | **PASS** | Unit tests; `walk-*`: no one crossed a face. Before the fix, characters could step up or down through them | |
| Arrival view | **PASS** | `v1-arrival`: south-east chain pier no longer in front of the party; wolf's feet visible | Unexplored rooms stay black by design |
| A2 door | **PASS** | `d1-closed-blocked` (order onto A3 floor; all remain north), `d2-a2-hover` (native Open/Door), `d3-a2-open`, `d4-through` (full party in A3), log `opened by`, `closed by` | |
| East door | **PASS** | `e-door-closed`, `e-door-hover`, `e-door-open`, then A4 and A5 reached; `old-door-open`, `old-out` from inside | Edge-on from the default camera: the top of the leaf is the click target until the camera is rotated |
| Closing | **PASS** | Log `A2: closed by`; refusal "Someone is standing in the doorway." seen in development run `wr03a-dev1` on the same code path | |
| Door persistence | **PASS** | `saved`, full process restart, `r1-reloaded`, `walk-r`: east door stayed open and was walked through; A2 stayed closed and held Mara in A1 | The A2-closed state came from a stray helper click, noted in `operator-notes.txt` |
| No door bypass | **PASS** | `TestSeparateSpacesKeepClearOfEachOther`, `TestClosedDoorsSealTheirRoutes`; `d1-closed-blocked`; A3 circuit with the east door closed never entered A4 | |
| Rune fixtures | **PASS** | `fixtures-after`: A3a–g at mapped positions, seven identities unchanged, three figures each | A3c and A3e lean far back to read from the default camera |
| A4 | **PASS** | `v-a4`, `before-after-2`: stepped altar, book, burner, reagents, set emerald, iron braziers, iron mosaic around a dark door-sized space, pedestal and bowl | Portal space is inert |
| A5 | **PASS** | `v-a5`: hexagonal sarcophagus, 12 cracks, four chains to wall piers, restrained mist, energy flashes | The south-east chain is near-level so its pier stays below the view of the arrival floor |
| Older saves | **PASS** | WR-03 A1, A3 and A4 saves load; `old-a4`, `old-door-open`, `old-out`: party in A4 opened the door from inside and left | A save standing on a terrace or stair was not available to test |
| Exploration state | **PASS** | `old-a4`, `r1-reloaded`: previously explored rooms remain revealed; unexplored stay dark | |
| Refuge token | **PASS** | `pre-departure` (`tokens=1`), `t-belt-hover`, `t-fade-a/b/c`, `arrival.txt` (`tokens=0`) | Belt route only. Inventory Use was not re-run; that code is unchanged |
| Favored Terrain | **PASS** | `ft-bram`: Perception 12 with +2 Underground. `ft-kira`: Perception 10, no bonus. `lootSetting=Cyclops` | Read in A3 only |
| Scope | **PASS** | No enemy, trap, rune, clue, loot, awakening or portal logic added; `git diff` against WR-03 | |
| Package | **PASS** | Zip holds `Info.json`, DLL, icon and portraits; no `capture.flag`. Installed files match the zip | |

## Problems found and fixed during validation
- **Tier faces were steppable.** The navmesh's 1.5 m step allowance, after 0.5 m voxel rounding, reached the 1.83 m terrace. Fixed with walking gaps in front of faces; this was a real source of clipping.
- **Doors could be bypassed.** Walls have no thickness on the navmesh, so the A2 stairs joined A3 through their side walls, and an A4 lobe sat 6 cm from A3. Fixed with walking clearances and a plan-wide test.
- **Door cuts did not apply**, then, once they did, reloaded tiles during party placement and **stacked everyone on the stairs**. Found in the acceptance run; cuts now apply after the area finishes loading.
- **Open east door caught floor clicks.** Rehinged so it opens against the passage's far wall.

## Known limitations
- Upper tiers hide part of the south terrace and the head of the A2 stairs from the default camera. Rotating the camera, or clicking the door, gets past this.
- Doors are silent: neither donor scene has a door to borrow a sound from.
- Warning text "Someone is standing in the doorway." is hard-coded English.
- A native `ObstaclesHelper.TryConnectUnits` null reference is logged while reloading a sanctum save from inside the sanctum (3 times in the acceptance run), as the old units are destroyed. The stack has no mod frames and every reload completed normally. The retained WR-03 logs contain none, and the cause is not determined, so it may be new with this build.
- Audio was not assessed, and no video was captured.

## Restoration
All transactions closed with profile, Steam mirror, registry and UMM params verified; `No open transaction`. The final `End -KeepInstalled` stopped at its Mods check because that check assumes no earlier install, so the transaction was finished with a normal `End`, which restored WR-03 0.3.0 (`50b88f11…`), and the tested zip was then installed by hand at the owner's request. Installed: 18 files matching the zip, DLL `344843a7…`, no harness flags. The previous 0.3.0 install is backed up and verified with a receipt at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr03-install-before-wr03a-candidate\`. The owner's saves are unchanged.
