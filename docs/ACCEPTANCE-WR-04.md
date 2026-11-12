# WR-04 acceptance report

**Status: candidate for owner review. Not merged, tagged or published.** Coverage is partial: see the matrix and the closing list.

| | |
|---|---|
| Source | commit `4a109d4` on `feature/wr-04-sanctum-wards` |
| Package | `artifacts/KingmakerWakingRune-0.4.0.zip`, SHA-256 `2bcab97444d4f70e234d0aa02c37b7562d54fc65a198bfeecebf0458ec362252` |
| DLL | SHA-256 `83e75cc76cfa4ec0c0eac73affd0b6aae20f3282267aa89d836bc078fa55ba6a` |
| Unit tests | 136 passed, 0 failed (38 from the first checkpoint, including all 128 Krune reference masks, plus ward and geometry checks). They exercise pure rule code, not the running game. |
| Game | Pathfinder: Kingmaker 2.1.7b, UMM 0.33.2 |
| Evidence | `runtime-evidence\wr04-dev2` (first real-input click and study), `runtime-evidence\wr04-dev3` (integrated pass, DLL above) |
| Save used | disposable `KWR_AUTOMATION_WR03_RANGERS_A3` from earlier milestones, in a guarded transaction |

**How to read the labels.** PASS means observed natively or, where noted, in the pure tests. **HARNESS** marks a step that used a labelled test command to position a character, grant a spell, inject a credential, or advance time; effects still resolved through native rules. PLAYER means real mouse and keyboard input in the game. NOT RUN means not exercised this cycle.

## Results

