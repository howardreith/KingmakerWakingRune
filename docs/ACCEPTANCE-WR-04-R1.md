# WR-04-R1 continuation acceptance

**Status: candidate for owner review. Not merged, tagged or published.** This continues `docs/ACCEPTANCE-WR-04.md`; every original row is kept below with its current status.

| | |
|---|---|
| Tested build | commit `eeab9d0` (runs `wr04r1-c` end and `wr04r1-d`); DLL SHA-256 `0280b431c7d4231cc068b94d1d218a900dcbf28d9610f82af516d4d929b00c4f`, zip `7425b7056a24b69d2f5e424547f727a8d2766b8e149b47975279b27da4018020` |
| Earlier R1 builds in the record | `86fa288` (run `wr04r1-a`), `c6d2f08` (run `wr04r1-b`), `198b66c` (run `wr04r1-c` start). Each later build fixed a defect the previous run found; rows say which run proved them. |
| Package delivered | see STATUS.md; it differs from the tested build only by documentation commits |
| Unit tests | 145 passed, 0 failed (pure rules, including all 128 Krune masks; not the running game) |
| Game | Pathfinder: Kingmaker 2.1.7b, UMM 0.33.2, owner's difficulty preset (it lowers skill DCs by 4 and adjusts enemies) |
| Evidence | `runtime-evidence\wr04r1-a` … `wr04r1-d` (screenshots, `umm-log.txt`, harness command receipts, collected test saves) |
| Saves | Owner's `Manual_313_Before_the_Sanctum_of_Sloth` loaded read-only inside the guarded transaction (a single level 11 Neutral Good rogue, Str 10, light crossbow); disposable `KWR_AUTOMATION_R1_*` saves made from it. |

Labels: **PLAYER** real mouse/keyboard; **HARNESS** a labelled test command (position, grant, injected damage or time, or running a dialogue action as its opener). Effects still resolved natively.

## First checkpoint: ordinary solo door solution — PASS (PLAYER)

Run `wr04r1-a`, build `86fa288`, one unbranded level 11 rogue, no Stone to Flesh, no injected credential:

1. Used the Refuge Token from the belt (right-click, Use); arrived at A1 with fresh state (version 2, not legacy, door latched, 75 hp).
2. Clicked a marked figure (Support-N4). The look text describes the palm brand. Arcana study failed silently: no total, no banner, the cue only said "You study the markings but can make out nothing beyond the details already visible." The impression choice stayed available.
3. "Take an impression of the marked palm (1 minute)": the rogue walked to the figure; log `Time passed: 1m 0s`, `Received item: Sihedron Palm Impression` (notable, cost 0).
4. Walking round to A2 legitimately set off the Alarm (alert 1, Vandiana aware). It was **triggered**, not avoided or disarmed. The rogue had spotted it natively, but not in time.
5. Clicked the A2 leaf. Perception DC 22 succeeded (silent); the clue text linked the door's star to the palm. Chose "Press the Sihedron impression to the handprint": latch released, real door opened, feeder touches 0/0, presentation recorded.
6. Walked through the open doorway (no feeder contact).
7. F5, full game restart, reload: impression held once, latch released, door open, feeder still armed, door 75 hp, position unchanged.

Ordinary weapon damage: two real crossbow hits (4 and 2) did 0 after hardness, each with "The blow leaves no more than a scratch." No proxy HP bar is shown for the door.

## WR-04-R1 items

