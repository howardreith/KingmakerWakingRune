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
| Engagement | The first hostile rule between the party's side and a defender: an attack roll, damage, a saving throw against a hostile effect, or a harmful spell aimed at one. A defender and a party member in combat with sight of each other also counts. So does an aggressive act against the rite (striking the altar's tools or the emerald). This calls `RitualRules.CommitConfrontation` once: 1d6+6, with feeder acceleration once. The engagement also ends the Blackstrikes' ambush, as the game's own start-combat action does. |
| Combat | Native brains. Vandiana casts from her two books, and the naga from its slots. The Blackstrikes fight, hide, and drink Blur and their other potions as their own brains' choices. |
| Preparations | Each one is settled from the field every tick, whoever performed it: the director's command, the actor's own brain, or whoever controls it. The record keeps who did it and what was spent. It ends for good when its actor or subject dies, or when its potion or slot is gone for a full round with no effect. No preparation gates any other decision. |
| Control | A defender under a charm, domination, confusion or fear is not commanded and spends nothing for the encounter. The director cancels only its own commands and resumes when the effect ends. The tactics read native allegiance, so a turned defender treats the party as its allies. |
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
| Whip | +1 whip +6/+1, 1d3 nonlethal | the weapon's own nonlethal enchantment (below) | — | +2 | +8, 15 ft reach |

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
- **Disruptive:** a foe the Blackstrike threatens faces +4 on the game's own defensive-casting DC (observed: DC 15 → 19, 17 → 21).
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
- **Spells:** the printed list cast spontaneously, Shield included. Each spell is the game's own, at its own level, spending that level's daily slots. Silent Image is the one printed spell it does not have (see Ability coverage).
- **Sting:** a native Fortitude save with the poison's own descriptor. Immunity to poison or sleep, Delay Poison and Neutralize Poison all act natively. A failure is the native sleeping condition for 2d4 minutes, and damage or the sanctum's existing wake action rouses the sleeper. It is not Constitution damage or the mist's process.

## Ability coverage
| Ability | Treatment | Why |
|---|---|---|
| Vandiana's native spells: Baleful Polymorph, Confusion, Enervation, Haste, Slow, Sleet Storm, Summon Monster III, Glitterdust, Web, Grease, Resist Energy, Dimension Door, Cure Critical Wounds, Blindness (for Blindness/Deafness), Dispel Magic, Eagle's Splendor, Hold Person, Sound Burst, Divine Favor, Obscuring Mist, Remove Fear | native | The game's own spells, prepared in her books and spent there. |
| Order's Wrath, Unholy Blight | encounter-local | No native spell. Built on a native burst's look, carrying the printed [lawful] and [evil] descriptors. Spell resistance is native, and each creature makes a native Will save. Damage goes by the target's alignment on the spell's axis: full, half, or none (a quarter for a saving neutral). A full-share outsider (a chaotic one for Order's Wrath, a good one for Unholy Blight) takes 1d6 per caster level, at most 10d6 (CL 8: 8d6). Everyone else takes 1d8 per two levels, at most 5d8 (CL 8: 4d8). The rider (dazed 1 round, or sickened 1d4 rounds) applies only on a failed full share. Seen: a Movanic Deva, a good outsider, took 8d6 in the same burst where True Neutral party members took the half share of 4d8. |
| Command | encounter-local | No native spell. Halt only: a native Will save, then one round of no actions (mind-affecting, compulsion). |
| Darkness, Darkvision | encounter-local, bounded | Darkness radiates 20 ft from her person for a minute per level. It gives a creature inside 20% concealment, exactly as one more native concealment entry would. So every native reason to ignore partial concealment still applies: the attacker ignores concealment or ignores partial concealment, the attacker's blindsight reaches, or the target cannot benefit. Darkvision within 60 ft sees through it, and so does true seeing, which pierces magical darkness. Unrelated concealment (fog, blur, invisibility) is untouched. No light is dimmed and no sight is claimed beyond that rule. The Blackstrikes and the naga have darkvision, and so does Vandiana after her Darkvision. Before 0.7.16 the darkness area never appeared at all (see the acceptance report). |
| Magic Circle against Good | encounter-local on a native effect | A 10-ft emanation from the warded creature, 10 minutes per level. It and its allies inside get the game's own Protection from Good: its deflection and resistance bonuses against good creatures. The printed protections against mental control and summoned good creatures are as broad as the game's own Protection from Good, which has none. |
| Sanctuary | encounter-local | A foe that attempts a direct attack on her, or a harmful spell aimed at her, makes one Will save for this casting, at its first real attempt. On a failure that command is lost, and the failure bars its later hostile targeting of her while the ward lasts. It bars only a current enemy's hostile targeting: never a heal or other help, and never once she is no longer its enemy (domination, charm). Area effects are unaffected. The ward ends when she attacks or casts a harmful spell at a foe. Her tactics: a last resort when wounded and pressed, after which she recovers rather than attacks. |
| Chill Touch (×2) | encounter-local | One touch per caster level is held (CL 9: 9). Each touch is a native melee touch: 1d6 negative energy, and 1 Strength damage unless a Fortitude save succeeds (DC from her Int). Spell resistance applies. Casting another spell dissipates the rest. Her tactics: only with a foe at her side, and while holding the touch she casts no other spell at that foe. |
| Shield (naga) | native | The game's own Shield, cast from its 1st-level slots by its own brain. |
| Mage's Faithful Hound, Alarm, Glyph of Warding | the existing WR-04 wards | Their slots are spent once at the baseline. The glyph uses her spell context; its blast comes from the glyph itself. The hound's tether follows the real Vandiana. |
| False Life, Mage Armor | native buff (Mage Armor) / prepared temporary HP (False Life) | Cast before arrival, from her own slots, with remaining durations. Dispellable. Never re-added. |
| Channel negative energy, acid dart, blast rune | native features | Printed uses. Channel is at her cleric level. |
| Combined spells | native | Mystic theurge feature. |
| Improved Great Fortitude | built | A once-a-day failed-Fortitude reroll (native d20 reroll). The game's own feat is a flat +2. |
| Summoner's charm | built | Her summoned creatures last half her wizard level longer (+2 rounds), on the native summon's own bonus duration. Seen: Summon Monster III, 9 + 2 rounds. |
| Arcane bond (ring) | **accepted adaptation** | Once a day, as a free action, she recovers one spent wizard slot, then casts it with its own action. That is the printed "cast one spell from her book" limited to spells prepared today: her book's other contents are not printed. Slots spent before the party arrived are not eligible. Seen: Haste recovered in several fights. |
| Disruptive (Blackstrike) | built | +4 on the game's own defensive-casting DC while a Blackstrike threatens the caster. The previous "no casting defensively" reason was wrong: the game has it (Kurshu's acceptance used it). |

