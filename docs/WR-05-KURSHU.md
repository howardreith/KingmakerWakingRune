# WR-05: Kurshu the Undying (A1). Source, implementation and adaptations

## Sources
- **A1:** description, creatures, traps and behavior, printed page 5.
- **Kurshu's Subtier 10–11 block:** printed page 8.
- **Shared special abilities:** printed pages 7–8 (Divine Separation, Feed, Power Surge, Spell-Like Crafting), from the Subtier 7–8 block.
- **Development:** the blood message and the 90-minute deadline, printed pages 8–9.
- **"Scaling Breaching the Sanctum":** printed page 9.

**Profile:** Subtier 10–11, six-character baseline, extra challenge. The player's own difficulty preset is applied by the game on top of the authored values.

## Extra challenge, applied once
The printed Subtier 10–11 block already includes three negative levels and the named Weakness. For Subtier 10–11 the sidebar says: "Remove Kurshu's weakness ability and 1 negative level." So:
- **Weakness removed:** natural armor +2 and spell resistance +2, the two penalties the Subtier 10–11 Weakness names. Nothing else.
- **One negative level removed:** +1 on what a negative level lowers (attacks, saves, skills, CMB, CMD, caster level and concentration) and +5 hit points.
- **Kept:** two negative levels and Divine Separation.
- **Not imported:** the lower tier's 50-hit-point, DR and resistance penalties, which belong to the Subtier 7–8 Weakness only.
- **Hard mode removes the Weakness itself.** Removing her separation levels by feeding does not "restore" its penalties a second time.

## Stat sheet
"Final observed" is the live unit in run `wr05-a` on 0.6.0/0.6.1, under the default **Normal** preset ("somewhat easier enemies", enemy difficulty Normal).

| Statistic | Printed (10–11) | Hard-mode change | Authored | Final observed (Normal) | Native preset part |
|---|---:|---|---:|---:|---|
| AC / touch / flat-footed | 28 / 17 / 22 | +2 natural | 30 / 17 / 24 | 32 / 19 / 26 | Difficulty +2 |
| Natural armor | +11 | +2 | +13 | +13 | |
| Hit points | 214 | +5 | 219 | 219 | |
| Fort / Ref / Will | +17 / +8 / +13 | +1 each | +18 / +9 / +14 | +18 / +9 / +14 | |
| DR | 10/cold iron and magic | — | 10/cold iron **and** magic | native `AddDamageResistancePhysical`, `Or = false` | |
| Resist cold, electricity, fire, sonic | 30 | — | 30 | 30 | |
| Spell resistance | 24 | +2 | 26 | 26 | |
| Regeneration | 5 (acid) | — | 5 (acid) | native `AddEffectRegeneration` | the enemy-difficulty setting can halve or disable it natively |
| Slam ×2 | +18, 1d6+5 plus 1d4 Int drain | +1 | +19 | +21, 1d6+5 | Difficulty +2 |
| Tail slap | +13, 1d8+2 plus grab | +1 | +14 | +16, 1d8+2 | Difficulty +2 |
| CMB / grapple | +20 / +24 | +1 | +21 / +25 | 23 / 27 (0.6.2): **open** | |
| CMD | 38 | +1 | 39 | 42 (0.6.2): **open** | |
| Spell-like CL / concentration | 12 / +19 | +1 | 13 / +20 | 13 / +20 | |
| Initiative | +10 | — (printed without the negative-level penalty) | +10 | +10 | |
| Perception / Stealth | +22 / +19 | +1 | +23 / +20 | 25 / +20 (hidden checks 23, 27, 35 on d20+20) | Perception Difficulty +2 |
| Spell-like DC, 3rd level | 20 | — | 20 | 20 | the preset's `AbilityDCBonus` (0 at Normal) |
| Plane shift DC | 24 | — | 24 | not implemented; see below | |
| Negative levels | 3 | −1 | 2 | 2 (`Divine Separation`, −2 shown on rolls) | |
| Limited wishes left | 2 (one used) | — | 2 | 2 at arrival (resource 3 → 2), then 0 after two casts | |

