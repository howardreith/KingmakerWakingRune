# WR-03 acceptance report

**Candidate:** commit `ab56249a0dae237f8cd630a20932d3a30e580d3f` · package `KingmakerWakingRune-0.3.0.zip` SHA-256 `db11e1f3a95676420c96dd1bdc19ebcab9fc7330b4162df04cf7c7c2bc848639` · DLL SHA-256 `50b88f112f7172c738dbe7cbcea422e5658f559d3f313adb46ef74bb7210ee07`. Later commits change only docs and the test project; the rebuilt DLL hash is compared under P01.
**Environment:** Kingmaker 2.1.7b (Steam, cloud sync disabled), UMM 0.33.2, 1280×720. **Tests:** 35/35 (29 at the tested commit, plus six transit policy tests added afterwards).
**Method:** real mouse and keyboard input on the packaged candidate through the guarded harness. Captures were opened and inspected. Read-only state reports (`*.txt`) come from the harness probe, which is inert without `capture.flag`; that flag is written by the harness and is not in the package. No debug helper changes game state.

Evidence (outside the repo; contains game imagery): `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\wr03-accept-a\` and `...\wr03-accept-b\`. Filenames are timestamp-tag. The tested saves are under `wr03-accept-a\saves\`. `superseded-1e90b5b\` is development history. `operator-notes.txt` records operator mistakes that are not evidence.

| ID | Result | Evidence | Caveat |
|---|---|---|---|
| T01 | **PASS** | `t01-new-start`, `t01-after-brief`, `t01-reload-lodge`, `A4-t01-done`, `t05-solo-token`: one token in belt slot 2 of a new game; `tokens=1` after briefing, a load, and a native recruit | |
| T02 | **PASS** | `p-belt-full`, `p-fullbelt-pre.txt`, `t02-post.txt`, `t02-inventory`, `t02-tooltip`: WR-02 save with a full belt; token in slot 5, displaced potion in stash; the state diff shows only the token, its flag and pet/terrain report lines | Money 445,127, party, XP and briefing unchanged |
| T03 | **PASS** | `t03-belt-hover`, `t03-fade-0…5`, `t03-arrive-a/b`, `t03-arrived.txt`: click to control restored, six named characters and wolf in A1 | Stills plus action log, no video. `t03-fade-0` is mid-dim, the rest black |
| T04 | **PASS** | `s03-predeparture.txt` → `t04-moved`, `t04-context-menu` (native Equip/Use/Drop/Info), `t04-fade-*`, `t04-arrive-*`, `t04-arrived.txt`: separate run from a pre-departure save; inventory closed; `mode=Default` on arrival | |
| T05 | **PASS** | `t03-arrived.txt`: all seven at height 0 on the arrival floor, distinct positions, wolf `id` and master unchanged. `t05-solo-arrived`: a party of one departs | The large wolf's feet are overlapped by the low north wall (native blue silhouette) |
| T06 | **PASS** | Double-click on the belt token: one transfer, one token consumed, one new checkpoint (`t03-arrived.txt`, saves). `tests/SanctumPolicyTests.cs` covers the grant, every rejection, repeat clicks and single commit | The double-click run's log was not retained. The kept log (`output_log-152552`) shows one accept per departure. Rejections other than repeat clicks are test-only: no safe UI path reaches them. The 240 s recovery was not forced |
| E01 | **PASS** | `wr03-accept-b\e01-localmap` against page 6; `WR-03-SANCTUM.md`; `SanctumPlanTests` | Local map is rotated 180° from the page by the south-facing camera |
| E02 | **PASS** | `walk-party-up.txt`: full party up the front stairs, west stairs to apex (height 5.7), east stairs down, A2 down to A3. `walk-ccw.txt`: A3 up through A2 and back down the front stairs. `e02-terrace-north`, `e02-apex`, `e02-a3-south` | The A2-up traversal happened during a helper fault; its logged endpoints, and a member at height 1.8, are the evidence |
| E03 | **PASS** | `walk-cw.txt`, `walk-ccw2.txt`: full party and wolf both ways round A3. Seven plate captures `e03-a3a…g` | In `e03-a3a` the party covers the plate; it is clearly visible in `e02-terrace-north` |
| E04 | **PASS** | `e04-a4`, `e04-a5-east`, `e04-a5-west`, `e04-door-approach`, `e04-altar-south-return`, `walk-a4a5.txt`: past the altar on both sides | Sparks are brief; one is visible in `e04-a5-east` |
| E05 | **PASS** | Log: 175 donor entity views suppressed, including 51 spawners and 13 traps. No combat lines. All rooms walked. `s01-a1-*` shows no effects or conditions | No programmatic census of units. Effects were read in A1 only; the mist has no gameplay component by construction |
| E06 | **PASS** | Selection, movement, FOW, indoor lighting and local map across all captures | The A4 dome rim is a visible arc in this camera. Camera-near walls overlap feet with the native silhouette. See Tooling for a helper misread of this |
| S01 | **PASS** | Lodge control `s01-lodge-control.txt`: Underground Ranger +2, Urban Ranger +2 (both match `RichHuman`). Sanctum `s01-a1`, `s01-a3-true`, `s01-a5`, `s01-reloaded`: Underground +2 (Perception 12), Urban 0 (Perception 10). Native breakdown `s01-*-perception` | Recruits levelled to Ranger 3/Fighter 1 through native level-up |
| S02 | **PASS** | `s02-a4-before/after`, `s02-a1-before/after`, `s01-before-restart/reloaded`: full process restart, no state differences | |
| S03 | **PASS** | `s03-predeparture` (token present, `KWR_SanctumEntered` unset) then a second departure (T04). `t01-new-start`: new game with a new `gameId` and clean flags in a process that had loaded a departed save | Checkpoints are separate files: `..._Sloth`, `..._Sloth_1`, `..._Sloth_2` |
| R01 | **PASS** | Stock Main Story `r01-stock-started.txt`: native area, no token, no flags. Recruit flow works. Shop 239 lines / 759 items equals the WR-02 baseline. No Harmony hook touches ordinary usable items | Shop window was not reopened this run |
| P01 | **PASS** | `candidate-identity.txt`; zip holds `Info.json`, DLL, `Icons/RefugeToken.png`, portraits, no `capture.flag`; no lab path in the DLL; restoration below | The zip built with the docs and tests commit `ac942fc` (SHA-256 `525749178f15226619da96f5cc745578422ba4ae3c09ff8bb6b4ef2f85c443d8`, kept in `wr03-accept-a\`) is a repack of the tested zip `db11e1f3…`: same DLL hash, and all 18 packaged files match the tested install's recorded hashes |

**Optional teleport VFX:** omitted. Fade and transfer are implemented.

## Fixed during acceptance
Superseded candidate `1e90b5b` placed the party on the front stairs and terrace with a centred native formation, contrary to the arrival rule. Fixed in `ab56249` with native preset slots, also adding visible sparks and disabling camping. All checks above were rerun on `ab56249`.

## Product failures
None open.

## Tooling notes (no game defect)
- Portrait double-click needed a true double click; first walk attempts drifted.
- Twelve clicks hit the wolf standing next to the hero, then straight-line targets walked the party through A2 to A1.
- A menu misclick and the native "next level opens after Complete" behaviour absorbed queued input; each is in `operator-notes.txt`, and no such capture is cited.

## Not run
- Video of fades or motion (stills and action logs instead).
- Audio.
- Selling the token to a vendor (limitation by code reading).
- Forced timeout or crash paths (excluded by the mission).

## Exceptions seen
`WeatherSystemBehaviour.Update` and `BugReportCanvas.OnEnable` NREs have no mod frames and also appear in accepted WR-01 logs, including a stock-only session.

## Restoration
Both transactions closed. Harness verified profile, Steam mirror, registry, `Params.xml` and Mods restored; added files quarantined under `runtime-backups\wr03-accept-a\` and `-b\`. The owner's install is the WR-02 DLL `280E0C70…E80B`; no candidate is installed.
