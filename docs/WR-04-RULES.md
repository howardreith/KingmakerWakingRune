# WR-04 rules notes

The sanctum's wards, investigation, restoration and rune sabotage. Printed numbers come from the scenario at Subtier 10-11, six PCs, extra challenge on. CRPG rulings are the package's numbered adjudications plus the entries here. Pure rules live in `src/KingmakerWakingRune/Sanctum/Wards` and are unit tested; `WardObserver` feeds them resolved native results.

## How a player meets it

- Clicking a rune, door, gem or inspectable prop out of combat walks a character over and opens its dialogue. In combat, or with a spell selected, the same proxy is an ordinary native target.
- Each dialogue separates reading from touching. Study answers roll a real skill check by the best eligible character; a repeat at the same rank is refused. "Leave it be" costs nothing.
- A failed manipulation rolls a real Will save, then brands the creature once for that plate.

## Rune damage pipeline

Hardness 8 is taken once per independently resolved hit, before the threshold, and only qualifying components count. Listed energy applies at full value, with no generic object halving. The proxy has no hardness of its own, so nothing subtracts twice. `RuneRules` holds the worked arithmetic; every outcome is logged with the components and the running total.

Example: 110 negative energy against Life is 110 - 8 = 102 toward 27. Two hits of 20 and 23 are 12 + 15 = 27, which disables it inside one round.

## Fixtures

A fixture is an invisible Aberration-typed unit of mod origin with no AI. Neutral-faction proxies (runes, door, gem) take native attacks and hostile spells; a non-Neutral faction (statues, inspection props, hound and hidden wards) takes `CanTargetFriends` spells only. Aberration is what makes `HoldPerson` reject the Alacrity focus while `HoldMonster` works. Armor class uses Dexterity 1, the armor class of an object.

## Rulings beyond the package's list

| Topic | Ruling | Why |
|---|---|---|
| Fixture saves | Only a rune plate is denied a save, and only against an effect of a kind its own routes name (Life: death, poison, disease; Alacrity: paralysis, stagger, movement-impairing, cold; Defense: electricity, sonic, force; Armor: acid, fire; Control: compulsion). Statues, props, Krune and Vandiana save normally. A fixture cannot be killed by a death effect or have its faction changed by charm or domination. | The printed "no saving throw to resist effects to which they are vulnerable", scoped by `RuneVulnerability` (WR-04-R1; 0.4.0 denied every fixture every save). |
| Stone to Flesh | A fixture-only postfix allows `AbilityTargetStoneToFlesh`; the DC 15 Fortitude is rolled by the restored unit. | The stock check only accepts petrified party members and its effect does nothing on scenery. |
| Bearer cultists | One blueprint per figure, built on the native level-3 cleric body (`CR2_BanditPositiveClericLevel3`) for class levels and spellcasting, set to the GameMastery Guide p.278 Cultist: NE, Str 10, Dex 13, Con 12, Int 8, Wis 15, Cha 16, masterwork sickle, chainmail, light shield, with the figure's own model. Attackers use the native Mobs faction; reporters are neutral runners. Only Support-N1 and Support-N4 carry the Sihedron credential. | The donor's difficulty scaling is removed. Its native cleric spell list and brain are kept; the printed spell list (death knell, hold person, bane, doom...), channel negative energy, domain abilities, darts, bloodroot poison and unholy water are not reproduced. Observed in game (open defect): restored bodies show ability scores below and varying around the printed block on the owner's difficulty preset (Str 4-6, Dex 1-11, Con 6-10, Wis 11); the cause is not yet identified. |
| Hound | A scripted bite through native attack, armor class and damage rules, against native targets, once per round. | No native Mage's Faithful Hound exists. |
| Alarm and Glyph triggers | Polled against real positions, with entry memory saved, using native trap views for discovery and disarm only. | Native script zones lose their views after a reload. |
| Alarm range | Radius 20 feet around (13.15, 22.6), and it reaches the A2 door. | Measured against the plan; arrival stays outside. |
| A2 door credential | `KWR_SihedronBrand` is the printed credential; only the two marked restored cultists carry it. A held impression or branded hand authorises one contact (see below). | The printed Knock/Stone Shape bypass has no native spell to support it. |
| Vandiana's Darkvision | Stored as an intent. | No native Darkvision buff exists. |
| Cat's Grace duration | Three minutes. | A potion's caster level 3; duration is not printed. |
| Melee on the door | A melee weapon hit counts as contact; a spell or ranged hit does not. | Package ruling 6. |

## WR-04-R1: the A2 local solution (standalone CRPG adaptation, not printed)

The scenario prints that A1's supports are petrified human cultists, that A2 is a locked stone door (hardness 8, 75 hp, break DC 28), that Perception DC 22 reveals "my sign" recut to "the life sign", and that a Sihedron brand or tattoo bypasses the feeder and opens the door. It does **not** print marked palms, impressions or a severed hand. These are this adaptation's additions:

