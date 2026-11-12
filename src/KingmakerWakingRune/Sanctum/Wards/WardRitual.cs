using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>Vandiana's shared awareness and preparation, and the two printed clocks. Native buffs where the game has them.</summary>
    internal static class WardRitual
    {
        private const string CatsGraceBuff = "f011d0ab4a405e54aa0e83cd10e54430";
        private const string MageArmorBuff = "a92acdf18049d784eaa8f2004f5d2304";
        private const string FalseLifeBuff = "0fdb3cca6744fd94b9436459e6d9b947";
        private const double CatsGraceSeconds = 180d;

        private static BlueprintBuff Buff(string guid)
        {
            return ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(guid);
        }

        /// <summary>False Life and Mage Armor are her baseline for the ritual, not an alarm's reward. Applied once per save.</summary>
        public static void EnsureBaseline(SanctumState state)
        {
            if (state.Lashmistress.IsPresent)
            {
                // The real Vandiana's preparation is applied once by the encounter and never re-added here.
                return;
            }
            UnitEntityData vandiana = WardFixtures.Vandiana;
            if (vandiana == null || vandiana.Buffs.GetBuff(Buff(MageArmorBuff)) != null)
            {
                return;
            }
            vandiana.Descriptor.AddBuff(Buff(MageArmorBuff), vandiana);
            vandiana.Descriptor.AddBuff(Buff(FalseLifeBuff), vandiana);
            Log.Info("Vandiana baseline prepared: Mage Armor and False Life.");
        }

        public static void Raise(AwarenessCause cause, string detail)
        {
            SanctumState state = WardRuntime.State;
            bool first = RitualRules.RaiseAwareness(state.Ritual, cause, WardRuntime.Now);
            Log.Info($"Awareness {cause} ({detail}): first={first}; causes={string.Join(",", state.Ritual.Causes.Select(c => c.Cause.ToString()).ToArray())}.");
            if (first)
            {
                Log.Info("Vandiana warns her allies; Darkvision preparation stored for the encounter.");
                // The real defenders act on it with their own spell and potions; none of them is moved toward the cause.
                Lashmistress.LashmistressEncounter.OnAwareness(state);
            }
        }

        public static void AlarmAlert(string detail)
        {
            SanctumState state = WardRuntime.State;
            double now = WardRuntime.Now;
            if (state.Alarm.FirstAlertAt < 0d)
            {
                state.Alarm.FirstAlertAt = now;
            }
            state.Alarm.LastAlertAt = now;
            state.Alarm.Alerts++;
            Raise(AwarenessCause.Alarm, detail);
        }

        /// <summary>The east door really opened: she drinks her potion and the naga's preparation is recorded, once.</summary>
        public static void HearDoor()
        {
            SanctumState state = WardRuntime.State;
            if (!RitualRules.HearDoor(state.Ritual, WardRuntime.Now))
            {
                return;
            }
            if (Lashmistress.LashmistressEncounter.OnDoorHeard(state))
            {
                // Requested only: the potion and the naga's two castings happen as their own native actions.
                Log.Info("East door heard: Vandiana's potion and the naga's Invisibility requested from the real defenders.");
                return;
            }
            UnitEntityData vandiana = WardFixtures.Vandiana;
            if (vandiana != null)
            {
                vandiana.Descriptor.AddBuff(Buff(CatsGraceBuff), vandiana, TimeSpan.FromSeconds(CatsGraceSeconds));
            }
            Log.Info("East door heard: Vandiana drinks Cat's Grace; naga Invisibility intent recorded.");
        }

        public static void CommitConfrontation(UnitEntityData user)
        {
            SanctumState state = WardRuntime.State;
            int rolled = Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.RuleRollDice(user, new DiceFormula(1, DiceType.D6))).Result + 6;
            if (RitualRules.CommitConfrontation(state.Ritual, state.Door.Feeder, rolled, WardRuntime.Now))
            {
                Log.Info($"Ritual confrontation committed by {user.CharacterName}: 1d6+6 = {rolled}, acceleration {state.Ritual.AccelerationApplied}; due in {state.Ritual.ConfrontationDeadline - WardRuntime.Now:0} seconds.");
                WardRuntime.Say("You disturb the rite. The light in the west brightens at once.");
                Raise(AwarenessCause.Confrontation, "interrupt");
            }
            WardInteract.Refresh(WardText.ToolsId);
        }

        public static void Destroyed(SanctumState state)
        {
            if (RitualRules.RequestImmediateAwakening(state.Ritual, WardRuntime.Now))
            {
                Log.Info("Awakening due at once: the emerald was destroyed. One-round opening stagger stored for the encounter.");
                WardRuntime.Say("The ritual's light surges; Krune is waking.");
            }
        }

        public static void Tick(SanctumState state, double now)
        {
            if (RitualRules.Tick(state.Ritual, now))
            {
                Log.Info($"AWAKENING DUE at {state.Ritual.AwakeningAt:0}: {state.Ritual.Reason}.");
                WardRuntime.Say("The sarcophagus shudders, and the mist begins to glow. Krune is waking.");
            }
        }

        public static string AssessmentText(SanctumState state)
        {
            double? remaining = RitualRules.RemainingSeconds(state.Ritual, WardRuntime.Now);
            if (state.Ritual.AwakeningDue)
            {
                return "The ritual has run its course; the runelord's return is at hand.";
            }
            if (remaining == null)
            {
                return "The ritual is near completion, but nothing here lets you say how soon.";
            }
            int rounds = (int)Math.Ceiling(remaining.Value / WardProfile.RoundSeconds);
            return $"Krune's return is no more than moments off: {rounds} round{(rounds == 1 ? string.Empty : "s")} remain by your best reckoning.";
        }
    }
}
