using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.TurnBasedMode.Controllers;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Parts;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// Location wards, the glyph, the hound and the mist, driven from the shared clock against real unit positions. A
    /// per-unit entry memory is saved, so a reload never re-triggers a ward on someone already inside it.
    /// </summary>
    internal static class WardHazards
    {
        private static UnitEntityData s_LastTurnUnit;

        private static IEnumerable<UnitEntityData> Actors()
        {
            return Game.Instance.State.Units.Where(u => u.IsInGame && !u.Descriptor.State.IsDead && WardFixtures.IdOf(u) == null);
        }

        private static float GridX(Vector3 p)
        {
            return p.x / SanctumPlan.MetersPerSquare + SanctumPlan.OriginX;
        }

        private static float GridY(Vector3 p)
        {
            return SanctumPlan.OriginY - p.z / SanctumPlan.MetersPerSquare;
        }

        public static void Tick(SanctumState state, double now)
        {
            TickAlarm(state, now);
            TickGlyph(state, now);
            TickHound(state, now);
            if (!TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
            {
                foreach (UnitEntityData unit in Actors().ToList())
                {
                    MistRecord record = state.MistOf(unit.UniqueId);
                    if (MistRules.IsDue(record, now))
                    {
                        EndOfRound(unit, record, state, now);
                    }
                }
            }
        }

        /// <summary>A save that entered before these wards existed: nobody already inside one may trigger it by standing still.</summary>
        public static void SeedLegacyOccupants(SanctumState state)
        {
            foreach (UnitEntityData unit in Actors().ToList())
            {
                Vector3 p = unit.Position;
                if (WardGeometry.InAlarmZone(GridX(p), GridY(p), p.y))
                {
                    state.AlarmInside.Add(unit.UniqueId);
                }
                if (WardGeometry.InGlyphTrigger(GridX(p), GridY(p)))
                {
                    state.GlyphInside.Add(unit.UniqueId);
                }
            }
        }

        private static void TickAlarm(SanctumState state, double now)
        {
            if (!state.Alarm.IsActive(now))
            {
                return;
            }
            foreach (UnitEntityData unit in Actors().ToList())
            {
                Vector3 p = unit.Position;
                bool inside = WardGeometry.InAlarmZone(GridX(p), GridY(p), p.y);
                if (TransitionEntered(state.AlarmInside, unit.UniqueId, inside))
                {
                    WardRitual.AlarmAlert(unit.CharacterName + " entered the warded stair");
                    Log.Info($"Alarm: {unit.CharacterName} entered; alert {state.Alarm.Alerts}.");
                }
            }
        }

        private static void TickGlyph(SanctumState state, double now)
        {
            GlyphState glyph = state.Glyph;
            if (glyph.Triggered || glyph.Disarmed || glyph.Dispelled)
            {
                return;
            }
            foreach (UnitEntityData unit in Actors().ToList())
            {
                Vector3 p = unit.Position;
                bool inside = WardGeometry.InGlyphTrigger(GridX(p), GridY(p));
                if (TransitionEntered(state.GlyphInside, unit.UniqueId, inside) && !IsLissalan(unit))
                {
                    DetonateGlyph(unit.Position);
                    WardRitual.Raise(AwarenessCause.Glyph, unit.CharacterName + " set off the glyph");
                    return;
                }
            }
        }

        /// <summary>True on the single tick a unit first crosses in. Leaving and returning counts again, which is the spell's rule.</summary>
        private static bool TransitionEntered(List<string> inside, string id, bool isInside)
        {
            bool was = inside.Contains(id);
            if (isInside && !was)
            {
                inside.Add(id);
                return true;
            }
            if (!isInside && was)
            {
                inside.Remove(id);
            }
            return false;
        }

        private static bool IsLissalan(UnitEntityData unit)
        {
            return unit.Descriptor.HasFact(WardBlueprints.LissalaWorshipper);
        }

        public static void DetonateGlyph(Vector3? at)
        {
            SanctumState state = WardRuntime.State;
            GlyphState glyph = state.Glyph;
            if (glyph.Triggered)
            {
                return;
            }
            // Committed before any damage, so two arrivals in one tick cannot set it off twice.
            glyph.Triggered = true;
            glyph.TriggeredAt = WardRuntime.Now;
            WardTraps.Glyph?.Deactivate();
            UnitEntityData source = WardFixtures.Krune;
            foreach (UnitEntityData unit in Actors().ToList())
            {
                Vector3 p = unit.Position;
                if (WardGeometry.InGlyphBurst(GridX(p), GridY(p)))
                {
                    GlyphBurst(source, unit);
                }
            }
            Log.Info($"Glyph detonated at {WardRuntime.Now:0}.");
        }

        private static void GlyphBurst(UnitEntityData source, UnitEntityData unit)
        {
            RuleSavingThrow save = Rulebook.Trigger(new RuleSavingThrow(unit, SavingThrowType.Reflex, WardProfile.GlyphReflexDC));
            if (save.IsPassed && save.Evasion)
            {
                Log.Info($"Glyph: {unit.CharacterName} evades (Reflex {save.RollResult}).");
                return;
            }
            var energy = new EnergyDamage(new DiceFormula(WardProfile.GlyphDice, DiceType.D8), DamageEnergyType.Electricity);
            energy.Half = save.IsPassed || save.ImprovedEvasion;
            RuleDealDamage damage = Rulebook.Trigger(new RuleDealDamage(source, unit, new DamageBundle(energy)));
            Log.Info($"Glyph: {unit.CharacterName} Reflex {save.RollResult} vs {WardProfile.GlyphReflexDC}; took {damage.Damage} electricity.");
        }

        private static void TickHound(SanctumState state, double now)
        {
            HoundState hound = state.Hound;
            if (!hound.IsActive(now))
            {
                return;
            }
            var point = new Vector3(SanctumPlan.ToWorldX(WardGeometry.HoundPoint.X), 0f, SanctumPlan.ToWorldZ(WardGeometry.HoundPoint.Y));
            foreach (UnitEntityData unit in Actors().ToList())
            {
                float distance = new Vector2(unit.Position.x - point.x, unit.Position.z - point.z).magnitude;
                float feet = distance / SanctumPlan.MetersPerFoot;
                if (feet <= WardProfile.HoundNoticeFeet && !hound.BarkedAt.Contains(unit.UniqueId))
                {
                    hound.BarkedAt.Add(unit.UniqueId);
                    Bark(state, now, unit);
                }
                if (feet <= WardProfile.HoundBiteFeet && (hound.LastBiteAt < 0d || now - hound.LastBiteAt >= WardProfile.RoundSeconds))
                {
                    hound.LastBiteAt = now;
                    Bite(unit);
                    return;
                }
            }
        }

        private static void Bark(SanctumState state, double now, UnitEntityData unit)
        {
            if (state.Hound.FirstBarkAt < 0d)
            {
                state.Hound.FirstBarkAt = now;
                state.Hound.DischargedExpiresAt = now + WardProfile.HoundDischargedSeconds;
                WardRuntime.Say("Something unseen bays in the eastern dark.");
            }
            WardRitual.Raise(AwarenessCause.Hound, unit.CharacterName + " approached the hound");
        }

        private static void Bite(UnitEntityData unit)
        {
            UnitEntityData source = WardFixtures.Find("Hound") ?? WardFixtures.Krune;
            int d20 = RulebookEvent.Dice.D20;
            int attack = d20 + WardProfile.HoundBiteBonus;
            int ac = Rulebook.Trigger(new RuleCalculateAC(source, unit, Kingmaker.RuleSystem.AttackType.Melee)).TargetAC;
            bool hit = d20 == 20 || (d20 != 1 && attack >= ac);
            Log.Info($"Hound bite on {unit.CharacterName}: d20 {d20} + {WardProfile.HoundBiteBonus} = {attack} vs AC {ac}: {(hit ? "hit" : "miss")}.");
            if (!hit)
            {
                return;
            }
            var bite = new PhysicalDamage(new DiceFormula(WardProfile.HoundBiteDice, DiceType.D6), PhysicalDamageForm.Piercing);
            bite.AddBonus(WardProfile.HoundBiteDamageBonus);
            // Treated as magic for damage reduction, as the spell's bite is.
            bite.Enchantment = 1;
            bite.EnchantmentTotal = 1;
            Rulebook.Trigger(new RuleDealDamage(source, unit, new DamageBundle(bite)));
        }

        /// <summary>One native save per creature per round in the mist, whichever clock delivered the round.</summary>
        private static void EndOfRound(UnitEntityData unit, MistRecord record, SanctumState state, double now)
        {
            MistRules.Schedule(record, now);
            Vector3 p = unit.Position;
            if (!WardGeometry.InMist(GridX(p), GridY(p)))
            {
                MistRules.EndRoundOutside(record, now);
                return;
            }
            bool immune = unit == WardFixtures.Krune;
            int bonus = KruneSummonBonus(unit);
            RuleSavingThrow save = Rulebook.Trigger(new RuleSavingThrow(unit, SavingThrowType.Will, WardProfile.MistSaveDC));
            bool passed = save.IsPassed || (bonus > 0 && save.RollResult + bonus >= WardProfile.MistSaveDC && save.D20 != 1);
            bool sleepImmune = unit.Descriptor.State.HasConditionImmunity(UnitCondition.Sleeping);
            MistExposure result = MistRules.EndRoundInside(record, immune, passed, sleepImmune);
            record.Exposed = true;
            Log.Info($"Mist: {unit.CharacterName} Will {save.RollResult} vs {WardProfile.MistSaveDC}: {result}; streak {record.ConsecutiveFailures}.");
            ApplyExposure(unit, result);
        }

        private static void ApplyExposure(UnitEntityData unit, MistExposure result)
        {
            if (result == MistExposure.Staggered)
            {
                unit.Descriptor.AddBuff(WardBlueprints.MistStagger, unit, System.TimeSpan.FromSeconds(WardProfile.RoundSeconds));
            }
            else if (result == MistExposure.FellAsleep)
            {
                unit.Descriptor.AddBuff(WardBlueprints.MistSleep, unit);
            }
        }

        /// <summary>A real resistance bonus to these saves for Krune's own summons only, topped up against any larger one.</summary>
        private static int KruneSummonBonus(UnitEntityData unit)
        {
            UnitPartSummonedMonster summon = unit.Get<UnitPartSummonedMonster>();
            return summon != null && summon.Summoner == WardFixtures.Krune ? MistRules.KruneSummonResistanceBonus(0) : 0;
        }

        /// <summary>Turn-based: the previous unit's turn is over the moment the next begins, so it closes here exactly once.</summary>
        public static void TurnStarted(UnitEntityData next)
        {
            SanctumState state = WardRuntime.State;
            UnitEntityData ended = s_LastTurnUnit;
            s_LastTurnUnit = next;
            if (state == null || ended == null || !ended.IsInGame || WardFixtures.IdOf(ended) != null || !SanctumScope.IsSanctumLoaded)
            {
                return;
            }
            EndOfRound(ended, state.MistOf(ended.UniqueId), state, WardRuntime.Now);
        }
    }

    /// <summary>Hands turn-based turn starts to the hazards; this is the only reliable end-of-turn signal the game offers.</summary>
    internal sealed class WardTurnHandler : ITurnBasedModeHandler
    {
        public void HandleSurpriseRoundStarted()
        {
        }

        public void HandleRoundStarted(int round)
        {
        }

        public void HandleTurnStarted(UnitEntityData unit)
        {
            WardHazards.TurnStarted(unit);
        }

        public void HandleUnitControlChanged(UnitEntityData unit)
        {
        }

        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck check)
        {
        }
    }
}