- **Two marked sources.** Support-N1 (reporter) and Support-N4 (attacker) carry an authentic Sihedron palm mark: a small dark seven-pointed brand on the palm of a raised hand, plus a sentence in each figure's look text. Both hands are reachable from the A1 floor. Knowledge (Arcana) DC 20 on the figure adds that the star is a true Sihedron; no check is needed to see or copy it.
- **Impression.** "Take an impression of the marked palm (1 minute)" on a petrified marked figure, by the character who opened the dialogue, out of combat. The expedition's field kit supplies the materials. It yields one `Sihedron Palm Impression` (notable, cost 0, kept in inventory, not consumed at the door). It is offered only while the party holds none, so a lost impression can be remade from a still-petrified marked figure, and it is never regenerated on load.
- **Hand.** "Recover the marked hand" on the actual corpse of a marked restored cultist (failed survival save or later death), once per figure, yields one `Sihedron-Branded Hand`. Hands carry the same authority as an impression.
- **Presentation.** "Press the Sihedron impression / branded hand to the handprint" is offered while the door is closed and the party holds the item. The presenting body walks to the leaf; that one contact skips the feeder, releases the latch and opens the native door (with its sound). It does not disarm the feeder, remove hardness or collision, silence the Alarm, clear awareness, or grant anything else. A bare-handed press still meets the armed feeder.
- **Marked runner.** A restored marked reporter uses its own palm at A2 through the same contact action, then follows its saved route to Vandiana. Unmarked reporters are stopped by a latched A2 and turn on the nearest intruder.
- **Discovery note.** Having looked at a marked palm adds one sentence to a successful DC 22 inscription study linking the door's star to the palm. It unlocks nothing.

## WR-04-R1: other repairs and rulings

| Topic | Ruling |
|---|---|
| Physical actions | Every touch at a fixture (force, press, present, disarm, tamper with a rune, emerald, flail, impression, hand) is deferred until the dialogue closes and runs as a native standard-action command by the character who opened it: walk into reach, animate, then resolve against that same body. Cancelling it beforehand does nothing; the feeder strikes, and only that body can complete its Strength check if still alive, conscious and able. |
| Door and emerald hits | Hardness once per resolved hit (7 to 0, 12 to 4, 30 to 22; two 12s are 4 + 4). Adamantine bypasses the door's hardness 8, never the emerald's 20. Energy is halved before hardness (core object rule); negative and positive energy do not harm stone. Restrained feedback ("The blow leaves no more than a scratch."). Destruction removes the leaf, its target and its feeder, and stops attacks on it. |
| Break DCs | Door DC 28 and emerald DC 40 ignore the game's difficulty-preset skill DC adjustment. |
| Study presentation | Study rolls are native but silent (no battle-log total, no success/failure voice); the cue shows only earned text, or a neutral line when nothing new was learned. Numbers stay in the mod log. |
| Feeder temporary HP | Consumption of this source's own modifier is written to the saved grants, so spent points never return on refresh or load, and a fully spent grant earns no expiry acceleration. |
| Glyph | Resolved in Vandiana's divine CL 8 context: spell resistance (Lightning Bolt carries the SR eligibility, since the game has no Glyph of Warding), save bonuses against spells, then 4d8 electricity, Reflex 17 half, evasion. |
| Mist | Krune's summons get a typed +4 resistance modifier on the save itself, so native stacking keeps the larger of it and any existing resistance bonus. |
| Long work | Refused in combat, with anyone in the mist, near the active hound, or once the confrontation is running. A deadline inside the interval stops the work there with no reward. Completed work advances every clock at once. |
| Turn ends | Taken from the native `TurnController.Dispose`: a completed turn, the final turn as combat ends, a unit leaving combat, or a switch to real time. A delayed turn is not an end; a turn already exposed is not exposed twice. |
| Hound | Notices only Small or larger creatures in its own chamber with no wall between, never Vandiana's worshippers; invisibility does not hide anyone from it. It ends if Vandiana is ever more than 100 feet away. The bite remains a scripted +10 roll against native AC. |

## Files

`WardProfile` numbers · `KruneProjection` masks · `RuneRules`, `CurseRules`, `FeederRules`, `MistRules`, `RitualRules`, `SanctumState` state and rules · `WardGeometry` placements · `WardBlueprints`, `WardDialogs`, `WardText` content · `WardFixtures`, `WardObserver`, `WardRuntime`, `WardTraps`, `WardDoor`, `WardHazards`, `WardRitual`, `WardStatues`, `WardInteract` the native layer · `WardStore` the save-bound state on the main character's `SanctumWardPart`.

## Blueprints and sounds

| Purpose | Native reference |
|---|---|
| Door open / close | `DungeonStoneDoorOpen`, `DungeonDoorStone_Close` (`Main.bnk`) |
| Stone to Flesh | `StoneToFlesh` `e243740d…`, `ScrollOfStoneToFlesh` `394ba24a…` |
| +3 flail | `FlailPlus3` `bce3d017…` |
| Staggered / Fatigued / Sleeping donors | `df3950af…` / `e6f2fc5d…` / `5e0cd801…` |
| Aberration type | `3bec99ef…` |
| Cultist body | `CR2_BanditPositiveClericLevel3` `9ffceca1…` |
| Vandiana's buffs | Mage Armor `a92acdf1…`, False Life `0fdb3cca…`, Cat's Grace `f011d0ab…` |

## Migration

On a save that entered the empty sanctum, the door stays unlatched, the wards arm, occupants of a ward are recorded so standing still cannot trip it, the 18-hour and 9-hour lifetimes start at that first load, and a one-time warning recommends a pre-departure save. The mist begins on its next real tick.

## WR-04-R1 migration

- State version 2 adds credentials, per-runner route legs and the Krune-summon list; nothing recorded by version 1 is reset.
- A marked body restored by 0.4.0 gains its credential once on load; a marked body missing from an old save leaves one remnant hand target at its figure, recorded so it never repeats.
- Only the first initialization decides whether a save is legacy. 0.4.0 re-ran the legacy notice and alarm seeding on every reload inside the sanctum.
- 0.4.0 also opened A2 (and the east door) on every reload, because its doorway guard counted the invisible fixture standing in the doorway. Doors such an old save recorded as open stay open; their latch state is kept as saved.
- A runner from 0.4.0 restarts its route from the first leg.

## Not implemented

Knock/Stone Shape door bypass (no native spell); Krune's combat, Vandiana's lieutenants and all boss AI (deferred by the mission); the return portal; external scrying rewrite.
