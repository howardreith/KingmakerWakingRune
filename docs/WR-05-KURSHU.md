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
"Final observed" is the live unit in runs `wr05-a` (0.6.0–0.6.7) and `wr05-b` (0.6.8–0.6.15), under the default **Normal** preset ("somewhat easier enemies", enemy difficulty Normal). Only CMB/CMD changed between them.

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
| CMB / grapple | +20 / +24 | +1 | +21 / +25 | 23 / 27; after Feed 25 / 29 | Difficulty +2 (attack) |
| CMD | 38 | +1 | 39 | 41 (0.6.8 on); after Feed 43 | Difficulty +2 (touch AC) |
| Spell-like CL / concentration | 12 / +19 | +1 | 13 / +20 | 13 / +20 | |
| Initiative | +10 | — (printed without the negative-level penalty) | +10 | +10 | |
| Perception / Stealth | +22 / +19 | +1 | +23 / +20 | 25 / +20 (hidden checks 23, 27, 35 on d20+20) | Perception Difficulty +2 |
| Spell-like DC, 3rd level | 20 | — | 20 | 20 | the preset's `AbilityDCBonus` (0 at Normal) |
| Plane shift DC | 24 | — | 24 | not implemented; see below | |
| Negative levels | 3 | −1 | 2 | 2 (`Divine Separation`, −2 shown on rolls) | |
| Limited wishes left | 2 (one used) | — | 2 | 2 at arrival (resource 3 → 2), then 0 after two casts | |

How the numbers are reached, with nothing counted twice:
- **Class levels.** The donor's own native class (`OutsiderClass`, d10) at 17 levels supplies Hit Dice, base attack +17 and base saves 5/10/10.
- **Corrections.** A hidden fact adds only the difference to the printed totals before negative levels: BAB +0, Fort +7, Ref −5, Will +1, hit points −1, natural armor +13, deflection +2, Improved Initiative +4, Perception +20, Stealth +20 (the engine applies the Large −4 itself). Nothing is added to CMB or CMD (see the maneuver chain below).
- **Divine Separation** then applies the two levels exactly as the native negative-level buff does: skills, saves, attacks, CMD, 5 hp each, ability checks and caster level.
- **The preset.** The game adds its own difficulty features to every enemy (`DifficultyAdvancementBaseFeature`, `DifficultOpponent`). The Erinyes donor has no `DifficultyStatAdvancement` of its own, so nothing further is copied.

### Combat maneuver chain (resolved in 0.6.8)
The printed values are consistent once Large's special size modifier (+1) is used: CMB 17 + 5 + 1 − 3 = 20, grapple 20 + 4 (grab) = 24, CMD 10 + 17 + 5 + 6 + 1 + 2 (deflection) − 3 = 38. Hard mode removes one negative level: 21 / 25 / 39.

The engine (`RuleCalculateBaseCMB`, `RuleCalculateBaseCMD`, decompiled) computes:
- **CMB** = BAB + Str + the size table's maneuver modifier + `AdditionalAttackBonus` + `AdditionalCMB`. The table gives Large **+2**, and the −1 attack size modifier sits in `AdditionalAttackBonus`, so size nets the printed +1.
- **CMD** = 10 + BAB + Str + Dex + dodge + size (+2) + (touch AC − 10 − Dex − dodge) + `AdditionalCMD`. Touch AC carries the −1 size modifier, so size nets +1 here too.

So the engine already counts size once. The +1 `AdditionalCMD` the first builds added (0.6.0–0.6.7) assumed a double count that does not exist: that, and nothing else, was the unintended point. 0.6.8 removes it.

The remaining +2 on each is the player's preset (`Difficulty` +2 on attack, which CMB includes, and +2 on AC, which reaches CMD through touch AC). It applies to every enemy and is kept. The live sheet shows the chain (`kurshu` report, `maneuver chain` line), in run `wr05-b`:

| | CMB | Grapple | CMD |
|---|---:|---:|---:|
| Printed | 20 | 24 | 38 |
| Hard mode (2 separation levels) | 21 | 25 | 39 |
| + preset Difficulty | 23 | 27 | 41 |
| Native, observed (0.6.9 on) | **23** | **27** | **41** |
| Native after Feed removes both separation levels | **25** | **29** | **43** |

Breakdown observed: CMB = BAB 17 + Str 5 + size 2 + misc −1 (`AdditionalAttackBonus`: Size −1, Difficulty +2, NegativeEnergyPenalty −2); CMD = BAB 17 + Str 5 + Dex 6 + size 2 + misc 1 (touch 19 − 10 − 6, `AdditionalCMD` −2).

### Feats
Printed (Subtier 10–11): Combat Casting, Combat Reflexes, Craft Wondrous Item, Great Fortitude, Hover, Improved Initiative, Improved Iron Will, Iron Will, Scribe Scroll.

