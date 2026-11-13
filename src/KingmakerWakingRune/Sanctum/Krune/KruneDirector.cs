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
        private static KruneView s_LastView;
        private static double s_CachedAt = -1d;

        private static readonly string[] OwnBuffs = { "ForesightBuff", "StoneskinBuff", "TrueSeeingBuff", "MageShieldBuff" };
        private static readonly Dictionary<string, BlueprintBuff> s_Buffs = new Dictionary<string, BlueprintBuff>();

        public static bool Live(UnitEntityData krune)
        {
            KruneState state = WardRuntime.State?.Krune;
            return state != null && state.Stage == AwakeningStage.CombatReady && !KruneRules.CheckpointHolds(state) && krune != null && krune == KruneAwakening.Actor;
        }

        public static KrunePlan Plan(UnitEntityData krune)
        {
            double now = Game.Instance.TimeController.GameTime.TotalSeconds;
            if (s_Cached != null && Math.Abs(now - s_CachedAt) < 0.05d)
            {
                return s_Cached;
            }
            s_Cached = Decide(krune);
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
                return Idle("no foe within 40 m");
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

        private static KrunePlan Telekinesis(UnitEntityData krune, List<UnitEntityData> foes)
        {
            UnitEntityData adjacent = foes.Where(f => f.DistanceTo(krune) <= krune.Corpulence + f.Corpulence + 1.7f).OrderBy(f => f.DistanceTo(krune)).FirstOrDefault();
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
            TargetWrapper target = variant == KruneGear.WishSummon ? Aim(krune, foes, data, KruneAim.SummonPoint) : Aim(krune, foes, data, KruneAim.FoeCluster);
            return target != null && data.CanTarget(target) && InRange(krune, data, target) ? new KrunePlan { Ability = data, Target = target, Reason = reason, Key = variant.name } : null;
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

        private static List<UnitEntityData> Foes(UnitEntityData krune)
        {
            return Game.Instance.State.Units.Where(u => u.IsInGame && !u.Descriptor.State.IsDead && WardFixtures.IdOf(u) == null && krune.IsEnemy(u) && u.DistanceTo(krune) < 40f).ToList();
        }

        private static KruneView View(UnitEntityData krune, List<UnitEntityData> foes, KruneState state)
        {
            var v = new KruneView
            {
                HitPointFraction = krune.HPLeft / (double)Math.Max(1, krune.MaxHP),
                AdjacentFoes = foes.Count(f => f.DistanceTo(krune) <= krune.Corpulence + f.Corpulence + 1.7f),
                FoesWithin30 = foes.Count(f => f.DistanceTo(krune) <= 9.2f),
                FoeCasters = foes.Count(f => f.Descriptor.Spellbooks.Any(b => b.CasterLevel > 0)),
                UnseenFoeSuspected = !krune.Buffs.HasFact(KruneBlueprints.PermanentSeeInvisibility) && foes.Any(f => f.Descriptor.State.HasCondition(UnitCondition.Invisible) && f.DistanceTo(krune) <= 9.2f),
                SummonsAlive = Summons(krune).Count,
                HasForesight = HasBuff(krune, "ForesightBuff"),
                HasStoneskin = HasBuff(krune, "StoneskinBuff"),
                HasTrueSeeing = HasBuff(krune, "TrueSeeingBuff"),
                HasShield = HasBuff(krune, "MageShieldBuff"),
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
                if (s_Suppressed.TryGetValue(choice.Key, out double until) && WardRuntime.Now < until)
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
            return new KrunePlan { Ability = ability, Target = target, Reason = choice.Reason, Swift = swift, Key = choice.Key };
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
            Vector3 to = target.IsUnit ? target.Unit.Position : target.Point;
            return Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance == null || !Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(krune.EyePosition, to);
        }

        private static readonly Dictionary<string, double> s_Suppressed = new Dictionary<string, double>();
        private static string s_IssuedKey;
        private static double s_IssuedAt = -100d;
        private static int s_IssuedReady;

        /// <summary>A cast ordered more than a few seconds ago whose slot is still there did not go off: hold it back two rounds.</summary>
        private static void CheckLastCast(KruneView view)
        {
            if (s_IssuedKey == null || WardRuntime.Now - s_IssuedAt < 4d)
            {
                return;
            }
            int ready = view.Ready.TryGetValue(s_IssuedKey, out int n) ? n : 0;
            if (ready >= s_IssuedReady && ready > 0)
            {
                s_Suppressed[s_IssuedKey] = WardRuntime.Now + 12d;
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
                    UnitEntityData unseen = foes.FirstOrDefault(f => f.Descriptor.State.HasCondition(UnitCondition.Invisible));
                    return unseen != null ? new TargetWrapper(unseen.Position) : null;
                case KruneAim.FoeCluster:
                    float radius = Math.Max(ability.Blueprint.AoERadius.Meters, 10f.Feet().Meters);
                    bool poisonCloud = (ability.Blueprint.GetComponent<SpellDescriptorComponent>()?.Descriptor.Value & SpellDescriptor.Poison) != 0;
                    // Tentacles cannot hold a creature that cannot be held in place (freedom of movement): those do not count.
                    List<UnitEntityData> pool = ability.Blueprint.name == "KWR_KruneBlackTentacles" ? foes.Where(f => !f.Descriptor.State.HasConditionImmunity(UnitCondition.CantMove)).ToList() : foes;
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
                    float max = ability.Blueprint == KruneLocalSpells.PhaseDoorStep ? 40f.Feet().Meters : 0f;
                    Vector3? away = KruneMoves.Retreat(krune, foes, max);
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
            bool rod = (plan.Ability.Blueprint.name == "HorridWilting" || plan.Ability.Blueprint.name == "Cloudkill" || plan.Ability.Blueprint.name == "AcidFog") && plan.Ability.Spellbook != null && KwrKruneRod.Offer(krune, plan.Ability);
            KruneAwakening.Note($"director: {plan.Ability.Blueprint.name}{(plan.Ability.MetamagicData != null && plan.Ability.MetamagicData.NotEmpty ? " (" + plan.Ability.MetamagicData.MetamagicMask + ")" : string.Empty)} at {(plan.Target.IsUnit ? plan.Target.Unit.CharacterName : plan.Target.Point.ToString())}{(plan.Swift ? " (swift)" : string.Empty)}{(rod ? " with the rod's Empower" : string.Empty)}: {plan.Reason}.");
            return new KwrDirectedCast(plan.Ability, plan.Target);
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

    /// <summary>A directed cast is not replaced by the brain before it starts unless it has waited a full round (native rule then).</summary>
    public sealed class KwrDirectedCast : UnitUseAbility
    {
        private readonly TimeSpan m_Created = Game.Instance.TimeController.GameTime;

        public KwrDirectedCast(AbilityData spell, TargetWrapper target)
            : base(spell, target)
        {
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
    }
}
