# WR-03B acceptance report (code-reviewed sanctum repairs)

**Status: IN PROGRESS. No candidate has been accepted.** Runtime validation stopped when the owner needed the desktop. Everything below is development evidence from builds before the current commit, and the full acceptance run on the final package is still to do.

## Established so far
| Finding | Evidence | Build |
|---|---|---|
| Summit flicker confirmed in the old build: floor tile and ashlar swap between consecutive frames while the camera moves; no flicker with a still camera | `wr03b-dev1\110830-old-apex-camera.mp4`, `-eight.png`; `110308-old-summit-rest.mp4` | 0.3.1 |
| Generated upper surfaces hold no coplanar overlap | `surfaceAudit: coplanarOverlaps=0` on the real meshes | development 0.3.2 |
| Sight leak cause: a zero-thickness blocker traced one way. With a party in A5, A3-west read visible through two walls; tracing both ways made it hidden, same save | `wr03b-dev1` state reports `beforefix-a5` and `fix1` | development 0.3.2 |
| Second sight leak: the ziggurat's south terrace corners stand past the drawn core wall | `TestTheDrawnWallAloneWouldLeakAtTheCorners`; not yet observed live on its own | model only |
| Reload exception is native: the same `TryConnectUnits` stack reproduces in the stock game with the mod folder absent, on a vanilla save | `wr03b-dev1\output_log-native-no-mod.txt` (no mod lines in the log) | no mod |
| Same exception on the older builds, once per in-session reload | `reloads` runs | 0.3.0, 0.3.1 |
| Old-build saves at the east flight, apex and west flight load with party and door policy intact; doors open once for a pre-door save | `wr03b-accept\old-estair`, `old-apex`, `old-wstair` | cb395b2 |

The old-wstair report was identical to old-apex, so that one is to be re-run rather than counted.

## Still to demonstrate on the final package
Summit: rest, camera movement and traversal of all flights on video, then after reload. Doors: both closed on a never-explored approach, opening, passage, reclosing, explored memory, full restart, closure refusal for a character and for a large companion. Old saves: north flight, both doorways, A2 stairs and terrace, and the labelled `position-invalid` fixture, since no authentic old position has needed correcting so far. Statues: all groups at normal zoom. Token: belt and inventory departures from independent lodge saves, then each labelled injected fault. Reloads and full restart with Favored Terrain.

## Operator mistakes (not evidence)
A blind click on a shifted save list opened a Delete prompt, which was cancelled. An earlier reload helper clicked Delete and confirmed it, removing three disposable old-build saves made minutes before; they were recreated, and the helper now selects saves by title and uses only the facing page's Load button. Each transaction's restore verified the profile and Steam mirror against their snapshots.

## Restoration
`wr03b-dev1`, `wr03b-old2` and `wr03b-accept` are closed with profile, Steam mirror, registry, UMM params and Mods verified. The installed mod is the owner's WR-03A 0.3.1 (DLL `344843a7…`).
