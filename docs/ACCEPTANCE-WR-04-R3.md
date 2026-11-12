# WR-04-R3 acceptance: completion run

This answers the R2 review. Its scope:
- a properly prepared party;
- the ordinary journey;
- the remaining rune routes and trap disarms;
- the sustained-route timing boundary;
- turn-based and restart behaviour;
- the difficulty rulings.

PR #4 stays a draft. Nothing was merged, tagged or published.

## Setup

| | |
|---|---|
| Run | `wr04r3-a`, one guarded transaction. |
| Builds | `cb59cb5` (0.4.3, timing) · `c7f46ff` (0.4.4, lodge rest) · `c943c2b` (0.4.5, disarm reach, ineffective) · `6ea66cf` (0.4.6) · `fee00e6` (0.4.7, area-ward disarm) · `efbc941` (0.4.8, break-free) · `94c820d` (0.4.9, harness boundary commands). |
| Candidate | 0.4.9 is the last build tested and the one installed. DLL `7191e0cbf0e9d38230c47afecabb1794648a0d2f30780526806c2b59a8dd68d1`; zip `artifacts\KingmakerWakingRune-0.4.9.zip` `247f0360b1f1fb0edeb94de7bcbefcae05b0d5d60d23d1327bc718fc15d89571`. |
| Difficulty | Fresh game on the native **Challenging** preset: Core-rules damage and crits, enemies tougher. Loading the class premade build switched native auto level-up on, which relabels the preset "Custom"; no combat setting was changed. The ExtraDecline check used a disposable R1 save. |
| Saves | New disposable `KWR_AUTOMATION_R3_*` saves (evidence `runtime-evidence\wr04r3-a\saves`), plus a copy of R1's disposable `R1_ARRIVAL`. No owner save was opened. |
| Labels | **PLAYER**: real mouse and keyboard. **HARNESS**: a labelled test command (move order, spell grant, buff of exact length, restore). The engine resolves everything natively. |

## 1. Prepared party: PASS

The party was built entirely through the scenario's own preparation flow, with no injected stats:
- **New game:** New Game, The Waking Rune, Challenging (PLAYER).
- **Protagonist:** Ilsa Venn, a human Wizard (Conjuration), created at character creation with the class's native **Load premade build**.
- **Starting grants:** the protagonist received 155,000 XP (the level 11 threshold), and the treasury was 450,000 gp.
- **Recruits:** five, through the Society Coordinator (PLAYER), each on a native premade build and each committed at 155,000 XP:
  - Brena Holt, Cleric;
  - Marra Fen, Druid;
  - Dorn Askel, Fighter;
  - Tessa Quill, Rogue;
  - Kael Ashen, Sorcerer (red draconic).
- **Treasury:** still 450,000 after all five recruits.
- **Levels:** every level-up opened pre-filled by the premade plan; each character went 1 → 11 with native Complete clicks.
- **Quartermaster** (PLAYER, native shop): 250,280 gp spent on:
  - +4 headbands and belts, cloaks, rings, an amulet and bracers;
  - scrolls (Cone of Cold, Lightning Bolt, Dispel, Heal, Breath of Life, Death Ward, Restoration, Remove Curse and others);
  - healing wands;
  - a Lesser Empower Metamagic Rod.
- **Gap found and fixed:** a party that levels in the lodge cannot ready its new spell slots, because neither the lodge nor the sanctum allows camping. 0.4.4 adds a Quartermaster answer, "We need a night's rest", which runs the native scripted party rest (8 hours; slots, resources and charges restored).
- **Reusable seeds:**
  - `R3_PARTY_LEVELED`: levelled, unequipped;
  - `R3_PARTY_SEED`: equipped;
  - `R3_PARTY_RESTED`: rested and prepared.

## 2. Ordinary journey: PASS, with labelled moves

Most walking used HARNESS move orders (native pathing) to save time. Every interaction, check, spell choice and target below was PLAYER unless marked.

| Step | Result |
|---|---|
| Departure | Refuge Token, right-click Use, on the belt. Fade, checkpoint save "Before the Sanctum of Sloth", arrival in A1 with fresh state (Subtier 10–11, 6 PCs, extra challenge). |
| A1 investigation | Ilsa's Arcana study of the marked figure (success, petrified people, true Sihedron). A one-minute palm impression, native work, one item. |
| Alarm (DC 26) | Tessa spotted it natively (Perception); her approach to the perception band was a HARNESS move. Her disarm then worked from the edge of the area, without entering it (`Ward disarm: Tessa Quill works KWR_TrapAlarm from the edge of its area`). The first Trickery check failed within the margin; the second succeeded (`successfully disarms the trap`). State `Disarmed: true, Alerts: 0`. All six then walked through the stair: no alert. |
| A2 door | Perception DC 22 clue (recut word, star matches the brand). Impression pressed: `Sanctum door A2: opened by Tessa Quill`, real door. |
| Runes | All seven plates went dark in one walkthrough (table in section 3). The mask went 0 → 1 → 3 → 19 → 51 → 115 → 123 → 127. |
| Glyph (DC 28) | Spotted natively. Disarmed from the edge after several failures inside the margin (none sprang it). Dorn then walked across its centre: no detonation. |
| Hound | Noticed the party crossing its chamber. Native bites: `d20 16 + 12 vs AC 16 (flat-footed True): Hit` on Brena, `d20 12 + 12 … Hit` on Ilsa. |
| Mist | The route crossed the mist; Dorn and Kael rolled native Will saves each round (all resisted on this preset). |
| Save and restart | At mask 51: saved, full process exit, relaunch, load. Mask 51, alarm `Disarmed` with 0 alerts, glyph disarmed, door unlatched, positions and hit points all preserved. |

