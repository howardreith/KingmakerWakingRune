# WR-04 acceptance ledger

This is the **cumulative** ledger for the original WR-04 scope (package `tests/ACCEPTANCE.md`, A01–A31) and the review items added since, **through R5**. Each row is credited from the report that actually recorded a native result; a later report that does not repeat a test does not reset it. Finishing a review round does not by itself verify the rest of the original scope: rows not shown natively stay Partial or Not run.

R5 (0.5.6 to 0.5.15, run `wr04r5-a`) is credited below; see `ACCEPTANCE-WR-04-R5.md`. In R5 mouse input did not reach the game in-world, so its steps were labelled harness calls to the UI's own entry points, with native resolution; R5 rows are therefore "with setup".

Builds: 0.4.0 = `4a109d4` (`ACCEPTANCE-WR-04.md`) · R1 = `86fa288`, `c6d2f08`, `198b66c`, `eeab9d0` · R2 = 0.4.2 (`923f18e`, `b1f9e45`, `44a4b78`) · R3 = 0.4.3–0.4.9 (`cb59cb5` … `94c820d`) · R4 = `f718c4c`, 0.5.1–0.5.5 (`5e05519`) · R5 = 0.5.6–0.5.15 (`c30be66` … `0a7407b`; gameplay last changed in 0.5.11 `4fbef87`).

Status values: **Proven (native play)** · **Proven (with setup/injection — named)** · **Partial** · **Not run** · **Owner**. "Pure" means unit tests of rule code, never counted as a native pass. "Setup" covers labelled HARNESS/SETUP moves, positioning, spawns or dialogue-action openers; "injection" covers granted spells, credentials, damage, buffs or events.

