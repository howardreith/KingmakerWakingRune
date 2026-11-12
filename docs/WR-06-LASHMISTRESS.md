# WR-06: The Lashmistress (A4)

A4 is one coordinated encounter: Vandiana the Lashmistress, Agarik, Jorgas and the dark naga. It runs at Subtier 10–11 with a six-PC baseline and the extra challenge on. The player's native difficulty preset applies on top.

The source is The Waking Rune:
- page 6: the map;
- page 11: the room, placement, scouting and the countdown;
- page 12: the allies and the scaling sidebar;
- page 13: Vandiana, high tier.

The referenced blocks are the Blackstrike (NPC Codex 236), the dark naga (Bestiary 211) and the advanced simple template, rebuild rules.

The sidebar's "Jargas" is the encounter block's Jorgas: one identity, `A4-Jorgas`.

**Code:** `src/KingmakerWakingRune/Sanctum/Lashmistress/`.
- `LashmistressRules.cs` is pure and unit-tested: the sheets, the prepared lists, the saved state, the rules and the tactics.
- `LashmistressBlueprints.cs` builds the actors.
- `LashmistressEncounter.cs` runs them at play.
- `LashmistressComponents.cs` and `LashmistressObserver.cs`.

## Encounter at a glance
| Step | What happens |
|---|---|
| Placement | All four are placed once, when the party has control after arrival. An upgraded save places them at its first load (see Saves). They share one native unit group, `KWR-A4-Defenders`, which Kurshu is not in. |
| Baseline | Vandiana's books are filled entry for entry. False Life, Mage Armor, the hound, Alarm and the Glyph are spent from them once. Mage Armor and False Life run from the moment the sanctum was first entered. |
| Warning (alarm, messenger, hound, glyph) | The existing awareness state. The first warning requests Vandiana's Darkvision, which she casts from her own wizard slot. Nobody moves toward the cause. |
| East door heard | Requested: Vandiana's Cat's Grace potion, the naga's Invisibility on itself and then on Agarik, and both Blackstrikes' Bull's Strength potions. Each is a native command from the actor's own potion or slot. It is marked performed only when its effect is seen. |
| Engagement | The first hostile rule between the party's side and a defender: an attack roll, damage, a saving throw against a hostile effect, or a harmful spell aimed at one. A defender and a party member in combat with sight of each other also counts. So does an aggressive act against the rite (striking the altar's tools or the emerald). This calls `RitualRules.CommitConfrontation` once: 1d6+6, with feeder acceleration once. |
| Combat | Native brains. Vandiana casts from her two books, and the naga from its slots. The Blackstrikes fight, hide, and drink Blur and their other potions as their own actions. |
| Awakening due | Saved and announced once by the existing clock. A4 continues. Nobody is despawned, and no Krune is spawned. |
| All four dead | Reported once. The countdown is not cancelled, and nothing is cleared or refilled. |

## Source to runtime

Every value below was observed on the placed units (candidate 0.7.10, Normal preset). The preset's own `Difficulty` modifiers are listed separately, and they are native: the game's `DifficultyAdvancementBaseFeature` gives every unit +2 to AC, to attack and to Perception at Normal.