**Defect found and fixed: area-ward disarming.** On 0.4.3 the native disarm approach walked the rogue to 1 m from the alarm's marker, inside its 20 ft area, so she set it off before disarming it (`Alarm: Tessa Quill entered; alert 1`). The engine stops a disarm approach at the edge of a trap's own trigger zone, but these wards are polled areas, not native zones.
- 0.4.5 tried to lengthen the interaction reach. That had no effect, because the engine ignores it for traps.
- 0.4.6 applies the native rule against the wards' own areas. It still missed, because a fresh view instance was compared.
- 0.4.7 matches the ward by its saved id and logs each approach. Natively it now disarms from the edge without triggering, as above.

## 3. Rune routes

| Plate | Route | How | Native result |
|---|---|---|---|
| A3a Life | Death spell | Brena's prepared Slay Living (PLAYER) | `Rune save denied: A3a Fortitude 105 vs DC 22 … descriptor Death`; `RUNE A3a DISABLED by DeathSpell: … effective spell level 5`. **PASS** |
| A3b Alacrity | Entangle, 10 rounds | Marra's prepared Entangle (PLAYER) | 0.4.7: **FAIL**. The plate rolled a native escape each round and broke free after 7.0 of 10 rounds (curse save followed). This is a defect: a plate is denied saves against what it is vulnerable to, but not escape checks. 0.4.8 refuses its break-free attempt under the same scoping: `Rune break-free refused: A3b against EntangleBuff; descriptor Ground, MovementImpairing`, then `RUNE A3b DISABLED by EntangledDuration: … observed on for 10.0 rounds`. **PASS** |
| A3b Alacrity | Cold | Ilsa's prepared and Kael's known Cone of Cold, queued in pause and released together (PLAYER) | `[Cold 34] … 26/35`, then `[Cold 28] … 46/35`, `RUNE A3b DISABLED by ColdDamage`. **PASS** |
| A3c Defense | Energy | HARNESS-granted Lightning Bolt to Ilsa and Kael | Saves denied as electricity. `[Electricity 32] … 24/35`, `[Electricity 32] … 48/35`, `RUNE A3c DISABLED by EnergyDamage`. **PASS (HARNESS grant)**. Defense by dispel: R2 PASS. |
| A3d Life | Negative energy | Brena's prepared Harm (PLAYER) | `Will 104 vs DC 23 from HarmDamage; descriptor None; effects NegativeEnergyDamage` denied; `NegativeEnergy 110 … 102/27`. **PASS** |
| A3e Armor | Fire | Kael's Fireball with the Lesser Empower rod (PLAYER) | `[Fire 66] … 58/35`, `RUNE A3e DISABLED by AcidFireDamage`. **PASS**, one caster. This closes R2's 28/35 failure. |
| A3f Control | Compulsion spell | Brena's prepared Serenity (level 5, Compulsion), an obtainable native spell (PLAYER) | `RUNE A3f DISABLED by CompulsionSpell: Serenity (effective level 5, MindAffecting, Compulsion, Emotion)`. **PASS** |
| A3g Life | Poison | HARNESS-granted Poison to Marra | Fort saves denied as Poison. `2 Constitution … 2/3`, then `3 … 5/3`, `RUNE A3g DISABLED by PoisonDiseaseDamage`. **PASS (HARNESS grant)**. No poisonous summon was in Marra's prepared lists. |

## 4. Sustained-route timing boundary (review item 3): fixed and PASS

**Code (`cb59cb5`).**
- `RuneConditionClock` records, from native buff events on the Alacrity plate, when each condition effect was applied (dated as the engine dates buffs, including the turn-based turn start) and when it was removed. A removal is capped at the effect's scheduled end.
- `ObserveCondition` starts the sustain at the application time. When the status is found gone, it credits up to the removal.
- Without that record (a reload in between) it credits only what was seen.
- There is no grace period, only a 1 µs allowance for converting engine ticks to seconds.
- New pure test `TestSustainedStatusIsTimedByItsOwnEffect`:
  - exact five-round expiry with late first and last polls: success;
  - removal 0.25 s short: failure;
  - an end time in the future, or before the last sighting: ignored;
  - an application time after the first sighting: ignored.