| Item | Status | Evidence | Notes |
|---|---|---|---|
| **Runes and routes** | | | |
| Rune study: silent checks, basic (23) and deep (33) tiers | Proven (native play) | R1 run a (silent); R4 0.5.x Arcana 25 / Religion 38 | Prose has no numbers (R4 unit test). |
| Failed study gives nothing and curses no one | Proven (native play) | 0.4.0, PLAYER Arcana 22 vs 23 | |
| No repeat-click reroll of knowledge | Partial | 0.4.0 pure only | |
| All seven plates dark in one walkthrough (mask 0 → 127) | Proven (with setup — harness move orders; A3c, A3g spells granted) | R3 0.4.7–0.4.9 | |
| Life: negative energy 27 in one round (Harm) | Proven (native play) | R3, Brena's prepared Harm | R2 same route with granted Harm. |
| Life: death spell level 4+ (Slay Living) | Proven (native play) | R3, prepared Slay Living | |
| Life: level 3 death spell rejected | Partial | 0.4.0 rejection predates the level fix | Not repeated. |
| Life: two negative levels | Proven (with setup — 0.4.0 integrated harness run; spell source not itemised) | 0.4.0 Enervation, 4 levels | Proven on 0.4.0; save-exception code changed in R2 (`RuneEffectScan`). Not re-run since. |
| Life: three poison/disease damage to one stat | Proven (with injection — harness-granted Poison) | R3, 2/3 did not disable, 3 → 5/3 did | Proven on 0.4.9; fixture ability-damage handling changed in 0.5.5. |
| Life: native poisonous summon | Partial | R4 0.5.5, monitor lizard rider reached rule, 1/3 Dex | Restacks one buff; no disable possible from this summon. |
| Alacrity: 5 rounds paralysis; Hold Person refused | Proven (with injection — harness-granted Hold Monster) | 0.4.0; R2 `923f18e` | Sustain timing changed in R2 and R3; re-proven below. |
| Alacrity: 10 rounds entangle | Proven (native play) | R3 0.4.8, Marra's prepared Entangle | Failed on 0.4.7 (break-free); fixed 0.4.8. |
| Alacrity: 35 cold in one round | Proven (native play) | R3, two prepared Cones of Cold | |
| Sustained-route boundary (exact, 0.25 s short, restart, turn-based) | Proven (with injection — harness `condition` buffs, goblin spawn) | R3 `cb59cb5` | |
| Interrupted attempt fails and curses | Proven (with injection — harness buff) | R3 28.5 s case; R3 entangle on 0.4.7 | |
| Defense: dispel at DC 25 (fail curses, 25 disables) | Proven (with injection — harness-granted Greater Dispel Magic) | 0.4.0 fail; R2 `923f18e` success | |
| Defense: 35 eligible energy | Proven (with injection — harness-granted Lightning Bolt) | R3 | |
| Defense: 35 single bludgeoning hit | Not run | | |
| Armor: 35 acid/fire | Proven (native play) | R3, Fireball + Lesser Empower rod | R2 28/35 failed correctly. |
| Armor: Trickery 35 | Proven (with setup — harness dialogue answer for Tessa) | R5 0.5.15: 23, 24, 31 failed and cursed correctly; 35 disabled A3e | 0.4.0 Trickery 25 failed correctly. |
| Armor: 35 single piercing/slashing | Partial | 0.4.0 9-point slash failed correctly | Success not run. |
| Control: compulsion 4+ | Proven (native play) | R3, prepared Serenity | 0.4.0 Dominate Monster was harness-granted. |
| Control: 50 summoned damage within the window | Proven (native play) | R4 0.5.x, Summon Monster VI + summon order | Re-proven on 0.5.10/0.5.11 across a full restart (R5, with setup): 42/50 saved, resumed, disabled; no double count. |
| Control: UMD 35 | Proven (with setup — harness dialogue answer for Kael) | R5 0.5.15: UMD 41 disabled A3f | 0.4.0 failure cursed correctly. |
| Control: window timeout / descriptor negatives | Not run | | R4 call-off at 32/50 left A3f active (related, not this test). |
| Damage pipeline: hardness once, thresholds | Proven (native play) | 0.4.0; R1; every R3/R4 hit logged | |
| Adamantine bypass; physical break leaves functional rune | Partial | pure only | |
| Fixture save/break-free scoping | Proven (native play) | R2 Harm (granted); R3 prepared Harm, Entangle | Other plates by pure test. |
| Ability damage never kills a fixture | Proven (native play) | R4 0.5.5, repeated lizard bites | |
| Krune masks (128), HP and CL vectors | Partial | pure, all 128; native mask progression R3 | Numeric HP/CL effects not shown natively. |
| **Curse** | | | |
| Will 20, first brand 1d6 Dex, second brand raises DC only, no same-plate stack | Proven (with injection — attempts used granted spells) | 0.4.0 | Proven on 0.4.0; "acting body" change in R1. Native curse saves seen again R2/R3. |
| Successful save prevents the mark | Proven (with injection — granted Greater Dispel) | R2, four failed dispels, all saves passed | |
| Remove Curse clears brand, damage stays; Restoration heals Dex | Proven (with setup — 0.4.0 harness run) | 0.4.0 narrow | |
| Fatigue clears only at Dex damage 0; unrelated fatigue kept | Not run | | |
| Daily progression once; no retick on load | Partial | pure only | |
| **Mist (A5)** | | | |
| End-of-turn Will 17, stagger, sleep at three failures | Proven (with setup — harness positioning/move orders) | R1 run b; R2 `923f18e`; R3 journey | |
| Sleep holds; survives full restart | Proven (with setup — R2 harness run; inputs not itemised) | R2 `923f18e`, `b1f9e45` | |
| Ally Wake (standard action) | Proven (with setup — harness order) | R2 `b1f9e45` | |
| Damage wake | Proven (native play) | R2 `b1f9e45`, hound bite on sleeper | |
| Streak reset on success; no save while asleep | Proven (with setup — R2 harness run; inputs not itemised) | R2 natural 20, `44a4b78`; R3 Resisted | Reset by a full round outside the mist not shown. |
| ExtraDecline 3-round cap reconciled as wake | Proven (with setup — teleport into mist) | R3 0.4.9 on R1 disposable save | Owner ruling: preset respected. |
| Turn-based exposure; switch to real time, no double tick | Proven (with setup — harness goblin spawn) | R2 `b1f9e45` | |
| Pause/dialogue/loading do not advance exposure | Not run | | |
| Sleep immunity vs stagger immunity | Not run | | |
| Krune immune; Krune summons +4 resistance | Partial | pure only | No Krune summons exist to test natively. |
| **Statues and reporters (A1)** | | | |
| Stone to Flesh, Fort 15 once, corpse or living, no duplicate | Proven (native play) | R4 0.5.x, bought scroll | 0.4.0 and R1 used granted StF. |
| Living attacker uses native abilities | Proven (with injection — harness Stone to Flesh) | R1 run d | |
| Cultist statistics (GMG p.278) | Proven (with injection — harness restoration) | R2 `44a4b78`; R3 | AC follows preset `DifficultOpponent` (ruling). |
| Marked reporter opens A2, reports only on arrival | Proven (native play) | R4 0.5.x, real time and turn-based | R1/R3 with harness StF. The R5 legacy runner did the same after migration. |
| Runner route survives mid-route reload | Proven (native play) | R1 run d; R4 `R4_RUNNER_MIDROUTE2` | |
| Hostile and targetable; Hold Person interception, no messenger | Proven (native play) | R4 0.5.3 | Real time only. |
| Turn-based flight | Proven (with setup — party moved off the stair) | R4 0.5.4 | |
| Blocked turn-based runner ends its turn | Proven (with setup — party placed on the stair, turn-based toggled by harness) | R5 0.5.15: "blocked where it stands; its turn ends", rounds 1–6 in 60 s | Not pushed through bodies. |
| Legacy neutral reporter migrates and resumes | Proven (with setup — legacy save made on 0.4.9 with injected StF) | R5 0.5.7+: hostile True, resumed leg 3/7, reported on arrival; canTarget True | 0.5.6 left the group hostility stale (fixed 0.5.7). A controlled legacy reporter cannot arise; not run. |
| Turn-based interception | Proven (with setup — turn-based toggle, positions, ended party turns, attack order; spells from own slots; injected StF) | R5 0.5.15: Hold Person round 1, killed round 2, leg 0, no messenger | A held runner breaks free on a later save if left alone (native). |
| Unmarked reporter stopped by latched A2 | Proven (with setup — turn-based, party moved clear; injected StF) | R5 0.5.15: N3 reached leg 1, door stayed latched, turned on the party, no report in 7 rounds | In real time the party's own AI killed it first (twice). |
| Corpse hand recovered once and presented | Proven (with setup — harness StF; R2 harness dialogue action) | R1 run b; R2 | |
| A3 husks: all 21 targetable, ash, no XP/loot; rune unaffected | Partial | 0.4.0, one husk to Ash | |
| **Credentials and door A2** | | | |
| Door open/close audio once per transition | Owner | 0.4.0 events post | By ear. |
| Look, study, press, force, disarm, attack as separate answers | Proven (native play) | R1 run a (inscription, press PLAYER) | Metal (Arcana/Nature 18) study not shown by a player. |
| Perception 22 clue, no unlock | Proven (native play) | R1 run a; R3 | |
| Palm impression: one minute, one item, no duplicate on reload | Proven (native play) | R1 run a + restart; R3 | Remake after loss: pure. |
| Presentation opens A2, feeder skipped for that contact | Proven (native play) | R1 run a; R3 | |
| Fresh latch; unmarked press stays latched | Proven (native play) | 0.4.0 press; R1 run a fresh state | |
| Hardness arithmetic; destruction stops attacks, persists | Proven (with injection — harness hits on door proxy) | R1 run a | Ordinary crossbow hits did 0 natively. |
| Same-actor force meets armed feeder | Proven (native play) | R1 run a | |
| Strength break DC 28 | Partial | R1 run a read DC 24 | DC fixed to 28 in `c6d2f08`, not re-run; no success seen. |
| **Alarm (A2)** | | | |
| Perception spot and disarm at 26 from the edge | Proven (with setup — move to approach point) | R3 0.4.7; R4 | |
| Trigger sets awareness; repeats do not re-prepare | Proven (native play) | R1 run a | 0.4.0 same. |
| Coverage includes approaches, excludes arrival | Partial | pure only | |
| Disarmed state survives restart | Proven (with setup — harness move orders) | R3 mask-51 restart | |
| Reload/dispel do not clear alert; Silence; dispel route | Not run | | |
| **Glyph (A4)** | | | |
| One detonation for several entrants; 10 ft, 4d8, Reflex 17 half | Proven (with setup — 0.4.0 harness run) | 0.4.0, three actors | Proven on 0.4.0; resolution changed in R1 (CL 8 context, SR). |
| Spell resistance stops the burst; awareness cause | Proven (with setup — R1 run b; inputs not itemised) | R1 `c6d2f08` | |
| Perception 28 spot; disarm 28 from edge; then no detonation | Proven (with setup — harness move orders) | R3 0.4.7 | Disarm state survived restart (R3). |
| SR penetration; evasion; electricity resistance | Not run | | Open since R1. |
| Lissala-worshipper bypass (not alignment/robe) | Not run | | |
| Spent (detonated) state survives restart; dispel | Not run | | |
| **Hound (A4)** | | | |
| Detects and barks; awareness | Proven (with setup — harness positioning/move orders) | 0.4.0; R1 run b; R2; R3 | |
| Bite +10, 2d6+3, once per round, native attack roll | Proven (with setup — harness move orders) | R2 `b1f9e45`; R3 | Proven on 0.4.0/R1; bite rewritten in `b1f9e45` and re-proven. |
| Nine-round discharge and expiry | Proven (with setup — R2 harness run; inputs not itemised) | R2 | Expired with no one in reach; round count not itemised. |
| Dispel (DC 20) | Not run | | Open since 0.4.0. |
| Not an ordinary attack/loot target | Not run | | |
| Through-wall negative; 100 ft tether | Not run | | |
| See invisibility; mirror images | Not run | | |
| Worshipper and size exceptions | Not run | | |
| **Krune and feeder (A2)** | | | |
| +8 touch, 10d6 empowered, real temp HP to Krune | Proven (native play) | R1 run a, 55 dealt and transferred | 0.4.0 with harness positioning. |
| Touch miss transfers nothing | Not run | | |
| Discovery and disarm 33 | Not run | | |
| Spent temp HP stays spent through restart; no stacking | Proven (with injection — harness feeder grant) | R2 `923f18e`, `44a4b78` | |
| One-hour expiry and one-round acceleration | Partial | pure only | |
| Feeder dispel suppression (1d4 rounds) | Not run | | |
| **Ritual, awareness and clocks** | | | |
| Alarm, glyph, hound, messenger converge; first cause prepares once | Proven (native play) | R1 run d; 0.4.0 | |
| East-door hearing, Cat's Grace once; no duplicate buffs on reload | Not run | | |
| Kurshu event starts 90 minutes once | Proven (with injection — labelled Kurshu event) | 0.4.0 | Injection is by design (spec). |
| A4 commitment 1d6+6 stored; earliest deadline; awakening | Partial | 0.4.0 rolled 9, stored | Firing and live display not run. |
| **Environment and inspection** | | | |
| A3d flail: ten-minute work, one item | Partial | 0.4.0 | Good-aligned draw not run; one-body change in R1. |
| Emerald and sarcophagus | Not run | | Pure only. |
| Altar, mist, portal, apex inspections | Partial | framework on runes only | |
| Magnifying-glass cursor on scenery; spells/attacks unaffected | Proven (native play) | R4 0.5.2+, read-only `cursor` | |
| **Departure and transit** | | | |
| Belt token departure, checkpoint, arrival | Proven (native play) | R1 run a; R3; R4 | |
| Lodge night's rest (Quartermaster) | Proven (native play) | R3 0.4.4 | |
| Departure readiness: notice, cancel, spellbook, warning, rest, transfer | Proven (native play) | R4 0.5.x | |
| Departure retry: changed preparation re-rests; unchanged retry does not | Proven (with setup — harness token/answer/memorize; injected save-failed) | R5 0.5.15: changed Brena re-rested with a notice naming her; unchanged retry departed in 8 s of game time | A recruit joining was not run. |
| No departure rest on sanctum reload | Proven (native play) | R4 | |
| Inventory route, checkpoint recovery, Favored Terrain, stock spells | Not run | | |
| Ordinary campaign untouched (incl. turn-based) | Proven (with setup — harness goblin spawn) | R2 `923f18e` | |
| Six-character prepared party journey | Proven (with setup — harness move orders) | R3 | |
| Profile: six PCs, extra challenge, no hard-mode slots | Proven (native play) | 0.4.0; R3 arrival | |
| **Shop** | | | |
| Existing lodge reconciles new stock only; purchases kept | Proven (native play) | R4, read-only `stock` | |
| Buy; no refill on reload | Proven (native play) | R4 | |
| Fresh new game's shop | Not run | | Same 275-line table. |
| **Save, load and migration** | | | |
| Mid-state reload: mask, windows, Krune, cures, corpse | Proven (with setup — 0.4.0 harness run) | 0.4.0 | |
| Mid-state reload: runner, destroyed door, impression, sleep, sustain, feeder ledger, spent slots | Proven (native play) | R1; R2; R3; R4 | Individual rows above. |
| Summon orders saved, resumed, recalled after a full restart | Proven (with setup — harness spell from own slot, dialogue answers) | R5 0.5.10/0.5.11 | 0.5.9 dropped the order on load (fixed 0.5.10). |
| Summon order released on control loss or expiry; redirect safe | Proven (with injection — granted Confusion) | R5 0.5.11: confused wolf released, Axiomite kept its order; expiry ended an order; redirect no longer hangs | 0.5.10 redirect hung the game (fixed 0.5.11). Domination of a summon not run. |
| Mid-state reload: curse, entangle windows, husk crumbling | Not run | | |
| Legacy saves initialise once; no retroactive triggers; no re-init on reload | Proven (native play) | 0.4.0; R1 fixes | |
| A real 0.4.0 save on a later build; pre-arrival lodge saves | Not run | | |

