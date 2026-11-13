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
        private static KruneView s_LastView;
        private static double s_CachedAt = -1d;

        /// <summary>
        /// Every transient decision belongs to one session of one Krune: cleared when a save loads (or the sanctum unloads),
        /// so a hold-back, an issued cast or a cached plan from another moment never carries over.
        /// </summary>
        public static void ResetSession()
        {
            s_Cached = null;
            s_CachedFor = string.Empty;
            s_LastView = null;
            s_CachedAt = -1d;
            s_Suppressed.Clear();
            s_IssuedKey = null;
            s_IssuedAt = -100d;
            s_IssuedReady = 0;
            s_LastIdle = string.Empty;
            s_LastIdleAt = -100d;
        }

        private static readonly string[] OwnBuffs = { "ForesightBuff", "StoneskinBuff", "TrueSeeingBuff", "MageShieldBuff" };
        private static readonly Dictionary<string, BlueprintBuff> s_Buffs = new Dictionary<string, BlueprintBuff>();

        /// <summary>Read only, for the harness: the session's transient decisions as they stand.</summary>
        public static string DebugState()
        {
            return $"issued={s_IssuedKey ?? "none"}@{s_IssuedAt:0.0} heldBack=[{string.Join(", ", s_Suppressed.Select(p => p.Key + " until " + p.Value.ToString("0.0")).ToArray())}] cachedFor={(s_CachedFor.Length > 0 ? s_CachedFor : "none")}@{s_CachedAt:0.0}";
        }

        public static bool Live(UnitEntityData krune)
        {
            KruneState state = WardRuntime.State?.Krune;
            return state != null && state.Stage == AwakeningStage.CombatReady && !KruneRules.CheckpointHolds(state) && krune != null && krune == KruneAwakening.Actor;
        }

        public static KrunePlan Plan(UnitEntityData krune)
        {
            double now = Game.Instance.TimeController.GameTime.TotalSeconds;
            if (s_Cached != null && s_CachedFor == krune?.UniqueId && Math.Abs(now - s_CachedAt) < 0.05d)
            {
                return s_Cached;
            }
            s_Cached = Decide(krune);
            s_CachedFor = krune?.UniqueId ?? string.Empty;
            s_CachedAt = now;
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
            List<UnitEntityData> foes = Foes(krune);
            if (foes.Count == 0)
            {
                return Idle("no foe he can see within 40 m");
            }
            KruneView view = View(krune, foes, state);
            CheckLastCast(view);
            s_LastView = view;
            KrunePhase next = KruneTactics.NextPhase(view);
            if (next != state.DirectorPhase)
            {
                KruneAwakening.Note($"director: phase {state.DirectorPhase} -> {next} (hp {view.HitPointFraction:0.00}, adjacent {view.AdjacentFoes}).");
                state.DirectorPhase = next;
                view.Phase = next;
            }
            Boots(krune);
            if (!krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Move) && KruneDancing.DroppedBeside(krune))
            {
                KrunePlan take = FactPlan(krune, KruneDancing.Grasp, krune, "take up his spear, dropped at his feet when its dance ended");
                if (take != null)
                {
                    return take;
                }
            }
            if (!krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift))
            {
                KrunePlan swift = First(krune, foes, KruneTactics.SwiftOptions(view), true) ?? Telekinesis(krune, foes);
                if (swift != null)
                {
                    return swift;
                }
            }
            if (krune.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard))
            {
                return null;
            }
            return GearFirst(krune, foes, view) ?? First(krune, foes, KruneTactics.StandardOptions(view), false) ?? GearLast(krune, foes, view) ?? Fallback(krune, foes) ?? Idle("nothing to cast: the native attack");
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
            if (view.HasPhaseDoor && KruneTactics.Overwhelmed(view))
            {
                KrunePlan step = PassagePlan(krune, foes);
                if (step != null)
                {
                    return step;
                }
            }
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
            return target != null && data.CanTarget(target) && InRange(krune, data, target) ? new KrunePlan { Ability = data, Target = target, Reason = "overwhelmed: through his phase door to another part of the sanctum", Key = data.Blueprint.name } : null;
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
            return target != null && data.CanTarget(target) && InRange(krune, data, target) ? new KrunePlan { Ability = data, Target = target, Reason = reason, Key = variant.name, Aim = aim } : null;
        }

        private static KrunePlan ItemPlan(UnitEntityData krune, Kingmaker.Blueprints.Items.BlueprintItem blueprint, TargetWrapper target, string reason)
        {
            Kingmaker.Items.ItemEntity item = KruneGear.Item(krune, blueprint);
            AbilityData data = item?.Ability?.Data;
            return data != null && item.Charges > 0 && data.IsAvailableForCast && data.CanTarget(target) ? new KrunePlan { Ability = data, Target = target, Reason = reason, Key = blueprint.name } : null;
        }

        private static KrunePlan FactPlan(UnitEntityData krune, BlueprintAbility blueprint, UnitEntityData target, string reason, bool swift = false)
        {
            Kingmaker.UnitLogic.Abilities.Ability fact = krune.Descriptor.Abilities.GetFact(blueprint) as Kingmaker.UnitLogic.Abilities.Ability;
            AbilityData data = fact?.Data;
            return data != null && data.IsAvailableForCast && data.CanTarget(target) && InRange(krune, data, target) ? new KrunePlan { Ability = data, Target = target, Reason = reason, Swift = swift, Key = blueprint.name } : null;
        }

        /// <summary>Control effects yield: no director decision while he is not his own master or cannot act.</summary>
        private static bool Free(UnitEntityData krune)
        {
            UnitState s = krune.Descriptor.State;
            return s.CanAct && !s.HasCondition(UnitCondition.Confusion) && !s.HasCondition(UnitCondition.Frightened) && !s.HasCondition(UnitCondition.Cowering)
                && krune.Descriptor.Faction == Game.Instance.BlueprintRoot.SystemMechanics.FactionMobs && krune.IsInCombat;
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
                TentaclesActive = OwnArea(krune, "KWR_KruneBlackTentaclesArea"),
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
                if (s_Suppressed.TryGetValue(choice.Key, out double until) && WardRuntime.Now < until && until - WardRuntime.Now <= SuppressSeconds)
                {
                    skipped.Add(choice.Key + " (held back after a cast that did not go off)");
                    continue;
                }
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
                Idle((swift ? "swift" : "standard") + " options not castable: " + string.Join("; ", skipped.ToArray()));
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
            return new KrunePlan { Ability = ability, Target = target, Reason = choice.Reason, Swift = swift, Key = choice.Key, Aim = choice.Aim };
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
            return ability.Blueprint == KruneLocalSpells.PhaseDoorStep || ability.Blueprint.name == "KWR_KruneGreaterTeleport";
        }

        /// <summary>He casts from where he stands: a target beyond the spell's range (this game's fixed ranges) is not chosen.</summary>
        private static bool InRange(UnitEntityData krune, AbilityData ability, TargetWrapper target)
        {
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

        private const double SuppressSeconds = 12d;
        private static readonly Dictionary<string, double> s_Suppressed = new Dictionary<string, double>();
        private static string s_IssuedKey;
        private static double s_IssuedAt = -100d;
        private static int s_IssuedReady;

        /// <summary>A cast ordered more than a few seconds ago whose slot is still there did not go off: hold it back two rounds.</summary>
        private static void CheckLastCast(KruneView view)
        {
            if (s_IssuedKey != null && WardRuntime.Now < s_IssuedAt)
            {
                // Time ran backwards (a load the session reset missed): the order is from another moment.
                s_IssuedKey = null;
            }
            if (s_IssuedKey == null || WardRuntime.Now - s_IssuedAt < 4d)
            {
                return;
            }
            int ready = view.Ready.TryGetValue(s_IssuedKey, out int n) ? n : 0;
            if (ready >= s_IssuedReady && ready > 0)
            {
                s_Suppressed[s_IssuedKey] = WardRuntime.Now + SuppressSeconds;
                KruneAwakening.Note($"director: {s_IssuedKey} was ordered but did not go off; held back for two rounds.");
            }
            s_IssuedKey = null;
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
            return chosen == null ? spell : new AbilityData(spell, chosen);
        }

        private static TargetWrapper Aim(UnitEntityData krune, List<UnitEntityData> foes, AbilityData ability, KruneAim aim)
        {
            UnitEntityData nearest = foes.OrderBy(f => f.DistanceTo(krune)).First();
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
                    // Tentacles cannot hold a creature that cannot be held in place (freedom of movement): those do not count.
                    List<UnitEntityData> pool = ability.Blueprint.name == "KWR_KruneBlackTentacles" ? foes.Where(f => !f.Descriptor.State.HasConditionImmunity(UnitCondition.CantMove)).ToList() : foes;
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
                    Vector3 front = krune.Position + (nearest.Position - krune.Position).normalized * 2f;
                    return new TargetWrapper(Kingmaker.View.ObstacleAnalyzer.GetNearestNode(front).clampedPosition);
                case KruneAim.Retreat:
                    // Within the spell's own reach (a phase door passage: 40 ft) and, except through stone, in his sight.
                    float span = ability.Blueprint.GetRange().Meters;
                    float max = ability.Blueprint == KruneLocalSpells.PhaseDoorStep ? 40f.Feet().Meters : span > 0f && span < 500f ? span - 0.5f : 0f;
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
            if (nearest.DistanceTo(krune) <= krune.Corpulence + nearest.Corpulence + 1.7f)
            {
                return null;
            }
            Kingmaker.UnitLogic.Abilities.Ability dart = krune.Descriptor.Abilities.Enumerable.FirstOrDefault(a => a.Blueprint.name == "ConjurationAcidDartAbility");
            if (dart != null && dart.Data.IsAvailableForCast && dart.Data.CanTarget(nearest))
            {
                return new KrunePlan { Ability = dart.Data, Target = nearest, Reason = "acid dart" };
            }
            Kingmaker.Items.ItemEntity wand = krune.Body.QuickSlots.Select(s => s.MaybeItem).FirstOrDefault(i => i != null && i.Blueprint == KruneBlueprints.WandOfAcidArrow && i.Charges > 0);
            AbilityData wandAbility = wand?.Ability?.Data;
            if (wandAbility != null && wandAbility.IsAvailableForCast && wandAbility.CanTarget(nearest))
            {
                return new KrunePlan { Ability = wandAbility, Target = nearest, Reason = "wand of acid arrow" };
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
            if (plan.Key != null)
            {
                s_IssuedKey = plan.Key;
                s_IssuedAt = WardRuntime.Now;
                s_IssuedReady = s_LastView != null && s_LastView.Ready.TryGetValue(plan.Key, out int ready) ? ready : 0;
            }
            s_Cached = null;
            var command = new KwrDirectedCast(plan.Ability, plan.Target);
            string rod = RuneRod.Offer(krune, plan.Ability, RodRequest(plan), command);
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
            return RodRules.KruneRequest(damaging, summons, RuneRod.Charges());
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
            return !context.Unit.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard);
        }

        public override UnitCommand CreateCommand(DecisionContext context, UnitEntityData target)
        {
            return UnitAttack.CreateAttackCommand(context.Unit, target);
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

        public KwrDirectedCast(AbilityData spell, TargetWrapper target)
            : base(spell, target)
        {
            // A phase door passage goes through stone: the only cast that needs no line of sight to its point.
            if (KruneDirector.ThroughStone(spell))
            {
                NeedLoS = false;
            }
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
            base.OnEnded(raiseEvent);
            RuneRod.CommandEnded(this);
        }
    }
}
