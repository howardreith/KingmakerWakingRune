using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Controllers.Brain;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>A decision of the director: the spell from a real slot (or item ability) and its real target.</summary>
    internal sealed class KrunePlan
    {
        public AbilityData Ability;
        public TargetWrapper Target;
        public string Reason;
        public bool Swift;
        public string Key;
        public KruneAim Aim;
    }

    /// <summary>
    /// What Krune last saw of each foe (SightRules): its place and the moment, while it was visible to him by the game's own
    /// detection. A foe he can no longer see is suspected at that place for a short while; he never learns where it is now.
    /// Session only: cleared on every load (KruneSession).
    /// </summary>
    internal static class KruneSight
    {
        private static readonly Dictionary<string, KeyValuePair<Vector3, double>> s_Seen = new Dictionary<string, KeyValuePair<Vector3, double>>();

        public static void ResetSession()
        {
            s_Seen.Clear();
        }

        public static void Saw(UnitEntityData foe, double now)
        {
            s_Seen[foe.UniqueId] = new KeyValuePair<Vector3, double>(foe.Position, now);
        }

        /// <summary>Foes he remembers but cannot see now, at the place he last saw them, most recent first.</summary>
        public static List<KeyValuePair<UnitEntityData, Vector3>> Suspected(UnitEntityData krune, double now)
        {
            var result = new List<KeyValuePair<UnitEntityData, Vector3>>();
            foreach (UnitEntityData u in Game.Instance.State.Units.Where(u => u.IsInGame && u.Descriptor.State.IsConscious && krune.IsEnemy(u)))
            {
                if (!s_Seen.TryGetValue(u.UniqueId, out KeyValuePair<Vector3, double> seen) || !krune.Memory.Contains(u) || !SightRules.Suspected(seen.Value, now, krune.Memory.ContainsVisible(u)))
                {
                    continue;
                }
                result.Add(new KeyValuePair<UnitEntityData, Vector3>(u, seen.Key));
            }
            return result.OrderByDescending(p => s_Seen[p.Key.UniqueId].Value).ToList();
        }
    }

    /// <summary>
    /// Krune's director. When the native brain gives him a decision (in real time when he is free to act, in turn-based play
    /// on his own turn), it reads the battlefield into a KruneView, asks KruneTactics, and turns the choice into a native cast
    /// command from a real slot with its real metamagic. It chooses only: attack rolls, saves, spell resistance, slots,
    /// actions and every effect stay native. It does nothing while he is dominated, confused, frightened or unable to act, and
    /// nothing before the encounter is live.
    /// </summary>
    internal static class KruneDirector
    {
        private static KrunePlan s_Cached;
        private static string s_CachedFor = string.Empty;
        private static KwrDirectedCast s_Active;
        internal static KwrDirectedCast Executing { get; set; }
        internal static bool ProbeSummonRejection;
        private static FailedKrunePlans s_Failed = new FailedKrunePlans();
        private static int s_CachedFrame;
        private static double s_CachedAt = -1d;

        /// <summary>
        /// Every transient decision belongs to one session of one Krune: cleared when a save loads (or the sanctum unloads),
        /// so a hold-back, an issued cast or a cached plan from another moment never carries over.
        /// </summary>
        public static void ResetSession()
        {
            s_Cached = null;
            s_CachedFor = string.Empty;
            s_Active = null;
            Executing = null;
            ProbeSummonRejection = false;
            s_FreeSince = -1f;
            s_WatchNext = 0f;
            s_CachedAt = -1d;
            s_Failed = new FailedKrunePlans();
            s_LastUnavailable = string.Empty;
            s_LastIdle = string.Empty;
            s_LastIdleAt = -100d;
        }

        private static readonly string[] OwnBuffs = { "ForesightBuff", "StoneskinBuff", "TrueSeeingBuff", "MageShieldBuff" };
        private static readonly Dictionary<string, BlueprintBuff> s_Buffs = new Dictionary<string, BlueprintBuff>();

        /// <summary>Read only, for the harness: the session's transient decisions as they stand.</summary>
        public static string DebugState()
        {
            return $"command={s_Active?.Combination ?? "none"} cachedFor={s_CachedFor}@{s_CachedAt:0.0}";
        }

        private static float s_FreeSince = -1f;
        private static float s_WatchNext;
        // Check twice per real second. Allow two seconds of unpaused, actionable inactivity for the brain.
        internal static void Watch()
        {
            float time = Time.unscaledTime;
            if (time < s_WatchNext) return;
            s_WatchNext = time + 0.5f;
            var unit = KruneAwakening.Actor;
            ObserveBudget(unit);
            bool tb = TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
            if (unit == null || !Live(unit) || !Free(unit) || Game.Instance.IsPaused || Game.Instance.CurrentMode != Kingmaker.GameModes.GameModeType.Default
                || (tb && TurnBased.Controllers.CombatController.CurrentUnit != unit)
                || unit.Commands.Raw.Any(c => c != null && !c.IsFinished) || unit.Commands.Queue.Count != 0
                || (unit.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard) && unit.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift)))
            {
                s_FreeSince = -1f;
                return;
            }
            if (s_FreeSince < 0f) s_FreeSince = time;
            if (time - s_FreeSince < 2f) return;
            if (s_Active != null) s_Active.Abandoned();
            var plan = Plan(unit);
            if (plan != null)
            {
                KruneAwakening.Note("director watchdog: free with an empty native queue; resuming with " + plan.Ability.Blueprint.name);
                UnitCommand replacement = Command(unit);
                if (replacement != null) unit.Commands.Run(replacement);
            }
            else if (WardRuntime.State.Krune.Pursuing && !unit.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Move))
            {
                var state = WardRuntime.State.Krune;
                Vector3 seen = KruneMoves.World(state.EnemySeenX, state.EnemySeenY);
                if (unit.DistanceTo(seen) > 2f) unit.Commands.Run(new UnitMoveTo(seen, 1.5f));
                else state.Pursuing = false;
            }
            else if (tb && Game.Instance.TurnBasedCombatController.CurrentTurn.CanEndTurn())
            {
                KruneAwakening.Note("director watchdog: empty queue and no legal cast; ending turn.");
                Game.Instance.TurnBasedCombatController.CurrentTurn.ForceToEnd();
            }
            s_FreeSince = time;
        }

        internal static bool Pending => s_Active != null;

        public static bool Live(UnitEntityData krune)
        {
            KruneState state = WardRuntime.State?.Krune;
            return state != null && state.Stage == AwakeningStage.CombatReady && !KruneRules.CheckpointHolds(state) && krune != null && krune == KruneAwakening.Actor;
        }

        public static KrunePlan Plan(UnitEntityData krune)
        {
            double now = Game.Instance.TimeController.GameTime.TotalSeconds;
            if (!Live(krune) || !Free(krune))
            {
                return null;
            }
            bool turnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
            if (turnBased && TurnBased.Controllers.CombatController.CurrentUnit != krune)
            {
                return null;
            }
            ObserveBudget(krune);
            if (s_Active != null)
            {
                // The brain may construct a candidate and discard it before Init or OnEnded.
                // A full round without acceptance is a terminal rejection for this decision.
                if (s_Active.Executor == null && now - s_Active.CreatedAt >= 6d)
                {
                    s_Active.Abandoned();
                }
                else
                {
                    return null;
                }
            }
            if (s_CachedFor == krune?.UniqueId && s_CachedFrame == Time.frameCount)
            {
                return s_Cached;
            }
            s_Cached = Decide(krune);
            s_CachedFor = krune?.UniqueId ?? string.Empty;
            s_CachedAt = now;
            s_CachedFrame = Time.frameCount;
            return s_Cached;
        }

        private static KrunePlan Decide(UnitEntityData krune)
        {
            if (!Live(krune))
            {
                return Idle("not live");
            }
            if (!Free(krune))
            {
                return Idle($"not free (can act {krune.Descriptor.State.CanAct}, in combat {krune.IsInCombat}, faction {krune.Descriptor.Faction?.name})");
            }
            KruneState state = WardRuntime.State.Krune;
            // His spear lying at his feet comes first, foes in sight or not (a move action).
            if (!krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Move) && KruneDancing.DroppedBeside(krune))
            {
                KrunePlan take = FactPlan(krune, KruneDancing.Grasp, krune, "take up his spear, dropped at his feet when its dance ended");
                if (take != null)
                {
                    return take;
                }
            }
            List<UnitEntityData> foes = Foes(krune);
            if (foes.Count == 0 && state.DirectorPhase != KrunePhase.Recover)
            {
                return Idle("no foe he can see within 40 m");
            }
            if (foes.Count > 0)
            {
                Vector3 seen = foes.OrderBy(f => f.DistanceTo(krune)).First().Position;
                state.EnemySeenX = KruneMoves.GridX(seen);
                state.EnemySeenY = KruneMoves.GridY(seen);
                state.Pursuing = false;
            }
            KruneView view = View(krune, foes, state);
            KrunePhase next = foes.Count == 0 ? KrunePhase.Recover : KruneTactics.NextPhase(view);
            if (next != state.DirectorPhase)
            {
                KruneAwakening.Note($"director: phase {state.DirectorPhase} -> {next} (hp {view.HitPointFraction:0.00}, adjacent {view.AdjacentFoes}).");
                state.DirectorPhase = next;
                view.Phase = next;
            }
            Boots(krune);
            if (!krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift))
            {
                KrunePlan swift = First(krune, foes, KruneTactics.SwiftOptions(view), true) ?? Telekinesis(krune, foes);
                if (swift != null)
                {
                    return swift;
                }
            }
            if (KruneTactics.Overwhelmed(view))
            {
                KrunePlan passage = view.HasPhaseDoor ? PassagePlan(krune, foes) : null;
                if (passage != null) return passage;
                var step = krune.Descriptor.Abilities.Enumerable.FirstOrDefault(a => a.Blueprint.GetComponents<AbilityResourceLogic>()
                    .Any(r => r.RequiredResource == KruneBlueprints.DimensionalSteps && r.IsSpendResource));
                if (step != null && step.Data.IsAvailable && !krune.CombatState.HasCooldownForCommand(step.Data.RuntimeActionType))
                {
                    Vector3? to = KruneMoves.Retreat(krune, foes, 30f.Feet().Meters, true);
                    if (to.HasValue && step.Data.CanTarget(new TargetWrapper(to.Value)))
                    {
                        var jump = Eligible(new KrunePlan { Ability = step.Data, Target = new TargetWrapper(to.Value), Aim = KruneAim.Retreat, Reason = "dimensional steps out of the surround" });
                        if (jump != null) return jump;
                    }
                }
            }
            if (krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard))
            {
                return null;
            }
            if (foes.Count == 0)
            {
                if (view.SummonsAlive >= 3 && view.HasForesight && view.HasStoneskin) return null;
                return First(krune, foes, KruneTactics.StandardOptions(view), false);
            }
            return GearFirst(krune, foes, view) ?? First(krune, foes, KruneTactics.StandardOptions(view), false) ?? GearLast(krune, foes, view) ?? Fallback(krune, foes) ?? Idle("no legal cast: " + s_LastUnavailable);
        }

        /// <summary>Boots of speed: switched on (a free action) once he is fighting, while they have rounds left that day.</summary>
        private static void Boots(UnitEntityData krune)
        {
            Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbility boots = krune.Descriptor.ActivatableAbilities.Enumerable.FirstOrDefault(a => a.Blueprint == KruneGear.BootsToggle);
            if (boots != null && !boots.IsOn && boots.IsAvailable)
            {
                boots.IsOn = true;
                KruneAwakening.Note($"director: boots of speed on ({KruneGear.Item(krune, KruneGear.Boots)?.Charges} rounds left today).");
            }
        }

        /// <summary>Uses that come before his prepared list: wish's summons while he recovers; the ring when foes cast and his own spell turning is gone.</summary>
        private static KrunePlan GearFirst(UnitEntityData krune, List<UnitEntityData> foes, KruneView view)
        {
            if (view.Phase == KrunePhase.Recover)
            {
                KrunePlan wish = WishPlan(krune, foes, KruneGear.WishSummon, "wish: a summoned army while he recovers");
                if (wish != null)
                {
                    return wish;
                }
            }
            if (view.FoeCasters > 0 && !view.HasSpellTurning && !view.Ready.ContainsKey("Spell Turning"))
            {
                KrunePlan ring = ItemPlan(krune, KruneGear.RingOfSpellTurning, krune, "ring of spell turning: foes cast at him");
                if (ring != null)
                {
                    return ring;
                }
            }
            return null;
        }

        /// <summary>Uses once his prepared list has nothing that fits: the pearl, wish's wilting, the spear's powers.</summary>
        private static KrunePlan GearLast(UnitEntityData krune, List<UnitEntityData> foes, KruneView view)
        {
            KrunePlan pearl = ItemPlan(krune, KruneGear.Pearl, krune, "pearl of power: recall a spent 8th-level spell");
            if (pearl != null)
            {
                return pearl;
            }
            if (view.BestCluster >= 3)
            {
                KrunePlan wilting = WishPlan(krune, foes, KruneGear.WishWilting, "wish: horrid wilting on a cluster");
                if (wilting != null)
                {
                    return wilting;
                }
            }
            UnitEntityData nearest = foes.OrderBy(f => f.DistanceTo(krune)).First();
            if (view.FoesWithin30 > 0 && KruneDancing.FormOf(WardRuntime.State.Krune) == null)
            {
                KrunePlan dance = FactPlan(krune, KruneDancing.Loose, krune, "his spear dances against the foes closing in");
                if (dance != null)
                {
                    return dance;
                }
            }
            return FactPlan(krune, KruneGear.SpearSlow, nearest, "the spear's slow") ?? FactPlan(krune, KruneGear.SpearRay, nearest, "the spear's ray of enfeeblement");
        }

        /// <summary>Through an open phase door passage (a move action): up to 40 ft through stone to the floor farthest from the foes.</summary>
        private static KrunePlan PassagePlan(UnitEntityData krune, List<UnitEntityData> foes)
        {
            Kingmaker.UnitLogic.Abilities.Ability fact = krune.Descriptor.Abilities.GetFact(KruneLocalSpells.PhaseDoorStep) as Kingmaker.UnitLogic.Abilities.Ability;
            AbilityData data = fact?.Data;
            TargetWrapper target = data != null && data.IsAvailableForCast ? Aim(krune, foes, data, KruneAim.Retreat) : null;
            return target != null && data.CanTarget(target) && InRange(krune, data, target) ? Eligible(new KrunePlan { Ability = data, Target = target, Reason = "overwhelmed: through his phase door to another part of the sanctum", Key = data.Blueprint.name, Aim = KruneAim.Retreat }) : null;
        }

        private static KrunePlan Telekinesis(UnitEntityData krune, List<UnitEntityData> foes)
        {
            UnitEntityData adjacent = foes.Where(f => f.DistanceTo(krune) <= krune.Corpulence + f.Corpulence + 1.7f && f.CanBeKnockedOff()).OrderBy(f => f.DistanceTo(krune)).FirstOrDefault();
            return adjacent == null ? null : FactPlan(krune, KruneGear.SpearTelekinesis, adjacent, "the spear's telekinesis trips the foe at his side", true);
        }

        private static KrunePlan WishPlan(UnitEntityData krune, List<UnitEntityData> foes, BlueprintAbility variant, string reason)
        {
            Kingmaker.UnitLogic.Abilities.Ability wish = krune.Descriptor.Abilities.GetFact(KruneGear.Wish) as Kingmaker.UnitLogic.Abilities.Ability;
            if (wish == null)
            {
                return null;
            }
            var data = new AbilityData(wish.Data, variant);
            if (!data.IsAvailableForCast)
            {
                return null;
            }
            KruneAim aim = variant == KruneGear.WishSummon ? KruneAim.SummonPoint : KruneAim.FoeCluster;
            TargetWrapper target = Aim(krune, foes, data, aim);
            return target != null && data.CanTarget(target) && InRange(krune, data, target) ? Eligible(new KrunePlan { Ability = data, Target = target, Reason = reason, Key = variant.name, Aim = aim }) : null;
        }

        private static KrunePlan ItemPlan(UnitEntityData krune, Kingmaker.Blueprints.Items.BlueprintItem blueprint, TargetWrapper target, string reason)
        {
            Kingmaker.Items.ItemEntity item = KruneGear.Item(krune, blueprint);
            AbilityData data = item?.Ability?.Data;
            return data != null && item.Charges > 0 && data.IsAvailableForCast && data.CanTarget(target) ? Eligible(new KrunePlan { Ability = data, Target = target, Reason = reason, Key = blueprint.name }) : null;
        }

        private static KrunePlan FactPlan(UnitEntityData krune, BlueprintAbility blueprint, UnitEntityData target, string reason, bool swift = false)
        {
            Kingmaker.UnitLogic.Abilities.Ability fact = krune.Descriptor.Abilities.GetFact(blueprint) as Kingmaker.UnitLogic.Abilities.Ability;
            AbilityData data = fact?.Data;
            return data != null && data.IsAvailableForCast && data.CanTarget(target) && InRange(krune, data, target) ? Eligible(new KrunePlan { Ability = data, Target = target, Reason = reason, Swift = swift, Key = blueprint.name }) : null;
        }

        /// <summary>Control effects yield: no director decision while he is not his own master or cannot act.</summary>
        private static bool Free(UnitEntityData krune)
        {
            return OwnMaster(krune) && krune.IsInCombat;
        }

        /// <summary>
        /// He acts by his own will: able to act, not confused, frightened or cowering, and still of his own allegiance
        /// (domination and charm change it). Whatever controls him otherwise owns his actions, in combat or out.
        /// </summary>
        internal static bool OwnMaster(UnitEntityData krune)
        {
            UnitState s = krune.Descriptor.State;
            return DanceRules.OwnMaster(s.CanAct, s.HasCondition(UnitCondition.Confusion), s.HasCondition(UnitCondition.Frightened), s.HasCondition(UnitCondition.Cowering),
                krune.Descriptor.Faction == Game.Instance.BlueprintRoot.SystemMechanics.FactionMobs);
        }

        /// <summary>
        /// Conscious foes within 40 m that he detects now by the game's own perception (his group's memory: sight, his see
        /// invisibility, blindsense): a fallen foe is no target for his spells, his clusters or his trips, and a foe he cannot
        /// detect is not one either (KruneSight keeps only where he last saw it).
        /// </summary>
        private static List<UnitEntityData> Foes(UnitEntityData krune)
        {
            double now = WardRuntime.Now;
            List<UnitEntityData> foes = Game.Instance.State.Units.Where(u => u.IsInGame && u.Descriptor.State.IsConscious && WardFixtures.IdOf(u) == null && krune.IsEnemy(u) && u.DistanceTo(krune) < 40f && krune.Memory.ContainsVisible(u)).ToList();
            foreach (UnitEntityData foe in foes)
            {
                KruneSight.Saw(foe, now);
            }
            return foes;
        }

        private static KruneView View(UnitEntityData krune, List<UnitEntityData> foes, KruneState state)
        {
            var v = new KruneView
            {
                HitPointFraction = krune.HPLeft / (double)Math.Max(1, krune.MaxHP),
                AdjacentFoes = foes.Count(f => f.DistanceTo(krune) <= krune.Corpulence + f.Corpulence + 1.7f),
                FoesWithin30 = foes.Count(f => f.DistanceTo(krune) <= 9.2f),
                FoeCasters = foes.Count(f => f.Descriptor.Spellbooks.Any(b => b.CasterLevel > 0)),
                UnseenFoeSuspected = SuspectedPoint(krune).HasValue,
                SummonsAlive = Summons(krune).Count,
                HasForesight = HasBuff(krune, "ForesightBuff"),
                HasStoneskin = HasBuff(krune, "StoneskinBuff"),
                HasTrueSeeing = HasBuff(krune, "TrueSeeingBuff"),
                HasShield = HasBuff(krune, "MageShieldBuff"),
                HasPhaseDoor = KruneLocalSpells.PhaseDoorPassages != null && krune.Buffs.HasFact(KruneLocalSpells.PhaseDoorPassages),
                HasSpellTurning = KruneLocalSpells.SpellTurning != null && krune.Buffs.HasFact(KruneLocalSpells.SpellTurning),
                HasGlobe = KruneLocalSpells.InsideGlobe != null && krune.Buffs.HasFact(KruneLocalSpells.InsideGlobe),
                WallStanding = WardRuntime.State.Walls.Any(w => w.Caster == krune.UniqueId && WallRules.Standing(w)),
                CloudActive = OwnArea(krune, "Cloudkill") || OwnArea(krune, "AcidFog") || OwnArea(krune, "StinkingCloud"),
                FoeHasStrongBuffs = foes.Any(f => f.Buffs.Enumerable.Count(b => b.IsFromSpell) >= 3),
                RoundsInCombat = state.CombatStartedAt < 0d ? 0 : (int)((WardRuntime.Now - state.CombatStartedAt) / 6d),
                Phase = state.DirectorPhase,
                BestCluster = BestPoint(krune, foes, 20f.Feet().Meters, false, out _, 40f.Feet().Meters),
            };
            foreach (SpellSlot slot in KruneSpellbook.Ready(krune))
            {
                string key = KruneTactics.Key(SpellName(slot.Spell), Metamagic(slot.Spell));
                v.Ready[key] = v.Ready.TryGetValue(key, out int n) ? n + 1 : 1;
            }
            return v;
        }

        /// <summary>The printed spell name of a slot (KruneBook), from its blueprint.</summary>
        private static string SpellName(AbilityData spell)
        {
            BookEntry entry = KruneBook.Plan().FirstOrDefault(e => e.Blueprint == spell.Blueprint.name);
            return entry?.Spell ?? spell.Blueprint.name;
        }

        private static string Metamagic(AbilityData spell)
        {
            if (spell.MetamagicData == null)
            {
                return string.Empty;
            }
            return spell.MetamagicData.Has(Kingmaker.UnitLogic.Abilities.Metamagic.Quicken) ? "Quickened" : spell.MetamagicData.Has(Kingmaker.UnitLogic.Abilities.Metamagic.Empower) ? "Empowered" : string.Empty;
        }

        /// <summary>The first option he can really cast and aim; the skipped ones are noted with why.</summary>
        private static KrunePlan First(UnitEntityData krune, List<UnitEntityData> foes, List<KruneChoice> options, bool swift)
        {
            var skipped = new List<string>();
            foreach (KruneChoice choice in options)
            {
                KrunePlan plan = Resolve(krune, foes, choice, swift, out string why);
                if (plan != null)
                {
                    if (skipped.Count > 0)
                    {
                        KruneAwakening.Note("director skipped: " + string.Join("; ", skipped.ToArray()) + ".");
                    }
                    return plan;
                }
                skipped.Add(choice.Key + " (" + why + ")");
            }
            if (skipped.Count > 0)
            {
                s_LastUnavailable = string.Join("; ", skipped.ToArray());
            }
            return null;
        }

        private static KrunePlan Resolve(UnitEntityData krune, List<UnitEntityData> foes, KruneChoice choice, bool swift, out string why)
        {
            why = string.Empty;
            if (choice == null)
            {
                why = "none";
                return null;
            }
            SpellSlot slot = KruneSpellbook.Ready(krune).FirstOrDefault(s => SpellName(s.Spell) == choice.Spell && Metamagic(s.Spell) == choice.Metamagic);
            if (slot == null)
            {
                why = "no slot";
                return null;
            }
            AbilityData ability = WithVariant(slot.Spell, choice.Spell);
            if (ability == null || !ability.IsAvailable)
            {
                why = ability == null ? "no castable variant" : "ability unavailable";
                return null;
            }
            TargetWrapper target = Aim(krune, foes, ability, choice.Aim);
            if (target == null)
            {
                why = "no worthwhile target";
                return null;
            }
            if (!ability.CanTarget(target))
            {
                why = "target refused";
                return null;
            }
            if (!InRange(krune, ability, target))
            {
                why = "out of range";
                return null;
            }
            var plan = Eligible(new KrunePlan { Ability = ability, Target = target, Reason = choice.Reason, Swift = swift, Key = choice.Key, Aim = choice.Aim });
            if (plan == null) why = "failed this round or action unavailable";
            return plan;
        }

        /// <summary>
        /// The place a foe vanished from his sight, within 30 ft, when that place itself is in his line of sight (he sees the
        /// foe is gone, not merely behind a wall): the reason for a search spell, aimed there.
        /// </summary>
        private static Vector3? SuspectedPoint(UnitEntityData krune)
        {
            foreach (KeyValuePair<UnitEntityData, Vector3> s in KruneSight.Suspected(krune, WardRuntime.Now))
            {
                if (Vector3.Distance(s.Value, krune.Position) > 9.2f)
                {
                    continue;
                }
                if (Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance != null && Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(krune.EyePosition, s.Value))
                {
                    continue;
                }
                return s.Value;
            }
            return null;
        }

        /// <summary>A phase door passage (through stone) or a greater teleport (anywhere he knows): no line of sight needed.</summary>
        public static bool ThroughStone(AbilityData ability)
        {
            return ability.Blueprint == KruneBlueprints.DimensionDoorFx || ability.Blueprint == KruneLocalSpells.PhaseDoorStep || ability.Blueprint.name == "KWR_KruneGreaterTeleport";
        }

        /// <summary>He casts from where he stands: a target beyond the spell's range (this game's fixed ranges) is not chosen.</summary>
        private static bool InRange(UnitEntityData krune, AbilityData ability, TargetWrapper target)
        {
            if (ability.Blueprint == KruneBlueprints.DimensionDoorFx) return true;
            float range = ability.Blueprint.GetRange().Meters;
            if (range <= 0f || range > 500f)
            {
                return true;
            }
            float distance = target.IsUnit ? krune.DistanceTo(target.Unit) - target.Unit.Corpulence : Vector3.Distance(krune.Position, target.Point);
            // Inside the approach distance with a margin, and in his line of sight (the cast command needs both), so it starts
            // where he stands and never walks him around a wall to find its target.
            if (distance > range - 0.5f)
            {
                return false;
            }
            if (ThroughStone(ability))
            {
                return true;
            }
            Vector3 to = target.IsUnit ? target.Unit.Position : target.Point;
            return Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance == null || !Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(krune.EyePosition, to);
        }

        private static void ObserveBudget(UnitEntityData krune)
        {
            if (krune == null || !Live(krune)) return;
            int spent = (krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard) ? 1 : 0)
                | (krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift) ? 2 : 0)
                | (krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Move) ? 4 : 0);
            var turn = TurnBased.Controllers.CombatController.IsInTurnBasedCombat() ? Game.Instance.TurnBasedCombatController.CurrentTurn : null;
            var state = WardRuntime.State.Krune;
            string tacticalState = krune.HPLeft + "/" + krune.Memory.Enemies.Count + "/" + state.DirectorPhase
                + "/" + state.RodCharges + "/" + KruneSpellbook.Ready(krune).Count();
            s_Failed.Observe(turn, spent, tacticalState);
        }

        private static string PlanKey(KrunePlan plan) => plan.Ability.Blueprint.AssetGuid + "/" + Metamagic(plan.Ability) + "/" + plan.Ability.RuntimeActionType;

        private static string TargetKey(KrunePlan plan)
        {
            Vector3 point = plan.Target.Point;
            string target = plan.Target.IsUnit ? plan.Target.Unit.UniqueId
                : Mathf.RoundToInt(point.x) + "/" + Mathf.RoundToInt(point.y) + "/" + Mathf.RoundToInt(point.z);
            return target;
        }

        private static KrunePlan Eligible(KrunePlan plan)
        {
            var actor = plan.Ability.Caster.Unit;
            return !actor.CombatState.HasCooldownForCommand(plan.Ability.RuntimeActionType)
                && s_Failed.Allows(PlanKey(plan), TargetKey(plan)) ? plan : null;
        }

        internal static void Ended(KwrDirectedCast command, CastResult reason)
        {
            RuneRod.CommandEnded(command);
            if (command != s_Active)
            {
                return;
            }
            if (reason == CastResult.Delivered && command.Relocation && WardRuntime.State != null)
            {
                var state = WardRuntime.State.Krune;
                state.HasRetreat = state.Pursuing = true;
                state.RetreatX = KruneMoves.GridX(command.Target.Point);
                state.RetreatY = KruneMoves.GridY(command.Target.Point);
                state.DirectorPhase = KrunePhase.Recover;
            }
            if (reason == CastResult.Delivered && command.Spell.Blueprint == KruneBlueprints.DimensionDoorFx && command.Executor != null)
            {
                // Dimension door ends the caster's actions even when quickened. The native teleport delivery
                // moves the unit and plays its effects. Leave cooldown expiry and turn advancement to the engine.
                var cooldown = command.Executor.CombatState.Cooldown;
                cooldown.StandardAction = Math.Max(cooldown.StandardAction, 6f);
                cooldown.MoveAction = Math.Max(cooldown.MoveAction, 3f);
                cooldown.SwiftAction = Math.Max(cooldown.SwiftAction, 6f);
            }
            if (reason != CastResult.Committed && reason != CastResult.Delivered)
            {
                s_Failed.Reject(command.PlanKey, command.TargetKey, reason);
            }
            s_Active = null;
            s_Cached = null;
            s_CachedFrame = -1;
            KruneAwakening.Note($"director terminal: {command.Spell.Blueprint.name} {command.Combination}: {reason}.");
        }

        private static AbilityData WithVariant(AbilityData spell, string printed)
        {
            BookEntry entry = KruneBook.Plan().First(e => e.Spell == printed);
            string variant = entry.Variant;
            if (variant.Length == 0 && spell.Blueprint.name.StartsWith("SummonMonster", StringComparison.Ordinal))
            {
                variant = spell.Blueprint.name.Replace("Base", "Single");
            }
            BlueprintAbility chosen = variant.Length == 0 ? null : spell.Blueprint.GetComponent<AbilityVariants>()?.Variants.FirstOrDefault(v => v.name == variant);
            if (chosen != null)
            {
                return new AbilityData(spell, chosen);
            }
            // Some profiles replace the base with a directly executable spell. Never dispatch a selector.
            var variants = spell.Blueprint.GetComponent<AbilityVariants>();
            if (variants != null)
            {
                if (!printed.StartsWith("Summon Monster", StringComparison.Ordinal)) return null;
                // Compatibility profiles can replace the native Single child. Select only a live executable leaf.
                return variants.Variants.Where(v => v != null && v.GetComponent<AbilityVariants>() == null)
                    .Select(v => new AbilityData(spell, v)).FirstOrDefault(v => v.IsAvailable);
            }
            if (variant.Length == 0) return spell;
            // Retain only profile replacements with an executable summon, never a group teleport.
            return printed.StartsWith("Summon Monster", StringComparison.Ordinal)
                && spell.Blueprint.GetComponent<AbilityEffectRunAction>() != null ? spell : null;
        }

        private static TargetWrapper Aim(UnitEntityData krune, List<UnitEntityData> foes, AbilityData ability, KruneAim aim)
        {
            if (foes.Count == 0 && aim != KruneAim.Self && aim != KruneAim.SummonPoint) return null;
            UnitEntityData nearest = foes.OrderBy(f => f.DistanceTo(krune)).FirstOrDefault();
            switch (aim)
            {
                case KruneAim.Self:
                    return krune;
                case KruneAim.NearestFoe:
                case KruneAim.LinkFoe:
                    return nearest;
                case KruneAim.FoeCaster:
                    return foes.OrderByDescending(f => f.Buffs.Enumerable.Count(b => b.IsFromSpell) + f.Descriptor.Spellbooks.Count()).ThenBy(f => f.DistanceTo(krune)).First();
                case KruneAim.UnseenFoe:
                    // Where he last saw a foe that has vanished from his sight: never where it is now.
                    Vector3? suspected = SuspectedPoint(krune);
                    return suspected.HasValue ? new TargetWrapper(suspected.Value) : null;
                case KruneAim.FoeCluster:
                    float radius = Math.Max(ability.Blueprint.AoERadius.Meters, 10f.Feet().Meters);
                    bool poisonCloud = (ability.Blueprint.GetComponent<SpellDescriptorComponent>()?.Descriptor.Value & SpellDescriptor.Poison) != 0;
                    List<UnitEntityData> pool = foes;
                    // Foes already inside one of his own areas of the same spell are not a reason to cast it again.
                    List<Vector3> covered = Game.Instance.State.AreaEffects.Where(a => a.Context?.MaybeCaster == krune && a.Context.SourceAbility != null
                        && (a.Context.SourceAbility == ability.Blueprint || a.Context.SourceAbility == ability.Blueprint.Parent)).Select(a => a.Position).ToList();
                    pool = pool.Where(f => !covered.Any(p => Vector3.Distance(p, f.Position) <= radius)).ToList();
                    float reach = ability.Blueprint.GetRange().Meters;
                    int caught = BestPoint(krune, pool, radius, poisonCloud, out Vector3 at, reach > 0f && reach < 500f ? reach : 0f);
                    return caught > 0 ? new TargetWrapper(at) : null;
                case KruneAim.Barrier:
                    List<UnitEntityData> closing = foes.Where(f => f.DistanceTo(krune) <= 9.2f).ToList();
                    if (closing.Count == 0)
                    {
                        return null;
                    }
                    Vector3 centre = closing.Aggregate(Vector3.zero, (s, f) => s + f.Position) / closing.Count;
                    Vector3 toward = centre - krune.Position;
                    float d = toward.magnitude;
                    Vector3 point = krune.Position + toward.normalized * Mathf.Clamp(d * 0.5f, 2f, 6f);
                    return new TargetWrapper(point);
                case KruneAim.SummonPoint:
                    Vector3 direction = nearest == null ? Vector3.forward : (nearest.Position - krune.Position).normalized;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 front = krune.Position + Quaternion.Euler(0f, i * 45f, 0f) * direction * 3f;
                        Vector3 landing = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(front).clampedPosition;
                        if (KruneMoves.LegalDestination(landing, null)) return new TargetWrapper(landing);
                    }
                    return null;
                case KruneAim.Retreat:
                    // Within the spell's own reach (a phase door passage: 40 ft) and, except through stone, in his sight.
                    float span = ability.Blueprint.GetRange().Meters;
                    float max = ability.Blueprint == KruneLocalSpells.PhaseDoorStep ? 40f.Feet().Meters : ability.Blueprint == KruneBlueprints.DimensionDoorFx ? 0f : span > 0f && span < 500f ? span - 0.5f : 0f;
                    Vector3? away = KruneMoves.Retreat(krune, foes, max, !ThroughStone(ability));
                    return away.HasValue ? new TargetWrapper(away.Value) : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// The point that catches the most foes in a radius without catching him or his summons (a poison cloud he is immune
        /// to may include him). Candidate centres are the foes themselves and the midpoints of pairs.
        /// </summary>
        private static int BestPoint(UnitEntityData krune, List<UnitEntityData> foes, float radius, bool selfImmune, out Vector3 best, float maxRange = 0f)
        {
            List<UnitEntityData> allies = Summons(krune);
            var candidates = foes.Select(f => f.Position).ToList();
            for (int i = 0; i < foes.Count; i++)
            {
                for (int j = i + 1; j < foes.Count; j++)
                {
                    candidates.Add((foes[i].Position + foes[j].Position) / 2f);
                }
            }
            best = Vector3.zero;
            int bestCount = 0;
            foreach (Vector3 c in candidates)
            {
                if (maxRange > 0f && Vector3.Distance(c, krune.Position) > maxRange - 0.5f)
                {
                    continue;
                }
                if (maxRange > 0f && Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance != null && Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(krune.EyePosition, c))
                {
                    continue;
                }
                if (!selfImmune && Vector3.Distance(c, krune.Position) <= radius + krune.Corpulence)
                {
                    continue;
                }
                if (allies.Any(a => Vector3.Distance(c, a.Position) <= radius))
                {
                    continue;
                }
                int count = foes.Count(f => Vector3.Distance(c, f.Position) <= radius + f.Corpulence);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = c;
                }
            }
            return bestCount;
        }

        private static string s_LastUnavailable = string.Empty;
        private static string s_LastIdle = string.Empty;
        private static double s_LastIdleAt = -100d;

        /// <summary>Why there is no plan, logged when it changes (at most once every 6 s of game time).</summary>
        private static KrunePlan Idle(string why)
        {
            double now = WardRuntime.Now;
            if (why != s_LastIdle || now - s_LastIdleAt > 30d)
            {
                if (now - s_LastIdleAt > 6d || why != s_LastIdle)
                {
                    KruneAwakening.Note("director idle: " + why + ".");
                }
                s_LastIdle = why;
                s_LastIdleAt = now;
            }
            return null;
        }

        public static List<UnitEntityData> Summons(UnitEntityData krune)
        {
            return Game.Instance.State.Units.Where(u => u.IsInGame && !u.Descriptor.State.IsDead && u.Get<UnitPartSummonedMonster>()?.Summoner == krune).ToList();
        }

        private static bool OwnArea(UnitEntityData krune, string name)
        {
            return Game.Instance.State.AreaEffects.Any(a => a.Context?.MaybeCaster == krune && a.Blueprint.name.IndexOf(name, StringComparison.Ordinal) >= 0);
        }

        private static bool HasBuff(UnitEntityData krune, string name)
        {
            if (!s_Buffs.TryGetValue(name, out BlueprintBuff buff))
            {
                buff = ResourcesLibrary.GetBlueprints<BlueprintBuff>().FirstOrDefault(b => b.name == name);
                s_Buffs[name] = buff;
            }
            return buff != null && krune.Buffs.HasFact(buff);
        }

        /// <summary>When no prepared spell fits: acid dart (his school's own uses), then his wand of acid arrow; else the native attack.</summary>
        private static KrunePlan Fallback(UnitEntityData krune, List<UnitEntityData> foes)
        {
            UnitEntityData nearest = foes.OrderBy(f => f.DistanceTo(krune)).First();
            Kingmaker.UnitLogic.Abilities.Ability dart = krune.Descriptor.Abilities.Enumerable.FirstOrDefault(a => a.Blueprint.name == "ConjurationAcidDartAbility");
            if (dart != null && dart.Data.IsAvailableForCast && dart.Data.CanTarget(nearest))
            {
                KrunePlan dartPlan = Eligible(new KrunePlan { Ability = dart.Data, Target = nearest, Reason = "acid dart" });
                if (dartPlan != null && InRange(krune, dart.Data, nearest)) return dartPlan;
            }
            Kingmaker.Items.ItemEntity wand = krune.Body.QuickSlots.Select(s => s.MaybeItem).FirstOrDefault(i => i != null && i.Blueprint == KruneBlueprints.WandOfAcidArrow && i.Charges > 0);
            AbilityData wandAbility = wand?.Ability?.Data;
            if (wandAbility != null && wandAbility.IsAvailableForCast && wandAbility.CanTarget(nearest) && InRange(krune, wandAbility, nearest))
            {
                return Eligible(new KrunePlan { Ability = wandAbility, Target = nearest, Reason = "wand of acid arrow" });
            }
            return null;
        }

        public static UnitCommand Command(UnitEntityData krune)
        {
            KrunePlan plan = Plan(krune);
            if (plan == null)
            {
                return null;
            }
            s_Cached = null;
            var command = new KwrDirectedCast(plan.Ability, plan.Target, PlanKey(plan), TargetKey(plan), plan.Aim == KruneAim.Retreat);
            s_Active = command;
            string rod = RuneRod.Offer(krune, plan.Ability, RodRequest(plan), command);
            if (ProbeSummonRejection && plan.Key == "Quickened Summon Monster V" && rod == "Extend"
                && Game.Instance.State.Units.Any(u => u.IsInGame && !u.Descriptor.State.IsDead && u.Get<UnitPartSummonedMonster>() != null
                    && u.Blueprint.name.IndexOf("FrostGiant", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                command.RejectForProbe = true;
                ProbeSummonRejection = false;
            }
            KruneAwakening.Note($"director: {plan.Ability.Blueprint.name}{(plan.Ability.MetamagicData != null && plan.Ability.MetamagicData.NotEmpty ? " (" + plan.Ability.MetamagicData.MetamagicMask + ")" : string.Empty)} at {(plan.Target.IsUnit ? plan.Target.Unit.CharacterName : plan.Target.Point.ToString())}{(plan.Swift ? " (swift)" : string.Empty)}{(rod != null ? " with the rod's " + rod : string.Empty)}: {plan.Reason}.");
            return command;
        }

        /// <summary>
        /// The rod's metamagic he asks for (RodRules.KruneRequest): a spell against foes that accepts Maximize or Empower is
        /// damaging; a summons is lasting (Extend doubles how long it fights for him).
        /// </summary>
        private static List<string> RodRequest(KrunePlan plan)
        {
            BlueprintAbility spell = plan.Ability.Blueprint;
            bool summons = plan.Aim == KruneAim.SummonPoint;
            bool damaging = !summons && (plan.Aim == KruneAim.FoeCluster || plan.Aim == KruneAim.NearestFoe || plan.Aim == KruneAim.FoeCaster)
                && (spell.AvailableMetamagic & (Kingmaker.UnitLogic.Abilities.Metamagic.Maximize | Kingmaker.UnitLogic.Abilities.Metamagic.Empower)) != 0;
            return RodRules.KruneRequest(damaging, summons, RuneRod.Charges(), plan.Ability.SpellLevel);
        }
    }

    /// <summary>The native brain's decision point for Krune: available when the director has a plan; the plan is a native cast.</summary>
    public sealed class KwrKruneDirectorAction : BlueprintAiAction
    {
        public override bool IsAvailable(DecisionContext context)
        {
            return KruneDirector.Plan(context.Unit) != null;
        }

        public override UnitCommand CreateCommand(DecisionContext context, UnitEntityData target)
        {
            return KruneDirector.Command(context.Unit);
        }

        public override Kingmaker.Controllers.Brain.Blueprints.TargetType GetTargetType(DecisionContext context)
        {
            return Kingmaker.Controllers.Brain.Blueprints.TargetType.Self;
        }

        public override float GetRange(DecisionContext context)
        {
            return 0f;
        }

        public override float GetRadius(DecisionContext context)
        {
            return 0f;
        }
    }

    /// <summary>
    /// His spear (the native attack command), offered only once his standard action is ready. The native attack action queues
    /// its next attack while the action is still spent and starts it the moment the action returns, before the director is
    /// asked again: in close quarters he then only ever fought with the spear. Offered only when ready, the director (which
    /// scores higher) is asked first each round, and the spear is what is left when it has nothing to cast.
    /// </summary>
    public sealed class KwrKruneMeleeAction : BlueprintAiAction
    {
        public override bool IsAvailable(DecisionContext context)
        {
            UnitEntityData krune = context.Unit;
            if (!KruneDirector.Live(krune) || !KruneDirector.OwnMaster(krune) || KruneDirector.Pending) return false;
            return KruneTactics.MayUseHeldSpear(!krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard),
                KruneDirector.Plan(krune) != null, krune.Memory.Enemies.Any(e => InReach(krune, e.Unit)));
        }

        public override bool IsTargetValid(DecisionContext context)
        {
            return InReach(context.Unit, context.Target.Unit) && base.IsTargetValid(context);
        }

        public override UnitCommand CreateCommand(DecisionContext context, UnitEntityData target)
        {
            return IsAvailable(context) && InReach(context.Unit, target) ? UnitAttack.CreateAttackCommand(context.Unit, target) : null;
        }

        private static bool InReach(UnitEntityData krune, UnitEntityData target)
        {
            return target != null && target.IsInGame && target.Descriptor.State.IsConscious && krune.IsEnemy(target)
                && krune.Memory.ContainsVisible(target) && krune.DistanceTo(target) <= krune.Corpulence + target.Corpulence + (krune.Body.PrimaryHand.MaybeWeapon?.AttackRange.Meters ?? 0f);
        }

        public override Kingmaker.Controllers.Brain.Blueprints.TargetType GetTargetType(DecisionContext context)
        {
            return Kingmaker.Controllers.Brain.Blueprints.TargetType.Enemy;
        }

        public override float GetRange(DecisionContext context)
        {
            return context.Unit.Body.PrimaryHand.MaybeWeapon?.AttackRange.Meters ?? 1.5f;
        }

        public override float GetRadius(DecisionContext context)
        {
            return 0f;
        }
    }

    /// <summary>A directed cast is not replaced by the brain before it starts unless it has waited a full round (native rule then).</summary>
    public sealed class KwrDirectedCast : UnitUseAbility
    {
        private readonly TimeSpan m_Created = Game.Instance.TimeController.GameTime;

        internal readonly string PlanKey, TargetKey;
        internal string Combination => PlanKey + "@" + TargetKey;
        internal readonly DirectedCastLifecycle Lifecycle;
        private Vector3 m_StartPosition;
        internal readonly bool Relocation;
        internal bool RejectForProbe;
        internal double CreatedAt => m_Created.TotalSeconds;

        public KwrDirectedCast(AbilityData spell, TargetWrapper target, string planKey, string targetKey, bool relocation)
            : base(spell, target)
        {
            PlanKey = planKey;
            TargetKey = targetKey;
            Lifecycle = new DirectedCastLifecycle(spell);
            Relocation = relocation;
            // His long-distance teleports and phase passage can target unseen sanctum floor.
            if (KruneDirector.ThroughStone(spell))
            {
                NeedLoS = false;
            }
        }

        public override void Init(UnitEntityData executor)
        {
            Lifecycle.Initialize();
            base.Init(executor);
            if (Spell.Blueprint == KruneBlueprints.DimensionDoorFx) ApproachRadius = float.MaxValue;
            if (RejectForProbe && !IsFinished)
            {
                KruneAwakening.Note("SETUP/INJECTED: rejecting the quickened summon after native Init and the Extend offer, before Start.");
                Interrupt();
            }
        }

        protected override void OnStart()
        {
            Lifecycle.Start();
            m_StartPosition = Executor.Position;
            base.OnStart();
        }

        protected override ResultType OnAction()
        {
            var outer = KruneDirector.Executing;
            KruneDirector.Executing = this;
            try { return base.OnAction(); }
            finally { KruneDirector.Executing = outer; }
        }

        internal void Abandoned()
        {
            if (!IsFinished) Interrupt(raiseEvent: Executor != null);
        }

        public override bool AiCanInterrupt
        {
            get
            {
                if (!IsStarted && Game.Instance.TimeController.GameTime - m_Created < TimeSpan.FromSeconds(6d))
                {
                    return false;
                }
                return base.AiCanInterrupt;
            }
        }

        /// <summary>Cast, interrupted or replaced: a rod order made for this command ends with it.</summary>
        protected override void OnEnded(bool raiseEvent = true)
        {
            var terminal = CastResult.NativeFailure;
            try { terminal = Classify(); }
            finally
            {
                Lifecycle.End(terminal, Relocation, result => KruneDirector.Ended(this, result), () => base.OnEnded(raiseEvent));
            }
        }

        private CastResult Classify()
        {
            var failure = Result == ResultType.Interrupt ? CastResult.Interrupted : CastResult.NativeFailure;
            if (Executor == null) failure = CastResult.Rejected;
            else if (!Lifecycle.Committed)
            {
                if (IsStarted && Result == ResultType.Interrupt) failure = CastResult.Interrupted;
                else if (!Spell.CanTarget(Target)) failure = CastResult.TargetInvalid;
                else if (!IsStarted && !IsUnitEnoughClose) failure = CastResult.RangeInvalid;
                else if (!IsActed && Executor.CombatState.HasCooldownForCommand(this)) failure = CastResult.ActionUnavailable;
                else if (!Spell.IsAvailable) failure = CastResult.ResourceUnavailable;
                else if (!IsStarted) failure = CastResult.Rejected;
            }
            else if (Relocation && Result == ResultType.Success && Vector3.Distance(Executor.Position, Target.Point) <= 0.8f
                && Vector3.Distance(m_StartPosition, Executor.Position) > 1f && KruneMoves.LegalDestination(Executor.Position, Executor))
            {
                Lifecycle.Delivered();
            }
            return failure;
        }
    }
}
