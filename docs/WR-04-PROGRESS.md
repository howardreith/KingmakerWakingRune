# WR-04 progress record

Branch `feature/wr-04-sanctum-wards` from accepted `main` (`c503c0e`, the merge of PR #3). Package SHA-256 `CF4BBC32…F04264A8`, extracted to `analysis\wr04-package`; 18 listed files verified. Recon dumps are in `analysis\wr04-recon`.

## Findings that cost time; do not rediscover

### Time
- `Player.GameTime` is the clock: saved, and it stops in pause, dialogue, full-screen UI and loading.
- In turn-based combat `CombatController.TickTime` rewrites it from `m_StartTime` plus elapsed time between turns, so `AdvanceGameTime` during turn-based combat is undone. Long work actions are refused in combat.
- Turn-based: no turn-ended event. Use `ITurnBasedModeHandler.HandleTurnStarted`, which closes the previous unit's turn. `IUnitNewCombatRoundHandler` does not fire for idle units in real time, so it is not a six-second clock.
- `TimeController.Tick` runs only in simulation modes, which makes a postfix the one per-tick hook needed.

### Targets
- A fixture is a real unit (Neutral faction, `State.IsIgnoredByCombat`, `UnitAggroFilter` with empty conditions, no loot).
- `CanAttack` is true for any Neutral faction, so left-click attacks and native dialog-on-click is unreachable. Rune clicks need a fixture-only route through `ClickUnitHandler`.
- A non-Neutral mod faction with no enemies gets the native click interaction but cannot be chosen by hostile spells. That suits statues, because Stone to Flesh has `CanTargetFriends`.
- A hit raises an offensive action, and `UnitMemoryController` adds the attacker to the target's enemies unless a `UnitAggroFilter` says otherwise. Without the filter, fixtures turn hostile.
- `AbilityTargetStoneToFlesh` restricts the stock spell to petrified party members. Native effect does nothing on fixtures, so the scenario adapter patches that one check and rolls the DC 15 Fortitude itself.
- `RuleSavingThrow` has `AutoPass` but no auto-fail; a natural 20 still saves. Fixture saves need a scoped override, not a stat penalty.
- `HoldPerson` carries an `AbilityTargetHasFact` naming type facts including `AberrationType`, which is what makes an Aberration-typed Alacrity focus reject it while `HoldMonster` works.

### Traps
- A runtime-spawned `ScriptZone` has no view after reload. Build zone and trap views in `LodgeScope.Filter`, as the doors are, so native persistence matches them by `UniqueId`.
- `TrapObjectView.Interact` triggers on `RollResult < DisableDC - DisableTriggerMargin`: missing by 6+ springs a trap, missing by exactly 5 does not. There is no native magic-trap concept: eligibility is `SkillThievery.BaseValue > 0`.

### Saves
- No type allowlist exists, but persistence members need `[JsonProperty]`, and `DictionaryConverter` silently drops an entry it cannot resolve. A mod `UnitPart` must be proven by a save round trip.
- `UnitPart` has `PreSave` / `PostLoad`; serialize the state in `PreSave`.

### Content
- No Sihedron or Lissala feature, deity or item exists in the game, so the printed door credential has nothing native to recognize.
- `ScrollOfStoneToFlesh` (`394ba24a…`) and `FlailPlus3` (`bce3d017…`) exist. `GiantSpiderSummoned` carries `HuntingSpiderPoisonFeature`, the candidate real summoned poison.
- Door audio: `DungeonStoneDoorOpen` and `DungeonDoorStone_Close` in `Main.bnk` (exact case from `SoundbanksInfo.xml`).
- `Sleeping` (`BuffSleeping`) wakes on leaving combat and on a Perception DC 5, which the mist's sleep must not do.

## Tooling
`KwrRuntime.ps1 Collect` failed when the UMM log had not grown, losing the recon folder when `End` removed the mod; fixed.

## More findings from native runs
- Native `AddBuff(blueprint, caster, duration, abilityParams)` ignores its parameters; set caster level and DC on the buff's `MechanicsContext.m_Params`.
- A load must go through `MainMenu.LoadGame` from the menu, which wraps the loader in `EnterGame`; a direct `Game.LoadGame` throws "Destination scene is not valid".
- A fact-granted ability reports a spell level of 0 or 1; use `AbilityData.OverrideSpellLevel` for harness casts and `Math.Max` of the context and spell level in the observer.
- Global `JsonConvert` settings merge into `JsonSerializerSettings` passed to `SerializeObject`; build a `JsonSerializer` directly for mod state.

## Checkpoints
All six delivery checkpoints have code. Native proof is partial: see `ACCEPTANCE-WR-04.md`. Commits: `a0a59d6`, `4f55798`, `87e6277`, `4a109d4`.

## WR-04-R1 continuation (2026-11-09/10)

Checkpoint record, in order:

1. `86fa288` A2 credential route, same-actor commands, hardness pipeline and integration repairs. Run `wr04r1-a`: solo rogue PASS through normal input. The run also found the legacy re-initialisation on reload, the difficulty-adjusted break DC, and that the party cannot damage the peaceful Krune proxy.
2. `c6d2f08` legacy decided once, bodies step out on the floor, printed break DCs. Run `wr04r1-b`: corpse hand, hand presentation, glyph SR, hound, mist sleep anomaly.
3. `198b66c` cultist difficulty component removed (not the cause of the low scores), mist diagnostics.
4. `eeab9d0` doorway guards ignore fixtures, unmarked runners blocked. Run `wr04r1-d`: marked reporter route, mid-route reload, living attacker.

Findings worth keeping:
- `GameHelper.IsAttackingGreenNPC` silently drops party damage to peaceful-faction units: the Krune, statue and ward proxies cannot be hurt by the party at all.
- `RuleSkillCheck` adds the difficulty preset's `SkillCheckDCBonus` to every DC unless `IgnoreDifficultyBonusToDC` is set.
- `RuleSkillCheck.Silent` keeps a check out of the battle log and the voiced success/failure barks.
- Kingmaker items have no unique id; credential provenance keys on kind and source slot.
- Every turn-based turn ends through `TurnController.Dispose` (completed, combat end, leave combat, delay); `Status == Ended` tells a completed turn from a delay.
- Dice repeat after a reload only partly; restoring other figures or rolling studies first changes later outcomes.
- The game's quicksave rotates into existing owner quick slots; the guarded profile restore covers it.

## WR-04-R2 review repairs (2026-11-10)

Commits `52a76c2` … `44a4b78`, one per finding, then docs. Run `wr04r2-a` (builds `923f18e`, `b1f9e45`, `44a4b78`); results in `docs/ACCEPTANCE-WR-04-R2.md`.

Findings worth keeping:
- `UnitDescriptor.Ensure<T>` creates; `Get<T>` reads. Any state accessor reachable from a global hook must use `Get`.
- `RuleApplyBuff` under stats preset `ExtraDecline`: a negative-descriptor buff on a player-faction unit lasts at most three rounds, and a repeat of the same descriptor in one combat is refused (`StoryModeImmunity`). The owner's saves use this preset.
- `RuleDealDamage.ApplyDifficulty` under `ExtraDecline` strips immunity, halving and reduction from damage to the party's enemies.
- A cloned `BlueprintUnit` keeps the donor's `CustomizationPreset`; `SpawnUnit` then picks a random variation (race and prefab) every time.
- The native `HumanRace` adds +2 to Constitution on an NPC; printed NPC scores already include it.
- NPC class hit points are fixed averages (`ApplyClassMechanics`: hit die / 2 + level % 2, + 1 for a base class); a negative `BlueprintUnit.MaxHP` is clamped, so an hp offset needs a fact (`AddStatBonus` on `HitPoints`).
- The engine gives every enemy `DifficultyAdvancementBaseFeature` and `DifficultOpponent`; under this save's preset that is AC −2.
- A helpless or dead unit reads Dex 1 through the native `Helpless` modifier; probe a living body.
- The engine spends one temporary-hp modifier after another, in place, and the cached total can lag by a tick.
- Faithful hound: a native `RuleAttackRoll` with `UnitCondition.Invisible` on the attacker gives +2 (`ConcealmentBonus`) and a flat-footed target natively.
- Harness: a unit with a stuck move order overrides an injected position until it gets a new order; the in-game menu pauses everything. Turn-based mode is a game option (Options > Game > Turn-based mode); the hourglass is pause.
- `Harm` has no spell descriptor; its negative energy is only visible in the action tree.

## WR-04-R3 completion run (2026-11-10)

Commits `cb59cb5` … `94c820d`, then docs. Run `wr04r3-a`; results in `docs/ACCEPTANCE-WR-04-R3.md`.

Findings worth keeping:
- Native class **premade builds** fill every level-up: at character creation, choose the class and press "Load premade build" (it turns on auto level-up, which relabels the difficulty "Custom" without changing any combat setting). A Coordinator recruit at the target XP then levels 1 → 11 with one Complete click per level. The six-character seed takes about 25 minutes.
- Neither the lodge nor the sanctum allows camping, so a freshly levelled party cannot ready its slots without the lodge rest (Quartermaster, 0.4.4). Prepare before resting: `Spellbook.Memorize` leaves a slot unavailable until a rest.
- The native disarm approach ignores `ProximityRadius` for traps (`UnitInteractWithObject` uses 0.3 m) and stops at the edge of the trap's own `ScriptZoneTrigger` through `UpdateTarget`. Polled wards need the same check against their own areas. Match the view by `UniqueId`: the scope filter can build a view more than once.
- Native Entangle makes its target roll `UnitHelper.TryBreakFree` every round; a fixture denied its saves still escaped until 0.4.8.
- Reporters are native Neutrals: enemy-only single-target spells report `canTarget=False` against them, and they outrun melee.
- A spell-bar variant row (Summon Nature's Ally) and the spellbook popup take the level tab at x≈1118; Esc opens the game menu (which pauses), and right-click does not cancel a variant targeting cursor.
- A save copied into the profile while the game runs is not seen by `load` until the game restarts.
- `spawnfoe` places the unit "ahead" of the first party member; facing a wall puts it out of reach. Several goblins at 3 m start a turn-based fight reliably; the end-turn button is the large round button at window (552, 1352).
- Harness `condition <fixture> <buff> <seconds>` / `uncondition <fixture> <buff>` apply or remove a native buff of exact length for boundary tests.

## WR-04-R4 player-facing gaps (2026-11-10/11)

Commits `4d7b72b` … `5e05519`, then docs. Run `wr04r4-a`; results in `docs/ACCEPTANCE-WR-04-R4.md`.

Findings worth keeping:
- The native shared vendor table already migrates an existing save. `SharedVendorTables.GetTable` compares the saved `KnownItems` with the blueprint's current fixed items and applies only the difference. Adding stock to the CSV needs no migration code. Harness `stock` shows saved, reconciled and live counts.
- Screenshots do not capture the mouse cursor. Harness `cursor` reads the texture the native `PCCursor` is drawing. The magnifying glass is `BlueprintRoot.Cursors.InfoCursor`, chosen in `CursorController.SetUnitCursor`.
- A unit spawned hostile acts in its first moments. Turn its AI off (`IsBrainActive = false`) at the spawn, not on the first ward tick, and drop commands with an `AiAction`.
- In turn-based combat the engine wipes a unit's commands at its turn start, and an idle AI turn ends after 0.2 s. A scripted runner must act from `ITurnBasedModeHandler.HandleTurnStarted` and on each command end, not on the 0.25 s ward tick. A move there needs a `ForcedPath`. The party standing on the stair blocks the runner's path.
- `RuleDealStatDamage` marks any unit for death when an ability score falls below 1. Fixtures have Dexterity 1, so ability damage on them must be put back (0.5.5).
- The turn-based toggle is the rook icon beside the hourglass, at window (426, 1340). The native turn-based tutorial appears on first use; close it at (2020, 192).
- Spellbook memorization is a drag from the spell card to a slot; clicks do not memorize. In the Quartermaster shop, filter and scroll with the bar's drag. A click moves one item into the offer, and "Deal" is at (1272, 1202).
- Stone to Flesh restorations are native survival saves, and in this run they failed often (Fortitude 6 or 13 against 15). Script the reload-and-restore when a living reporter is needed.