## WR-05: Kurshu the Undying (A1), 0.6.0–0.6.7, runs `wr05-input-a` and `wr05-a`

The WR-04 rows above are carried forward unchanged. WR-05 changes only A1 and the Alarm's exception for Vandiana's guardian, and A2's Alarm still alerted for a party member afterwards. See `ACCEPTANCE-WR-05.md`.

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Ordinary in-world input (select, move, hover, examine, target, message box, shop, token OK/Cancel) | Proven (native play) | `wr05-input-a`, 0.5.15 | R5's failure did not reproduce; no repair. |
| Fresh-game shop stock | Proven (native play) | `wr05-input-a`, 275 lines / 814 items | Closes the R5 open row. |
| Kurshu appears only after control is restored | Proven (native play) | 0.6.2–0.6.7 | |
| Hidden check and conditional Displacement | Proven (native play) | 0.6.2–0.6.7 | Both outcomes seen. |
| Spell-like abilities, finite uses, pre-spent wish | Proven (native play) | 0.6.2–0.6.7 | |
| Power Surge | Proven (native play) | DC 22 vs 20 | The lapse is unit-tested only. |
| Intelligence drain | Proven (native play) | 0.6.0 | |
| Tail grab and constrict | Proven (native play) | 0.6.0: grab 28 vs CMD 20, constrict 8 | |
| DR, resistance, SR | Proven (with injection: native damage); SR native | 0.6.4, 0.6.3 | |
| Regeneration, acid, death | Proven (with injection: native damage) | 0.6.4 | |
| Wound speech, defeat, blood message, 90-minute deadline once | Proven (with injection: native damage) | 0.6.4 | |
| Reload: no duplicate, no refill, deadline kept | Proven (with setup: save after the fight) | 0.6.4 → 0.6.5 | Mid-combat saves are refused by the game. |
| Feed valid and invalid, real-time and turn-based | Proven (with injection: corpses) | 0.6.4, 0.6.7 | |
| Turn-based action economy | Proven (native play; party turns ended by setup) | 0.6.6, 0.6.7 | |
| Legacy inside-sanctum save: not inserted, no clock | Proven (native play) | 0.6.0 | The notice timing changed in 0.6.1; partial. |
| Legitimate victory | Not run | | A native defeat is recorded. |
| Stock campaign unaffected | Not run | | Scoped by construction. |
| Live CMB and CMD calibration | Partial | 23 / 27 / 42 vs 21 / 25 / 39 | |
## WR-05 review round, 0.6.8–0.6.15, run `wr05-b`

Rows above that this round changes are restated here. Everything else carries forward.

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Preparation after the departure rest; explicit departure without a second rest | Proven (native play; buff casts by setup) | 0.6.8–0.6.11 | Spending never forces a rest; a changed selection is named, with a choice. `WR-04-RULES.md` revised. |
| Departure checkpoint is the prepared party; no rest on load | Proven (native play) | 0.6.10, 0.6.11 | |
| Grab refused by freedom of movement | Proven (with injection: tail hit) | 0.6.11 | |
| Grab, constrict, both grappled; one hold | Proven (with injection: tail hit); one hold pure | 0.6.11 | Grab 46 vs CMD 26, constrict 13. |
| She lets go to cast; maintains with a standard action when out of spells | Proven (with injection: tail hit, spent uses) | 0.6.11 | Maintain 48 vs 29, constrict and tail 18. |
| Hold ends out of reach, or by freedom of movement on the victim | Proven (native scroll; setup placement) | 0.6.11 | |
| Combat Casting, Combat Reflexes, Improved Iron Will | Proven (native play) for the first two; reroll present, unused | 0.6.9 | The game's Improved Iron Will is a flat +2, so not used. |
| CMB / grapple / CMD chain | Proven (native play) | 23/27/41; after Feed 25/29/43 | Replaces "Live CMB and CMD calibration: Partial". |
| Intelligence drain to 0 is comatose; restoration wakes | Proven (with injection: drain; native restoration) | 0.6.15 | Held-back count persists through a reload from 0.6.15. |
| Legitimate victory | Proven (native play; pre-buffs by setup) | 0.6.10 | Replaces "Not run". |
| Defeat to A2 Alarm | Proven (native play) | 0.6.10 | |
| Reload after a full restart, dead | Proven (with setup: save) | 0.6.11 | |
| Reload after a full restart, alive mid-fight | Proven (with injection: one-frame combat pause for the save) | 0.6.11 → 0.6.12 | Replaces the refused mid-combat save. |
| Stock campaign unaffected | Proven (native play; disposable stock save; setup foe) | 0.6.15 | Replaces "Not run". |
| Legacy notice on screen | Proven (native play) | 0.6.9 | Replaces "partial". |
| Power Surge lapse | Proven (native play) | 0.6.11 | Was unit-tested only. |
| Plane shift | Not implemented | | Deliberate. |

## WR-05 closeout, 0.6.16, run `wr06-a`

| Item | Status | Evidence | Notes |
|---|---|---|---|
| A slam right after restoration, before the recovery check, keeps its drain | Proven (with injection: fixed drains and the `armslam` ordering fixture; native Restoration scrolls by player input) | 0.6.16, cases A–C, across a full restart | Defect in 0.6.15 found by owner review. Unit tests added. |

## WR-06 (A4, The Lashmistress), 0.7.0–0.7.10, run `wr06-recon`

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Four real A4 defenders, advanced template on three | Proven (native play) | 0.7.10 sheets | Vandiana cleric 3 / wizard 4 / mystic theurge 5 with two native books, full printed lists. |
| Support Vandiana retired; one canonical Vandiana | Proven | 0.7.2+ | Glyph context and hound tether follow her. |
| Door heard: real potion and naga Invisibility ×2 | Proven (player door click) | 0.7.6 | No clock. |
| Warning prepares Darkvision once | Proven (upgrade path) | 0.7.6 | Messenger not run. |
| Engagement commits one 1d6+6; no reroll on reload | Proven | 0.7.7–0.7.10 | The glyph no longer commits (0.7.7 fix). |
| Awakening due during A4 fires once; A4 continues | Proven | 0.7.10 | Krune (A5) is next. |
| Legitimate victory | Proven (party AI; setup placement) | 0.7.10 | Three of six party members fell. |
| Persistence through full restarts | Proven | 976–978 | Live mid-combat save not run. |
| Stock isolation | Proven | 0.7.10 | |
| Turn-based | Partial | 0.7.10 | Defenders act; party AI does not act in turn-based mode. |
| Undetected scout, Darkness concealment, Blur/hide in combat, sting save, dispel, fresh pre-departure path | Not shown in surviving evidence | | See `ACCEPTANCE-WR-06.md`. Every one was shown in the review pass below. |
| Deferred abilities | Not implemented | | Listed in `WR-06-LASHMISTRESS.md`. Re-audited in the review pass. |

