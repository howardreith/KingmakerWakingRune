# WR-05 acceptance: Kurshu the Undying (A1)

**Candidate:** 0.6.7, commit `15f1c71`.
- DLL `07a6823c490b57418d775a2852f26d3af4377e8b09dcb9807b581da71e9baa06`
- Zip `artifacts\KingmakerWakingRune-0.6.7.zip`, `46396df1ca1582b0e548119600eecfd447fadc1b40c56c6baf2fbe93e1add272`
- Branch `feature/wr-05-kurshu`, a child of `feature/wr-04-sanctum-wards` (draft PR #4, unmerged).
- 163 unit tests pass: 151 existing, 12 new in `tests/KurshuRulesTests.cs`.

**Runs:** `wr05-input-a` (checkpoint A, on 0.5.15) and `wr05-a` (0.6.0 to 0.6.7). Evidence is in `C:\Dev\KingmakerWakingRuneLab\runtime-evidence\<run>`.

**Profile:** Subtier 10–11, six-character baseline, extra challenge, under the game's default **Normal** preset.

**Labels:**
- **PLAYER**: ordinary mouse and keyboard.
- **SETUP**: a labelled harness move: preparing spells in the lodge, ending a party turn, a save.
- **INJECTED**: a labelled harness effect: native damage of a named kind, or a corpse of a known creature placed by her.

A native rule still resolves every injected effect. "Pure" means unit tests only.

## Checkpoint A: ordinary input (0.5.15)
**PASS.** R5's in-world input failure did not reproduce. The results by step are in `WR-05-PROGRESS.md`:
- **In the lodge:** selection, floor movement, hover, examine, spell targeting, message boxes, the Quartermaster's shop, and the token's Cancel and OK paths.
- **Fresh game:** a new pregenerated protagonist reached the lodge and opened the full shop: 275 lines, 814 items.

No input repair was needed, so this branch contains none.

## Results
| # | Check | Result | Build | Evidence | Notes |
|---|---|---|---|---|---|
| 1 | Arrival by ordinary input; she appears only after control is restored; nothing attacks under the fade | **PASS** (PLAYER) | 0.6.2–0.6.7 | Log: "waiting to appear: transfer still in progress", then "appears at the apex … control restored", 0.7 s of game time after arrival committed; combat joined after | She does not exist until arrival commits, loading ends, the fade is hidden and the game mode is normal or paused. |
| 1a | Before combat: Displacement only if she believes she is hidden | **PASS** (PLAYER) | 0.6.2–0.6.7 | Stealth 27 vs Perception 37 and 23 vs 35: no precast. Stealth 35 vs 29: "she believes she is hidden", precast Displacement (a real use: 3 → 2) | Native skill checks. |
| 2 | Real-time-with-pause fight | **PASS** (PLAYER) | 0.6.2, 0.6.3 | `132249-fight0`, `133115-b2`, `133209-b3` | Party orders, Freedom of Movement and Acidic Spray by clicks; she held the apex and the party reached her by the east stair. |
| 3 | Turn-based fight and action economy | **PASS** (PLAYER rook button; SETUP end-turn for party turns) | 0.6.6, 0.6.7 | "READ turn: Kurshu … Acting", one swift Power Surge plus one standard ability per turn, then the next unit; Feed took her whole turn | Her first surge was armed in the moment before turn-based took over and was spent on her opening hidden action. |
| 4 | Real spell-like abilities and finite uses | **PASS** | 0.6.2–0.6.7 | Hold Person 3 → 0; Stinking Cloud 3 → 0 (0.6.2); limited wish 2 left at arrival, then two Cones of Cold, then 0; native resource = ledger in every sheet | One wish was pre-spent (reconciled 3 → 2 at spawn). |
| 5 | Regeneration and acid | **PASS** (INJECTED damage) | 0.6.4 | 159 → 164 over a round; −36: "unconscious, regenerating"; acid 6 then "slain … regeneration ended" | Native `AddEffectRegeneration` and the native death threshold. |
| 6 | Intelligence drain on a valid target, persisting | **PASS** (PLAYER fight) | 0.6.0 | "Slam hit Brena Holt: Intelligence drain 2 … drain 2", then 4 more, then Brena died at Intelligence 1 (native rule: an ability score below 1 kills); `IntelligenceDrain` status buff | Saved as native stat drain. A reload with a drained character was not separately shown. |
| 7 | DR, resistance and SR with negative controls | **PASS** (INJECTED damage; PLAYER for SR) | 0.6.4; 0.6.3 | magic only 30 → 20; plain 30 → 20; cold iron + magic 30 → 30; fire 40 → 10; plain arrow 7 → 0 (PLAYER); "Spell resistance 26 against Ilsa Venn's AcidicSpray: roll 10, resisted" | DR needs both cold iron and magic. In this engine a +3 weapon counts as cold iron. |
| 8 | Power Surge: one-round effect and cost | **PASS** | 0.6.2–0.6.7 | Armed, then "Power Surge spent on KWR_KurshuHoldPerson" at DC 22 (20 + 2); unsurged Hold Person in the same fight at DC 20; 3 → 0 uses | Lapse when unused is unit-tested; it was not seen in play. |
| 9 | Valid Feed and invalid-corpse controls | **PASS** (INJECTED corpses) | 0.6.4, 0.6.7 | A wolf (not an outsider) and a hell hound (4 HD) ignored; Movanic Deva (12 HD): "FEEDS … 2 Divine Separation level(s) removed, 0 left; other negative levels untouched"; caster level 13 → 15 | 0.6.4 froze her on an unreachable feed path (fixed in 0.6.5). 0.6.6 never fed in turn-based mode (fixed in 0.6.7). |
| 10 | Save and full restart | **PASS** (SETUP save outside combat) | 0.6.4 → 0.6.5 | `KWR_AUTOMATION_WR05_KURSHU_SLAIN`, full restart, reload: same entity re-bound, nothing spawned, uses unchanged (Hold Person 0, Surge 0, Displacement 2), slain kept, blood message not repeated, deadline unchanged | **Mid-combat saving is refused by the game itself** (also for players), so the reload was taken after the fight. The live-Kurshu, damaged-mid-fight reload is **NOT RUN**. |
| 11 | Defeat starts the existing 90-minute deadline once | **PASS** (INJECTED damage) | 0.6.4 | "DEFEATED at 101263 (unconscious, regenerating); deadline started True, due at 106663"; after death and reload, still 106663 | `RitualRules.DefeatKurshu`, idempotent; unit-tested against regeneration, a second fight and repeated callbacks. |
| 11a | Substantial wound speech, once | **PASS** (INJECTED damage) | 0.6.4 | "substantial wound at 159/219: she speaks" (bark and log) | Trigger: the first time she falls to three quarters of her hit points. |
| 11b | Blood message | **PASS** | 0.6.4 | Notification and log, `134137-defeat` | |
| 12 | Continue to A2's existing content | **PASS** (PLAYER) | 0.6.5 | After reload: Alarm alerts 0 and Vandiana unaware (Kurshu never set it off); Ilsa walked onto the warded stair: "Alarm: Ilsa Venn entered; alert 1" | Deeper A2–A5 content is unchanged code and carried forward in the ledger. |
| 13 | Legacy inside-sanctum save | **PASS** (policy), **PARTIAL** (notice) | 0.6.0 | `KWR_AUTOMATION_R4_A1_ARRIVED`: "migrated to version 4", "legacy save: encounter not inserted", no Kurshu entity, no clock | The notice moved to the first tick in 0.6.1 (it was logged but not visible on 0.6.0); not re-observed since. |
| 14 | Stock campaigns unaffected | **NOT RUN** natively | — | By construction: every hook runs inside `SanctumScope.IsSanctumLoaded` or on the encounter's own unit and blueprints | |
| 15 | A legitimate victory by a suitably equipped party | **NOT RUN** | — | The one fight that ran to its end was a defeat, `133209-b3` | See "Difficulty". |
| — | Stat sheet observed | **PARTIAL** | 0.6.2 | `WR-05-KURSHU.md` | AC, hp, saves, SR, initiative, attack and DCs match the authored values plus the preset. Native CMB 23, grapple 27 and CMD 42 against authored 21/25/39: calibration is still open. |

## Difficulty
- **The fight that ran to its end** was the seeded six-character party (level 11, shop gear, about 74 prepared slots) against her on Normal. They lost: Ilsa, the main character, died, with Kurshu at 219/219.
- **The causes were printed values, working:**
  - Hold Person at DC 22 with Power Surge, three times.
  - Two limited-wish Cones of Cold at DC 24.
  - Spell resistance 26 against the party's caster level 11.
  - DR 10 that needs cold iron **and** magic, where the party's weapons are magic but not cold iron.
  - Resistance 30 to fire, cold, electricity and sonic.
- **The implementation errors found on the way are fixed:** no spells at all before 0.6.2, and self-centred clouds in 0.6.2.
- **What a winning party needs.** The authored values and the preset were not lowered. A party that wins here needs the printed counters, which the Quartermaster sells:
  - +3 weapons (cold iron and magic in this engine);
  - acid (flasks, or acid spells that beat SR), which also stops regeneration;
  - Freedom of Movement before the fight;
  - Spell Penetration or touch attacks.
- **The departure flow** rests and transfers in one step, so a party cannot cast pre-buffs before she acts.

## Defects found and fixed in the run
| Build | Defect | Fix |
|---|---|---|
| 0.6.0 | Daily resources were reconciled to 0 at spawn, so she cast nothing | 0.6.1 reconciles only resources the unit carries at their full maximum |
| 0.6.0 | The A2 Alarm counted her as an intruder | 0.6.1 excepts Vandiana's guardian |
| 0.6.0 | Stealth and CMD miscounted the engine's own Large modifiers | 0.6.1 calibration |
| 0.6.1 | Daily resources built in code had null class lists; the native maximum threw, so the resources never existed, and the swallowed exception stopped her setup | 0.6.2 empty lists; encounter faults are now logged |
| 0.6.2 | All three Stinking Clouds were centred on herself | 0.6.3: never aims an attack or area at herself; point-only areas aim at a foe |
| 0.6.4 | Frozen on a feed path that never arrived | 0.6.5: a director command is dropped after two rounds; that corpse is not retried for three rounds |
| 0.6.5 | Divine Separation re-applied to her corpse on reload | 0.6.6 |
| 0.6.6 | In turn-based mode her brain spent the standard action before Feed could be chosen | 0.6.7 decides Feed at her turn start |

## Not run, or open
- A legitimate victory.
- The stock-campaign check.
- A reload with Kurshu alive and damaged (the game refuses saves in combat).
- An unused surge lapsing in play.
- Plane shift, which is deliberately not implemented (see `WR-05-KURSHU.md`).
- The live CMB and CMD calibration.

## Environment
- `wr05-input-a` and `wr05-a` ended with `End -KeepInstalled`:
  - **Profile:** 7 and 21 differences reverted, added files quarantined.
  - **Steam mirror:** 3 and 11 reverted.
  - **Registry:** restored, which resets the turn-based setting.
  - **Params and Mods:** verified.
- **Installed:** 0.6.7.
- **Backed up:** the previous candidate, 0.5.15 (DLL `462a2ab2…`), at `C:\Dev\KingmakerWakingRuneLab\runtime-backups\wr05-a\mod-existing`.
- **Saves:** only disposable seeds and their checkpoints were loaded. Owner save groups were collapsed and never opened.
- **Python:** none used.
