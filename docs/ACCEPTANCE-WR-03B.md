# WR-03B acceptance report (code-reviewed sanctum repairs)

**Status: candidate ready for owner review. Not merged, tagged or published.**

| | |
|---|---|
| Final source | commit `28f2b3f` (package built clean from it) |
| Package | `artifacts/KingmakerWakingRune-0.3.2.zip`, SHA-256 `1d9467cdd6963ac290c7d49f56ab857d5375c5def0877ba3260fb59aaebe4fab` |
| DLL | SHA-256 `6b402b7eccfcf61029237957dcf2fa64cfa07a237c732b9b0ccfc3332d606cb3` |
| Unit tests | 95 passed, 0 failed. They check generated data and pure logic, not the running game |
| Evidence | `runtime-evidence\wr03b-final1` (names below), earlier investigation in `wr03b-dev1` |
| Game | Pathfinder: Kingmaker 2.1.7b, UMM 0.33.2, 1280×720 |

Three builds were exercised in one guarded transaction, because testing found two defects that needed fixing:

| Build | Commit | What ran on it |
|---|---|---|
| A | `a815ef8` | First belt departure |
| B | `7025ae2` | Summit, traversal, both doors, restart, old saves, faults, inventory route, statues |
| Final | `28f2b3f` | Belt departure, arrival, Favored Terrain, ten reloads |

From B to Final the only code change is one added condition in `SanctumMist.Update`, a using line, and two documentation edits. Everything marked B was not repeated on Final.

**Supersedes** the screenshot-only visibility claims in `ACCEPTANCE-WR-03A.md` and `ACCEPTANCE-WR-03.md`. Closed-door visibility below is measured from the fog mask and native line-of-sight state, as well as seen.

## Results

| # | Requirement | Result | Build | Evidence |
|---|---|---|---|---|
| 1 | Summit stable at rest, under camera pan and zoom, after reload | **PASS** by eye on consecutive frames | B | `140722-final-summit-rest`, `140731-final-summit-camera`, `final-summit-camera-six.png`, `152420-final-summit-after-reload`, `final-summit-reload-six.png`; old build for comparison `wr03b-dev1\110830-old-apex-camera` |
| | Generated upper surfaces hold no coplanar overlap | **PASS** | B, on real meshes | `surfaceAudit: triangles=150 coplanarOverlaps=0` in every state report |
| | Six characters and wolf on north, east and west flights | **PASS** | B | `140639-summit-traverse-east`, `140752-final-traverse-west-north`, `141531-final-descend-north` |
| 2 | Closed A2 from the stairs, A3 never explored, all seven in range | **PASS** | B | `142512-stairs-click`: `behind-A2 visibleNow=0 explored=0`, `revealersInRange=7 withClearLine=0` |
| | Closed east door, A4 and A5 never explored | **PASS** | B | `144135-east-hover-closed`: `behind-east` 0/0 with 7 in range, `before-east` visible |
| | Terrace corner sight, with the wolf at the south-east terrace corner | **PASS** | B | `142422-door-top-view.txt`: `A3-se` 0/0 with 7 in range |
| | Hover target and prompt, including the edge-on east door at normal zoom | **PASS** | B | `142512-stairs-click`, `144135-east-hover-closed`, `144348-east-leaf-hover2` |
| | Opening, passage of all seven including the wolf, reclosing | **PASS** | B | `142527-final-a2-open`, `143419-final-a2-close`, `144145-final-east-open`, `144229-final-east-passage`, `144404-final-east-close` |
| | Explored memory stays, current sight stops | **PASS** | B | `143428-a2-closed-hall`: A2 stairs 0 now, 1 explored; `144413-east-closed-a4`: `before-east` 0 now, 1 explored |
| | Door state and exploration after a full restart | **PASS** | B | `restart-b2` state matches `before-restart` |
| | Refusal: large companion, centre outside the doorway, body overlapping | **PASS** | B | `143105-final-wolf-refused`; wolf centre 1.19 m from the door line, corpulence 1.06, refused and unmoved |
| | Refusal: character | **PASS, with a limit** | B | `a2-refused-serin`: refused in the leaf's swing, 1.87 m from the door line. A character centred exactly on the line was not reached by clicking |
| | Distant units do not block | **PASS** | B | `143358-party-away`: `closeRefusedBy=-`, then closed |
| 3 | Old saves at each changed stair, landing, terrace and both doorways | **PASS** | B | `old-*` states for ESTAIR, APEX, WSTAIR, NSTAIR, NSTAIR2, A2STAIR, A2DOOR, EDOOR, TERRACE, A1BASE: 31 statues, no duplicates, money 445,127, six characters and wolf |
| | Only invalid positions corrected | **PASS, see note** | B | Authentic corrections: Wolf and Bram (NSTAIR2), Aria and Mara (TERRACE). Labelled fixture `fixture-position` also corrected |
| | Pre-door saves open once; an explicit state is kept | **PASS** | B | All pre-door saves open; `old-MIXED` keeps east open, A2 closed |
| 4 | 31 statues, three at each of seven plates, five rune types | **PASS, owner to judge the art** | B | `statues-final-sheet.png`, `*-st-a/b`, `*-st2-c…g`, A1 north and terrace figures in `141521-pan-view`, side-wing figures only partly in frame; manifest in `WR-03B-STATUES.md` |
| 5 | Belt departure: verified checkpoint, party and wolf arrive, token used once | **PASS** | Final | `152707-z-final-belt-departure`, `z-belt-departure-sheet.png`, `z-arrived` |
| | Inventory Use departure from a separate lodge save | **PASS** | B | `150532-inv-context`, `150542-inventory-departure`, `inv-arrived` (`WR03_MIGRATED`) |
| | Injected: save refused, failed, timed out | **PASS, injected** | B | `fault-save-*`: no departure, token kept, message shown, control back |
| | Injected: build failure, invalid arrival | **PASS, injected** | B | `fault-build-failed`, `fault-arrival-invalid`: partial build removed, token kept, native dialog offers the checkpoint; accepting it returned to the lodge with `tokens=1` (`recovered-lodge`) |
| 6 | Reload exception: mod's own | **FIXED** | Final | See below |
| | Reload exception: native | **Inherited, not suppressed** | Final | See below |
| | Favored Terrain | **PASS** | Final | `z-arrived`: `FavoriteTerrainUnderground+2` on Bram Holt |

