using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// Location wards, the glyph, the hound and the mist, driven from the shared clock against real unit positions. A
    /// per-unit entry memory is saved, so a reload never re-triggers a ward on someone already inside it.
    /// </summary>
    internal static class WardHazards
    {
        /// <summary>The native scroll whose spell carries the glyph's spell-resistance eligibility; the game has no Glyph of Warding.</summary>
        private const string LightningBoltScroll = "9d6d963e1a2d2474db1a23250abeb170";

        /// <summary>When each unit's current turn began, this session only; cleared on every load so no stale actor survives.</summary>
        private static readonly Dictionary<string, double> s_TurnStartedAt = new Dictionary<string, double>();

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

        public static bool AnyoneInMist()
        {
            return Actors().Any(u => WardGeometry.InMist(GridX(u.Position), GridY(u.Position)));
        }

        /// <summary>True while the active hound would notice someone of the party: long work beside it is refused.</summary>
        public static bool HoundWouldNotice()
        {
            SanctumState state = WardRuntime.State;
            return state != null && state.Hound.IsActive(WardRuntime.Now) && Game.Instance.Player.Party.Any(u => HoundNotices(u) && HoundFeet(u) <= WardProfile.HoundNoticeFeet);
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

        /// <summary>
        /// One committed detonation. Its source is Vandiana's divine CL 8 casting, so spell resistance, save bonuses against
        /// spells and resistances all read the right context. A worshipper's passage never sets it off, but a worshipper
        /// standing in someone else's blast is caught like anyone.
        /// </summary>
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
            MechanicsContext context = GlyphContext();
            foreach (UnitEntityData unit in Actors().ToList())
            {
                Vector3 p = unit.Position;
                if (WardGeometry.InGlyphBurst(GridX(p), GridY(p)))
                {
                    GlyphBurst(context, unit);
                }
            }
            Log.Info($"Glyph detonated at {WardRuntime.Now:0}.");
        }

        private static MechanicsContext GlyphContext()
        {
            UnitEntityData caster = WardFixtures.Vandiana ?? WardFixtures.Krune;
            BlueprintAbility spell = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipmentUsable>(LightningBoltScroll)?.Ability;
            var context = new MechanicsContext(caster, caster.Descriptor, spell);
            AccessTools.Field(typeof(MechanicsContext), "m_Params").SetValue(context, new AbilityParams { CasterLevel = WardProfile.DivineCasterLevel, DC = WardProfile.GlyphReflexDC, SpellLevel = 3 });
            return context;
        }

        private static void GlyphBurst(MechanicsContext context, UnitEntityData unit)
        {
            RuleSpellResistanceCheck resistance = Rulebook.Trigger(new RuleSpellResistanceCheck(context, unit));
            if (resistance.IsSpellResisted)
            {
                Log.Info($"Glyph: {unit.CharacterName} resists with spell resistance (CL {WardProfile.DivineCasterLevel}).");
                return;
            }
            RuleSavingThrow save = Rulebook.Trigger(new RuleSavingThrow(unit, SavingThrowType.Reflex, WardProfile.GlyphReflexDC) { Reason = context });
            if (save.IsPassed && save.Evasion)
            {
                Log.Info($"Glyph: {unit.CharacterName} evades (Reflex {save.RollResult}).");
                return;
            }
            var energy = new EnergyDamage(new DiceFormula(WardProfile.GlyphDice, DiceType.D8), DamageEnergyType.Electricity);
            energy.Half = save.IsPassed || save.ImprovedEvasion;
            RuleDealDamage damage = Rulebook.Trigger(new RuleDealDamage(context.MaybeCaster, unit, new DamageBundle(energy)) { Reason = context });
            Log.Info($"Glyph: {unit.CharacterName} Reflex {save.RollResult} vs {WardProfile.GlyphReflexDC}; took {damage.Damage} electricity from {context.MaybeCaster?.CharacterName}.");
        }

        private static Vector3 HoundWorld => new Vector3(SanctumPlan.ToWorldX(WardGeometry.HoundPoint.X), SanctumPlan.HeightAt(WardGeometry.HoundPoint.X, WardGeometry.HoundPoint.Y) ?? 0f, SanctumPlan.ToWorldZ(WardGeometry.HoundPoint.Y));

        private static float HoundFeet(UnitEntityData unit)
        {
            Vector3 point = HoundWorld;
            return new Vector2(unit.Position.x - point.x, unit.Position.z - point.z).magnitude / SanctumPlan.MetersPerFoot;
        }

        /// <summary>
        /// Whom the sentry reacts to: a Small or larger creature that is not one of Vandiana's own, in the hound's own chamber
        /// with no wall between. It sees invisible creatures, so invisibility hides no one from it.
        /// </summary>
        private static bool HoundNotices(UnitEntityData unit)
        {
            if (unit == null || !unit.IsInGame || unit.Descriptor.State.IsDead || WardFixtures.IdOf(unit) != null || IsLissalan(unit) || unit.Descriptor.State.Size < Size.Small)
            {
                return false;
            }
            Vector3 hound = HoundWorld;
            if (SanctumPlan.SpaceAt(GridX(unit.Position), GridY(unit.Position)) != SanctumPlan.SpaceAt(WardGeometry.HoundPoint.X, WardGeometry.HoundPoint.Y) || Mathf.Abs(unit.Position.y - hound.y) > 2.5f)
            {
                return false;
            }
            var eye = new Vector3(0f, 1f, 0f);
            return !Kingmaker.Visual.FogOfWar.LineOfSightGeometry.Instance.HasObstacle(hound + eye, unit.Position + eye, 0);
        }

        private static void TickHound(SanctumState state, double now)
        {
            HoundState hound = state.Hound;
            if (!hound.IsActive(now))
            {
                return;
            }
            // The spell ends if its caster is ever more than 100 feet away.
            UnitEntityData vandiana = WardFixtures.Vandiana;
            if (vandiana != null && HoundFeet(vandiana) > WardProfile.HoundCasterSeparationFeet)
            {
                hound.Dispelled = true;
                Log.Info("Hound ended: its caster is more than 100 feet away.");
                return;
            }
            foreach (UnitEntityData unit in Actors().Where(HoundNotices).ToList())
            {
                float feet = HoundFeet(unit);
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

        /// <summary>A native natural bite, used only so the attack roll has a real melee weapon; its own damage is not used.</summary>
        private const string Bite2d6 = "2abc1dc6172759c42971bd04b8c115cb";

        /// <summary>
        /// The printed bite, resolved by the native attack roll: an invisible attacker (the hound is invisible for the attack
        /// itself, so it stays targetable once found), so a target that cannot see invisible is flat-footed and grants +2;
        /// concealment, mirror images, natural 1 and 20 and critical confirmation are all native. The attack bonus is held at
        /// the printed +10 before those situational terms; damage is the printed 2d6+3 piercing, magic for damage reduction.
        /// </summary>
        private static void Bite(UnitEntityData unit)
        {
            UnitEntityData source = WardFixtures.Find("Hound") ?? WardFixtures.Krune;
            Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon blueprint = ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(Bite2d6);
            if (source == null || blueprint == null)
            {
                Log.Error($"Hound bite unavailable: source {source?.CharacterName}, weapon {blueprint?.name}.", null);
                return;
            }
            var weapon = new Kingmaker.Items.ItemEntityWeapon(blueprint);
            // The fixture's own scores are an object's: replace its whole unconditional bonus with the printed one.
            int own = Rulebook.Trigger(new RuleCalculateAttackBonusWithoutTarget(source, weapon, 0)).Result;
            bool wasInvisible = source.Descriptor.State.HasCondition(UnitCondition.Invisible);
            if (!wasInvisible)
            {
                source.Descriptor.State.AddCondition(UnitCondition.Invisible);
            }
            RuleAttackRoll roll;
            try
            {
                roll = Rulebook.Trigger(new RuleAttackRoll(source, unit, weapon, own - WardProfile.HoundBiteBonus));
            }
            finally
            {
                if (!wasInvisible)
                {
                    source.Descriptor.State.RemoveCondition(UnitCondition.Invisible);
                }
            }
            Log.Info($"Hound bite on {unit.CharacterName}: d20 {(int)roll.Roll} + {roll.AttackBonus} vs AC {roll.TargetAC} (flat-footed {roll.IsTargetFlatFooted}): {roll.Result}.");
            if (!roll.IsHit)
            {
                return;
            }
            var bite = new PhysicalDamage(new DiceFormula(WardProfile.HoundBiteDice, DiceType.D6), PhysicalDamageForm.Piercing);
            bite.AddBonus(WardProfile.HoundBiteDamageBonus);
            // Treated as magic for damage reduction, as the spell's bite is.
            bite.Enchantment = 1;
            bite.EnchantmentTotal = 1;
            if (roll.IsCriticalConfirmed)
            {
                bite.CriticalModifier = 2;
            }
            Rulebook.Trigger(new RuleDealDamage(source, unit, new DamageBundle(bite)) { Reason = new RuleReason(roll) });
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
            bool sleeping = unit.Buffs.GetBuff(WardBlueprints.MistSleep) != null;
            if (record.Asleep && !sleeping)
            {
                // Ended by something other than this mod's wake (that path resets the record itself): still a real wake,
                // so a later sleep needs a fresh three failures. The removal path was logged when it happened.
                s_SleepRemovals.TryGetValue(unit.UniqueId, out string path);
                Log.Info($"Mist: {unit.CharacterName} was recorded asleep but carries no mist sleep now; removed via {path ?? "no observed removal (never applied or lost on load)"}; treated as woken.");
                MistRules.Wake(record);
            }
            s_SleepRemovals.Remove(unit.UniqueId);
            record.Asleep = sleeping;
            RuleSavingThrow save = MistSave(unit, state);
            bool sleepImmune = unit.Descriptor.State.HasConditionImmunity(UnitCondition.Sleeping);
            MistExposure result = MistRules.EndRoundInside(record, immune, save.IsPassed, sleepImmune);
            record.Exposed = true;
            if (result != MistExposure.NoEffect)
            {
                Log.Info($"Mist: {unit.CharacterName} Will {save.RollResult} (d20 {save.D20}) vs {WardProfile.MistSaveDC}: {result}; streak {record.ConsecutiveFailures}.");
            }
            if (!ApplyExposure(unit, result) && result == MistExposure.FellAsleep)
            {
                // The native apply rule refused it (an immunity): the creature never slept, so the record must not say so.
                record.Asleep = false;
                Log.Info($"Mist: {unit.CharacterName} did not fall asleep; the sleep effect was refused natively.");
            }
        }

        /// <summary>The call path of each mist sleep removal not yet seen by a round, from the removal trace.</summary>
        private static readonly Dictionary<string, string> s_SleepRemovals = new Dictionary<string, string>();

        public static void NoteSleepRemoval(string unitId, string path)
        {
            s_SleepRemovals[unitId] = path;
        }

        /// <summary>
        /// The native Will save. Krune's own summons carry a typed +4 resistance bonus for it, so the native stacking rules
        /// keep only the larger of it and any resistance bonus they already have; natural 1 and 20 stay native.
        /// </summary>
        private static RuleSavingThrow MistSave(UnitEntityData unit, SanctumState state)
        {
            ModifiableValue.Modifier bonus = null;
            if (IsKruneSummon(unit, state))
            {
                bonus = unit.Stats.SaveWill.AddModifier(WardProfile.MistKruneSummonResistance, null, ModifierDescriptor.Resistance);
            }
            try
            {
                return Rulebook.Trigger(new RuleSavingThrow(unit, SavingThrowType.Will, WardProfile.MistSaveDC));
            }
            finally
            {
                bonus?.Remove();
            }
        }

        private static bool IsKruneSummon(UnitEntityData unit, SanctumState state)
        {
            UnitPartSummonedMonster summon = unit.Get<UnitPartSummonedMonster>();
            return summon != null && ((summon.Summoner != null && summon.Summoner == WardFixtures.Krune) || state.KruneSummons.Contains(unit.UniqueId));
        }

        /// <summary>False when the native rule refused the effect.</summary>
        private static bool ApplyExposure(UnitEntityData unit, MistExposure result)
        {
            if (result == MistExposure.Staggered)
            {
                return unit.Descriptor.AddBuff(WardBlueprints.MistStagger, unit, System.TimeSpan.FromSeconds(WardProfile.RoundSeconds)) != null;
            }
            if (result == MistExposure.FellAsleep && unit.Buffs.GetBuff(WardBlueprints.MistSleep) == null)
            {
                return unit.Descriptor.AddBuff(WardBlueprints.MistSleep, unit) != null;
            }
            return true;
        }

        public static void ForgetTurns()
        {
            s_TurnStartedAt.Clear();
        }

        public static void TurnStarted(UnitEntityData unit)
        {
            if (unit != null && SanctumScope.IsSanctumLoaded)
            {
                s_TurnStartedAt[unit.UniqueId] = WardRuntime.Now;
            }
        }

        /// <summary>
        /// A turn really ended, from the native turn controller: the actor's own completed turn, the final turn when combat
        /// ends, or a switch out of turn-based mode. A delayed turn is not an end. One exposure per turn, never twice.
        /// </summary>
        public static void TurnEnded(UnitEntityData unit, bool completed)
        {
            // Area first: a turn anywhere else in the campaign must not even look for scenario state.
            if (!SanctumScope.IsSanctumLoaded || Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess)
            {
                return;
            }
            SanctumState state = WardRuntime.State;
            if (state == null || !state.Initialized || unit == null || !unit.IsInGame || unit.Descriptor.State.IsDead || WardFixtures.IdOf(unit) != null)
            {
                return;
            }
            bool stillTurnBased = Game.Instance.Player.IsInCombat && TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
            if (!completed && stillTurnBased)
            {
                return;
            }
            MistRecord record = state.MistOf(unit.UniqueId);
            double lastTick = record.NextDueAt - WardProfile.RoundSeconds;
            if (s_TurnStartedAt.TryGetValue(unit.UniqueId, out double started) && record.NextDueAt >= 0d && lastTick >= started)
            {
                Log.Info($"Turn end for {unit.CharacterName}: already exposed this turn; no second tick.");
                return;
            }
            s_TurnStartedAt.Remove(unit.UniqueId);
            EndOfRound(unit, record, state, WardRuntime.Now);
        }
    }

    /// <summary>Records turn starts for the hazards; turn ends come from the native turn controller's disposal.</summary>
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
