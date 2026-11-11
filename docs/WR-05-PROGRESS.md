# WR-05 progress: Kurshu the Undying (A1)

Branch `feature/wr-05-kurshu`, a child of `feature/wr-04-sanctum-wards` (draft PR #4, head `207df16`). PR #4 is open and unmerged, so this branch carries only the encounter on top of it.

## Checkpoint A: ordinary input (run `wr05-input-a`, package 0.5.15, zip `b80c2617…`)

Configuration:
- Owner display settings: one 2560×1440 monitor at 96 DPI (100 %).
- The game window is borderless at 2560×1440, client area 2560×1440 at (0,0), style `0x94000000`, window DPI 96.
- The UI is laid out at 1280×720 and upscaled, so a point on a 1280×720 probe still is clicked at twice its coordinates.
- The harness sends `SetCursorPos` plus `mouse_event`, only to the owned, foreground game window.
- No test command was active during the checks below. The harness's read-only `cursor`, `party`, `slots` and `state` reports were used only to read results.

Results, all by ordinary mouse and keyboard input:

| Check | Result | Evidence (`runtime-evidence\wr05-input-a`) |
|---|---|---|
| Load a disposable save from the Load menu (owner saves left closed) | PASS | `122736-load`, `122751-load3` |
| Hover an NPC: dialogue cursor, unit highlighted | PASS | `cursor`: `DialogCursor hovered=Ambrus Valsin` |
| Select a party member by portrait | PASS | `122814-select-brena` |
| Click the floor: native movement | PASS | Brena moved (-1.88,17.33) → (-1.58,16.96) → (0.20,16.33) |
| Cast a personal spell from the spellbook (Bless) | PASS | `BlessBuff` on the party, slot spent |
| Select and target a spell (Shield of Faith on Tessa) | PASS | `CastCursor`, `pointerMode=Ability`, `ShieldOfFaithBuff` on Tessa, slot spent |
| Open the Quartermaster's shop by dialogue | PASS | `123110-qm-shop` |
| Token, right-click Use, Cancel | PASS | "Departure put off before the rest. Nothing was spent.", token still on the belt (`123206-token-cancelled`) |
| Token, Use, OK, spellbook review, empty-slot warning OK | PASS | Rest 8 h, checkpoint, arrival at A1, version 3 state (`123253-a1`) |
| Hover and examine a statue in A1 | PASS | `InfoCursor hovered=Stone figure (Support-N2)`, the inspection dialogue opened (`123307-examine`) |
| Message-box and dialogue buttons | PASS | Token OK/Cancel, "Quit to main menu" OK, dialogue answers |
| Fresh game: create a pregenerated protagonist, briefing, lodge shop | PASS | `123701-fresh-shop`, `stock`: 275 lines, 814 items, 450,000 gp |

Outcome:
- R5's in-world input failure did not reproduce with the same harness, display and candidate. Its cause was not determined, and no input repair was needed, so this branch has no input change.
- R5's handler-level rows stay as recorded ("with setup").
- Found in passing: the new-game scenario text still says "The adventure does not leave the lodge yet." It is corrected in this branch.
- Environment: `End -KeepInstalled` restored the profile (7 differences), the Steam mirror (3) and the registry; Params.xml and Mods verified. 0.5.15 stays installed.

## Checkpoints B–E (run `wr05-a`, 0.6.0 to 0.6.7)
- **B. Stat sheet and actor.**
  - Source-to-runtime sheet in `WR-05-KURSHU.md`, encoded in `KurshuProfile` and unit-tested.
  - One unit, `KWR_KurshuTheUndying`, identity `A1-Kurshu`.
- **C. Attacks, defenses and finite abilities.** All native components; the ledger is mirrored by the native resources.
  - 0.6.0 wiped her uses at spawn.
  - Until 0.6.2 she had none at all, because the resource blueprints had null class lists.
- **D. Tactics and both modes.**
  - Native brain with her considerations.
  - Fixed along the way: self-centred areas (0.6.3), a frozen feed path (0.6.5), and Feed never chosen in turn-based mode (0.6.7).
- **E. Narrative, deadline, saves and integration.**
  - Wound, blood message and the deadline once.
  - Reload after her death.
  - The A2 Alarm afterwards.
  - The legacy policy.

Results and labels are in `ACCEPTANCE-WR-05.md`. Open: a legitimate victory, the stock-campaign run, a live mid-fight reload (the game refuses saves in combat), and the CMB/CMD calibration.