- 149 tests pass.

**Native** (HARNESS `condition`/`uncondition` apply or remove a native `HoldMonsterBuff` of exact length; the engine times and expires it; plate saves denied as paralysis):

| Case | Log | Result |
|---|---|---|
| Natural expiry at exactly 30 s | `begun at 72901.30` … `removed at 72931.30; scheduled end 72931.30; credited to 72931.30` … `DISABLED … observed on for 5.0 rounds` | **PASS** |
| Interrupted just short (28.5 s) | `begun at 72906.77` … `removed at 72935.27; scheduled end 72966.77; credited to 72935.27` … `last observed on after 4.7 of 5 rounds`, failed attempt, native curse save | **PASS** |
| Restart mid-effect | 60 s hold, saved at 15 s (`StartedAt 72906.746`), full process restart, load, then `DISABLED … 5.0 rounds` | **PASS** |
| Turn-based combat | Turn-based mode on (Options, PLAYER), goblins spawned (HARNESS), the hold applied mid-combat. Native rounds advanced by the end-turn button: `begun at 72998.53` … `DISABLED … 5.0 rounds` | **PASS** |

## 5. Difficulty rulings (review item 2)

- **Mist under ExtraDecline (current build): PASS.**
  - Setup: R1's disposable save (`difficultyStats=ExtraDecline`), with the solo rogue teleported into the mist (HARNESS).
  - The saves ran Staggered, Staggered, Resisted (streak reset), Staggered, Staggered, then FellAsleep at streak 3.
  - Apply trace: `duration=18s difficultyStats=ExtraDecline`. Removal: by the engine's own expiry (`BuffCollection.Tick`, `endTime=43380`).
  - The mod then reported `recorded asleep but carries no mist sleep now … treated as woken`, and the next saves ran from streak 0.
  - So the preset's three-round cap is honoured and reconciled.
- **Cultist AC follows the preset.**
  - The same restored cultist reads AC 20 under Challenging (`Difficulty +2` from the native `DifficultOpponent`) and AC 16 under Decline (R2).
  - Every other printed statistic is unchanged: Str 10, Dex 13, Con 12, Wis 15, Cha 16, Fort 4, Ref 2, Will 5, hp 16.
  - No compensating patch is applied, per the review's recommendation, now recorded as a ruling in WR-04-RULES.

## 6. Reporter interception: NOT ACHIEVED

The marked reporter (N1) was restored with HARNESS Stone to Flesh after native survival saves (several failed, then 23, 19 and 18 vs 15).
- In every attempt it ran its saved seven-leg route, and the Messenger cause was raised only on arrival. That is correct behaviour.
- The party never stopped it.

Attempts:
- **Melee:** HARNESS attack orders from where the party stood. The runner was 10+ squares ahead.
- **Single-target spells:** Magic Missile reported `canTarget=False`. Reporters belong to the native Neutral faction, and enemy-only spells cannot target them.
- **Fireball:** misaimed in pause; it hit the stone figures.
- **Entangle at the start point** (HARNESS `castpoint`): the runner was entangled, but walked out of the area.
- **Melee with Dorn adjacent** (HARNESS attack orders, then a PLAYER click): it left reach before the attack.

**Finding for the owner.** A fleeing reporter can be stopped only by area effects or by catching it. Hold Person, Magic Missile and other enemy-only spells cannot target it. Making restored reporters count as the party's enemies would fix targeting, but their AI might then fight instead of running. This needs a ruling, so it was not changed.

## 7. Not run

- An unmarked reporter stopped at a latched A2. The door was already open on this party's path.
- Glyph spell-resistance penetration and evasion; the Krune-summon mist bonus natively; hound see-invisibility, through-wall and tether controls.
- Armor Trickery DC 35, Control UMD DC 35 and summoned-damage routes; Defense bludgeoning single hit; Life negative levels on this build.
- Door audio: the owner's own listening.

## 8. Restoration

Transaction `wr04r3-a` was closed with `-KeepInstalled`:
- **Profile:** 35 differences reverted, added files quarantined.
- **Steam mirror:** 23 differences reverted.
- **Registry:** restored, including the turn-based option switched on for the test.
- **UMM `Params.xml`:** verified.
- **Install:** candidate 0.4.9 is installed. The 0.4.2 install it replaced (DLL `68db679c…`) is backed up at `runtime-backups\wr04r3-a\mod-existing`.
- **Note:** the local 0.4.2 zip was overwritten by a mislabelled dirty build early in this cycle and was deleted. The tested 0.4.2 DLL survives in that backup and in the R2 run.

The Python 3.15 install manager state was not touched this cycle. It remains as recorded in STATUS.