### Vandiana (cleric 3 / conjurer 4 / mystic theurge 5, human, LE)
| Line | Unbuffed source | Gear and class facts | Pre-combat buffs | Preset | Observed |
|---|---|---|---|---|---|
| Str/Dex/Con/Int/Wis/Cha | 8/10/12/17/18/14 (the printed Int 17 includes the headband) | Human +2 spent on Cha (built at 12) | Cat's Grace: Dex 14 | none on scores at Normal | 8/10→14/12/17/18/14 |
| Classes | cleric 3, wizard 4, theurge 5 | native `ClericClass`, `WizardClass` (conjuration; opposition evocation and illusion), `MysticTheurgeClass` (cleric and wizard progressions) | — | — | divine CL 8, arcane CL 9 |
| BAB / CMB / CMD | +6 / +5 / 17 base (19 buffed) | native | — | CMD +2 | 6 / 7 / 19 (21 with Cat's Grace) |
| AC | 16 base (the printed base 18 is inconsistent with its own components); 22 buffed | +2 light shield (3), ring +2, amulet +1 | Mage Armor +4, Cat's Grace +2 | +2 | 22 before the potion, 24 after |
| HP | 74 | native class HP 76; calibrated −2 | False Life, 14 temporary (printed) | — | 74 + 14 temporary |
| Fort / Ref / Will | 11 / 8 / 16 | native (Great Fortitude, Lightning Reflexes, cloak +2) | Cat's Grace: Ref +2 | — | 11 / 8 (10) / 16 |
| Concentration | +12 / +12 printed | native | Combat Casting +4 conditional | — | 16 (includes Combat Casting) |
| Channel negative | 5/day, DC 13, 2d6 | native `ChannelNegativeFeature`. The game reads the character level: `KwrClericChannel` sets the cleric level. | — | — | 5/5, DC 13 |
| Acid dart | 6/day, 1d6+2 | native conjuration school | — | — | 6/6 |
| Blast rune | 7/day, 1d6+1, 3 rounds | native Rune domain | — | — | 7/7 |
| Whip | +1 whip +6/+1, 1d3 nonlethal | adaptation, below | — | +2 | +8, 15 ft reach |

**Headband:**
- This game gives a non-party caster bonus spells from the casting score's base value only.
- With the headband worn on Int 15, she was two wizard slots short of her printed list (3rd and 4th level).
- So the printed Int 17 is her base score, and the headband of vast intelligence +2 is carried as loot. It is counted once.

**Prepared, observed full: cleric 20 of 20, wizard 22 of 22, entry for entry.**
- **Domain entries:** a per-unit special list lets them sit in her domain slot (no global spell list is changed).
- **School entries:** the school slot takes conjuration spells (Dimension Door is the game's own `DimensionDoorBase`).

### Agarik and Jorgas (Blackstrike, fighter 7 / shadowdancer 2, human, CN, advanced)
| Line | Unbuffed source | Advanced (rebuild) | Pre-combat buffs | Preset | Observed |
|---|---|---|---|---|---|
| Scores | 18/14/14/12/10/8 (printed 22 Str includes Bull's Strength) | +4 each: 22/18/18/16/14/12 | Bull's Strength: Str 26 | — | 22 (26) / 18 / 18 / 16 / 14 / 12 |
| HP | 86 | +2/HD: 104 | — | — | 104 (native 100, calibrated +4) |
| AC | 20 (5 armor, 1 deflection, 2 Dex, 1 dodge, 1 natural) | Dex +2, natural armor +2: 24 | Blur 20% concealment | +2 | 26 |
| Saves | 9 / 8 / 4 (bravery +2 vs fear) | 11 / 10 / 6 | — | — | 11 / 10 / 6, native |
| Greatsword | +17/+12, 2d6+13 (with Bull's) | +19 base with Bull's | Haste from Vandiana | +2 | +16 with Power Attack on (+19 −3), 2d6+22 with Power Attack |
| Perception / Stealth | 12 / 14 | 14 / 16 | — | Perception +2 | 16 / 16 |
| Attacks of opportunity | Combat Reflexes | Dex 18 | — | — | 5 |

- **Shadowdancer:** the game has none. Its two levels are a class built here with the shadowdancer's own progression: d8, medium BAB, good Reflex, poor Fortitude and Will. It has no features and is never offered to the player.
- **Shadowdancer features:** evasion and uncanny dodge are the native features. Hide in plain sight is the native forced stealth, as a move-action ability, usable anywhere in this dim sanctum.
- **Fighter:** feats, weapon training (heavy blades), bravery and armor training are native selections.
- **Disruptive** is listed but not implemented (deferred, below).
- **Gear:** +1 chain shirt, +1 greatsword, masterwork shortbow (second weapon set), amulet of natural armor +1, cloak of resistance +1, ring of protection +1, and potions of Bull's Strength, Blur and Cure Moderate Wounds.
- **Darkvision** is a fact that sees through Vandiana's darkness.
- **Starting position:** they start in native ambush stealth.
- **Appearances:** two different donor models.

### Dark naga (Large aberration, LE, advanced)
| Line | Source | Advanced | Preset | Observed |
|---|---|---|---|---|
| Scores | 14/21/18/16/15/17 | 18/25/22/20/19/21 | — | same |
| HP | 85 (10d8+40) | 105 | — | 105, native |
| AC | 22 (5 Dex, 1 dodge, 7 natural, −1 size) | 26 (natural armor 9, Dex 7) | +2 | 28 |
| Saves | 7 / 10 / 9 (+2 vs charm) | 9 / 12 / 11 | — | native |
| Bite / sting | +8, 1d4+2 / +8, 2d4+2 plus poison | +10, 1d4+4 / 2d4+4 | +2 | +12, 1d4+4 / +12, 2d4+4 |
| Poison | Fort DC 19, sleep 2d4 minutes, 1 save | DC 21 (Con-based) | — | — |
| Spells | sorcerer CL 7: 7/7/5 a day (Cha 17) | 8/7/5 (Cha 21), DC 10 + level + 5 | the preset's native DC bonus | CL 7, DC 16/17/18; slots 8/7/5 |
| Perception / Stealth | 19 / 18 | 21 / 20 | Perception +2 | 23 / 20 |

- **Natural armor:** stated as one natural armor bonus of 9, because two natural armor modifiers would not stack in this game.
- **Immunities:** poison, by descriptor, for buffs and spells.
- **Guarded thoughts:** +2 racial against charm.
- **Trip:** maneuver immunity.
- **Spells:** the printed list cast spontaneously. Each spell is the game's own, at its own level, spending that level's daily slots.
- **Sting:** a native Fortitude save with the poison's own descriptor. Immunity to poison or sleep, Delay Poison and Neutralize Poison all act natively. A failure is the native sleeping condition for 2d4 minutes, and damage or the sanctum's existing wake action rouses the sleeper. It is not Constitution damage or the mist's process.

## Ability coverage
| Ability | Treatment | Why |
|---|---|---|
| Vandiana's native spells: Baleful Polymorph, Confusion, Enervation, Haste, Slow, Sleet Storm, Summon Monster III, Glitterdust, Web, Grease, Resist Energy, Dimension Door, Cure Critical Wounds, Blindness (for Blindness/Deafness), Dispel Magic, Eagle's Splendor, Hold Person, Sound Burst, Divine Favor, Obscuring Mist, Remove Fear | native | The game's own spells, prepared in her books and spent there. |
| Order's Wrath, Unholy Blight | encounter-local | No native spell. Built on a native burst's look. Spell resistance is native. Each creature makes a native Will save. Damage is 1d8 per two caster levels (CL 8: 4d8) by the target's alignment on the spell's axis: full, half, or none (quarter for a saving neutral). The rider (dazed 1 round, or sickened 1d4 rounds) applies only on a failed full share. |
| Command | encounter-local | No native spell. Halt only: a native Will save, then one round of no actions (mind-affecting, compulsion). |
| Darkness, Darkvision | encounter-local, bounded | Darkness radiates 20 ft from her person for a minute per level. A creature inside has 20% concealment against an attacker without darkvision within 60 ft. No light is dimmed and no sight is claimed beyond that rule. The Blackstrikes and the naga have darkvision, and so does Vandiana after her Darkvision. The party has none in this game. |
| Mage's Faithful Hound, Alarm, Glyph of Warding | the existing WR-04 wards | Their slots are spent once at the baseline. The glyph uses her spell context; its blast comes from the glyph itself. The hound's tether follows the real Vandiana. |
| False Life, Mage Armor | native buff (Mage Armor) / prepared temporary HP (False Life) | Cast before arrival, from her own slots, with remaining durations. Dispellable. Never re-added. |
| Channel negative energy, acid dart, blast rune | native features | Printed uses. Channel is at her cleric level. |
| Combined spells | native | Mystic theurge feature. |
| Improved Great Fortitude | built | A once-a-day failed-Fortitude reroll (native d20 reroll). The game's own feat is a flat +2. |
| Summoner's charm | **deferred** | The game's conjuration school has no such feature. Her one Summon Monster III keeps native duration. |
| Arcane bond (ring) | **deferred** | The game's wizard bond offers only familiars. The ring of protection +2 is worn. |
| Magic Circle against Good, Wind Wall, Sanctuary, Chill Touch, Bleed | **deferred** | No native spell or supported treatment in this milestone. Each holds its printed slot and cannot be cast; the slot shows the reason. |
| Explosive Runes, Detect Thoughts, Comprehend Languages, Arcane Sight, Command Undead, Message, Read Magic, lore keeper | **deferred** | No combat use here. Slots held, as above. |
| Magical Aptitude, Scribe Scroll | **deferred** | Skill-only and crafting. |
| Disruptive (Blackstrike) | **deferred** | This game has no casting defensively for it to modify. |
| Naga detect thoughts (continuous) | **deferred** | No mind reading in this game. |
| Naga Shield, Silent Image | **deferred** | Not cast by its tactics here. Its slots remain spontaneous. |

## Explicit adaptations
1. **Whip.** The game has no whip, and no nonlethal damage. Her +1 whip is a one-handed weapon with the whip's 15-foot reach and 1d3. Each hit is a native attack roll; its nonlethal points are recorded in the log and not dealt as lethal damage. Her tactics keep her out of melee anyway (whip action score 1).
2. **Naga model.** The game has no naga. The Young Hydra's body is drawn one size step down, at Large.
3. **Calibration.** A small, once-only difference between this game's class tables and the printed base values: Vandiana HP −2 and Perception −7; the Blackstrikes HP +4.
   - It is computed at placement with the preset's Difficulty modifiers left out, so the preset still applies.
   - It is held on a buff, which death removes, so a corpse shows the class-table maximum.
4. **Glyph exclusions.** Vandiana and her two champions are Lissala's worshippers. The glyph and the hound ignore all four defenders, since they are her own. The naga is not a worshipper.
5. **Bull's Strength timing.** A Blackstrike's three-minute Bull's Strength is drunk when the east door is heard or when combat begins, not on a distant warning. The source says only "before combat", and a warning does not tell it when the fight will come.
6. **Potions in combat.** In combat a defender's own brain drinks its potions in its own turn: Blur, Bull's Strength, Cure Moderate Wounds, and Vandiana's Cat's Grace if the door was never heard. Before combat, the preparation step issues native commands. Nothing is applied for free.

## The countdown and the awakening
- Only an engagement commits A4's clock (above). The old placeholder action on the ritual tools is now "Strike at the ritual tools", an aggressive act through the same single commit, never a second trigger.
- Opening the east door, scouting, inspection, the glyph and the hound do not commit it. Each of these may raise awareness or preparation.
- The roll, the acceleration, the deadline and the reason are saved and never rerolled. Kurshu's 90-minute clock is unchanged, and the earlier of the two decides.
- When the awakening comes due during A4, the existing event fires once and the fight continues. **Krune's own encounter (A5) is the next milestone and is not part of WR-06.**

## Saves
- **State version 5.**
- **Fresh arrival:** placed.
- **Version 4 state already inside the sanctum:** checked at its first load. The defenders are added only if every one of these holds:
  - Kurshu's encounter is the current one;
  - the party and its pets stand outside A4/A5;
  - the east door was never opened;
  - the hound never noticed anyone;
  - the glyph never went off;
  - neither clock has started;
  - the emerald is intact.
- **Anything else:** the save is left as it was, with one notice, and the support Vandiana remains.
- **The support Vandiana** (`WardFixtures`) is taken out of the game when the real one is placed. It keeps only its identity for saved contexts. It is never resolved, protected or recreated, and `WardFixtures.Vandiana` returns the real Vandiana.
- **Persistence:** spells, slots, potions, daily uses, temporary HP, buffs, deaths and preparation records are all saved natively or in the state. A load re-binds and recomputes only the brain's list of what is still castable.

## Rewards
- Native XP and the defenders' worn and carried gear (Vandiana's headband included) are recoverable.
- The Society's per-player gold, prestige and final scenario rewards are deferred.
- Defeating A4 clears no curse and refills nothing.
