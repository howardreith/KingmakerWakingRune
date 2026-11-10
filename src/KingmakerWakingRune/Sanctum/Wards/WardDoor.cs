using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.View.MapObjects;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// A2's conduit door: latch, leaf and feeder are saved separately. A touch springs the armed feeder once per action, and
    /// only a true brand, a force past the latch, or the leaf's destruction gets anyone through.
    /// </summary>
    internal static class WardDoor
    {
        private const string VampiricTouch = "6cbb040023868574b992677885390f92";

        private static SanctumDoor A2 => UnityEngine.Object.FindObjectsOfType<SanctumDoor>().FirstOrDefault(d => d.Plan.Id == "A2");

        private static bool HasBrand(UnitEntityData unit)
        {
            return unit.Descriptor.HasFact(WardBlueprints.SihedronBrand);
        }

        public static bool CanDisarm()
        {
            TrapObjectView trap = WardTraps.Feeder;
            return trap != null && trap.TrapActive && trap.Data.IsPerceptionCheckPassed && !WardRuntime.State.Door.Feeder.Disarmed;
        }

        public static void Disarm(UnitEntityData user)
        {
            WardTraps.Feeder?.Interact(user);
        }

        /// <summary>Direct body contact with the door.</summary>
        public static void Press(UnitEntityData user)
        {
            DoorWardState door = WardRuntime.State.Door;
            StrikeIfArmed(user);
            bool branded = HasBrand(user);
            if (door.Latched && branded)
            {
                door.Latched = false;
                Log.Info($"A2 latch released by {user.CharacterName}'s brand.");
            }
            if (door.Latched)
            {
                WardRuntime.Say("The door does not move.");
                return;
            }
            A2?.Open();
        }

        /// <summary>A brute Strength check is still body contact, so an armed feeder strikes first.</summary>
        public static void Force(UnitEntityData user)
        {
            DoorWardState door = WardRuntime.State.Door;
            StrikeIfArmed(user);
            UnitEntityData strongest = WardActions.Party().OrderByDescending(u => u.Stats.Strength.ModifiedValue).FirstOrDefault() ?? user;
            RuleSkillCheck check = Rulebook.Trigger(new RuleSkillCheck(strongest, StatType.Strength, WardProfile.DoorBreakDC));
            Log.Info($"A2 break: {strongest.CharacterName} Strength {check.RollResult} against DC {WardProfile.DoorBreakDC}.");
            if (check.IsPassed)
            {
                Destroy("forced open");
            }
            else
            {
                WardRuntime.Say("The door holds.");
            }
        }

        /// <summary>An armed feeder touches the one who contacted it, once for this action. Called from every contact path.</summary>
        public static void StrikeIfArmed(UnitEntityData victim)
        {
            SanctumState state = WardRuntime.State;
            FeederState feeder = state.Door.Feeder;
            if (state.Door.Destroyed || !FeederRules.IsArmed(feeder, WardRuntime.Now) || HasBrand(victim))
            {
                return;
            }
            UnitEntityData krune = WardFixtures.Krune;
            if (krune == null)
            {
                return;
            }
            int d20 = RulebookEvent.Dice.D20;
            int attack = d20 + WardProfile.FeederTouchBonus;
            int ac = Rulebook.Trigger(new RuleCalculateAC(krune, victim, AttackType.Touch)).TargetAC;
            bool hit = d20 == 20 || (d20 != 1 && attack >= ac);
            Log.Info($"Feeder touch on {victim.CharacterName}: d20 {d20} + {WardProfile.FeederTouchBonus} = {attack} vs touch AC {ac}: {(hit ? "hit" : "miss")}.");
            if (!hit)
            {
                feeder.TouchesMissed++;
                WardRuntime.Say($"The door's feeder lashes at {victim.CharacterName} and misses.");
                return;
            }
            feeder.TouchesHit++;
            if (Resisted(krune, victim))
            {
                Log.Info($"Feeder: {victim.CharacterName} resisted the touch with spell resistance.");
                WardRuntime.Say("The door's feeder gutters against spell resistance.");
                return;
            }
            int rolled = Rulebook.Trigger(new RuleRollDice(krune, new DiceFormula(WardProfile.FeederDice, DiceType.D6))).Result;
            int damage = FeederRules.EmpoweredDamage(rolled);
            int hitPoints = victim.Descriptor.HPLeft;
            int constitution = victim.Stats.Constitution.ModifiedValue;
            RuleDealDamage dealt = Rulebook.Trigger(new RuleDealDamage(krune, victim, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), damage))));
            int stolen = FeederRules.StolenAmount(dealt.Damage, hitPoints, constitution);
            FeederRules.Grant(feeder, stolen, WardRuntime.Now);
            WardKrune.Refresh(state, WardRuntime.Now);
            Log.Info($"Feeder: rolled 10d6={rolled}, empowered {damage}, dealt {dealt.Damage} to {victim.CharacterName}; stole {stolen} (cap {hitPoints} HP + {constitution} Con).");
            WardRuntime.Say($"Life drains out of {victim.CharacterName} and into the door.");
        }

        private static bool Resisted(UnitEntityData krune, UnitEntityData victim)
        {
            var blueprint = Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(VampiricTouch);
            var context = new MechanicsContext(krune, krune.Descriptor, blueprint);
            AccessTools.Field(typeof(MechanicsContext), "m_Params").SetValue(context, new AbilityParams { CasterLevel = WardProfile.FeederStoredCasterLevel });
            return Rulebook.Trigger(new RuleSpellResistanceCheck(context, victim)).IsSpellResisted;
        }

        /// <summary>The leaf's own damage track: 75 hit points, hardness 8 per hit, and a melee strike is a contact.</summary>
        public static void TakeHit(UnitEntityData attacker, int amount, bool melee)
        {
            SanctumState state = WardRuntime.State;
            DoorWardState door = state.Door;
            if (door.Destroyed)
            {
                return;
            }
            if (melee)
            {
                StrikeIfArmed(attacker);
            }
            int dealt = System.Math.Max(0, amount - WardProfile.DoorHardness);
            door.HitPoints -= dealt;
            Log.Info($"A2 leaf: {amount} - hardness {WardProfile.DoorHardness} = {dealt}; {door.HitPoints}/{WardProfile.DoorHitPoints} left.");
            if (door.HitPoints <= 0)
            {
                Destroy("battered down");
            }
        }

        private static void Destroy(string how)
        {
            DoorWardState door = WardRuntime.State.Door;
            door.Destroyed = true;
            door.Latched = false;
            Log.Info($"A2 door destroyed: {how}.");
            WardRuntime.Say("The stone door breaks and falls.");
            ApplyVisual();
        }

        /// <summary>A destroyed door has no leaf to touch; the opening stays open on every load.</summary>
        public static void ApplyVisual()
        {
            SanctumDoor door = A2;
            if (door == null || !WardRuntime.State.Door.Destroyed)
            {
                return;
            }
            if (!door.IsOpen)
            {
                door.OpenSilently();
            }
            door.HideLeaf();
        }
    }

    /// <summary>The soul emerald's physical route: hardness 20, 40 hit points, break DC 40, and awakening if it is destroyed.</summary>
    internal static class WardGem
    {
        public static void Force(UnitEntityData user)
        {
            UnitEntityData strongest = WardActions.Party().OrderByDescending(u => u.Stats.Strength.ModifiedValue).FirstOrDefault() ?? user;
            RuleSkillCheck check = Rulebook.Trigger(new RuleSkillCheck(strongest, StatType.Strength, WardProfile.GemBreakDC));
            Log.Info($"Emerald break: {strongest.CharacterName} Strength {check.RollResult} against DC {WardProfile.GemBreakDC}.");
            if (check.IsPassed)
            {
                Destroy();
            }
            else
            {
                WardRuntime.Say("The emerald holds firm in its setting.");
            }
        }

        public static void TakeHit(int amount)
        {
            GemState gem = WardRuntime.State.Gem;
            if (gem.Destroyed)
            {
                return;
            }
            int dealt = System.Math.Max(0, amount - WardProfile.GemHardness);
            gem.HitPoints -= dealt;
            Log.Info($"Emerald: {amount} - hardness {WardProfile.GemHardness} = {dealt}; {gem.HitPoints}/{WardProfile.GemHitPoints} left.");
            if (gem.HitPoints <= 0)
            {
                Destroy();
            }
        }

        private static void Destroy()
        {
            SanctumState state = WardRuntime.State;
            state.Gem.Destroyed = true;
            WardRuntime.Say("The emerald shatters, and something vast stirs in the sarcophagus.");
            WardRitual.Destroyed(state);
        }
    }
}