| Feat | Runtime |
|---|---|
| Great Fortitude, Iron Will, Improved Initiative | Already in the calibrated Fortitude, Will and initiative; not added again. |
| **Combat Casting** | The game's own `CombatCasting` feat (`ConcentrationBonus` +4). Observed in play: "casting defensively check succeeded". |
| **Combat Reflexes** | The game's own `CombatReflexes` feat (`DerivativeStatBonus` Dex → attacks of opportunity). Observed: `AttackOfOpportunityCount 7 (base 1) [Feat +6]`. |
| **Improved Iron Will** | The game's own `IronWillImproved` is a flat +2 on Will (it is Iron Will again, with Iron Will as a prerequisite), not the printed once-a-day reroll, and would lift her Will above the printed value. Her reroll is a one-use buff built from the native `ModifyD20`: Will saves only, reroll only on a failure, removed by its own use. It is given once, when she appears; a reload carries its absence. |
| Craft Wondrous Item, Scribe Scroll | Nothing to act on in this encounter. |
| Hover | No flight in this game; not implemented. |

The `kurshu` report lists the native feats with "present" or "MISSING", and whether the reroll is unused.

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
- **Intelligence drain (high tier).** Each slam hit drains 1d4 Intelligence with a native `RuleDealStatDamage` (`IsDrain`): saved with the stat, not healed by rest, removed by restoration. The lower tier's fixed 1 drain plus 1d4−1 damage is not used.
- **Intelligence 0 is comatose, not death (adaptation, 0.6.8–0.6.15).** In the source a creature at Intelligence 0 is comatose and helpless. The engine instead kills any creature whose ability score falls below 1 (`RuleDealStatDamage` sets `MarkedForDeath`). The 0.6.0–0.6.7 builds let that happen; a character died at Intelligence 1. Only her drain is changed, nothing global:
  - Drain that would take the score below 1 is applied only down to 1, and the rest is **held back** on a comatose buff (the native `Unconscious` condition: helpless, cannot act). Further slams add to the held-back points.
  - The true drain is the engine's drain plus the held-back points. **Restoration** (the game's restores "all points permanently drained from a single ability score") clears the engine's drain, and that clears the held-back points too: the creature wakes. A smaller restoration pays the held-back points first. Lesser restoration does not touch drain.
  - Recovery is checked from the encounter's tick about once a second of game time, as well as on the creature's own round, because turn-based combat skips a helpless creature's turn.
  - Recovery is also settled before every new slam drain (0.6.16), and the new drain then goes through the native rule, so native immunity applies. Without this, a restoration followed by a slam before the next check would wake the creature with the new slam's drain erased too.
  - The held-back points are saved with the buff (as fields; 0.6.11–0.6.14 used properties, which the game does not save).
  - A comatose creature can still die from hit point damage; she keeps slamming a helpless target.
- **Grab, grapple and constrict (0.6.8).** A bounded, encounter-local grapple with the normal costs:
  - **Grab.** A tail hit makes a free grapple check (native `RuleCombatManeuver`, grapple +27 including the printed +4) against a creature of her size or smaller, only while her tail holds no one. Freedom of movement makes the attempt fail outright (in this game's own words, "all combat maneuver checks made to grapple the target automatically fail").
  - **Both grappled.** She grapples normally: the victim and she both take the grappled condition (no movement, −4 Dexterity, −2 on attacks, no attacks of opportunity, and a spell needs the native concentration check for difficult casting). The −2 does not apply to checks to grapple or escape. The body-part-only hold (−20, not grappled herself) is not used: at +7 she would almost never hold, and her tactics release to cast anyway.
  - **Constrict** 1d8+5 on the successful grab check.
  - **Each turn** (turn-based: at her turn start, before her brain acts; real time: when her standard action is ready) she decides. While she has any offensive spell-like ability left, she lets go as a free action and casts ("she lets go to cast"). When she has none, she **maintains** with a standard action: a grapple check at +5, and on a success the tail's damage and constrict.
  - **The hold ends** when a maintenance chosen last turn was never made, a maintenance check fails, she casts or attacks with that action instead, the victim is out of reach or dead, she cannot act, or freedom of movement reaches the victim.
  - **The victim** tries to break free on each of its rounds with its own maneuver roll against her CMD. This attempt costs it no action (there is no grapple interface for the player), which favours the party.
  - **Saved:** the held creature, the owed maintenance and the counts. A reload re-checks the hold and ends one that no longer stands.
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

## Known deviations and notes
- **CMB/CMD (0.6.0–0.6.7):** CMD was one point high through an unneeded correction; resolved in 0.6.8 (see the maneuver chain). The earlier note here, which called the printed CMD inconsistent, used the size modifier with the wrong sign and was wrong.
- **Plane shift** is not implemented (above); her spell-like coverage is complete except for it and the non-combat at-will abilities.
- **Spell level of two utility abilities.** Her dispel magic and self-only teleport show spell level 6–7 in the sheet (the engine's default for an ability without a spell list). Neither allows a save, so no DC is affected; dispel uses her caster level.
- **Divine Separation on load.** Its count is re-applied from the saved encounter state on every load ("now 2 negative level(s) (was 0)" in the log); the effect is identical.
