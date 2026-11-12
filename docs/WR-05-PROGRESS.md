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

At the end of the first round the open items were: a legitimate victory, the stock-campaign run, a live mid-fight reload (the game refuses saves in combat), and the CMB/CMD calibration.

## Review round (run `wr05-b`, 0.6.8 to 0.6.15)
The owner's review kept PR #5 in draft and asked for fidelity repairs and the completion proof, not a weaker Kurshu.

1. **Preparation.** The departure rest no longer departs. Control returns in the lodge for preparation that stays spent, and a second token use departs on an explicit OK without another rest. The record compares only selections, never spending, and is saved with the game (`DepartureRecord`, `DepartureRecordPart`). `WR-04-RULES.md` is updated.
2. **Grapple.** A bounded, encounter-local grapple:
   - She holds one creature, and both sides are grappled.
   - Each turn she maintains with a standard action (+5, tail and constrict), or lets go to cast while she has offensive spell-like abilities.
   - Freedom of movement defeats the grab and frees a held creature.
   - The hold ends on any lapse.
3. **Feats.** The native Combat Casting and Combat Reflexes, and the printed Improved Iron Will reroll built from the native d20 reroll. The game's own Improved Iron Will is a flat +2, so it is not used.
4. **CMB/CMD.** Traced through the decompiled maneuver rules. The engine already counts Large once (+2 table, −1 size). The +1 `AdditionalCMD` correction was the only unintended point and is removed. The chain printed → hard mode → preset → native matches, before and after Feed.
5. **Intelligence drain.** Drain past 0 leaves the victim comatose instead of dead; restoration wakes it.
6. **Completion proof:**
   - a legitimate victory by ordinary input;
   - defeat to A2;
   - reloads after a full restart, dead and alive (the live one through a labelled combat pause);
   - stock isolation;
   - the legacy notice on screen.

Each build has its own version and zip. 0.6.8 to 0.6.15 are listed with their defects in `ACCEPTANCE-WR-05.md`, and none replaced an earlier package. Results and labels are in `ACCEPTANCE-WR-05.md`.

## Closeout (run `wr06-a`, 0.6.16)
- **The owner's review of 0.6.15 found an ordering defect:** a slam right after restoration, before the periodic recovery check, was erased along with the old held-back points.
- **Fix (`b630849`):** 0.6.16 settles recovery before each new drain, then applies the new drain through the native rule.
- **Verification:** four new unit tests (174 pass), and native cases A, B and C with real Restoration scrolls, across a full restart. All pass; see `ACCEPTANCE-WR-05.md`.
- WR-06 (A4) continues on a child branch.