# WR-07A progress notes

Durable notes for Krune's awakening, newest last. Results and labels are in `ACCEPTANCE-WR-07A.md`.

## 1. Ordinary route to A4 (run `wr07a-traverse`, candidate 0.7.22 as installed)

- Seed: 976 (post-Kurshu, A4 placed), a disposable copy. Six members and Marra's bear.
- **The earlier "native pathing could not reach the east corridor" was the latched A2 door.** In 976 the saved state has `Door.Latched: true`. A2 is the only way from A1 into the A3 ring (map page 6), so no path existed. Nothing is broken.
- Route, all by real mouse input (PLAYER):
  1. Brena selected by portrait; the marked figure Support-N1 clicked; "Take an impression of the marked palm (1 minute)": `Impression of Support-N1 made by Brena Holt`.
  2. The A2 door clicked; "Press the Sihedron impression to the handprint": `A2 latch released by Brena Holt with the Sihedron impression; feeder bypassed for this contact (armed=True)`, `Sanctum door A2: opened`.
  3. Ctrl+A, then clicks along A3: south tip → east arm (17–18, 22–24) → past A3a (21–23, 18–20) → beside the east door (21.5–23.3, 15.2–17.8).
  4. The east door clicked ("Open Door" under the cursor): the defenders heard it and drank their potions natively.
  5. A click on the lit A4 floor: the party walked in, the glyph fired, the engagement committed 1d6+6 = 10.
- Two things worth knowing:
  - Three clicks on the A2 stair landed on the alarm ward's highlighted trap area, which the game treats as a disarm order: Tessa disarmed the alarm natively. Avoid the orange-outlined stair when clicking.
  - Clicks into unexplored black (fog) inside A4 gave no move; the lit floor did.
- One HARNESS-COMMANDED move (Tessa to the east side of the stair block) was a diagnostic of connectivity after the trap clicks; it is not part of the route.
- Evidence: `runtime-evidence\wr07a-traverse` (screens 140339–141158, `state-a4.txt`).

## 2. Design (code `src/KingmakerWakingRune/Sanctum/Krune/`)

- `KruneRules.cs`, `KruneManifest.cs`: pure, linked into the tests.
- `KruneBlueprints.cs`, `KruneComponents.cs`, `KruneDialog.cs`, `KruneAwakening.cs`: the native layer.
- State version 6 adds `SanctumState.Krune`.

## 3. Native qualification (run `wr07a-b1`, builds 0.8.0–0.8.7)

Each build was committed and packaged before it was tested; what each test found is in `ACCEPTANCE-WR-07A.md`. In short:
- 0.8.0: the blueprint did not build.
- 0.8.1: out-of-game units are invisible to the game's enumeration; a mist save showed for the immune Krune; dimensional steps duplicated a native resource.
- 0.8.4: the calibration was lost when a save made at ActorReady was reloaded after a restart.
- 0.8.5: the turn-based stagger expired before his first turn.
- 0.8.6: a legacy adoption skipped the revival study.

Practical notes for the next milestone:
- **The game's own log** is recreated at each launch. Run `KwrRuntime.ps1 Collect` before every Close.
- **Clicks that do nothing:** the checkpoint box (and any message box) swallows the space key, so dismiss it first. A click on a highlighted trap orders a disarm. A click into unexplored black gives no move.
- **This party cannot break the emerald legally:** hardness 20, immune to critical hits and precision damage. Use `hitgem` (INJECTED) after a real dialogue attack.
- **Long work** (an impression, chipping out the flail) is refused once awakening is due. Open A2 before waking him.
- **Melee on the A2 leaf** sets off the feeder on the attacker every time. A test that breaks the door drains its fighter.
- **Ilsa is the main character:** her death ends the game. Keep her out of A4 fights.
- **Turn-based mode** is a saved setting that survives a relaunch until the transaction's End restores the registry. Check `paused` and `turn` before toggling.
