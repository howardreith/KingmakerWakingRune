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
| Fixture saves | A fixture never passes a saving throw. | The printed "no saving throw to resist effects to which they are vulnerable", delivered by one postfix scoped to fixtures. |
| Stone to Flesh | A fixture-only postfix allows `AbilityTargetStoneToFlesh`; the DC 15 Fortitude is rolled by the restored unit. | The stock check only accepts petrified party members and its effect does nothing on scenery. |
| Bearer cultists | Reused native body: `CR2_BanditPositiveClericLevel3`, a human level 3 cleric. Attackers use the native Mobs faction; reporters are neutral runners. | Closest native match to the GameMastery Guide cultist. Stats differ from the printed block. |
| Hound | A scripted bite through native attack, armor class and damage rules, against native targets, once per round. | No native Mage's Faithful Hound exists. |
| Alarm and Glyph triggers | Polled against real positions, with entry memory saved, using native trap views for discovery and disarm only. | Native script zones lose their views after a reload. |
| Alarm range | Radius 20 feet around (13.15, 22.6), and it reaches the A2 door. | Measured against the plan; arrival stays outside. |
| A2 door credential | `KWR_SihedronBrand` is recognised but nothing in the game grants it. | No native Sihedron exists; the door has no free bypass. The printed Knock/Stone Shape bypass has no native spell to support it. |
| Vandiana's Darkvision | Stored as an intent. | No native Darkvision buff exists. |
| Cat's Grace duration | Three minutes. | A potion's caster level 3; duration is not printed. |
| Melee on the door | A melee weapon hit counts as contact; a spell or ranged hit does not. | Package ruling 6. |

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

## Not implemented

Knock/Stone Shape door bypass (no native spell); Krune's combat, Vandiana's lieutenants and all boss AI (deferred by the mission); the return portal; external scrying rewrite.