**Printed abilities not implemented** (audit). The slot or feature is held and shown, so it isn't silently dropped.

| Ability | Category | Why |
|---|---|---|
| Wind Wall | **mechanic unavailable; explicit incompleteness** | The game has no wall that deflects projectiles crossing it. Building one (a placed segment, line tests on every ranged attack, the 30% miss for other missiles) is a new subsystem, not a spell entry. Its 3rd-level slot is held and cannot be cast. |
| Silent Image (naga) | **mechanic unavailable; explicit incompleteness** | The game has no figment that creatures react to. It is left out of the naga's known spells. |
| Bleed (orison) | **mechanic unavailable** | Its only use is to make a stabilized dying creature resume dying, and this game has no stabilization for NPCs or the party at those hit points. |
| Naga detect thoughts (continuous) | **mechanic unavailable** | No mind reading in this game. |
| Explosive Runes, Comprehend Languages, Read Magic, Message, lore keeper | **out of scope, no combat use** | Runes on a written text, languages, messages, lore: nothing in A4 uses them. Slots held. |
| Detect Thoughts, Arcane Sight | **out of scope, information only** | They tell a caster what the game's AI already knows. Slots held. |
| Command Undead | **out of scope here** | There is no undead in A4. A party with undead summons would see it unused. Slot held. |
| Magical Aptitude, Scribe Scroll | **out of scope** | Skills and crafting. |

"Full printed lists" therefore means **full slot accounting**: every printed entry occupies its slot. All but the entries above are castable, and the uncastable ones are named here, with their reasons.

