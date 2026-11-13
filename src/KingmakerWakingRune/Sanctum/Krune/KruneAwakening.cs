using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.Blueprints.Items;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.Visual.Particles;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's awakening at play. The ritual's existing clocks decide when; this commits the awakening once, makes the one
    /// real actor from the sanctum's saved state, hands the feeder protection over from the dormant receiver, shows his
    /// emergence and opening, and stops at the labelled prototype checkpoint. Every stage is saved before the next begins.
    /// </summary>
    internal static class KruneAwakening
    {
        private const string GroupId = "KWR-A5-Krune";
        private const double RetrySeconds = 5d;

        private static UnitEntityData s_Bound;
        private static double s_NextAttempt;
        private static bool s_LegacyAsked;
        private static bool s_ProblemReported;

        private static SanctumState State => WardRuntime.State;

        public static void Note(string text)
        {
            Log.Info($"KRUNE [{WardRuntime.Now:0.0}]: {text}");
        }

        /// <summary>The real actor once its identity is committed, living or dead; null before. Never the receiver.</summary>
        public static UnitEntityData Actor
        {
            get
            {
                KruneState krune = State?.Krune;
                if (krune == null || string.IsNullOrEmpty(krune.EntityId))
                {
                    return null;
                }
                if (s_Bound == null || s_Bound.UniqueId != krune.EntityId || s_Bound.Descriptor == null)
                {
                    // The pool by identity: its enumeration skips anyone out of the game, as he is until he steps out.
                    s_Bound = Game.Instance.State.Units[krune.EntityId];
                }
                return s_Bound;
            }
        }

        /// <summary>Whether the real actor is authoritative: from the committed identity on, the receiver never is again.</summary>
        public static bool Authoritative
        {
            get
            {
                KruneState krune = State?.Krune;
                return krune != null && !string.IsNullOrEmpty(krune.EntityId);
            }
        }

        public static bool IsKrune(UnitEntityData unit)
        {
            return unit != null && unit.Descriptor.Get<KrunePart>() != null;
        }

        public static int CasterLevelLoss()
        {
            SanctumState state = State;
            return state == null ? 0 : KruneRules.RevivalCasterLevelLoss(state.Krune, WardRuntime.Now);
        }

        public static KruneSheet LinkedSheet()
        {
            SanctumState state = State;
            return state == null ? KruneRules.Compose(new KruneState(), 0, 0d) : KruneRules.Compose(state.Krune, state.RuneMask, WardRuntime.Now);
        }

        /// <summary>The prototype boundary is in force for the loaded save (read by the pause guard).</summary>
        public static bool CheckpointHolds
        {
            get
            {
                SanctumState state = SanctumScope.IsSanctumLoaded ? WardStore.Existing : null;
                return state != null && KruneRules.CheckpointHolds(state.Krune);
            }
        }

        public static void AfterLoad(SanctumState state)
        {
            s_Bound = null;
            s_NextAttempt = 0d;
            s_LegacyAsked = false;
            KruneState krune = state.Krune;
            if (krune.ParleyStarted && !krune.ParleyFinished)
            {
                // The game cannot save inside a dialogue, so this is an interrupted exchange: it is never replayed.
                krune.ParleyFinished = true;
                KruneRules.TurnHostile(krune, HostilityCause.ParleyEnded, "the exchange was interrupted by a load", WardRuntime.Now);
            }
            UnitEntityData actor = Actor;
            if (Authoritative)
            {
                RetireReceiver(state);
                if (actor == null)
                {
                    // A missing view or index entry is never permission to recreate him: his identity stays authoritative.
                    Note($"the committed actor {krune.EntityId} is not in this area's state; nothing is recreated.");
                }
                else if (!actor.Descriptor.State.IsDead && krune.Stage >= AwakeningStage.Emerged && !actor.IsInGame)
                {
                    actor.IsInGame = true;
                }
            }
            SyncLinks(state, WardRuntime.Now);
            Note($"after load: stage {krune.Stage}, cause {krune.Cause}, actor {(actor == null ? "none" : actor.UniqueId + (actor.Descriptor.State.IsDead ? " (dead)" : $" hp {actor.Descriptor.HPLeft}/{actor.Descriptor.MaxHP}"))}, revival mask {krune.RevivalMask}, current mask {state.RuneMask}, checkpoint {krune.CheckpointReached}{(krune.DiagnosticRelease ? " (released for diagnostics)" : string.Empty)}, legacy {krune.Legacy}.");
            KruneCombat.EnsureBrain(actor);
            if (krune.CheckpointReached && !krune.PrototypeRetired)
            {
                RetirePrototype(state);
            }
        }

        /// <summary>
        /// A save made at the WR-07A development checkpoint: the full encounter replaces the stop, once. Nothing it recorded is
        /// reset: damage, the feeder's depletion, steps spent, removed buffs, the runes, elapsed time and the stagger all stand,
        /// the party is not rested, and the entrance and exchange are not replayed. Only the prototype's own hold ends (it was
        /// never saved: the pause guard and the turn-based freeze lived only in that session). A Krune killed during a labelled
        /// diagnostic release stays dead and is marked as such; it is not a completed adventure.
        /// </summary>
        private static void RetirePrototype(SanctumState state)
        {
            KruneState krune = state.Krune;
            krune.PrototypeRetired = true;
            if (krune.CombatStartedAt < 0d && krune.Stage == AwakeningStage.CombatReady)
            {
                krune.CombatStartedAt = krune.CheckpointAt >= 0d ? krune.CheckpointAt : WardRuntime.Now;
            }
            UnitEntityData actor = Actor;
            bool dead = actor != null && actor.Descriptor.State.IsDead;
            Note($"WR-07A checkpoint save brought into the full encounter once (diagnostic release {krune.DiagnosticRelease}, Krune {(dead ? "dead" : "alive")}); nothing reset.");
            if (dead)
            {
                return;
            }
            const string text = "This save was made at the development checkpoint of an earlier build.\n\nKrune's full encounter is now available: the fight continues from exactly this moment. Nothing has been reset or healed.";
            EventBus.RaiseEvent(delegate(IDialogMessageBoxUIHandler h) { h.HandleOpen(text, DialogMessageBoxBase.BoxType.Message, button => { }); });
        }

        /// <summary>SETUP (labelled test harness only): pause the game the moment the lifecycle reaches this stage, for a save there.</summary>
        public static AwakeningStage? HarnessPauseAt;

        public static void Tick(SanctumState state, double now)
        {
            AwakeningStage before = state.Krune.Stage;
            Advance(state, now);
            if (HarnessPauseAt.HasValue && state.Krune.Stage != before && state.Krune.Stage == HarnessPauseAt.Value)
            {
                HarnessPauseAt = null;
                Game.Instance.IsPaused = true;
                Note($"SETUP harness pause at stage {state.Krune.Stage} (for a save at this point).");
            }
        }

        private static void Advance(SanctumState state, double now)
        {
            KruneState krune = state.Krune;
            if (krune.Legacy == LegacyAwakening.AwaitingChoice)
            {
                AskLegacy(state);
                return;
            }
            if (!KruneRules.MayAdvance(krune))
            {
                if (!krune.LegacyNoticeShown)
                {
                    krune.LegacyNoticeShown = true;
                    WardRuntime.Say("This save is kept as a legacy exploration save: Krune's awakening was recorded by an older build and is not added here.");
                }
                return;
            }
            switch (krune.Stage)
            {
                case AwakeningStage.Dormant:
                    if (state.Ritual.AwakeningDue && KruneRules.Commit(krune, state.Ritual, state.RuneMask, now))
                    {
                        OnCommitted(state, now);
                    }
                    break;
                case AwakeningStage.Committed:
                    if (now >= s_NextAttempt)
                    {
                        s_NextAttempt = now + RetrySeconds;
                        Initialize(state, now);
                    }
                    break;
                case AwakeningStage.ActorReady:
                    if (now >= krune.ActorCreatedAt + KruneRules.EmergenceDelaySeconds)
                    {
                        Emerge(state, now);
                        // The opening is decided in the same tick he steps out: turn-based combat would otherwise give him a
                        // turn before it (0.8.11). A dialogue still opens; a bark or an unobserved waking hands off and holds
                        // at once. Only the labelled harness pause at Emerged keeps the moment apart, for its tests.
                        if (krune.Stage == AwakeningStage.Emerged && HarnessPauseAt != AwakeningStage.Emerged)
                        {
                            Opening(state, now);
                        }
                    }
                    break;
                case AwakeningStage.Emerged:
                    Opening(state, now);
                    break;
                case AwakeningStage.OpeningResolved:
                    HandOff(state, now);
                    break;
            }
            if (krune.Stage >= AwakeningStage.Emerged)
            {
                KruneCombat.EnsureInitialized(state, Actor);
            }
            if (krune.Stage == AwakeningStage.CombatReady && krune.CombatStartedAt < 0d)
            {
                // The handoff: he is hostile and the fight is his director's from here. No hold, no pause, no extra turn.
                krune.CombatStartedAt = now;
                Note($"COMBAT-READY HANDOFF at {now:0}: the encounter begins. {Sheet(Actor)}");
            }
            HoldStaggerForTurn(state);
            SyncLinks(state, now);
        }

        private static void OnCommitted(SanctumState state, double now)
        {
            KruneState krune = state.Krune;
            Note($"AWAKENING COMMITTED: cause {krune.Cause} at {krune.EffectiveAt:0} (committed {now:0}); revival mask {krune.RevivalMask}{(krune.RevivalMaskFromMigration ? " (the saved mask at migration)" : string.Empty)}, caster level {KruneProjection.BaseCasterLevel - KruneRules.RevivalCasterLevelLoss(krune, now)} for 24 hours; opening stagger {krune.OpeningStaggerOwed}.");
            SoulReturnCue(state);
            StudyRevival(state, now);
        }

        /// <summary>
        /// Builds the one real actor, or adopts the partial one an interrupted attempt left, then hands the feeder ledger
        /// over and retires the receiver. A failure leaves the awakening committed with a diagnostic, to be retried.
        /// </summary>
        private static void Initialize(SanctumState state, double now)
        {
            KruneState krune = state.Krune;
            if (!KruneBlueprints.Ready)
            {
                Fail(krune, "Krune's blueprint is unavailable: " + KruneBlueprints.Problem);
                return;
            }
            try
            {
                krune.InitializationAttempts++;
                // 1-2. The receiver's real consumption into the ledger, and elapsed grants expired at their own times.
                WardKrune.Sync(state, now);
                FeederRules.Expire(state.Door.Feeder, now);
                // 3. One actor: a partial one from an interrupted attempt is adopted, never duplicated.
                UnitEntityData actor = Game.Instance.State.Units.All.FirstOrDefault(IsKrune);
                if (actor == null)
                {
                    actor = Spawn(state, out string refusal);
                    if (actor == null)
                    {
                        Fail(krune, refusal);
                        return;
                    }
                }
                else
                {
                    Note($"attempt {krune.InitializationAttempts}: adopting the partial actor {actor.UniqueId} from an earlier attempt.");
                }
                // 4-5. Identity committed, then the remaining protection follows the ledger to him with its original expiry.
                int amount = KruneRules.FeederHandoff(state.Door.Feeder, now, out double expiresAt);
                krune.EntityId = actor.UniqueId;
                s_Bound = actor;
                WardKrune.Refresh(state, now);
                krune.FeederHandedOff = true;
                krune.FeederHandedOffAmount = amount;
                // 6. The receiver leaves the game without its cleanup counting as consumption or a new grant.
                RetireReceiver(state);
                krune.ActorCreatedAt = now;
                krune.LastFailure = string.Empty;
                KruneRules.Advance(krune, AwakeningStage.Committed, AwakeningStage.ActorReady);
                Note($"ACTOR READY: {actor.UniqueId}; feeder protection handed over: {amount}{(amount > 0 ? $" until {expiresAt:0}" : string.Empty)}; receiver {krune.ReceiverId} retired {krune.ReceiverRetired}. Built from his blueprint (class levels, gear, feats); the sanctum baseline applies as he steps into the game.");
            }
            catch (Exception ex)
            {
                Fail(krune, ex.GetType().Name + ": " + ex.Message);
                Log.Error("Krune's initialization failed; the awakening stays committed and will be retried.", ex);
            }
        }

        private static void Fail(KruneState krune, string reason)
        {
            if (krune.LastFailure != reason || !s_ProblemReported)
            {
                Note($"AWAKENING PENDING (attempt {krune.InitializationAttempts}): {reason}. Nothing was consumed; it will be retried.");
                s_ProblemReported = true;
            }
            krune.LastFailure = reason;
        }

        private static UnitEntityData Spawn(SanctumState state, out string refusal)
        {
            refusal = null;
            List<Occupant> occupied = Occupants();
            GridPoint? chosen = KruneRules.ChoosePlacement(KruneRules.EmergenceCandidates(), occupied, Floor);
            if (chosen == null)
            {
                refusal = "no clear A5 floor for his emergence";
                return null;
            }
            GridPoint p = chosen.Value;
            Vector3 at = ObstacleAnalyzer.GetNearestNode(World(p)).clampedPosition;
            // Facing the altar and the east door, as someone stepping out to see who has come.
            UnitEntityData actor = Game.Instance.EntityCreator.SpawnUnit(KruneBlueprints.Unit, at, Quaternion.Euler(0f, 90f, 0f), Game.Instance.State.LoadedAreaState.MainState);
            actor.Descriptor.Ensure<KrunePart>();
            actor.GroupId = GroupId;
            // Hidden until he steps out: the soul has returned, the body is still in the coffin.
            actor.IsInGame = false;
            state.Krune.EmergedX = p.X;
            state.Krune.EmergedY = p.Y;
            Note($"actor created at ({p.X:0.00},{p.Y:0.00}) as {actor.UniqueId}, out of the game until he emerges; {occupied.Count} creature(s) kept clear.");
            return actor;
        }

        /// <summary>
        /// Once: his day's resources, the printed hit points, the rune links, the spell he cast before hibernating and his
        /// permanent spells. Each step is skipped if already present, so an interrupted attempt completes without doubling.
        /// </summary>
        private static void ApplyBaseline(SanctumState state, UnitEntityData actor, double now)
        {
            KruneState krune = state.Krune;
            if (krune.BaselineApplied)
            {
                return;
            }
            UnitDescriptor d = actor.Descriptor;
            foreach (BlueprintScriptableObject resource in ResourcesOf(d))
            {
                d.Resources.Restore(resource);
            }
            if (d.Buffs.GetBuff(KruneBlueprints.Calibration) == null)
            {
                int native = WithoutPreset(actor);
                Lashmistress.KwrA4Calibration.Pending = new Dictionary<StatType, int> { { StatType.HitPoints, KruneRules.BaseHitPointsWithoutLife - native } };
                d.AddBuff(KruneBlueprints.Calibration, actor);
                Note($"hit points calibrated once: native {native} (without the preset), printed {KruneRules.BaseHitPointsWithoutLife} without his Runes of Life.");
            }
            if (d.Buffs.GetBuff(KruneBlueprints.RunesOfPower) == null)
            {
                KwrKruneRunesOfPower.PendingMask = state.RuneMask;
                d.AddBuff(KruneBlueprints.RunesOfPower, actor);
            }
            if (d.Buffs.GetBuff(KruneBlueprints.MageArmor) == null)
            {
                // Cast before hibernation and sustained by the sanctum (page 17): from his waking it runs its own course.
                double seconds = KruneProjection.BaseCasterLevel * WardProfile.HourSeconds - Math.Max(0d, now - krune.EffectiveAt);
                if (seconds > 0d)
                {
                    Buff armor = d.AddBuff(KruneBlueprints.MageArmor, SpellContext(actor, KruneBlueprints.MageArmorSpell, KruneProjection.BaseCasterLevel, 1), TimeSpan.FromSeconds(seconds));
                    if (armor != null)
                    {
                        armor.IsFromSpell = true;
                    }
                }
            }
            if (d.Buffs.GetBuff(KruneBlueprints.PermanentDarkvision) == null)
            {
                Buff darkvision = d.AddBuff(KruneBlueprints.PermanentDarkvision, SpellContext(actor, null, KruneProjection.BaseCasterLevel, 2), null);
                if (darkvision != null)
                {
                    darkvision.IsFromSpell = true;
                }
            }
            if (KruneBlueprints.PermanentSeeInvisibility != null && d.Buffs.GetBuff(KruneBlueprints.PermanentSeeInvisibility) == null)
            {
                Buff see = d.AddBuff(KruneBlueprints.PermanentSeeInvisibility, SpellContext(actor, null, KruneProjection.BaseCasterLevel, 2), null);
                if (see != null)
                {
                    see.IsFromSpell = true;
                }
            }
            AddOnce(d, KruneBlueprints.Rod, 1);
            AddOnce(d, ResourcesLibrary.TryGetBlueprint<BlueprintItem>(KruneBlueprints.DiamondGuid), KruneBlueprints.Diamonds);
            AddOnce(d, ResourcesLibrary.TryGetBlueprint<BlueprintItem>(KruneBlueprints.DiamondDustGuid), KruneBlueprints.DiamondDust);
            krune.BaselineApplied = true;
            Note($"baseline applied once: Mage Armor (CL {KruneProjection.BaseCasterLevel}, cast before hibernation, dispellable), permanent darkvision and see invisibility (dispellable), Runes of Power at mask {state.RuneMask}, resources {string.Join(", ", ResourcesOf(d).Select(r => $"{r.name} {d.Resources.GetResourceAmount(r)}").ToArray())}.");
        }

        private static List<BlueprintScriptableObject> ResourcesOf(UnitDescriptor d)
        {
            var list = new List<BlueprintScriptableObject>();
            foreach (BlueprintScriptableObject resource in d.Resources)
            {
                list.Add(resource);
            }
            return list;
        }

        private static void AddOnce(UnitDescriptor d, BlueprintItem item, int count)
        {
            if (item == null || d.Inventory.Items.Any(i => i.Blueprint == item))
            {
                return;
            }
            d.Inventory.Add(item, count);
        }

        /// <summary>
        /// The dormant receiver leaves the game for good once the real actor owns the protection. It keeps only its identity,
        /// for saved contexts (ward markers) that name it; it is never resolved, protected, fed or recreated again.
        /// </summary>
        private static void RetireReceiver(SanctumState state)
        {
            KruneState krune = state.Krune;
            WardFixtures.Live.Remove("Krune");
            UnitEntityData receiver = WardFixtures.FindRetired("Krune");
            if (receiver != null)
            {
                Buff feeder = receiver.Buffs.GetBuff(WardBlueprints.FeederTempHp);
                if (feeder != null)
                {
                    // Already reconciled: this removal is not consumption and is never read as such.
                    feeder.Remove();
                }
                if (receiver.IsInGame)
                {
                    receiver.IsInGame = false;
                }
                krune.ReceiverId = receiver.UniqueId;
            }
            krune.ReceiverRetired = true;
        }

        private static void Emerge(SanctumState state, double now)
        {
            KruneState krune = state.Krune;
            UnitEntityData actor = Actor;
            if (actor == null)
            {
                return;
            }
            if (actor.Descriptor.State.IsDead)
            {
                // Killed before he stepped out (only a test could): the death stands and he does not emerge.
                Note("the actor is dead before emergence; the death stands.");
                KruneRules.Advance(krune, AwakeningStage.ActorReady, AwakeningStage.Emerged);
                return;
            }
            // The point was chosen when the hidden actor was made; anyone may have moved onto it since (or across a save and
            // reload). Checked again now: kept if clear, otherwise the same policy chooses another; nowhere clear, he waits.
            var chosen = new GridPoint(krune.EmergedX, krune.EmergedY);
            GridPoint? place = KruneRules.ConfirmPlacement(chosen, Occupants(), Floor);
            if (place == null)
            {
                Fail(krune, "no clear A5 floor at the moment of emergence; he waits in the coffin");
                return;
            }
            if (place.Value.X != chosen.X || place.Value.Y != chosen.Y)
            {
                actor.Position = ObstacleAnalyzer.GetNearestNode(World(place.Value)).clampedPosition;
                Note($"emergence point ({chosen.X:0.00},{chosen.Y:0.00}) is occupied now; he steps out at ({place.Value.X:0.00},{place.Value.Y:0.00}) instead, and nobody is moved.");
                krune.EmergedX = place.Value.X;
                krune.EmergedY = place.Value.Y;
            }
            krune.LastFailure = string.Empty;
            actor.IsInGame = true;
            // Facts on a unit out of the game wait to activate, and a restart in between would lose what they read as they
            // apply: the baseline goes on now, in the game, where it takes effect at once.
            ApplyBaseline(state, actor, now);
            GridPoint coffin = SanctumPlan.SarcophagusCenter;
            int feet = KruneRules.StepFeet(coffin.X, coffin.Y, krune.EmergedX, krune.EmergedY);
            if (krune.StepsFeetSpent == 0)
            {
                actor.Descriptor.Resources.Spend(KruneBlueprints.DimensionalSteps, KruneRules.StepUses(feet));
                krune.StepsFeetSpent = feet;
            }
            SpawnStepFx(World(coffin), actor);
            if (krune.OpeningStaggerOwed && !krune.OpeningStaggerApplied)
            {
                // One round, a real condition with a native duration: no game time passes while paused, in dialogue or loading.
                actor.Descriptor.AddBuff(KruneBlueprints.OpeningStagger, actor, TimeSpan.FromSeconds(WardProfile.RoundSeconds));
                krune.OpeningStaggerApplied = true;
                krune.OpeningStaggerAppliedAt = now;
            }
            krune.EmergedAt = now;
            KruneRules.Advance(krune, AwakeningStage.ActorReady, AwakeningStage.Emerged);
            bool seen = Perceivers(actor).Any();
            if (seen)
            {
                WardRuntime.Say(krune.OpeningStaggerOwed
                    ? "The sarcophagus convulses. A rune-covered man steps out of the air beside it, standing but unsteady, as if torn from sleep."
                    : "Light pours from the emerald into the sarcophagus. A moment later a rune-covered man steps out of the air beside it: Runelord Krune is awake.");
            }
            else
            {
                EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning("Somewhere deep in the sanctum, a vast presence stirs."); });
            }
            Note($"EMERGED at ({krune.EmergedX:0.00},{krune.EmergedY:0.00}) by dimensional steps, {feet} ft of {KruneRules.StepsFeetPerDay}/day spent once as {KruneRules.StepUses(feet)} use of the game's own conjuration-school resource ({actor.Descriptor.Resources.GetResourceAmount(KruneBlueprints.DimensionalSteps)} of 17 uses of 30 ft left); stagger {krune.OpeningStaggerApplied}; seen by the party {seen}.");
        }

        private static void Opening(SanctumState state, double now)
        {
            KruneState krune = state.Krune;
            UnitEntityData actor = Actor;
            if (actor == null || actor.Descriptor.State.IsDead)
            {
                KruneRules.TurnHostile(krune, HostilityCause.Unobserved, "no living actor", now);
                KruneRules.Advance(krune, AwakeningStage.Emerged, AwakeningStage.OpeningResolved);
                return;
            }
            if (krune.Opening == OpeningMode.None)
            {
                if (Game.Instance.Player.IsInCombat)
                {
                    krune.Opening = OpeningMode.BattleBark;
                    const string bark = "(In Thassilonian) \"Who dares brawl in my sanctum? Kneel, all of you!\"";
                    Game.Instance.UI.Bark(actor, bark, Kingmaker.UI.Common.UIUtility.GetBarkDuration(bark));
                    WardRuntime.Say("Krune: " + bark);
                    KruneRules.TurnHostile(krune, HostilityCause.BattleUnderWay, "the A4 fight was under way", now);
                }
                else if (Perceivers(actor).Any())
                {
                    krune.Opening = OpeningMode.Parley;
                }
                else
                {
                    krune.Opening = OpeningMode.Unobserved;
                    KruneRules.TurnHostile(krune, HostilityCause.Unobserved, "he woke with nobody in sight", now);
                }
                Note($"opening: {krune.Opening}.");
            }
            if (krune.Opening == OpeningMode.Parley && !krune.Hostile && !krune.ParleyStarted)
            {
                if (KruneDialog.Opening == null)
                {
                    KruneRules.TurnHostile(krune, HostilityCause.ParleyEnded, "no dialogue available", now);
                }
                else if (CommittedPreparation(actor, out string preparation))
                {
                    // Battle preparation he can see, already under way as the exchange would open: no exchange.
                    KruneRules.TurnHostile(krune, HostilityCause.PartyCastSpell, preparation, now);
                    Note($"hostile before the exchange: {preparation}.");
                }
                else if (Game.Instance.CurrentMode == GameModeType.Default)
                {
                    UnitEntityData speaker = Perceivers(actor).FirstOrDefault() ?? Game.Instance.Player.MainCharacter.Value;
                    Game.Instance.DialogController.StartDialogWithoutTarget(KruneDialog.Opening, null, speaker);
                }
                return;
            }
            if (krune.Opening == OpeningMode.Parley && krune.ParleyStarted && !krune.ParleyFinished)
            {
                return;
            }
            if (krune.Hostile)
            {
                KruneRules.Advance(krune, AwakeningStage.Emerged, AwakeningStage.OpeningResolved);
                // The hostile handoff in the same tick: no slice of simulation with him resolved but not yet hostile.
                HandOff(state, now);
                Note($"OPENING RESOLVED: {krune.Opening}; hostile ({krune.HostileCause}: {krune.HostileDetail}); identified {krune.Identified}, told of the fall {krune.ToldOfFall}, calculated {krune.Calculated}.");
            }
        }

        /// <summary>
        /// Turn-based play counts the opening stagger in his turns, not seconds: each turn spends game time, so a round of
        /// game time can run out before his first turn. While he has not ended a turn, the stagger is kept from expiring;
        /// his first completed turn ends it. Real time keeps its native one round of game time.
        /// </summary>
        private static void HoldStaggerForTurn(SanctumState state)
        {
            KruneState krune = state.Krune;
            UnitEntityData actor = Actor;
            if (!krune.OpeningStaggerApplied || krune.OpeningStaggerTurnServed || actor == null || !actor.IsInCombat || !TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
            {
                return;
            }
            Buff stagger = actor.Buffs.GetBuff(KruneBlueprints.OpeningStagger);
            TimeSpan floor = Game.Instance.TimeController.GameTime + TimeSpan.FromSeconds(1d);
            if (stagger != null && stagger.EndTime < floor)
            {
                stagger.EndTime = floor;
            }
        }

        /// <summary>His first completed turn in turn-based play spends the opening stagger.</summary>
        public static void OnTurnEnded(UnitEntityData unit, bool completed)
        {
            KruneState krune = State?.Krune;
            if (krune == null || !completed || !IsKrune(unit) || !krune.OpeningStaggerApplied || krune.OpeningStaggerTurnServed)
            {
                return;
            }
            krune.OpeningStaggerTurnServed = true;
            Buff stagger = unit.Buffs.GetBuff(KruneBlueprints.OpeningStagger);
            if (stagger != null)
            {
                stagger.Remove();
            }
            Note("the opening stagger ends with his first turn (turn-based).");
        }

        /// <summary>
        /// A spell cast already ordered and not finished (issued, moving into range or casting) by someone of the party side
        /// he can see. Only a committed order counts: hovering an ability or cancelling its cursor creates no command.
        /// </summary>
        private static bool CommittedPreparation(UnitEntityData actor, out string what)
        {
            what = null;
            Note($"before the exchange he sees: {string.Join("; ", Perceivers(actor).Select(u => u.CharacterName + " [" + string.Join(",", u.Commands.Raw.Concat(u.Commands.Queue).Where(c => c != null).Select(c => c.GetType().Name + (c.IsStarted ? " started" : string.Empty) + (c.IsFinished ? " finished" : string.Empty)).ToArray()) + "]").ToArray())}.");
            foreach (UnitEntityData unit in Perceivers(actor))
            {
                Kingmaker.UnitLogic.Commands.UnitUseAbility cast = unit.Commands.Raw.Concat(unit.Commands.Queue).OfType<Kingmaker.UnitLogic.Commands.UnitUseAbility>()
                    .FirstOrDefault(c => !c.IsFinished && c.Spell?.Blueprint != null && c.Spell.Blueprint.IsSpell);
                if (cast != null)
                {
                    what = $"{unit.CharacterName} was casting {cast.Spell.Blueprint.name} as the exchange would begin";
                    return true;
                }
            }
            return false;
        }

        /// <summary>Every living creature in the game that is not a scenery proxy: what a placement must keep clear of.</summary>
        private static List<Occupant> Occupants()
        {
            return Game.Instance.State.Units.Where(u => u.IsInGame && u.Descriptor.Get<WardFixturePart>() == null && !u.Descriptor.State.IsDead)
                .Select(u => new Occupant { X = GridX(u.Position), Y = GridY(u.Position), Radius = u.Corpulence / SanctumPlan.MetersPerSquare }).ToList();
        }

        private static bool Floor(float x, float y)
        {
            return KruneRules.A5Floor(x, y) && NodeNear(x, y);
        }

        public static void OnParleyStarted()
        {
            KruneState krune = State?.Krune;
            if (krune != null)
            {
                krune.ParleyStarted = true;
                Note("opening exchange begun (native dialogue: every side is paused alike).");
            }
        }

        public static void OnParleyBeat(string beat)
        {
            KruneState krune = State?.Krune;
            if (krune == null)
            {
                return;
            }
            switch (beat)
            {
                case "identified":
                    krune.Identified = true;
                    break;
                case "told":
                    krune.ToldOfFall = true;
                    break;
                case "calculated":
                    krune.Calculated = true;
                    break;
                case "challenged":
                    KruneRules.TurnHostile(krune, HostilityCause.Challenged, "the party challenged him openly", WardRuntime.Now);
                    break;
            }
            Note($"parley beat: {beat}.");
        }

        public static void OnParleyFinished()
        {
            KruneState krune = State?.Krune;
            if (krune == null)
            {
                return;
            }
            krune.ParleyFinished = true;
            KruneRules.TurnHostile(krune, HostilityCause.ParleyEnded, "the exchange ended without submission", WardRuntime.Now);
            Note("opening exchange finished.");
        }

        /// <summary>
        /// An observable hostile act by someone he perceives ends the parley before it starts: an attack roll or a spell
        /// actually cast. A cancelled cursor, a hover or the inventory produces no rule and so is never one.
        /// </summary>
        public static void OnPartyRule(UnitEntityData initiator, HostilityCause cause, string what)
        {
            SanctumState state = State;
            if (state == null || initiator == null)
            {
                return;
            }
            KruneState krune = state.Krune;
            UnitEntityData actor = Actor;
            if (krune.Stage != AwakeningStage.Emerged || krune.Hostile || actor == null || !IsPartySide(initiator) || !Perceives(actor, initiator))
            {
                return;
            }
            if (KruneRules.TurnHostile(krune, cause, $"{initiator.CharacterName}: {what}", WardRuntime.Now))
            {
                Note($"hostile before the exchange: {initiator.CharacterName} {what}.");
            }
        }

        private static void HandOff(SanctumState state, double now)
        {
            KruneState krune = state.Krune;
            UnitEntityData actor = Actor;
            if (actor != null && !actor.Descriptor.State.IsDead && actor.Descriptor.Faction != Game.Instance.BlueprintRoot.SystemMechanics.FactionMobs)
            {
                actor.Descriptor.SwitchFactions(Game.Instance.BlueprintRoot.SystemMechanics.FactionMobs, true);
                actor.Group.UpdateAttackFactionsCache();
                actor.Group.ResetFactionSet();
                EventBus.RaiseEvent(delegate(IUnitFactionHandler h) { h.HandleFactionChanged(actor); });
            }
            KruneRules.Advance(krune, AwakeningStage.OpeningResolved, AwakeningStage.CombatReady);
        }

        /// <summary>
        /// The linked rune benefits follow the current mask: a newly disabled focus updates them at once, damage taken is
        /// kept, and the same mask again changes nothing. The revival's caster-level loss is read from its own snapshot.
        /// </summary>
        public static void SyncLinks(SanctumState state, double now)
        {
            UnitEntityData actor = Actor;
            if (actor == null || actor.Descriptor.State.IsDead || !actor.IsInGame || state.Krune.Stage < AwakeningStage.ActorReady)
            {
                return;
            }
            UnitDescriptor d = actor.Descriptor;
            Buff runes = d.Buffs.GetBuff(KruneBlueprints.RunesOfPower);
            KwrKruneRunesOfPower logic = runes?.SelectComponents<KwrKruneRunesOfPower>().FirstOrDefault();
            if (logic != null)
            {
                int before = logic.AppliedMask;
                int damage = d.Damage;
                if (logic.Apply(state.RuneMask))
                {
                    Note($"rune links updated: mask {before} -> {state.RuneMask}; damage kept {damage} -> {d.Damage}; {Sheet(actor)}");
                }
            }
            int healing = KruneProjection.Project(state.RuneMask).LifeFastHealing;
            for (int heal = 1; heal <= 3; heal++)
            {
                BlueprintBuff blueprint = KruneBlueprints.LifeFastHealing[heal];
                Buff present = d.Buffs.GetBuff(blueprint);
                if (heal == healing && present == null)
                {
                    d.AddBuff(blueprint, actor);
                }
                else if (heal != healing && present != null)
                {
                    present.Remove();
                }
            }
        }

        /// <summary>The WR-07A harness release: there is no checkpoint any more, so nothing to release.</summary>
        public static string ReleaseForDiagnostics()
        {
            return "no development checkpoint exists in this build: the encounter runs natively.";
        }

        private static void AskLegacy(SanctumState state)
        {
            if (s_LegacyAsked || Game.Instance.CurrentMode != GameModeType.Default)
            {
                return;
            }
            s_LegacyAsked = true;
            KruneState krune = state.Krune;
            string text = $"An older build of this mod recorded Krune's awakening as due ({state.Ritual.Reason}, {Math.Max(0d, WardRuntime.Now - state.Ritual.AwakeningAt) / 60d:0} minutes ago) but could not create him.\n\nInitialize the awakening from this save's recorded cause, time and current runes? The game will stop at the development checkpoint that follows his opening.\n\nChoose OK to initialize it, or Cancel to keep this as a legacy exploration save with no Krune added (asked once).";
            Game.Instance.IsPaused = true;
            EventBus.RaiseEvent(delegate(IDialogMessageBoxUIHandler h)
            {
                h.HandleOpen(text, DialogMessageBoxBase.BoxType.Dialog, button =>
                {
                    double now = WardRuntime.Now;
                    if (button == DialogMessageBoxBase.BoxButton.Yes)
                    {
                        KruneRules.AdoptLegacy(krune, state.Ritual, state.RuneMask, now);
                        Note($"LEGACY ADOPTED: the recorded awakening ({krune.Cause} at {krune.EffectiveAt:0}) is initialized now; the current saved mask {krune.RevivalMask} is the migration snapshot (no history was recorded).");
                        OnCommitted(state, now);
                    }
                    else
                    {
                        KruneRules.KeepLegacy(krune);
                        Note("LEGACY KEPT: an exploration save with no Krune added.");
                    }
                });
            });
        }

        /// <summary>Who of the party side he can see now: inside the A4/A5 chamber, in his line of sight. No sense reaches further.</summary>
        private static IEnumerable<UnitEntityData> Perceivers(UnitEntityData actor)
        {
            return Game.Instance.Player.Party.Concat(Game.Instance.Player.Party.Select(u => u.Descriptor.Pet).Where(p => p != null))
                .Where(u => u.IsInGame && !u.Descriptor.State.IsDead && Perceives(actor, u));
        }

        private static bool Perceives(UnitEntityData actor, UnitEntityData other)
        {
            return SanctumPlan.SpaceAt(GridX(other.Position), GridY(other.Position)) == SanctumSpace.Atrium && actor.HasLOS(other);
        }

        private static bool IsPartySide(UnitEntityData unit)
        {
            UnitEntityData owner = unit.Get<Kingmaker.UnitLogic.Parts.UnitPartSummonedMonster>()?.Summoner ?? unit;
            return owner.IsPlayerFaction;
        }

        /// <summary>The soul's return: a flare at the emerald when it is still whole, the coffin's flashes surging. Visual only.</summary>
        private static void SoulReturnCue(SanctumState state)
        {
            SanctumFlash.Surge(4f);
            GameObject prefab = StepPrefab();
            if (prefab == null)
            {
                return;
            }
            if (!state.Gem.Destroyed)
            {
                FxHelper.SpawnFxOnPoint(prefab, World(new GridPoint(14.8f, 9.6f)) + Vector3.up * 1.1f);
            }
            FxHelper.SpawnFxOnPoint(prefab, World(SanctumPlan.SarcophagusCenter) + Vector3.up * 1.5f);
        }

        private static void SpawnStepFx(Vector3 from, UnitEntityData actor)
        {
            SanctumFlash.Surge(2f);
            GameObject prefab = StepPrefab();
            if (prefab == null)
            {
                return;
            }
            FxHelper.SpawnFxOnPoint(prefab, from + Vector3.up * 1.5f);
            if (actor.View != null)
            {
                FxHelper.SpawnFxOnUnit(prefab, actor.View);
            }
        }

        /// <summary>The game's own dimension door effect, reused for the dimensional steps' arrival and departure.</summary>
        private static GameObject StepPrefab()
        {
            AbilitySpawnFx fx = KruneBlueprints.DimensionDoorFx?.ComponentsArray.OfType<AbilitySpawnFx>().FirstOrDefault(f => f.PrefabLink != null);
            return fx?.PrefabLink.Load();
        }

        /// <summary>The Osirion study of his soul's return (page 14): one native check by the best present, once, descriptive only.</summary>
        private static void StudyRevival(SanctumState state, double now)
        {
            KruneState krune = state.Krune;
            if (krune.RevivalStudyAttempted)
            {
                return;
            }
            UnitEntityData best = Game.Instance.Player.Party.Where(u => !u.Descriptor.State.IsDead && u.Descriptor.State.CanAct && SanctumPlan.SpaceAt(GridX(u.Position), GridY(u.Position)) == SanctumSpace.Atrium)
                .OrderByDescending(u => u.Stats.SkillKnowledgeArcana.ModifiedValue).FirstOrDefault();
            if (best == null)
            {
                return;
            }
            krune.RevivalStudyAttempted = true;
            RuleSkillCheck roll = Rulebook.Trigger(new RuleSkillCheck(best, StatType.SkillKnowledgeArcana, 27) { Silent = true });
            InspectionRules.Record(state.Knowledge, "Krune.Revival", best.UniqueId, "Arcana", best.Stats.SkillKnowledgeArcana.ModifiedValue, roll.RollResult);
            Note($"revival study: {best.CharacterName} Arcana {roll.RollResult} vs 27: {(roll.IsPassed ? "learned" : "nothing learned")}.");
            if (roll.IsPassed)
            {
                WardRuntime.Say($"{best.CharacterName} follows the surge of light into the sarcophagus and reads the rite in it. His soul was held apart from his preserved body. The ritual has reunited them.");
            }
        }

        private static int WithoutPreset(UnitEntityData unit)
        {
            int preset = unit.Stats.Constitution.Modifiers.Where(m => m.ModDescriptor == ModifierDescriptor.Difficulty).Sum(m => m.ModValue);
            int score = unit.Stats.Constitution.ModifiedValue;
            int modLoss = Lashmistress.Abilities.Mod(score) - Lashmistress.Abilities.Mod(score - preset);
            return unit.Descriptor.MaxHP - modLoss * unit.Descriptor.Progression.CharacterLevel;
        }

        private static MechanicsContext SpellContext(UnitEntityData caster, Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility spell, int casterLevel, int spellLevel)
        {
            var context = new MechanicsContext(caster, caster.Descriptor, (BlueprintScriptableObject)spell ?? KruneBlueprints.Nature);
            Harmony12.AccessTools.Field(typeof(MechanicsContext), "m_Params").SetValue(context, new AbilityParams { CasterLevel = casterLevel, SpellLevel = spellLevel, DC = 10 + spellLevel });
            return context;
        }

        /// <summary>One line with what the actor really shows: the sheet is read from native statistics, never recomputed here.</summary>
        public static string Sheet(UnitEntityData actor)
        {
            if (actor == null)
            {
                return "no actor";
            }
            UnitDescriptor d = actor.Descriptor;
            CharacterStats s = d.Stats;
            Spellbook book = d.GetSpellbook(KruneBlueprints.WizardClass);
            var probe = new RuleCalculateAbilityParams(actor, KruneBlueprints.MageArmorSpell, book);
            int casterLevel = Rulebook.Trigger(probe).Result.CasterLevel;
            int presetAC = s.AC.Modifiers.Where(m => m.ModDescriptor == ModifierDescriptor.Difficulty).Sum(m => m.ModValue);
            return $"SHEET {actor.CharacterName}: hp {d.HPLeft}/{d.MaxHP} (damage {d.Damage}, temp {s.TemporaryHitPoints.ModifiedValue}); AC {s.AC.ModifiedValue} (preset {presetAC:+0;-0}); Fort {s.SaveFortitude.ModifiedValue} Ref {s.SaveReflex.ModifiedValue} Will {s.SaveWill.ModifiedValue}; init {s.Initiative.ModifiedValue}; " +
                $"Str {s.Strength.ModifiedValue} Dex {s.Dexterity.ModifiedValue} Con {s.Constitution.ModifiedValue} Int {s.Intelligence.ModifiedValue} Wis {s.Wisdom.ModifiedValue} Cha {s.Charisma.ModifiedValue}; BAB {s.BaseAttackBonus.ModifiedValue}; " +
                $"wizard level {book?.CasterLevel.ToString() ?? "none"}, caster level for a spell {casterLevel}; fast healing {KruneBlueprints.LifeFastHealing.Skip(1).Select((b, i) => d.Buffs.GetBuff(b) != null ? i + 1 : 0).Max()}; " +
                $"staggered {d.State.HasCondition(UnitCondition.Staggered)}{StaggerLeft(d)}; faction {d.Faction?.name}; in game {actor.IsInGame}; dead {d.State.IsDead}; buffs [{string.Join(",", d.Buffs.Enumerable.Select(b => b.Blueprint.name + (b.IsFromSpell ? "*" : string.Empty)).ToArray())}].";
        }

        private static string StaggerLeft(UnitDescriptor d)
        {
            Buff stagger = d.Buffs.GetBuff(KruneBlueprints.OpeningStagger);
            return stagger == null ? string.Empty : $" (opening stagger ends at {stagger.EndTime.TotalSeconds:0.0}, {stagger.EndTime.TotalSeconds - WardRuntime.Now:0.0} s left)";
        }

        private static bool NodeNear(float x, float y)
        {
            Vector3 wanted = World(new GridPoint(x, y));
            Vector3 node = ObstacleAnalyzer.GetNearestNode(wanted).clampedPosition;
            return (new Vector2(node.x, node.z) - new Vector2(wanted.x, wanted.z)).magnitude < 0.25f;
        }

        private static float GridX(Vector3 p)
        {
            return p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX;
        }

        private static float GridY(Vector3 p)
        {
            return SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare;
        }

        private static Vector3 World(GridPoint p)
        {
            return new Vector3(SanctumPlan.ToWorldX(p.X), SanctumPlan.HeightAt(p.X, p.Y) ?? 0f, SanctumPlan.ToWorldZ(p.Y));
        }
    }

    /// <summary>Observable hostile acts toward the parley: an attack roll or a spell actually cast. Inert unless he has just emerged.</summary>
    internal sealed class KruneObserver : IGlobalRulebookHandler<RuleAttackRoll>, IGlobalRulebookHandler<RuleCastSpell>
    {
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            Guard(() => KruneAwakening.OnPartyRule(evt.Initiator, HostilityCause.PartyAttacked, "attacked " + evt.Target?.CharacterName));
        }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            Guard(() => KruneAwakening.OnPartyRule(evt.Initiator, HostilityCause.PartyCastSpell, "cast " + evt.Spell?.Blueprint.name));
        }

        private static void Guard(Action action)
        {
            try
            {
                if (WardRuntime.State != null)
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Krune observer failed.", ex);
            }
        }
    }
}