| ID | Result | Evidence and limits |
|---|---|---|
| A01 door audio | **NOT RUN** (capture) | Both events post successfully (playing IDs 19, 20, HARNESS). A door transition's audio was not captured: no loopback recording was available. Load, refusal and silent-restore paths are coded and untested by ear. |
| A02 door interactions | **PARTIAL** | Separate look, study, touch, force, disarm and attack answers exist. PLAYER click and dialogue proven on a rune; the door's press ran through the dialogue action (HARNESS). Inscription and metal studies NOT RUN. |
| A03 barrier and bypass | **PARTIAL** | Unmarked press stayed latched; a brand holder opened it (HARNESS credential, since none exists natively). Force and destruction routes NOT RUN. |
| A04 alarm | **PARTIAL** | Entry alerts with awareness, repeat entries add alerts without re-preparing. Coverage and arrival exclusion pass in pure tests. Perception and disarm at DC 26 NOT RUN. |
| A05 feeder | **PASS, narrow** | Native touch 18 vs AC 10; 10d6 = 38, empowered 57; 57 dealt; Krune received 57 real temporary HP (HARNESS positioning). Miss, disarm and discovery NOT RUN. |
| A06 feeder non-stacking | **PASS (pure)** | Native one-hour expiry and acceleration NOT RUN. |
| A07 glyph | **PASS, narrow** | One detonation, 10-foot burst, Reflex 17, half on success, three actors. SR, worshipper bypass and disarm NOT RUN. |
| A08 hound | **PASS, narrow** | Bark raised awareness; bites resolved once per round with native rolls (HARNESS positioning). Nine-round discharge, dispel and attack immunity NOT RUN. |
| A09 awareness | **PARTIAL** | Alarm, Glyph and Hound converged on one state, first cause prepares once. Messenger arrival and east-door hearing NOT RUN. |
| A10 restoration | **PARTIAL** | Real Stone to Flesh cast on a bearer (HARNESS granted); Fortitude 7 vs 15 failed; persistent corpse, no duplicate figure. Living attacker and reporter behaviour NOT RUN. |
| A11 husks | **PARTIAL** | Stone to Flesh started the crumble and it reached Ash. All 21 not exercised. |
| A12 rune study | **PARTIAL** | PLAYER click, dialogue, Arcana roll 22 vs 23 revealed nothing and cursed no one. Hint and exact tiers NOT RUN natively. Reroll prevention passes in pure tests. |
| A13 Life | **PARTIAL** | Harm 110 disabled A3a; Enervation's 4 levels disabled A3g. Poison/summon route NOT RUN. Death-spell route rejected its spell, but that run's level reading predates the level fix and was not repeated. |
| A14 Alacrity | **PARTIAL** | Real Hold Monster paralysis disabled A3b at 5.0 rounds; Hold Person rejected at targeting. Entangle and cold NOT RUN. |
| A15 Defense | **PARTIAL** | Native dispel at DC 25: an 18 failed and cursed the caster. A success at DC 25 not re-run after the DC fix. Energy and bludgeoning NOT RUN. |
| A16 Armor | **PARTIAL** | Trickery 25 and a 9-point slash each failed correctly. Success routes NOT RUN. |
| A17 Control | **PARTIAL** | Dominate Monster disabled A3f; UMD failed and cursed. Summoned damage NOT RUN. |
| A18 damage pipeline | **PASS, narrow** | Logged hardness once and thresholds on real hits. Adamantine bypass passes in pure tests only. |
| A19 curse | **PASS** | Native Will saves, first brand 1d6 Dexterity, added brand raised DC 20→21→22 with no added damage, same-plate repeat refused. |
| A20 cures | **PASS, narrow** | Native Remove Curse cleared the brand and left Dexterity damage and fatigue; Lesser Restoration healed Dexterity. Daily progression passes in pure tests only. |
| A21 mist | **PARTIAL** | Real end-of-round Will DC 17, stagger then sleep at three failures, on a real unit. Wake NOT RUN; Krune and his summons have nothing to test against. |
| A22 mode parity | **NOT RUN** | Real-time only. Turn-based timing is coded and unit-tested only. |
| A23 flail | **PARTIAL** | Ten-minute chip advanced simulation time and gave one real flail. Good-aligned draw NOT RUN. |
| A24 emerald and sarcophagus | **NOT RUN (native)** | Pure rules only. |
| A25 inspections | **PARTIAL** | Dialogue framework proven on runes; other props NOT RUN. |
| A26 clocks | **PARTIAL** | HARNESS Kurshu event set the deadline; interrupt rolled 9 and stored the countdown; awakening firing NOT RUN natively. |
| A27 Krune masks | **PASS (pure)** | All 128 masks against the reference. Native reporting shows the mask matching disabled foci. |
| A28 mid-state reload | **PARTIAL** | Rune mask, windows, Krune state, cure state and corpse survived a real save and reload. Curse-and-trip interleavings NOT RUN. |
| A29 migration | **PASS, narrow** | Old saves initialise once as legacy, door unlatched, wards armed, no retroactive trigger. Pre-arrival lodge saves NOT RUN. |
| A30 regression | **NOT RUN** | Belt/inventory departure, checkpoint recovery, Favored Terrain and stock spell smoke test not repeated. |
| A31 profile | **PASS** | Save-bound profile, no four-player relief, no spell expenditure. |

## Defects found and fixed during native runs

- `Collect` lost evidence when the UMM log had not grown.
- Native `AddBuff` ignores its ability parameters, so caster level and curse DC never applied. Set on the context; dispel now reads DC 25.
- Sleep could desync across a reload; the buff is now reconciled.
- Log spam during a held status.

## Honest open list

The largest gaps are the NOT RUN and PARTIAL rows above, chiefly turn-based timing, captured door audio, the poison/summon and entangle/cold/energy routes, the attacker and reporter cultists, and force/destruction of the door. These routes are written and exercised through unit tests or shared code, but were not proven in a running game this cycle. Nothing here should be read as complete until those rows are run.

No Sihedron credential or Knock/Stone Shape spell exists natively, so the marked bypass needs a labelled harness credential to test, and normal players open the door by force or by hitting it.

## Install and restore

The candidate above is installed in `Mods/KingmakerWakingRune`, following the owner's standing preference from earlier milestones. The 0.3.2 install is preserved at `runtime-backups\wr04-dev3\mod-existing`. Profile, Steam mirror, registry and UMM params were restored after every run, including this one.
