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