How the numbers are reached, with nothing counted twice:
- **Class levels.** The donor's own native class (`OutsiderClass`, d10) at 17 levels supplies Hit Dice, base attack +17 and base saves 5/10/10.
- **Corrections.** A hidden fact adds only the difference to the printed totals before negative levels: BAB +0, Fort +7, Ref −5, Will +1, hit points −1, natural armor +13, deflection +2, Improved Initiative +4, Perception +20, Stealth +20 (the engine applies the Large −4 itself), and CMD +1.
- **CMD.** The engine's CMD counts the Large AC penalty inside touch AC as well as its +1 size bonus; the +1 offsets that second count.
- **Divine Separation** then applies the two levels exactly as the native negative-level buff does: skills, saves, attacks, CMD, 5 hp each, ability checks and caster level.
- **The preset.** The game adds its own difficulty features to every enemy (`DifficultyAdvancementBaseFeature`, `DifficultOpponent`). The Erinyes donor has no `DifficultyStatAdvancement` of its own, so nothing further is copied.

## The unit
- **Identity.** `KWR_KurshuTheUndying`, a real hostile Mobs unit marked by a `KurshuPart` identity (`A1-Kurshu`). She has no ward fixture part and none of the fixture protections.
- **Creature type.** Large outsider: the native `OutsiderType`, `SubtypeEvil`, `SubtypeLawful` and `SubtypeExtraplanar` facts. Alignment lawful evil.
- **Scores:** Str 20, Dex 23, Con 26, Int 31, Wis 20, Cha 25.
- **Attacks.** Two native `SlamLarge1d6` limbs (primary) and a `TailLarge1d8` secondary limb: −5 and half Strength, natively.
- **Trip.** Immune to trip (`ManeuverImmunity Trip`).
- **Senses.** Blindsense, darkvision, low-light vision and scent have no separate effect in this engine; the native vision applies.
- **Appearance.**
  - The game has no serpentine winged woman. The Erinyes devil (`CR8_ErinyesDevilStandard`, prefab `75b74b2a…`), a winged female outsider, is the model.
  - The engine scales a view only when a unit's size differs from its blueprint, so her Medium model is drawn at the engine's own one-size step (×1/0.66) while her rules size stays Large.
  - Her collision footprint stays the model's own, which fits the two-foot apex better than a full Large footprint.
  - **Compromises:** human legs in place of a serpent's tail, and the donor's colors.
- **Position.** She is placed on the navigation node nearest the apex rune (grid 13, 19.6, height 20 ft). Nothing in the geometry was changed.

## Attacks and special attacks
- **Intelligence drain (high tier).** Each slam hit drains 1d4 Intelligence with a native `RuleDealStatDamage` (`IsDrain`): saved with the stat, not healed by rest, removed by restoration.
  - The lower tier's fixed 1 drain plus 1d4−1 damage is not used.
  - **Engine rule:** the game kills a creature whose ability score falls below 1 (`MarkedForDeath`), where the tabletop would leave Intelligence 0 helpless.
- **Grab and constrict (adaptation).** The game has a grapple roll but no grab, constrict or grapple condition that leaves the grappler free to act.
  - A tail hit makes a free native grapple maneuver roll (CMB +4 to grapple, printed) against a creature of her size or smaller.
  - Success holds the target: cannot move, −4 Dexterity, −2 on attacks. The target constricts once for 1d8+5 bludgeoning, on the same check, as constrict deals damage on a successful grapple check.
  - Each round the held creature makes its own native maneuver roll against her CMD to break free. The hold ends if she falls, is held herself, or is out of reach.
  - She holds with the tail only, so she keeps acting. She does not spend standard actions to maintain the grapple and constrict again, because her tactics favor spells.
- **Power Surge.** Three per day, swift, a one-round buff: +2 to the DC of her next spell-like ability (native `AddBonusDC`). It is removed by that ability's cast, and an unused surge lapses at the end of its round. The use is spent either way; nothing stays.

## Spell-like abilities
Each is the native spell cloned as a spell-like ability, with its own daily resource saved natively on the unit and mirrored in the saved ledger.
- **Caster level** is 15 (the printed 12 plus the three printed negative levels); Divine Separation lowers it to 13 natively.
- **DCs** are the native formula: 10 + spell level + Cha +7, so 3rd-level spells are DC 20, plus the preset's native enemy DC bonus.
- **The pre-combat expenditure:** one limited wish is already spent.

