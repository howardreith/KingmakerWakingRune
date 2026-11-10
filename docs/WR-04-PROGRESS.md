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