## Explicit adaptations
1. **Whip.** The game has no whip. Her +1 whip is a one-handed weapon with the whip's 15-foot reach and 1d3.
   - **Its own enchantment:** the rules belong to the item, not to Vandiana, so they follow the whip to any wielder.
   - **Damage:** each hit is a native attack roll, and its damage is the native damage rule's result (damage reduction, the preset). That amount goes to the game's own nonlethal total, which the game already counts toward unconsciousness and shows on the character sheet.
   - **Armor rule:** a creature with an armor bonus of +1, or a natural armor bonus of +3, takes no damage (Core Rulebook). Nearly every party member has one or the other (Barkskin, Mage Armor, armor), so her whip rarely matters. Her tactics keep her out of melee anyway (whip action score 1).
   - **Ownership:** a whip hit records its share of the game's nonlethal total in a ledger on the creature (`KwrWhipNonlethalPart`). Only that share is ever healed, recovered, cleared or reduced by this mod, and only on a creature with the ledger. Nonlethal damage from any other source, in this campaign or another, is left exactly as the game leaves it.
   - **Healing:** healing magic heals the same amount of the whip's share. For that one heal the game's cap at lethal damage is lifted.
   - **Recovery:** natural recovery is a Hit Die an hour, and a full rest clears the whip's share.
   - **Past maximum HP:** nonlethal past maximum hit points is lethal.
   - **Party-side creatures:** the game's own threshold knocks them out (lethal plus nonlethal ≥ hit points). Seen: Kael Unconscious at 4/90 HP.
   - **Hostile NPCs:** they have no unconscious state in this game (they die at 0), so the whip's total on them stops one short of the threshold. A helpless knock-out for them would be a new encounter-local condition; that is the owner's decision.
   - **After the fight:** the game's own revival lifts the fallen. The whip's share is then reduced only as far as needed to stop holding them down, as that revival does for lethal damage.
2. **Naga model.** The game has no naga. The Young Hydra's body is drawn one size step down, at Large.
3. **Calibration.** A small, once-only difference between this game's class tables and the printed base values: Vandiana HP −2 and Perception −7; the Blackstrikes HP +4.
   - It is computed at placement with the preset's Difficulty modifiers left out, so the preset still applies.
   - It is held on a buff, which death removes, so a corpse shows the class-table maximum.
4. **Glyph exclusions.** Vandiana and her two champions are Lissala's worshippers. The glyph and the hound ignore all four defenders, since they are her own. The naga is not a worshipper.
5. **Bull's Strength timing.** A Blackstrike's three-minute Bull's Strength is drunk when the east door is heard or when combat begins, not on a distant warning. The source says only "before combat", and a warning does not tell it when the fight will come.
6. **Potions in combat.** In combat a defender's own brain drinks its potions in its own turn: Blur, Bull's Strength, Cure Moderate Wounds, and Vandiana's Cat's Grace if the door was never heard. Before combat, the preparation step issues native commands. Nothing is applied for free. Three engine details made this work for the first time in 0.7.15:
   - **The AI never drinks potions:** the native AI cast action treats any ability from an item as spent. So a small AI action finds the potion in the creature's own quick slots.
   - **The drink kept being replaced:** a potion is a move action, and the native brain replaced an unstarted drink every few tenths of a second. That drink may now wait one round before the brain can replace it.
   - **Blur couldn't target its drinker:** the game's own target check refuses a spell that cannot target allies, such as Blur, on any creature outside the player's faction, the drinker included. Only an A4 defender's own potion, drunk on itself, is let through.
7. **Ambush ends at engagement.** The Blackstrikes start in the game's own ambush stealth. In ambush, a hidden unit stays out of the fight until a foe comes within 5 feet. The engagement ends their ambush (not their stealth), as the game's own start-combat action ends a group's. Without that, a Blackstrike flickered in and out of combat and lost every action it began.
8. **Control yields.** Charm, domination, confusion and fear are the game's own effects. Confusion is read from the Confusion condition: the game leaves its confusion part on a unit after the confusion ends out of combat. The game's Dominate Person gives the caster's side no direct control (the creature fights under its own AI for them), so "a player order" to a dominated defender is not something this game offers.
9. **Spell effects are dispellable.** Every spell effect the encounter builds carries the game's "from a spell" buff flag, which native Dispel Magic requires: Darkness, Darkvision, Sanctuary, Magic Circle, the held Chill Touch, Command's halt and the pre-cast False Life. Before 0.7.22 none did, and none could be dispelled. Effects saved by earlier builds are marked at load.

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
- **Persistence:** spells, slots, potions, daily uses, temporary HP, nonlethal damage, buffs, deaths and preparation records are all saved natively or in the state. A load re-binds and recomputes only the brain's list of what is still castable.
- **A save made by 0.7.0–0.7.10 with the defenders placed** gains, once at load:
  - her arcane bond and its daily use;
  - the Blackstrikes' Disruptive;
  - the current brain actions.

  Her prepared Sanctuary, Magic Circle against Good and two Chill Touch slots are swapped in place for the real spells, each spent or unspent as saved. The old placeholders are still built, so such a save loads.

## Rewards
- Native XP and the defenders' worn and carried gear (Vandiana's headband included) are recoverable.
- The Society's per-player gold, prestige and final scenario rewards are deferred.
- Defeating A4 clears no curse and refills nothing.