## WR-06 review pass, 0.7.11–0.7.19, run `wr06-pass2`

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Combat preparations settle whoever acts, and gate nothing | Proven (one potion taken, INJECTED) | 0.7.15, 0.7.19 | Agarik's brain drank Blur and then hid. Jorgas's missing Blur ended after a round. |
| In-combat potions actually drunk | Proven | 0.7.15 | Three engine details. The AI treats item abilities as spent (0.7.12). A potion is a move action the brain kept replacing (0.7.13). Ambush flicker (0.7.14). Blur can't target a non-party drinker (0.7.15). Every 0.7.10 in-combat potion action had been inert. |
| Director yields to domination, confusion, fear and resumes | Proven in real time (native buffs applied by INJECTION; a natural confusion too) | 0.7.17, 0.7.11 | Cancel-at-change count was 0 in every observed case. Not landed in turn-based play. |
| Darkness: native exemptions, darkvision, true seeing | Proven | 0.7.16 | The area had never appeared before 0.7.16 (null effect link); Magic Circle had the same defect. Dispel of the darkness itself not shown. |
| Burst outsider rule and descriptors | Proven (SETUP deva) | 0.7.18 | Deva 8d6 against the True Neutral party's 4d8 half share. |
| Whip: weapon enchantment, nonlethal, armor rule, healing | Proven, partly unit-tested (Barkskin removed, INJECTED) | 0.7.16–0.7.17 | Knock-out, revival, rest and another wielder: unit tests only. |
| Naga sting: save, immunity, cure, persistence | Proven (Delay Poison removed, INJECTED) | 0.7.18 | Asleep 4 min, Delay Poison refuses it, Neutralize Poison cures it, survives a restart. |
| Undetected peek | Proven (SETUP Invisibility) | 0.7.18 | Clock untouched. |
| Dispel and restart | Proven (INJECTED combat exit for the save) | 0.7.16 | Mage Armor stays gone, and nothing refills. |
| Fresh arrival from the lodge | Proven (player) | 0.7.18 | Policy Active, placed after the transfer, books filled. |
| Turn-based with manual party orders | Proven (both sides act; the clock commits by rounds) | 0.7.18 | The party lost. |
| Shield, Disruptive, summoner's charm, arcane bond, Magic Circle, Sanctuary, Chill Touch | Proven | 0.7.11–0.7.19 | No failed Sanctuary save seen (DC 15). |
| Older 0.7.10 saves | Proven | 0.7.11, 0.7.19 | Placeholders swapped in place; new facts added. |
| Stock isolation | Proven | 0.7.18 | |
| Fresh A4 victory against 0.7.19 | Not run | | The defenders are stronger than in the 0.7.10 victory. |