Injected results come from the harness-only `fault.request` switch. It is inert without `capture.flag`, and each use logs `INJECTED FAULT`.

## Defects found during this run
- **Stair side gaps, caused by this mission's overlap fix.** Trimming the side flights' flanks left stepped holes where the steps rose past a course. The flank now starts at the top of the channel wall. Fixed in `7025ae2`, with two tests.
- **Mod exception on reload.** `SanctumMist.Update` asked for the party after a save load had disposed it. It appeared once in 22 reloads on build B and in older logs. Fixed in `28f2b3f` by not polling during a load. Ten more reloads on Final showed none; that sample cannot prove absence, but the code path is now closed.

## Reload exception
On Final, ten in-session reloads of two candidate-created saves each logged one `NullReferenceException` from `ObstaclesHelper.TryConnectUnits` via `UnitMovementAgent.OnDisable` during `UnloadEntitiesCoroutine`, and nothing from the mod. The same stack reproduced in the stock game with the mod folder absent on a vanilla save (`wr03b-dev1\output_log-native-no-mod.txt`) and on 0.3.0 and 0.3.1. Nothing filters it. The game continues normally after it. A single `BugReportCanvas.OnEnable` exception at startup is also native.

## Notes and untested cases
- **Terrace-edge corrections can move someone noticeably.** In TERRACE, Aria moved 2.7 m along the terrace, because nearer spots were taken, and Mara moved from the terrace's north edge down to the A1 floor. Both stood within the navmesh's margin of the edge. Neither lost anything, but the correction does not try to keep someone on the same level.
- **Old-save check depth.** Party, wolf, money, door policy and scenery count were compared. Inventories were not compared item by item.
- **OLD_WSTAIR and OLD_APEX** hold identical party positions. The log confirms each loaded its own file, so they are one position tested twice.
- **The east door's refusal** was shown by the state probe (`closeRefusedBy`), not by clicking it. The refusal path is shared with A2.
- **Flicker** has no reliable number here: the frame-difference metric moves with camera motion and idle animation, so the finding rests on viewing consecutive frames and the mesh audit.
- **Reload exception on old saves:** the 22 in-session reloads on build B included old saves; the ten on Final were candidate-created.
- **Side-wing bearers (Support-W and E)** were built and counted but seen only at the edge of frames, not framed individually.
- Not exercised: a real disk-full or permission failure (the failures were injected), and Ironman mode, where the token refuses with a message by code path only.
- Loading any save rewrites its file natively, so timestamps of loaded test saves changed. Unrelated saves were not loaded.

## Operator mistakes (not evidence)
In the development transactions, a reload helper confirmed Delete on three of my own disposable saves, which were recreated, and a blind click opened a Delete prompt, which was cancelled. In this run, a misclick started an NPC conversation in a disposable lodge save. The helpers now select by title and use only the facing page's Load button.

## Restoration
Transaction `wr03b-final1` closed: profile (40 differences reverted), Steam mirror (8), registry and UMM params restored and verified, added test saves quarantined in `runtime-backups\wr03b-final1\quarantine`. Per the owner's standing preference, the candidate stays installed: `Mods\KingmakerWakingRune` is 0.3.2, DLL `6b402b7e…6cb3`, without harness flags. The previous 0.3.1 install (DLL `344843a7…57cf`) is kept in `runtime-backups\wr03b-final1\mod-existing`.
