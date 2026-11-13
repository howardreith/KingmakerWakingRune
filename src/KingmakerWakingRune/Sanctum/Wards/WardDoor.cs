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
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.View.MapObjects;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// A2's conduit door: latch, leaf and feeder are saved separately. Every touch is one body's real action: an armed
    /// feeder strikes whoever made contact, and only that same body can finish what it started. A true sign (a marked
    /// cultist's own hand, or a precise impression of one) authorises that one contact and releases the latch.
    /// </summary>
    internal static class WardDoor
    {
        private const string VampiricTouch = "6cbb040023868574b992677885390f92";

        public static SanctumDoor A2 => UnityEngine.Object.FindObjectsOfType<SanctumDoor>().FirstOrDefault(d => d.Plan.Id == "A2");

        private static bool HasBrand(UnitEntityData unit)
        {
            return unit.Descriptor.HasFact(WardBlueprints.SihedronBrand);
        }

        private static bool Capable(UnitEntityData unit)
        {
            return unit != null && !unit.Descriptor.State.IsDead && unit.Descriptor.State.IsConscious && unit.Descriptor.State.CanAct;
        }

        public static bool IsClosed => A2 != null && !A2.IsOpen;

        public static bool CanDisarm()
        {
            TrapObjectView trap = WardTraps.Feeder;
            return trap != null && trap.TrapActive && trap.Data.IsPerceptionCheckPassed && !WardRuntime.State.Door.Feeder.Disarmed;
        }

        /// <summary>Orders one physical action at the leaf for this body; it resolves only once the body is there.</summary>
        public static void Order(UnitEntityData actor, string label, System.Func<UnitEntityData, bool> valid, System.Action<UnitEntityData> body)
        {
            UnitEntityData proxy = WardFixtures.Door;
            if (actor == null || proxy == null)
            {
                return;
            }
            KwrFixtureCommand.Issue(actor, KwrFixtureCommand.AtUnit(proxy, WardProfile.ContactReachMeters, label, false, valid, body));
        }

        public static void Disarm(UnitEntityData user)
        {
            WardTraps.Feeder?.Interact(user);
        }

        /// <summary>Bare-handed contact. The feeder strikes first; a survivor who still can then tries the leaf.</summary>
        public static void Press(UnitEntityData user)
        {
            DoorWardState door = WardRuntime.State.Door;
            if (door.Destroyed)
            {
                return;
            }
            StrikeIfArmed(user);
            if (!Capable(user))
            {
                return;
            }
            if (door.Latched && HasBrand(user))
            {
                door.Latched = false;
                Log.Info($"A2 latch released by {user.CharacterName}'s own brand.");
            }
            if (door.Latched)
            {
                WardRuntime.Say("The door does not move.");
                return;
            }
            A2?.Toggle(user);
        }

        /// <summary>
        /// One explicit presentation of a held credential by the body at the door: no feeder strike for this contact, the
        /// latch released, the real leaf opened. The trap, hardness and collision are untouched.
        /// </summary>
        public static void Present(UnitEntityData user, CredentialRecord credential)
        {
            SanctumState state = WardRuntime.State;
            if (state.Door.Destroyed || credential == null)
            {
                return;
            }
            CredentialRules.RecordPresentation(state.Credentials, user.UniqueId, credential, WardRuntime.Now);
            ReleaseAndOpen(user, credential.Kind == CredentialKind.Hand ? "the branded hand" : "the Sihedron impression");
            WardRuntime.Say(credential.Kind == CredentialKind.Hand
                ? "The dead palm meets the handprint. Something in the stone yields, and the door swings open."
                : "The impression settles into the handprint. Something in the stone yields, and the door swings open.");
        }

        /// <summary>A marked cultist's own hand at the door, on its way to warn Vandiana.</summary>
        public static void PresentOwnBrand(UnitEntityData cultist, string slot)
        {
            SanctumState state = WardRuntime.State;
            state.Credentials.Presentations.Add(new DoorPresentation { Actor = cultist.UniqueId, Credential = "OwnBrand", SourceSlot = slot, At = WardRuntime.Now });
            ReleaseAndOpen(cultist, "their own branded palm");
        }

        private static void ReleaseAndOpen(UnitEntityData user, string how)
        {
            DoorWardState door = WardRuntime.State.Door;
            door.Latched = false;
            Log.Info($"A2 latch released by {user.CharacterName} with {how}; feeder bypassed for this contact (armed={FeederRules.IsArmed(door.Feeder, WardRuntime.Now)}).");
            SanctumDoor a2 = A2;
            if (a2 != null && !a2.IsOpen)
            {
                a2.Toggle(user);
            }
        }

        /// <summary>A brute Strength check is body contact too: the feeder strikes, then the same body, if still able, heaves.</summary>
        public static void Force(UnitEntityData user)
        {
            DoorWardState door = WardRuntime.State.Door;
            if (door.Destroyed)
            {
                return;
            }
            StrikeIfArmed(user);
            if (!Capable(user))
            {
                Log.Info($"A2 break: {user.CharacterName} can no longer complete the attempt.");
                return;
            }
            // The printed break DC holds whatever global difficulty preset the game uses.
            RuleSkillCheck check = Rulebook.Trigger(new RuleSkillCheck(user, StatType.Strength, WardProfile.DoorBreakDC) { IgnoreDifficultyBonusToDC = true });
            Log.Info($"A2 break: {user.CharacterName} Strength {check.RollResult} (d20 {check.D20}) against DC {check.DC}.");
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
            WardKrune.Sync(state, WardRuntime.Now);
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

        /// <summary>
        /// One independently resolved hit on the leaf: hardness 8 once, unless the hit bypasses it, then the saved 75-point
        /// track. A melee strike is a contact with the armed leaf.
        /// </summary>
        public static void TakeHit(UnitEntityData attacker, int amount, bool melee, bool bypassHardness)
        {
            SanctumState state = WardRuntime.State;
            DoorWardState door = state.Door;
            if (door.Destroyed)
            {
                return;
            }
            if (melee && attacker != null)
            {
                StrikeIfArmed(attacker);
            }
            int dealt = DoorRules.HitDamage(amount, WardProfile.DoorHardness, bypassHardness);
            door.HitPoints = System.Math.Max(0, door.HitPoints - dealt);
            Log.Info($"A2 leaf hit by {attacker?.CharacterName}: {amount}{(bypassHardness ? " (bypasses hardness)" : $" - hardness {WardProfile.DoorHardness}")} = {dealt}; {door.HitPoints}/{WardProfile.DoorHitPoints} left.");
            if (door.HitPoints <= 0)
            {
                Destroy("battered down");
                return;
            }
            WardRuntime.Say(DoorRules.Describe(dealt, door.HitPoints, WardProfile.DoorHitPoints));
        }

        private static void Destroy(string how)
        {
            DoorWardState door = WardRuntime.State.Door;
            door.Destroyed = true;
            door.Latched = false;
            door.HitPoints = 0;
            Log.Info($"A2 door destroyed: {how}.");
            WardRuntime.Say("The stone door breaks and falls.");
            WardTraps.Feeder?.Deactivate();
            ApplyVisual();
        }

        /// <summary>
        /// A destroyed door has no leaf to touch or hit: the opening stays open on every load, its target leaves the game,
        /// and anyone still swinging at it stops.
        /// </summary>
        public static void ApplyVisual()
        {
            if (!WardRuntime.State.Door.Destroyed)
            {
                return;
            }
            UnitEntityData proxy = WardFixtures.Door;
            if (proxy != null)
            {
                foreach (UnitEntityData unit in Game.Instance.State.Units.Where(u => u.Commands.Raw.Any(c => c != null && c.TargetUnit == proxy)).ToList())
                {
                    unit.Commands.InterruptAll();
                }
                if (proxy.IsInGame)
                {
                    proxy.IsInGame = false;
                }
            }
            SanctumDoor door = A2;
            if (door == null)
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
        public static void Order(UnitEntityData actor)
        {
            UnitEntityData proxy = WardFixtures.Gem;
            if (actor != null && proxy != null)
            {
                KwrFixtureCommand.Issue(actor, KwrFixtureCommand.AtUnit(proxy, WardProfile.ContactReachMeters, "force the emerald", false, u => !WardRuntime.State.Gem.Destroyed, Force));
            }
        }

        private static void Force(UnitEntityData user)
        {
            RuleSkillCheck check = Rulebook.Trigger(new RuleSkillCheck(user, StatType.Strength, WardProfile.GemBreakDC) { IgnoreDifficultyBonusToDC = true });
            Log.Info($"Emerald break: {user.CharacterName} Strength {check.RollResult} (d20 {check.D20}) against DC {check.DC}.");
            if (check.IsPassed)
            {
                Destroy();
            }
            else
            {
                WardRuntime.Say("The emerald holds firm in its setting.");
            }
        }

        public static void TakeHit(int amount, bool bypassHardness)
        {
            GemState gem = WardRuntime.State.Gem;
            if (gem.Destroyed)
            {
                return;
            }
            // Adamantine ignores hardness below 20 only; the emerald's is exactly 20.
            int dealt = DoorRules.HitDamage(amount, WardProfile.GemHardness, false);
            gem.HitPoints = System.Math.Max(0, gem.HitPoints - dealt);
            Log.Info($"Emerald: {amount} - hardness {WardProfile.GemHardness} = {dealt}; {gem.HitPoints}/{WardProfile.GemHitPoints} left.");
            if (gem.HitPoints <= 0)
            {
                Destroy();
            }
            else if (dealt <= 0)
            {
                WardRuntime.Say("The blow leaves no more than a scratch.");
            }
        }

        private static void Destroy()
        {
            SanctumState state = WardRuntime.State;
            state.Gem.Destroyed = true;
            WardRuntime.Say("The emerald shatters, and something vast stirs in the sarcophagus.");
            UnitEntityData proxy = WardFixtures.Gem;
            if (proxy != null)
            {
                foreach (UnitEntityData unit in Game.Instance.State.Units.Where(u => u.Commands.Raw.Any(c => c != null && c.TargetUnit == proxy)).ToList())
                {
                    unit.Commands.InterruptAll();
                }
                proxy.IsInGame = false;
            }
            WardRuntime.ApplyEmeraldVisual(state);
            WardRitual.Destroyed(state);
        }
    }
}