## WR-06 third pass, 0.7.20–0.7.22, run `wr06-pass3`

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Nonlethal recovery scoped to the whip's own damage | Proven (negative control with INJECTED foreign nonlethal on a stock save) | 0.7.20 | Heal, rest and revival left 10 and 60 foreign nonlethal untouched. |
| Sanctuary: a kept failure bars only an enemy's hostile targeting | Proven (a real failed save; INJECTED halt and domination) | 0.7.20 | Barred re-attack, then the same character's heal landed after the allegiance change. |
| Director cancels its own running command at a control change | Proven (INJECTED timing fixture) | 0.7.21 | Found the stuck-confusion defect on 0.7.20. |
| Darkness dispelled | Proven (the party's own Greater Dispel, harness-commanded) | 0.7.22 | Found that none of the encounter's spell effects were dispellable before 0.7.22. |
| Whip: another wielder, knock-out, persistence, recovery | Proven (INJECTED setup: HP, Barkskin, halts) | 0.7.22 | Hostile NPCs cannot be knocked out in this game; the whip stops one short. |
| Victory against the repaired encounter | Proven (PARTY AI; PLAYER door and pause; SETUP placement; harness-commanded pre-buffs and opening spells) | 0.7.22 | One of six fell. Deadline passed natively afterwards. |
| Input labels | Corrected | | PLAYER now means real mouse or keyboard input only; harness dispatches are HARNESS-COMMANDED. |

## WR-07A, Krune's awakening, 0.8.0–0.8.7, runs `wr07a-traverse` and `wr07a-b1`

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Ordinary route A1 → A2 → A3 → east door → A4 | Proven (PLAYER) | 0.7.22 | The legitimate A2 opening (palm impression). The WR-06 "could not reach the east corridor" was the latched A2. |
| Awakening from A4's stored countdown, defenders alive | Proven (PARTY AI fight; PLAYER entry) | 0.8.1 | No reroll. The five who entered died, so he woke unobserved. |
| Awakening from Kurshu's deadline, party outside A5 | Proven (SETUP accelerated deadline; native trigger) | 0.8.3–0.8.7 | |
| Emerald early release, stagger in real time and turn-based | Proven (INJECTED defender deaths and breaking blow; PLAYER dialogue attack) | 0.8.5, 0.8.6 | The turn-based stagger outlived its six seconds only from 0.8.6. A late emerald is unit-tested. |
| One real actor, receiver retired, identity resolver | Proven | 0.8.1–0.8.7 | Found by identity in the whole pool (0.8.2). |
| Sheet against the source, masks 0 / 85 / 127 | Proven (SETUP masks) | 0.8.1–0.8.7 | HP 293 / 253 / 233, CL 17 / 15 / 14, class level 17. |
| Revival snapshot against the live rune links | Proven (harness-commanded Entangle from Marra's book) | 0.8.6 | Alacrity dark after waking: AC −2, initiative −2, damage kept, CL unchanged. |
| Feeder ledger handed over, spent, restarted | Proven (PLAYER feeder trigger; INJECTED damage and a False Life control) | 0.8.6 | 54 → 34 handed over with the original hour; never restored. |
| Placement: occupied point, party by the coffin, offscreen | Proven (SETUP positions) | 0.8.2, 0.8.7 | |
| Parley by real input; hostile on every path | Proven (PLAYER) | 0.8.2, 0.8.7 | |
| Battle-bark opening; hostility before the exchange | Proven in the closeout (below) | 0.8.10, 0.8.12 | |
| Development checkpoint: global pause, save and reload, release | Proven in real time, and in turn-based play from 0.8.11 | 0.8.1–0.8.12 | |
| Mage Armor removed stays removed; runes not dispellable | Proven (native dispel in the closeout) | 0.8.6, 0.8.12 | |

### WR-07A review closeout, 0.8.8–0.8.13, run `wr07a-c1`

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Emergence point checked again as he steps out | Proven (SETUP occupant; save and reload) | 0.8.8 | He stepped out one square east; the step was spent once from where he landed. |
| Darkvision in Vandiana's darkness, one shared check | Proven (READ, the game's concealment rule; INJECTED darkness, Blur, removal) | 0.8.8 | None with it, partial without; Blur unaffected. |
| Battle bark during a live real-time A4 fight | Proven (PARTY AI fight; SETUP deadline) | 0.8.10, 0.8.12 | Same tick as the emergence from 0.8.12; both sides held. |
| Checkpoint during turn-based combat | Proven (SETUP tbm; INJECTED emerald hit) | 0.8.11, 0.8.12 | The turn-based controller's own freeze: the turn order kept, no free turn. 0.8.10 and 0.8.11 found the two defects. |
| Opening stagger on his first turn, not renewed by a reload | Proven | 0.8.12 | Combat-save fixture 997. |
| Committed cast before the exchange; hover and cancel | Proven (HARNESS-COMMANDED cast; PLAYER hover and cancel) | 0.8.10 | |
| Emerald broken by legal play | Proven (PLAYER preparation and casts; SETUP native rest; INJECTED defender deaths) | 0.8.12 | Two Blade Barriers and an empowered Fireball. Printed numbers unchanged. |
| Broken model by eye; no re-awakening | Proven (PLAYER click; INJECTED hit; reload) | 0.8.12 | |
| Native Dispel Magic on Mage Armor, across a restart | Proven (HARNESS-COMMANDED dispel; INJECTED release and Domination to allow a save) | 0.8.12 | His Freedom of Movement defeated the INJECTED paralysis; he took Blade Barrier damage and killed Brena. |
| Force and untyped magic against stone | Fixed and proven | 0.8.13 | Disintegrate counted half before. |
| Emerald proxy's fixture Fortitude (+95) | Open for the owner | 0.8.12 | Disintegrate is always partial against it. |
| Saves: version 4 upgrade, version 5 due (adopt and keep), at ActorReady, at the checkpoint, after death | Proven | 0.8.2–0.8.7 | The ActorReady reload found the calibration defect (0.8.4 → 0.8.5). |
| Stock isolation | Proven | 0.8.6, 0.8.7 | |

## WR-FINAL draft, 0.9.0–0.9.18, runs `wrfinal-p1`, `wrfinal-p2`, `wrfinal-p3`

Classification of rows still open at the end of this section: **blocker** (fix before "complete"), **boundary** (an
integration boundary still to test), **adaptation** (accepted, documented), **residual** (optional or noncritical).

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Emerald: authored CL 17 object save, both outcomes | Proven (SETUP controlled d20) | 0.9.0 | d20 1: disintegrated, awakening once; d20 20: 10 damage, 0 after hardness. "Emerald proxy Fortitude +95" closed. |
| A2 door: object save only while its feeder is armed | Proven (SETUP) | 0.9.0 | Armed +10; unarmed never saves. |
| Stock creatures keep native saves | Proven | 0.9.0 | A wolf, negative control. |
| Development checkpoint removed; checkpoint saves migrate | Proven | 0.9.1; 0.9.11 (LEGALBREAK, 998) | One notice, nothing reset, the full encounter. |
| His book: 65 of 65 printed slots, metamagic in raised slots | Proven (READ) | 0.9.1 | Held entries are never castable. |
| Real casting in real time | Proven (PARTY AI and PLAYER orders) | 0.9.2–0.9.18 | Foresight, summons IV/V/VI(empowered)/IX, Cloudkill (empowered), Spell Turning (rebounds logged), Dispel and Greater Dispel, Web, Glitterdust, Solid Fog, Blindness, Acid Arrow, Shield, Globe, Hostile Juxtaposition (linked), Wall of Stone, quickened Dimension Door. |
| Standard spells in close quarters | Proven | 0.9.14 | Failed on 0.9.12 (the native attack queued itself); fixed 0.9.13. |
| Turn-based: quickened plus standard in one turn; summon turns; rounds advance; mode changes both ways | Proven (PLAYER rook, attacks, end turns) | 0.9.17 | Opening stagger in turn-based: WR-07A evidence carried. |
| Line of sight for point spells | Fixed and proven | 0.9.10 | Quickened tentacles and walls were ordered and never started. |
| Tentacles: freedom of movement; one grapple a round | Fixed | 0.9.11 | He was held by his own tentacles (ring enchantment only stopped holds). Re-run not yet seen with tentacles cast. **boundary** |
| Stoneskin on himself | Fixed and proven | 0.9.12 fix; 0.9.17 cast | Native spell admits party members only. |
| Phase Door passages | Fixed and proven | 0.9.12 fix; 0.9.17 probe | Cast on himself, then a passage step through stone ("a passage used, 7 left"). |
| Boots of speed, Wish (bond + diamonds), pearl, ring, rod Empower, telekinetic trip | Proven | 0.9.9–0.9.18 | Wish: 5 diamonds consumed, 10 left; pearl recalled an 8th-level slot; ring turned spells. |
| Dancing spear | Proven (start) | 0.9.17 probe | His hand empty while it fights; its return was not reached before the fight ended. **boundary** (return) |
| Bond limit (second Wish refused), rod depletion | Pure/code only | | **boundary** |
| Spear Ego check when wielded; refusal kept in the save | Code; refusal persistence 0.9.17 | | Not seen in play on 0.9.17. **boundary** |
| Death and aftermath, one loot bag, body consumed | Proven (INJECTED kill) | 0.9.6 (9 items), 0.9.17 (12 items) | |
| Out-of-sight death | Observed | 0.9.17 | Below zero far from the party, he died when the party approached (engine). |
| Portal home: success with both artifacts | Proven (INJECTED kill; PLAYER loot, pedestal, confirm) | 0.9.6 | Handover, Success, no replay on reload. |
| Portal home: withdrawal (Krune alive) | Proven (PLAYER) | 0.9.16 | Warning, verified save, withdrawn debrief, outcome Withdrawn, quest failed; quicksave reload: no replay. |
| Portal home: artifacts left behind | Proven (INJECTED kill; PLAYER) | 0.9.17 | Warning names both, partial debrief, outcome KruneSlainArtifactsMissing, quest complete with the artifacts objective failed. |
| Party defeat | Proven (native) | 0.9.11, 0.9.12 | Game over screen; Load Last Save loaded the profile's newest save. |
| Journal | Proven | 0.9.16, 0.9.17 | Caught up on load; withdrawal and partial outcomes; native journal screenshots. |
| Second new game in one session | Proven (PLAYER) | 0.9.16 | Fresh setup, new game id, journal fresh. |
| Stock campaign after the scenario | Proven (PLAYER) | 0.9.16 | No sanctum part, no journal, prologue normal. |
| Save with a renamed blueprint | Defect found and fixed | 0.9.13 → 0.9.14 | Every awakened-Krune save failed to load; names now kept. |
| Balance probe: same buffed party, mask 127 vs 0 | Proven (SETUP mask; HARNESS-COMMANDED buffs and casts from their own books; PLAYER attack orders; PARTY AI) | 0.9.17 | Seed 998. Mask 127 (233 hp, AC 27): slain in about 15 s of game time. Mask 0 (293 hp, AC 35): the party fell, Krune at 142. Unbuffed parties lost at both masks. |
| Legal victory over Krune | Not achieved | | Requires the legitimate journey with real rune sabotage; see the second attempt below (reached A4 at mask 50). **blocker** for "complete". |
| Complete legitimate playthrough | Not run | | **blocker** for "complete". |
| Contingency, runesight, dominion | Adaptation | | No trigger or eligible target in this game. |
| Mist resistance only for his summons | Proven in 0.9.24 | | See the review pass below. |
| Control effects through save/reload | Proven in 0.9.24 | | See the review pass below. |

### WR-FINAL legitimate journey attempt, 0.9.18–0.9.19, run `wrfinal-p3`

All PLAYER input (mouse and keyboard), PARTY AI in combat; READ diagnostics only, and the camera moved by the harness
(`look`) to find units on screen. Seeds in `runtime-evidence\wrfinal-p3\seeds`.

| Item | Status | Evidence | Notes |
|---|---|---|---|
| New Game, difficulty, pregen protagonist | Proven (PLAYER) | 0.9.18 | Custom difficulty (Normal defaults, auto level-up "All possible"); Yadmila (cleric). |
| Briefing, questions, journal start | Proven (PLAYER) | 0.9.18 | "Prepare and assemble" started; briefing recorded once. |
| Five recruits at the Coordinator, native premade builds, level 1 → 11 in the native level-up UI | Proven (PLAYER) | 0.9.18 | Fighter, paladin, druid (with bear), bard, wizard; the protagonist leveled the same way. |
| Shopping with the shared 450,000 gp | Proven (PLAYER) | 0.9.18–0.9.19 | Rings, belts, headbands, cloaks, amulets, +3 armor and weapons, potions and scrolls; 1,560 gp left. |
| Supplies audit | Proven | 0.9.19 | Two +3 daggers and six Stone to Flesh scrolls in stock; acid flasks, healing, restoration and freedom of movement stocked. **Gap found and fixed:** no cold iron for Kurshu's DR 10/cold iron and magic; native cold iron weapons added (existing lodges reconciled natively: seen in a 0.9.18 lodge save). |
| Spell preparation in the spellbook; departure warnings | Proven (PLAYER) | 0.9.18 | The departure rest named a paladin's empty slots; filled, then rested and departed. |
| Departure, verified save, arrival, journal | Proven (PLAYER) | 0.9.18, 0.9.19 | "Before the Sanctum of Sloth" saves verified; journal moved to "Overcome the sanctum's defenses". |
| Kurshu (A1) with the legitimate party | **Not passed** | 0.9.18–0.9.19 | Five attempts: no cold iron (lost: DR absorbed every hit); cold iron (won: Kurshu defeated, unconscious under regeneration, but no acid was carried, so she could not be finished); then with acid flasks three losses, two with Freedom of Movement on four members. Her Power-Surged Hold Person (DC 20) and Cone of Cold carried each loss. |
| Complete legitimate playthrough | **Blocked at A1** | | See STATUS: a party driven by real-time PARTY AI plus click orders does not reliably beat Kurshu at this profile; a hand-played tactical attempt is the next step. |

**Corrections to the attempt table above (review of PR #8):**
- "No cold iron (lost: DR absorbed every hit)" was wrong. The 0.9.18 log shows Bryn's **+3** longsword hit through the
  damage reduction (15 of 15); only Maren's +2 scimitar was absorbed (10 − 10 = 0). The game treats +3 weapons as cold
  iron (as WR-05 recorded), so the +3 weapons already stocked were an answer; the 0.9.19 cold iron +1 weapons add a
  cheaper one. Every 0.9.19 cold iron weapon carries both an enhancement bonus and cold iron (magic and cold iron).
- "Won, but no acid" was a **defeat**: "DEFEATED at 101206 (unconscious, regenerating); deadline started True, due at
  106606". The 90-minute deadline started as printed; she could not be killed without acid, but A2 was open to them.

## WR-FINAL review pass, 0.9.20–0.9.25, run `wrfinal-p4`

Builds: `1984511` (0.9.20), `0af1be8` (0.9.21), `34032cd` (0.9.22), `5a7fcec` (0.9.23), `5759e23` (0.9.24), `227c3d8`
(0.9.25). Labels as before. INJECTED areas are Krune's own local spell areas spawned at a point with a chosen caster's
context, for focused rule checks only. Logs: `runtime-evidence\wrfinal-p4\umm-log-launch1…7`.

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Solid fog: half of each creature's own speed | Proven (INJECTED area; SETUP positions; INJECTED Haste and Freedom of Movement as controls) | 0.9.20 launch 1 | Marra 30 ft: 3.05 → 1.52 m/s in the fog; the bear 40 ft: 4.06 → 2.03; the **hasted** bear 70 ft → 3.56 (half of 7.11); Ilsa with freedom of movement 3.05 inside (control); Marra 3.05 once given freedom of movement. |
| Solid fog: melee −2 attack and damage; ranged weapons fail; rays pass | Proven (HARNESS-COMMANDED attacks; SETUP spawned foes) | 0.9.20–0.9.23, launches 1 and 4 | Bear, hound and axiomite melee in the fog: −2 / −2 logged on each swing. Ilsa's crossbow from inside and Kael's crossbow from outside through the fog: "lost in the vapors" every shot. Ray of Enfeeblement (Ilsa, Kael) through the fog: "resolves normally". |
| Solid fog: no 5-foot step in turn-based play | Proven (SETUP turn-based; READ turn) | 0.9.20 launch 1 | Axiomite and bear in the fog: `restrictFiveFootStep=True hasFiveFootStep=False`; Ilsa and Marra (freedom of movement) and Kael (outside): step available. |
| Black tentacles: one roll per round, +5 to keep, failure releases, damage only on success | Proven (INJECTED area at CL 17; SETUP spawned axiomites) | 0.9.23 launch 4 | One "round's grapple check" per round for the area; "d20 8 + 22 vs CMD 27: Grappled", next round "d20 7 + 22 + 5 vs CMD 27: Maintained"; at CL 1: "13 + 6 + 5 vs CMD 26: Released" (no crush). |
| Black tentacles: freedom of movement rechecked on the held | Proven (INJECTED Freedom of Movement on a held axiomite) | 0.9.23 launch 4 | "Axiomite slips free of the tentacles (freedom of movement)", then "freedom of movement: Immune" at the next check. |
| Black tentacles: one resolution per creature per round | Proven | 0.9.23 launch 4 | A single area resolved each creature once per round. Two overlapping test areas each resolved it (as two castings would). A creature that broke free mid-round met the same round's check again on re-entry (a new grapple, by rule). |
| Session state cleared on load in the same process | Proven (SETUP saves and loads) | 0.9.21 launch 2 | Before: `issued=Blindness/Deafness@131198.9 cachedFor=b7dd…`, rod at 2 charges. After loading the earlier seed: `issued=none cachedFor=none rod: no order`, `rodCharges=-1` (the seed's own); "session state cleared (area unloading)" and "(area loaded)". |
| Rod order bound to one command | Proven in play | 0.9.21 launch 2 | One order per command ("rod: order SummonMonsterVSingle [Extend]"), spent on that cast only. Cancellation on command end, 12 s, load or death: code and unit tests. |
| Director ignores undetected foes | Proven (INJECTED removal of his see invisibility; INJECTED Greater Invisibility on Tessa) | 0.9.21 launch 2 | `kruneSees=False` for the invisible Tessa; with the party out of his sight: "director idle: no foe he can see within 40 m". |
| Dancing spear: his space, follows a relocation, untargetable | Proven (HARNESS-COMMANDED loose; SETUP positions as the relocation) | 0.9.21–0.9.22 | The form 0.4 m from him after each SETUP move, before and after a reload; `untargetable=True`. |
| Dancing spear: grasped at the end, 4-round lock | Proven | 0.9.21 | "he grasps the dragon-tooth spear as its dance ends (a free action); it cannot dance again until 131159"; Loose then "available False". |
| Dancing spear: reload while dancing | **Defect found and fixed**, then Proven | 0.9.21 → 0.9.22 | The mid-dance save did not load: "Failed to load blueprint by guid 71bfb7d3…", which is `KWR_KruneAi_Director`, never registered since 0.9.0 (so every save made after he woke). Registered in 0.9.22, with a start-up self-check. The same kind of save then loaded, the dance continued and ended in his hand at its saved time. |
| Dancing spear: dropped when he cannot act, then taken up | Proven (INJECTED stun at the dance's end; HARNESS-COMMANDED take-up) | 0.9.22 launch 3 | "its dance over, the dragon-tooth spear drops in his space: he could not grasp it"; take-up as a move action. **Finding:** his director looked for foes first and idled with the spear on the floor; 0.9.23 takes it up first (code; not re-seen in play). |
| Dancing spear: his death while it dances | Proven (INJECTED lethal damage) | 0.9.22 launch 3 | "the dancing spear falls with its master"; his one loot bag lists it. |
| Spear Ego: every attempt, a strike from the second, telekinetic disarm | Proven (INJECTED loot transfer; HARNESS-COMMANDED equip) | 0.9.23 launch 4 | Dorn: attempt 1, Will 23 vs 22, mastered; then "d20 18 + 20 vs CMD 26: disarmed"; attempt 2, struck for 14, Will 16, set down; attempt 3, struck for 10, Will 13, set down. The messages appear in the game log. |
| Rod: Krune's metamagic and charges | Proven in play | 0.9.21 launches 2 and 5 | Extend on his summons (1 charge each); Maximize + Empower on Acid Arrow (5). 0.9.22 keeps the damage feats for 5th-level-and-up spells (unit-tested; not re-seen). Charges taken over from his resource (10) and kept with the rod. |
| Rod: a party wielder | Proven (PLAYER toggle; HARNESS-COMMANDED cast from Kael's own book) | 0.9.23 launch 4 | Kael's abilities panel showed four rod toggles; Extend on by click; Haste: "the rune-carved rod adds Extend to Kael Ashen's Haste: 1 charges spent, 9 left". |
| Rod depletion | Proven in 0.9.26 | | See the second review pass below (insufficient charges: no metamagic, the spell still cast). |
| Wish once a day | Proven (HARNESS-COMMANDED use of his own ability) | 0.9.24 launch 5 | "5 diamonds (25,000 gp) consumed; 10 left", bond 1 → 0; both variants then "available False". |
| Mist +4 only for his summons | Proven | 0.9.24 launch 5 | "Summoned Soul Eater is Krune's summons: +4 resistance (Will 7 -> 11, the larger resistance bonus only)". |
| A control effect through save and reload | Proven (INJECTED Confusion; SETUP save and load) | 0.9.24 launch 5 | After the load: the buff present, "director idle: not free"; once it ended the director resumed ("phase Fight -> Recover", quickened Dimension Door, Wish summons). |
| Lethal damage off-screen | Proven, with a control | 0.9.23 launch 4 | Kept awake: dead in the same tick. Keep-awake suspended: "Conscious" at −7, healed to 2 by fast healing (the suspected defect was real). Implementation doc, section 7. |

### WR-FINAL legitimate journey, second attempt, 0.9.24–0.9.25, run `wrfinal-p4`

From `Manual_1006` (the same legitimate party, built and equipped by PLAYER input in `wrfinal-p3`). Every party action
below is PLAYER input (portraits, spellbook, belt, inventory, dialogue, the turn-based end-turn button); READ for state
and the camera (`look`) only; SETUP only for the named seed saves. No harness move, cast, attack or dialogue.

| Item | Status | Notes |
|---|---|---|
| Supplies | Proven (PLAYER) | The new diamond dust (0.9.25; reconciled into the existing lodge natively: "saved 0, live 40") bought, 13 for 650 gp; Bryn moved to the stocked Longsword +3. |
| Preparation | Proven (PLAYER) | Freedom of Movement scrolls on Sela, Orin and the bear (the rest had it); communal Stoneskin; Resist Cold and Fire (communal); True Seeing and Heroism on Bryn; Barkskin ×3; Mage Armor and False Life on Orin. Turn-based mode on (rook button). |
| Kurshu (A1), turn-based, hand-commanded | **Won, permanently** | Haste; Good Hope; Yadmila's Greater Dispel removed her Displacement (Maren's failed); Aldric's cold iron +1 earth breaker crit for 100 and Bryn's +3 longsword wore her down through two Cure Serious Wounds; "DEFEATED at 101855 (unconscious, regenerating); deadline started True, due at 107255"; Sela's acid flask (spell resistance overcome): "slain at 101866; regeneration ended". **No damage to the party.** Five rounds. |
| A2 by the printed credential | Proven (PLAYER) | Impression of Support-N1's palm; "A2 latch released by Yadmila with the Sihedron impression". The stair's alarm rang (alert 3). |
| Rune sabotage | **3 of 7 by legal play** (mask 50) | Alacrity (A3b): Maren's Entangle held 10 rounds. Armor (A3e): Orin's second Fireball (46 fire − 8 = 38 ≥ 35). Control (A3f): Sela's Use Magic Device 39 vs 35 (after 33 and 29). Defense (A3c) held: Aldric's hits top out below 43 and two dispels failed (17 and 20 vs 25); one curse brand on Aldric. Life ×3: no negative energy, poison or death spell prepared. |
| A4 door opened; defenders preparing | Reached | The defenders buffed natively (Blur, Bull's Strength, Cat's Grace, invisibility). |
| A4 fight, Krune, artifacts, return, debrief | **Not run in this pass** | Stopped at the A4 door for this session's budget (each turn costs many PLAYER clicks). Seeds: `Manual_1011` (Kurshu slain), `Manual_1012` (mask 50), `Manual_1013` (A4 door open). |

**Player planning note:** stoneskin takes more dust than the 13 bought covered for a second set; not a mod defect.

**Supersession (second review of PR #8).** Saves made after Krune woke could not load on 0.9.0–0.9.21 (his director
action was unregistered). Any earlier claim that rested on saving and reloading an *awakened* 0.9.x Krune before 0.9.22
is superseded by the 0.9.22+ reloads in the table above and below (mid-dance, control effect, rod balance). Earlier
evidence about pre-awakening saves, ordinary combat and unchanged rules stands. The combat-save fixture
(`combatsave`, INJECTED) remains a separately labelled fixture.

## WR-FINAL second review pass, 0.9.26–0.9.28, run `wrfinal-p5`

Builds: `0793c73` (0.9.26, rod renewal and shared charges), `6ca1d0c` (0.9.27, out-of-combat spear pickup), `253ee17`
(0.9.28, harness READ/camera for grid points only). Logs: `runtime-evidence\wrfinal-p5\umm-log-launch1…4b`.

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Rod renewal while full (the reviewer's sequence) | **Defect fixed (0.9.26)**, Proven | launch 1 | Seed 998, rod taken from Krune (INJECTED kill and loot). First read: `stored 10 renewedAt 131185`. INJECTED +25 h, unread: unchanged. Ilsa's Maximize (PLAYER toggle; HARNESS-COMMANDED cast from her book): "renewal due (10 -> 10); next renewal at 303985", then "3 charges spent, 7 left"; further READs and a cast without metamagic: still 7 (no refund). |
| Rod balance across save and load | Proven (SETUP save/load) | launch 1 | Saved at 1 charge; after the load `stored 1 renewedAt 217585`; a read: still 1. |
| Depleted rod renewed on schedule | Proven (INJECTED +25 h) | launch 1 | "renewal due (1 -> 10)", then Maximize spent 3: 7 left. |
| Two casters sharing the last charges; insufficient charges | Proven (PLAYER toggles on Ilsa and Brena; HARNESS-COMMANDED casts) | launch 1 | 4 left, both cast at once: Ilsa's Maximize applied (1 left), Brena's "[Maximize] not applied (1 charges free)" and her spell cast normally. At 0: "not applied (0 charges free)" each time; never a negative or unpaid balance. |
| Spear dropped, taken up by his director in combat | Proven (HARNESS-COMMANDED loose; INJECTED stun at the dance's end) | launch 1 (0.9.26) | "director: KWR_KruneGraspDancingSpear … take up his spear", then "he takes up the dragon-tooth spear from the floor (a move action)". **Finding:** out of combat his brain is never consulted, so it lay there until a fight began; 0.9.27 takes it up out of combat too: "out of combat, he takes up his dropped spear" (launch 2). |
| Rod damage feats kept for 5th level and up | Seen in play | launch 4 | "Cloudkill (Empower) … with the rod's Maximize"; Acid Arrow and lesser spells got none. |

### WR-FINAL legitimate journey, third attempt, 0.9.28, run `wrfinal-p5`

Every party action PLAYER (portraits, spellbook, belt, inventory, dialogue, turn-based buttons); PARTY AI in real-time
fights; READ for state and the camera; SETUP only for named seed saves and loads.

| Step | Result | Notes |
|---|---|---|
| A4 from `Manual_1013` (mask 50) | **Won** (PARTY AI + PLAYER orders) | All four defenders slain; Aldric and Maren died and rose after the fight at 11 hp (Normal difficulty rule); four Heal scrolls (PLAYER) restored everyone. |
| Krune at mask 50 | **Lost twice** | Attempt 1: Krune to 13/293, then Phase Door into A1 and recovered with three Runes of Life (fast healing 3) while summons and Cloudkill killed the party. Attempt 2: to 89/293, the same pattern. |
| Return to the lodge save `Manual_1010` | Legal (an earlier legitimate save) | Rest in the sanctum is refused ("You cannot rest here"), so preparation changed at the lodge. |
| New preparation | PLAYER | Yadmila: Slay Living ×2, Harm, Delay Poison (communal) ×2 in place of Righteous Might, Mass Cure, Blade Barrier and two level-3 slots; the departure flow noticed the change and rested again (PLAYER). Diamond dust 17 more (850 gp). |
| Kurshu, turn-based, hand-commanded | **Won, permanently** | Displacement dispelled by Maren; Bryn and Aldric; Sela's acid flask: "slain at 131085". |
| Rune sabotage | **4 of 7** (mask 75) | All three Life runes (Slay Living ×2: DeathSpell; Harm: 110 negative energy) and Alacrity (Entangle, 10 rounds). Armor held (Fireballs 38 and 33 in separate rounds; 35 after hardness needed in one round). Control held (Use Magic Device 26, 32, 28). Sela and Orin each took a curse brand. |
| A4 (replay) | **Won without a death** | Opened the A4 door (PLAYER); Haste; all four defenders slain. |
| Krune at mask 75 (233 hp, no fast healing) | **Lost three times** from `Manual_1022` (pre-fight save) | Attempt 3: to **12/233** within two rounds; he relocated (quickened Dimension Door, Phase Door ×4), summoned Wish's army, Summon Monster IX, VII and VI, and the party fell to the summons while chasing; he stayed at 12 (no fast healing). Attempts 4 and 5: to 149 and 146. |
| Artifacts, return, debrief, completed-outcome reload | **Not reached** | |

**Assessment.** These are real-time fights driven by party AI with my re-issued attack orders; the AI chases a
relocating Krune into his summons. Attempt 3 brought him to 12/233 at mask 75. *(Corrected after the third review:
this showed that the party could bring him close to death, not that the fight could be won, nor that the final attack,
his death, the loot and the return work together. The completed run in the next section now shows those.)* The five
losses were two at mask 50 (293 hp, fast healing 3) and three at mask 75 (233 hp, no fast healing). Nothing here
suggests lowering Krune.

## WR-FINAL third review pass, 0.9.29–0.9.30, run `wrfinal-p6`

Builds:
- `b973612` (0.9.29): the spear's control guard; the rod's promise bound to the cast rule; harness `armdamage`.
- `6886bcb` (0.9.30): the dance's end follows the same control test.

The harness also refuses F5 and F9 (script-only commit `78fc403`; see the install report). Logs are in
`runtime-evidence\wrfinal-p6\umm-log-launch1…6`.

| Item | Status | Evidence | Notes |
|---|---|---|---|
| Out-of-combat spear pickup under a control effect | **Defect fixed (0.9.29–0.9.30)**, Proven | launch 3 (0.9.30) | **Fixture (seed 998):** HARNESS-COMMANDED loose; INJECTED Stunned as the dance ended, plus INJECTED native Confusion for 36 s. **Result:** "its dance over… drops in his space", then "out of combat, his spear lies at his feet, but he is not his own master (… confused True …): it stays there". It lay there for 33 s and was taken up only once the confusion ended. **On 0.9.29 (launch 2), with Dominate Person:** the native buff lapsed about 3 s after the brief fight its faction change started, so that run did not isolate domination. It did show that the end of the dance still tested only "can act", so 0.9.30 applies the same control test there. |
| Dance ends while dominated | Seen (0.9.30) | launch 3 | **Fixture:** INJECTED Dominate Person for 60 s with no stun. **Result:** the spear dropped as the dance ended ("he could not grasp it"), because he was not his own master. His director took it up only after the domination had lapsed and he was back with the Mobs. |
| Rod: a preview and cancel by one caster, then another caster's cast | Proven (0.9.29) | launch 1 | **Fixture (seed `P5_ROD_ONE`):** time and rests INJECTED to reach 4 charges. **Preview (PLAYER):** Ilsa selected Burning Arc and aimed it (`pointerMode=Ability`), hovered an ally, then right-clicked to cancel. READ after each step: `pending []`, 4 stored. **Then:** Brena's Maximize (HARNESS-COMMANDED cast) applied: "3 charges spent, 1 left". A preview or cancel reserves nothing; the decompiled game never calculates spell parameters for the UI. |
| Rod: interrupted cast | **Source defect fixed (0.9.29)**, Proven | launch 1 | **The defect (source):** a concentration check calculates the spell's parameters (when entangled, or damaged mid-cast). In 0.9.28 that calculation reserved the charges for 6 s, so a failed check held them back from other casters. **Fix:** in 0.9.29 only the cast rule's own calculation promises charges. **Run:** INJECTED timing fixture `armdamage`: 60 damage while Ilsa's Maximize False Life was being cast. Logged: "read outside its cast (a concentration check): [Maximize], nothing promised (4 charges free)". The spell was interrupted and its slot spent natively; nothing was charged and `pending []`. Brena's Maximize then applied with the same 4 charges (1 left). Not reproduced on 0.9.28. |
| Rod: delayed cast | Proven (0.9.29) | launch 1 | With 4 charges, Brena was ordered (HARNESS-COMMANDED) to cast Maximize Breath of Life on Kael, placed 8 squares away by an INJECTED teleport, so she walked there. Ilsa's Maximize False Life went first ("3 charges spent, 1 left"). Brena's cast, when it resolved: "[Maximize] not applied (1 charges free)", cast normally, balance 1. Nothing was unpaid. |
| Rod: effects recalculating parameters later | Source-closed (0.9.29) | source | A recalculation without the spell's own data (a lingering effect's context) now gets nothing from the rod. Before 0.9.29 such a calculation could add rod metamagic that was never paid. Not observed in play. |
| Owner quicksave rotation | **Process corrected** | run end | **Cause (decompiled `SaveManager.GetNextQuickslot`):** quick slots rotate across every campaign, so a test F5 overwrites the owner's oldest quicksave; autosaves rotate per game. **Correction:** the harness now refuses F5 and F9, and test saves are named harness saves. **This run:** no quicksave or autosave was written; the owner's newest quick and auto saves still date from 11-10. |

### WR-FINAL legitimate journey, completed, 0.9.30, run `wrfinal-p6`

From `Manual_1022_KWR_AUTOMATION_P15_BEFORE_KRUNE_REPLAY`, the legitimate mask-75 seed of the third attempt:
- **Party:** six alive and buffed; Krune awake at 233.
- **Party actions:** all PLAYER (portraits, spellbook, quick bar, turn-based buttons, floor and target clicks, dialogue, inventory); PARTY AI in real-time fights.
- **Harness:** READ for state and the camera; SETUP only for named saves and loads.
- **Excluded:** no injected damage, buffs, rests, gold, masks, teleports or extra slots.

Legal retries were reloads of the same pre-fight save, with the pre-fight buffs re-cast each time.

| Step | Result | Notes |
|---|---|---|
| Attempt 6 (turn-based) | Abandoned in round 2 (reload) | Krune 233 → 218 (Magic Missile). He raised a quickened Wall of Stone between him and the party. Orin's Mass Dimension Door carried only Orin and Maren, so I reloaded. |
| Attempt 7 (turn-based, hand-commanded, 12 rounds) | Abandoned at Krune **18/233** (reload) | **Damage:** Aldric's smite, then Bryn, Sela, Orin's Cone of Cold and two Fireballs. **His escapes:** at 25% and two foes adjacent, a quickened Dimension Door and a Wish (Frost Giant); then Phase Door, a Phase Door step into the north hall, Globe of Invulnerability, Summon Monster IX (a thanadaemon) and his pearl recall. **Why abandoned:** the party could not reach him in the hall. Orin's last Cone of Cold, cast from A5 at him in the hall, resolved with no damage or saves; not investigated (likely line of effect through the A5 wall: the Globe stops only 4th level and lower). |
| Attempt 8 (turn-based) | Abandoned in round 2 (reload) | A mis-click spent Aldric's turn, and a misaimed Fireball caught Sela. |
| Attempt 9 (real time, paused for orders) | **Krune slain** | **Setup:** Stoneskin ×2 and Delay Poison (Communal) ×2 before the fight, so his Cloudkill could not touch the party; Smite Evil on Krune. **Kill:** Aldric's Earth Breaker took him 233 → 208 → 175 → 61 → 21; his Dimension Door and Phase Door left Aldric adjacent; Aldric's next blow (40) killed him in the passage: "KRUNE SLAIN at 134007". **Friendly fire:** Orin's Cone of Cold, ordered at him, resolved after his death and hit Bryn, Maren, Sela and the wall. Sela died and rose after the fight (11 hp, Normal difficulty). |
| His death | **Native, once** | "his gear set down where he fell (12 items …; spear KWR_KruneDragonToothSpear, rod KWR_KruneRuneCarvedRod)", then "aftermath applied: sarcophagus, gem and foci crumbled; light faded; mist dissipated". **Journal:** Krune Completed, Artifacts Started. **Summons:** all gone with him. |
| Artifact recovery | **PLAYER** | **Loot (PLAYER):** Aldric opened the treasure where he fell, then "Collect all". **Stash:** Krune's Dragon-Tooth Spear (identified on pickup) and Krune's Rune-Carved Rod, 8 charges left of the 10 he started with. **Save:** `Manual_1024_…P17_ARTIFACTS_TAKEN_LEGIT`. |
| Post-death restart (new process) | **Proven** | **Krune:** dead and out of play (`in game False`); "after load … actor (dead)". **Others:** Kurshu and the A4 defenders reloaded dead; no summons; the wall's rubble stayed. **Items:** the spear in the stash and the rod's 8 charges kept. |
| Portal | **PLAYER** | The party gathered in A4, and Yadmila took the Pedestal's "Step through to the lodge"; no artifact warning, since both were carried. **Log:** "Pre-return save verified: Before the portal home"; "Return committed: Krune slain, spear carried True, rod carried True; party [all six]". |
| Debrief | **PLAYER** | **Speakers:** Valsin and Sheila Heidmarch, then the masked figure takes the spear and the rod ("Items lost: …"). **Log:** "Debrief handover recorded: [Krune's Dragon-Tooth Spear, Krune's Rune-Carved Rod]", "Scenario outcome recorded: Success", "Journal: Report Completed; quest Completed". **Save:** `Manual_1026_…P18_DEBRIEF_COMPLETE_LEGIT`. |
| Completed-outcome restart (new process) | **Proven** | **Lodge:** "Restored (setup previously recorded; one Sheila present)"; no debrief started and no handover. **Stash:** the spear and rod absent, not duplicated. **Journal:** The Waking Rune completed, every objective ticked, "The expedition is over and the Venture-Captain has your report." |
| Pre-return checkpoint restart | Proven | `Before the portal home` reloads in the sanctum: Krune dead and out of play, Kurshu and the A4 defenders dead, nothing renewed. **Gap:** the debrief begins as the party arrives, so no save exists between arrival and handover; this save and the completed one bracket the return. |

**Assessment.** The required chain now exists on 0.9.30, within one legitimate adventure (the party built and
played by real input since `wrfinal-p3`):
- A4 → Krune → both artifacts → portal → debrief → reload of the completed outcome.
- Krune was not weakened.
- The deciding moves were tactical: Delay Poison against his Cloudkill, Stoneskin, Smite Evil, and keeping Aldric
  adjacent through his relocations.

The win was in real time with pauses; the hand-commanded turn-based attempts (6–8) were abandoned by reload, not lost.

The unexplained empty Cone of Cold (attempt 7) is recorded above, not investigated.
