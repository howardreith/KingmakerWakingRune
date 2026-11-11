# WR-04 acceptance ledger (draft)

This is the **cumulative** ledger for the original WR-04 scope (package `tests/ACCEPTANCE.md`, A01–A31) and the review items added since, **through R4**. Each row is credited from the report that actually recorded a native result; a later report that does not repeat a test does not reset it. Finishing a review round does not by itself verify the rest of the original scope: rows not shown natively stay Partial or Not run.

**R5 rows are not here yet.** The R5 run (0.5.6, `c30be66`) will add: legacy neutral reporter migration, departure readiness revalidation on retry, summon order save/load and ownership release, the blocked turn-based runner, and the R5 interaction checks.

Builds: 0.4.0 = `4a109d4` (`ACCEPTANCE-WR-04.md`) · R1 = `86fa288`, `c6d2f08`, `198b66c`, `eeab9d0` · R2 = 0.4.2 (`923f18e`, `b1f9e45`, `44a4b78`) · R3 = 0.4.3–0.4.9 (`cb59cb5` … `94c820d`) · R4 = `f718c4c`, 0.5.1–0.5.5 (`5e05519`).

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
| Armor: Trickery 35 | Partial | 0.4.0 Trickery 25 failed correctly | Success not run. |
| Armor: 35 single piercing/slashing | Partial | 0.4.0 9-point slash failed correctly | Success not run. |
| Control: compulsion 4+ | Proven (native play) | R3, prepared Serenity | 0.4.0 Dominate Monster was harness-granted. |
| Control: 50 summoned damage within the window | Proven (native play) | R4 0.5.x, Summon Monster VI + summon order | Proven on 0.5.x; summon-order code changed in 0.5.6 (R5). |
| Control: UMD 35 | Partial | 0.4.0 UMD failed and cursed | Success not run. |
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
| Marked reporter opens A2, reports only on arrival | Proven (native play) | R4 0.5.x, real time and turn-based | R1/R3 with harness StF. Code changed in 0.5.6 (legacy migration, blocked runner). |
| Runner route survives mid-route reload | Proven (native play) | R1 run d; R4 `R4_RUNNER_MIDROUTE2` | |
| Hostile and targetable; Hold Person interception, no messenger | Proven (native play) | R4 0.5.3 | Real time only. |
| Turn-based flight | Proven (with setup — party moved off the stair) | R4 0.5.4 | Blocked runner fixed in 0.5.6 (R5). |
| Turn-based interception | Not run | | |
| Unmarked reporter stopped by latched A2 | Not run | code `eeab9d0` | Pre-fix run c showed N3 passing. |
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
| Departure readiness: notice, cancel, spellbook, warning, rest, transfer | Proven (native play) | R4 0.5.x | Retry revalidation changed in 0.5.6 (R5). |
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
| Mid-state reload: curse, entangle and summon windows, husk crumbling | Not run | | Summon orders are R5. |
| Legacy saves initialise once; no retroactive triggers; no re-init on reload | Proven (native play) | 0.4.0; R1 fixes | |
| A real 0.4.0 save on a later build; pre-arrival lodge saves | Not run | | |