| Printed | Runtime | Uses |
|---|---|---|
| cure serious wounds | `CureSeriousWoundsCast` | 3 |
| dispel magic | `DispelMagicTarget` | 3 |
| displacement | `Displacement` | 3 |
| fireball (DC 20) | `Fireball` | 3 |
| greater teleport (self) | `DimensionDoorCasterOnly`, as "Greater Teleport (self only)": she alone, to a clear point on A1's own floor; never the party, never through stone | 3 |
| hold person (DC 20) | `HoldPerson` | 3 |
| lightning bolt (DC 20) | `LightningBolt` | 3 |
| limited wish | Two remaining uses, shared by four bounded duplications at a 7th-level spell's DC (24): Break Enchantment on herself (when slowed or nauseated), Hold Monster (a non-humanoid), Cone of Cold (three or more foes), Greater Dispel Magic (a heavily buffed foe) | 3 (1 spent) |
| magic missile | `MagicMissile` | 3 |
| plane shift (DC 24) | **Not implemented.** An unwilling plane shift would strand a party member with no way back in this game; it is not offered and she never casts it | — |
| slow (DC 20) | `Slow` | 3 |
| stinking cloud (DC 20) | `StinkingCloud` | 3 |
| vampiric touch | `VampiricTouchCast` | 3 |
| at will: detect magic, floating disk, mage hand, read magic, tongues | not combat-relevant; not implemented | — |

## Feed and Divine Separation
- **Divine Separation** is her own fact. It applies the native negative-level penalties for its saved count (2), is not a native negative level, and so no restoration finds it, and the native "levels ≥ HD" death check never counts it.
- **Feed** is once per day, a full-round action, aimed at the point where a corpse lies within her reach.
  - The corpse must be a real, dead, non-summoned outsider of at least 8 Hit Dice that she has not already eaten.
  - Feeding removes one separation level per Hit Die, never more than she has, and never any negative level the party gave her.
  - A devoured monster's body leaves the world. A fallen party member's remains stay and can still be raised; they are marked consumed.
- **Summons.** The engine dismembers a summoned creature when it dies, leaving no corpse (`SummonedUnitBuff`, then `DismemberedUnitBuff`). So a summoned outsider is never food.
- **A3's stone husks** are scenery, not corpses, and never count.

## Encounter flow
- **Appearance.** She does not exist until the token's arrival has committed, nothing is loading, the arrival fade is hidden and the game is in its normal or paused mode. She cannot act under the fade.
- **Hidden check.** On appearing she makes a native Stealth check against every party member's native Perception. If no one beats it, she believes she is hidden and casts Displacement (a real use and duration) before her brain takes over. If anyone does, she does not.
- **Combat** starts natively. Her brain is native (`BlueprintAiCastSpell` per spell-like ability, the native attack action), with considerations that score her printed tactics: ranged spells where they pay, outsiders first, areas only on two or more, no spell on someone already held, slowed or nauseated, and no displacement twice. Native commands enforce range, line of sight, attacks of opportunity, action costs and turns.
- **The director** adds only two choices the brain cannot make. A command that has not finished in two rounds is dropped, and that corpse is not retried for three rounds. In turn-based mode Feed is decided when her turn starts, before her brain acts. It is used at most once per round, and only on her own turn in turn-based combat:
  - feeding on an eligible corpse;
  - teleporting herself away when two or more foes are in reach and she is below half her hit points.
- **The substantial wound.** The first time her hit points fall to three quarters or less, she speaks the printed warning as a bark and in the log. It is saved once.
- **Defeat.** The first time her own hit points run out, whether unconscious under regeneration or dead, the printed blood message appears in the log and a notification, and the existing `RitualRules.DefeatKurshu` starts the 90-minute deadline once.
  - Hold, sleep and paralysis do not count.
  - Regeneration, a second fight, a reload or a later death neither restarts nor extends the deadline.
  - Only her death (acid stops regeneration, then the native death threshold) marks her slain.
- **The A2 Alarm** is Vandiana's own spell around her own guardian, so Kurshu does not set it off. Her defeat neither disarms it nor counts as Vandiana hearing it.

## Saves
- **Version 4 state** adds `Kurshu`: policy, entity id, hidden check, wound and defeat flags, separation levels, feed record, the use ledger and the surge.
- **A pre-departure save** gets the fight on arrival.
- **A save already inside the sanctum** from an earlier build keeps its sanctum as it was. She is not inserted, nothing is marked defeated, no clock starts, and a one-time notice says why.

## Known deviation
The native CMB, grapple and CMD on 0.6.2 were 23, 27 and 42, against authored 21, 25 and 39. The engine's maneuver formulas count size and deflection differently from the printed totals, which are themselves not fully consistent (printed CMD 38 = 10 + BAB 17 + Str 5 + Dex 6 + deflection 2 − size 1 − 3 negative levels would be 36). The difference is recorded, not silently corrected; the calibration is open.
