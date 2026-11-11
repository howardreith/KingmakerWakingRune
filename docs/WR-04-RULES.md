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
| Long work | Refused in combat, with anyone in the mist, near the active hound, while any rune attempt is still being timed (R2), or once the confrontation is running. A deadline inside the interval stops the work there with no reward. Completed work advances every clock at once. |
| Turn ends | Taken from the native `TurnController.Dispose`: a completed turn, the final turn as combat ends, a unit leaving combat, or a switch to real time. A delayed turn is not an end; a turn already exposed is not exposed twice. Outside the sanctum the hook returns before touching any state (R2). |
| Hound | Notices only Small or larger creatures in its own chamber with no wall between, never Vandiana's worshippers; invisibility does not hide anyone from it. It ends if Vandiana is ever more than 100 feet away. The bite is a native attack roll (R2, below). |

## WR-04-R2: review repairs and rulings

| Topic | Ruling |
|---|---|
| Scenario state isolation | Reading state never creates it. `WardStore.Existing` uses `Get<SanctumWardPart>()`; only `WardRuntime.OnSanctumLoaded` (sanctum loaded) calls `WardStore.Create()`. The turn-end patch and `TurnEnded` check the area and loading before reading anything. |
| Sustained rune routes (refined in R3, below) | A status succeeds only on duration actually observed: each sustain records its last sighting with the status on, and success needs that sighting to be the full 5 or 10 rounds after the start. A status found gone after a gap counts only what was seen, so an unobserved lapse is a failure, never a success. Long work is refused while any focus has an open window, a sustain, a pending cast or expiring negative levels, so the gap cannot arise from work. A native rest during an attempt can still only under-count, never over-count. |
| Rune saving-throw exception | A plate is matched on the effect's descriptors or on what its action tree does (`RuneEffectScan`): negative-energy damage and energy drain for Life; cold for Alacrity; electricity, sonic and force for Defense; acid and fire for Armor. Harm (no descriptor) is denied its save against Life, never against another plate. Every denial is logged with the ability, descriptors and effects. |
| Mist sleep | A sleep that ends by any path (the mod's wake, a native rule, a refused application) is a real wake and resets the failure streak. The apply rule and every removal, with its call path, are logged. A sleeper rolls no further save. Under the `ExtraDecline` stats preset, the game itself caps every negative-descriptor effect on party members at three rounds and blocks a repeat of the same descriptor in one combat; the mist's sleep is a sleep effect and obeys that preset like any other. The mod does not override difficulty settings. |
| Cultist identity (GMG p.278) | The donor's `CustomizationPreset` is cleared: it picked a random race and body at every spawn, the source of R1's varying scores (with helpless or dead bodies reading Dex 1 natively). The printed Con 12 already includes the human bonus, which the native `HumanRace` would add again, so the blueprint sets 10. Printed hp 16 is held by a hidden saved fact, `AddStatBonus` HitPoints −7 (native NPC cleric dice 17 + Con 3 + the donor's Toughness 3 = 23), because the engine clamps a negative blueprint hp share. `DifficultyStatAdvancement` is removed from the unit and from any donor fact that carries it. The game's global `DifficultOpponent` feature, which it gives every enemy, is left alone: AC reads 16 at this save's difficulty rather than 18. Nothing overwrites statistics after spawn. |
| Hound bite | A native `RuleAttackRoll` with a native natural bite weapon. The hound is `Invisible` for that roll only (so it stays targetable once found), which gives the native +2 and flat-footed target against anyone who cannot see invisible, plus native concealment, mirror images, natural 1 and 20, and critical confirmation. The unconditional bonus is held at the printed +10. Damage is the printed 2d6+3 piercing, magic for damage reduction, doubled on a confirmed critical. |
| Feeder | A fully spent feeder buff is removed from Krune instead of remaining at zero. |

## WR-04-R3: completion-run rulings

| Topic | Ruling |
|---|---|
| Sustained rune routes (supersedes R2's wording) | The status is timed by the effect's own native lifecycle when the engine reports it (`RuneConditionClock`, an `IUnitBuffHandler` on the Alacrity plate only). The sustain starts when the effect was applied, dated as the engine dates a buff (turn start in turn-based combat), never later than the first sighting. When the status is found gone, it is credited up to the effect's removal, never past its scheduled end; an early removal (break-free, dispel) credits only up to the removal. Without that record (a reload between removal and the next sighting) only what an observation saw counts. Success needs the full 5 or 10 rounds; the only tolerance is 1 µs of floating-point rounding of engine ticks. |
| Rune plates and breaking free | A plate makes no break-free check (`UnitHelper.TryBreakFree`) against an effect its own routes name, as it rolls no save against one. Native Entangle otherwise had the Alacrity plate roll an escape every round (R3 run: free after 7 rounds). Scoped by `RuneVulnerability`, as the save exception is. |
| Area-ward disarming | The native disarm approach to a trap with a trigger zone stops where its next step would enter the zone. The alarm and glyph are polled areas, so the same rule is applied against their own areas (`WardDisarmApproachPatch`); the rogue works them from the edge instead of walking in. The feeder keeps the native reach. |
| Lodge rest | The Quartermaster offers "a night's rest" before departure: the native scripted party rest an inn gives (8 hours, spells and resources restored). Neither the lodge's donor area nor the sanctum allows camping, so prepared casters had no way to ready the spells their new levels brought. |
| Difficulty presets (owner ruling) | The scenario respects the chosen native preset. Under `ExtraDecline` the mist's sleep lasts three rounds and is reconciled as a wake when the engine removes it. Restored cultists carry the preset's `DifficultOpponent` AC adjustment (−2 under Decline, +2 under Challenging). No compensating patch is applied. |

## WR-04-R4: player-facing rulings

| Topic | Ruling |
|---|---|
| Reporters (owner ruling) | A restored reporter is hostile (`FactionMobs`), so enemy-only spells such as Hold Person can target it. Allegiance is separate from behaviour: its own AI is off from the moment it is restored (`IsBrainActive = false`), so it flees rather than fights. Combat does not stop the flight; being unable to move or act does, through the engine's own `CanMove`/`CanAct`. In turn-based combat it moves only in its own turn, along a precomputed path, within that turn's movement. A charmed, dominated, confused or panicked reporter is left to whoever controls it and its AI returns. The messenger warning is raised only when an able reporter stands in Vandiana's chamber. |
| Summon orders | Every active rune plate offers "Direct my summoned creatures to attack this rune" whenever the speaker has living summons of their own. It gives each of them a native attack command, renewed after each round of attacks and at each of their turn-based turns, until the rune goes dark, the plate is gone, the summon ends or the player chooses "Call the summoned creatures off this rune". Nothing else changes: summons are not made selectable, and they walk, attack, meet hardness and expire natively. |
| Departure readiness | Using the token now opens a departure step in the lodge: a notice that leaving includes a night's rest (effects already cast will run out; scrolls, potions and wand charges are not restored), the native spellbook for review, a warning listing any empty prepared slots (not filled for the player), then the native scripted party rest, and only then the checkpoint and transfer. Cancelling at any point spends nothing. A retry after a failed checkpoint or load departs without a second rest; a save load or a completed arrival clears that record. Nothing in the sanctum rests: a sanctum load never reaches this code. The Quartermaster's manual rest remains. |
| Quartermaster stock | Adds every exotic weapon family the installed game supports and the shop lacked (fauchard, sai, dwarven waraxe, estoc, falcata, tongi, orc double axe, two-bladed sword, dwarven urgrosh, gnome hooked hammer, sling staff; +1 to +3), the missing +3 kama and nunchaku, six Stone to Flesh scrolls and a second +3 dagger. Existing lodges take the change through the native shared-vendor reconciliation, which applies only the difference between the saved catalogue counts and the new ones. Purchases, sales and money are kept, and nothing is refilled on rest or reload. |
| Inspection | A plain click on authored scenery examines it, and its cursor is now the native magnifying glass (`InfoCursor`). An aimed spell or ability, combat, and turn-based combat keep the native cursor and targeting. Restored cultists are ordinary units. The deeper rune reading tells the methods as discoveries and replaces the identification. Damage totals, DCs and hardness stay in the rules, diagnostics and acceptance documents. |
| Ability damage on fixtures | A fixture's ability damage is recorded for its rune (the Life plate's poison and disease route), but the object's own score is put back at once. The engine kills any creature whose score falls below 1, and a plate has an object's Dexterity of 1, so one point of a summon's poison killed the Life plate and left its rune unbreakable (found in the R4 run, fixed in 0.5.5). |

## WR-04-R5: corrections

These supersede the R4 rows they name.

| Topic | Ruling |
|---|---|
| Legacy reporters | Migration runs before the control test. A reporter is migrated once if it stands in the neutral faction, is not in the player faction, holds no `ChangeFaction` effect, and is not yet marked `HostileSettled`. It is switched to the hostile faction and its group's cached factions are rebuilt. A reporter held by a charm, domination or other faction effect is left alone until the effect hands its faction back. New restorations are settled at spawn. |
| Departure retries (supersedes R4) | The rest is remembered as each member's state when it finished: prepared slots and their readiness, spontaneous slots, caster levels, ability resources, hit point and ability damage. A retry after a failed checkpoint departs without resting only if the same members match that record. A member who joined or left, a slot prepared, changed or spent, a resource used or a wound taken sends the party back through notice, review, warning and rest, and the notice says what changed. A load or a committed arrival clears the record. A rest soon after another lasts as long as the game's own minimum rest time requires (24 hours in the R5 run). |
| Summon orders (supersedes R4) | Orders are saved as summon, summoner and rune identities. On load each one is checked (rune lit, plate present, summon alive, linked to that summoner, not hostile to them, under no faction effect, not confused or panicked) and then resumed or removed. In turn-based combat a resumed order waits for the summon's own turn. Removing an order also stops that summon's attack in hand on the plate and clears the manual target. Only an attack that ran its course is renewed; an interrupted one is not, and a failed one ends the order. The same checks run on every renewal and on faction or control changes. A summon's duration, attacks and the rune's damage window are never restored or extended. |
| Blocked runners in turn-based combat | A runner whose turn move ends without getting anywhere ends its turn. Bodies in its path can intercept it; it is never moved through them, and the round is never held open. |

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

## WR-04-R2 migration

- State stays version 2. The only new saved field, `RuneSustain.LastSeenActiveAt`, defaults to −1 in older saves; such a sustain counts from its next sighting, so an attempt in progress at the upgrade can only under-count.
- An ordinary campaign that already carries a `SanctumWardPart` from a 0.4.1 turn-based fight keeps it, unread: nothing reads it outside the sanctum. A read-only scan of the owner's 290 saves found the part only in a sanctum save (four saves in an older format could not be opened).
- Cultists restored under 0.4.0 or 0.4.1 keep the race, scores and hit points they spawned with; only new restorations use the printed block.

## WR-04-R3 migration

- State stays version 2; nothing new is saved. A sustain already in progress keeps its saved start; the native application time is used only for effects applied after the update.

## WR-04-R4 migration

- State stays version 2. A reporter that an older save restored as a neutral switches to the hostile faction once, the first time its flight is ticked; a charmed or dominated one is left to its controller. Its AI is switched off with the same tick.
- Quartermaster stock reconciles natively on the first visit after the update (see the ruling above): +1 +3 dagger, six Stone to Flesh scrolls, the new exotic lines; nothing else changes.
- `WardFixtures.Protect` restores a fixture found dead. In the R4 run, the save made while the Life plate was dead loaded under 0.5.5 with the plate whole and its rune active, but the restore's log line did not appear, so what revived it on that load is unconfirmed. Summon orders are not saved: after a reload the summons follow their own AI until the order is given again.

## WR-04-R5 migration

- **State version 3.** It adds the saved summon orders (empty in older saves) and each statue's `HostileSettled` (false in older saves). Nothing recorded earlier is reset.
- **Reporters.**
  - The R4 note above is corrected: R4's migration was never reached, because the control test returned first for any neutral reporter.
  - Under 0.5.7 and later, a neutral reporter from an R1 to R3 save becomes hostile on its first tick and resumes its saved leg.
  - A reporter restored by R4 (already hostile) is simply marked settled.
- **Summon orders.** The R4 note that summon orders are not saved no longer holds. Orders given under 0.5.5 or earlier were never saved, so those summons keep following their own AI until ordered again.
- **Departure.** The readiness record is in memory only and is cleared by any load, so it needs no migration.

## Not implemented

Knock/Stone Shape door bypass (no native spell); Krune's combat, Vandiana's lieutenants and all boss AI (deferred by the mission); the return portal; external scrying rewrite.