| Item | Result | Evidence |
|---|---|---|
| Marked sources, look text, palm mesh | **PASS** | N4 look text (PLAYER); brand mesh built on both marked figures. N1 was not opened by look in a run; its text is the same code path. |
| Impression: one-minute work, item, no duplicate on reload | **PASS** | Run a (PLAYER) and restart. Remaking after loss: pure test only (notable items cannot be dropped in normal play). |
| Presentation opens A2, feeder bypassed for that contact | **PASS** | Run a (PLAYER, impression); run b (HARNESS action, branded hand: `armed=True`, latch released). |
| Corpse hand: real corpse, once, presentation | **PASS** | Run b: N1 failed survival; clicking the body (PLAYER) offered "Recover the marked hand"; one hand (cost 0); the body's target then left the game. Duplicate recovery is blocked in pure tests and by the hidden target. |
| Marked reporter opens A2 and reports only on arrival | **PASS** | Run d (HARNESS Stone to Flesh): N1 survived, released the latch with its own palm, `Sanctum door A2: opened by Fleeing Lissalan Cultist`, seven saved legs, Messenger raised only on arrival in Vandiana's chamber. |
| Runner route persists across reload | **PASS** | Run d: saved at leg 3; after reload it completed legs 4-7 and reported; one body, no duplicate. |
| Interception prevents the messenger | **NOT RUN** natively | Code path: a dead, held or non-acting runner makes no progress. The isolated test (alarm dealt with first, then the runner killed or held) was not run. |
| Actual cause when the alarm fires first | **PASS** | Run d: causes recorded `Alarm` (the runner's own walk through the stair) then `Messenger`. |
| Unmarked reporter stopped by a latched A2 | **PARTIAL** | Code fixed in `eeab9d0` after run c showed N3 passing. Not re-run. |
| Living attacker | **PASS, basic** | Run d: N4 survived, entered combat at once and used native cleric abilities. |
| Cultist identity vs GMG p.278 | **PARTIAL / open defect** | Per-figure blueprints with the figure's model, NE, masterwork sickle, chainmail, credential on N1/N4 only. Observed scores below and varying around the printed block (Str 4-6, Dex 1-11, Con 6-10, Wis 11; AC 12-13). Difficulty scaling was removed but was not the cause; cause not identified. Survival saves still roll natively at DC 15. |
| Door hardness arithmetic | **PASS** | Run a, HARNESS ranged hits on the door proxy: 7 to 0, 12 to 4, 30 to 22, 12 to 4, 12 to 4, 30 to 22, 30 to 22 (75 to 0); plus pure tests. |
| Destruction stops attacks, persists | **PASS** | Run a: the rogue's real attack loop stopped at destruction; leaf and feeder gone; destroyed state survived a full restart. |
| Same-actor force with armed feeder | **PASS** | Run a: Tester walked to the leaf, feeder hit Tester for 55 (10d6 = 37, empowered), Tester survived and rolled her own Strength. The DC read 24 (difficulty preset); fixed to the printed 28 in `c6d2f08`, not re-run. |
| Feeder spent HP stays spent (R1) | **NOT RUN** natively | The first attempt found the party cannot damage the peaceful Krune proxy; the harness was fixed but not re-run. Pure tests cover 57 to 37, no resurrection by weaker grants, no acceleration from a consumed grant. |
| Glyph SR and source (R2) | **PARTIAL** | Run b: Vandiana's CL 8 context; Spell Resistance stopped the burst; one detonation; awareness cause added. Penetrating, evasion and disarm cases not re-run. |
| Mist typed bonus and wake (R3) | **PARTIAL** | Run b: three failed Will 17 saves stagger, stagger, sleep. The sleep buff was then gone within a round with no wake recorded; diagnostics added in `198b66c`, cause not yet identified. Krune-summon bonus: pure test only. The summon negative control did not spawn. |
| Long work policy (R4) | **PASS, narrow** | Impression work completed out of combat (run a). Refusals and deadline cut-off: pure tests only. |
| Turn-based boundaries (R4) | **NOT RUN** natively | Native hook verified in the decompiled engine (`TurnController.Dispose` on every end path); no turn-based session was run. |
| Hound eligibility, barriers, tether (R6) | **PARTIAL** | Run b: bark on approach, one bite per round at +10 against native AC. Through-wall negative, tether, expiry and dispel not run. |
| Fixture save scoping and protection (R7) | **PARTIAL** | Pure tests for every rune kind and non-runes. Native rune routes not re-run on this build. |
| Silent knowledge checks | **PASS** | Run a: rune and figure studies and the door clue show descriptive text only; numbers in `umm-log.txt`. |
| Legacy re-initialisation on reload | **PASS** (fixed) | Found in run a (0.4.0 behaviour); run c reload shows `LegacyNoticeShown: false`. |
| Doors opened on every reload by fixtures | **PASS** (fixed) | Found in run c (`A2 opened on load`); run d reload left A2 closed. |

## Original matrix (A01–A31), current status

| ID | Status | Note |
|---|---|---|
| A01 door audio | **NOT RUN by ear** | Door transitions in R1 used the native open; owner confirmation still needed. |
| A02 door interactions | **PASS** | Look, metal and inscription study, own-hand press, credential press, force, disarm, attack all present; inscription and press PLAYER. |
| A03 barrier and bypass | **PASS** | Fresh latch; credential bypass (impression, hand, cultist palm); destruction route. Real DC 28 break success not observed (rogue Str 10). |
| A04 alarm | **PARTIAL** | Native spot and triggers seen; disarm at 26 not completed. |
| A05 feeder | **PASS** | Real touch 20 vs AC 15, 55 dealt and transferred (run a). Miss and disarm not re-run. |
| A06 feeder non-stacking/expiry | **PARTIAL** | Pure; native depletion NOT RUN (see above). |
| A07 glyph | **PARTIAL** | See R2. |
| A08 hound | **PARTIAL** | See R6. |
| A09 awareness | **PASS** | Alarm, Glyph, Hound, Messenger converge with causes in order. |
| A10 restoration | **PASS** | Living attacker, living reporter, corpse; no duplicate figure or body. Identity stats open (above). |
| A11 husks | **PARTIAL** | Unchanged from 0.4.0. |
| A12 rune study | **PARTIAL** | Silent presentation PASS; 23/33 tiers natively NOT RUN this cycle. |
| A13–A17 rune routes | **PARTIAL** | Unchanged from 0.4.0 natively: Harm, Enervation, Hold Monster, harness-granted Dominate Monster, failed dispel. Not re-run: Defense dispel success at 25, Armor success, Control by an obtainable route, poison summon, entangle, cold, energy, bludgeoning. Tamper/UMD now act by the body at the plate (not re-run). |
| A18 damage pipeline | **PASS** | Door pipeline above; rune pipeline unchanged. |
| A19–A20 curse | **PASS** (0.4.0) | Not re-run; the only change is that the failing body is the acting body. |
| A21 mist | **PARTIAL** | See R3. |
| A22 mode parity | **NOT RUN** | |
| A23 flail | **PARTIAL** | Now one body's action and refused near hazards; not re-run. |
| A24 emerald and sarcophagus | **NOT RUN** natively | |
| A25 inspections | **PARTIAL** | |
| A26 clocks | **PARTIAL** | Unchanged. |
| A27 Krune masks | **PASS (pure)** | |
| A28 mid-state reload | **PASS, narrow** | Runner mid-route, door destroyed, impression, latch. Curse, entangle and summon windows not re-run. |
| A29 migration | **PARTIAL** | v1 to v2 pure test; two legacy-reload defects fixed. A real 0.4.0 save was not loaded on this build. |
| A30 regression smoke | **PARTIAL** | Belt token departure and arrival (run a); inventory route, checkpoint recovery, pets, Favored Terrain and stock spells not re-run. |
| A31 profile | **PASS** | |

## Integrated six-character walkthrough — NOT RUN

The prepared six-character party walkthrough (lodge, token, A1 investigation, local A2 solution, rune interaction, wards and mist, curse and cures, emerald, clocks, trap disarm, restart) was not run this cycle.

## Install and restore

Every run restored the profile, Steam mirror, registry, UMM params and the previous install, each verified by `End`. Final install status is in STATUS.md.
