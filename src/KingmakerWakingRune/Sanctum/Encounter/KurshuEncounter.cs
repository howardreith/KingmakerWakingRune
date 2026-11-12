using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Brain;
using Kingmaker.Controllers.Brain.Blueprints.Considerations;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Encounter
{
    /// <summary>The brain's question to her tactics, answered from the live field. Selection lives here; native commands act.</summary>
    public sealed class KwrKurshuConsideration : Consideration
    {
        public KurshuAim Aim;
        public bool OnTarget;

        public override float Score(DecisionContext context)
        {
            try
            {
                return KurshuEncounter.Score(Aim, OnTarget, context);
            }
            catch (Exception ex)
            {
                Log.Error($"Kurshu consideration {Aim} failed.", ex);
                return 0f;
            }
        }
    }

    /// <summary>
    /// Kurshu's encounter at play: she appears only once the party stands in A1 with control restored, re-binds on every load,
    /// and reports her wounds, defeat and death once each. The fight itself is native: her own brain, rolls and commands.
    /// </summary>
    internal static class KurshuEncounter
    {
        /// <summary>The apex rune's centre (grid 13, 19.6) at the apex height: she is coiled around it.</summary>
        private static readonly Vector3 Apex = new Vector3(SanctumPlan.ToWorldX(13f), SanctumPlan.ApexHeight, SanctumPlan.ToWorldZ(19.6f));
        private const float FeedPointMeters = 1.5f;
        private const float FeedScanMeters = 20f;
        private const float TeleportClearanceMeters = 6f;

        private static UnitEntityData s_Bound;
        private static bool s_Logged;
        private static bool s_ProblemReported;
        private static bool s_Precasting;
        private static bool s_DirectorCommand;
        private static int s_TeleportRound = -1;
        private static double s_DirectorIssuedAt;
        private static double s_LastComaCheck = -1d;
        private static string s_DirectorWhat;
        private static string s_DirectorTarget;
        private static readonly Dictionary<string, double> s_RetryAfter = new Dictionary<string, double>();

        private static SanctumState State => WardRuntime.State;

        /// <summary>Her entity in this game, or null if she has not appeared (or never will in this save).</summary>
        public static UnitEntityData Kurshu
        {
            get
            {
                SanctumState state = State;
                if (state == null || string.IsNullOrEmpty(state.Kurshu.EntityId))
                {
                    return null;
                }
                if (s_Bound == null || s_Bound.UniqueId != state.Kurshu.EntityId || s_Bound.Descriptor == null)
                {
                    s_Bound = Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == state.Kurshu.EntityId)
                        ?? Game.Instance.State.Units.FirstOrDefault(u => u.Descriptor.Get<KurshuPart>()?.EncounterId == KurshuProfile.EncounterId);
                }
                return s_Bound;
            }
        }

        public static void Note(string text)
        {
            Log.Info($"Kurshu [{WardRuntime.Now:0.0}]: {text}");
        }

        /// <summary>Every load: nothing is spawned, refilled or repeated here. Saved uses and levels are pressed onto the unit.</summary>
        public static void AfterLoad(SanctumState state)
        {
            s_Bound = null;
            s_Logged = false;
            s_Precasting = false;
            s_DirectorCommand = false;
            s_TeleportRound = -1;
            s_RetryAfter.Clear();
            KurshuState kurshu = state.Kurshu;
            if (kurshu.Policy == KurshuPolicy.LegacyNotInserted)
            {
                Note($"legacy save: encounter not inserted (notice shown before: {kurshu.LegacyNoticeShown}).");
            }
            UnitEntityData unit = Kurshu;
            if (unit != null)
            {
                Reconcile(kurshu, unit);
                EnsureNature(kurshu, unit);
                if (!unit.Descriptor.State.IsDead)
                {
                    CheckHold(kurshu, unit);
                }
                // The brain is hers again after a load, unless the unit is still waiting for its first, prepared turn.
                unit.IsBrainActive = kurshu.HiddenCheckDone;
                Note($"re-bound after load: {unit.UniqueId} hp {unit.Descriptor.HPLeft}/{unit.Descriptor.MaxHP} defeated={kurshu.Defeated} slain={kurshu.Slain} separation={kurshu.SeparationLevels}.");
            }
            else if (!string.IsNullOrEmpty(kurshu.EntityId))
            {
                Note($"saved entity {kurshu.EntityId} is not in this area; nothing is respawned (defeated={kurshu.Defeated}, slain={kurshu.Slain}).");
            }
        }

        public static void Tick(SanctumState state, double now)
        {
            if (Math.Abs(now - s_LastComaCheck) >= 1d && KurshuBlueprints.MindDrained != null)
            {
                // A helpless creature's own round can be skipped (turn-based combat), so recovery is also checked from here.
                s_LastComaCheck = now;
                foreach (UnitEntityData creature in Game.Instance.State.Units.ToList())
                {
                    creature.Buffs.GetBuff(KurshuBlueprints.MindDrained)?.SelectComponents<KwrMindDrained>().FirstOrDefault()?.CheckRecovery();
                }
            }
            KurshuState kurshu = state.Kurshu;
            if (kurshu.Policy == KurshuPolicy.LegacyNotInserted && !kurshu.LegacyNoticeShown)
            {
                // Said at the first tick, once the loaded game's log is showing, rather than lost during the load.
                kurshu.LegacyNoticeShown = true;
                WardRuntime.Say("This save was made inside the sanctum before Kurshu the Undying guarded it, so she is not added to it. To meet her, load a save from before the Refuge Token was used.");
                Note("legacy notice shown once.");
            }
            if (kurshu.Policy != KurshuPolicy.Active)
            {
                return;
            }
            if (KurshuBlueprints.Unit == null)
            {
                if (!s_ProblemReported)
                {
                    s_ProblemReported = true;
                    Log.Error($"Kurshu's encounter is unavailable: {KurshuBlueprints.Problem}.");
                }
                return;
            }
            if (string.IsNullOrEmpty(kurshu.EntityId))
            {
                if (ControlRestored(out string waiting))
                {
                    Spawn(kurshu, now);
                }
                else if (!s_Logged)
                {
                    s_Logged = true;
                    Note($"waiting to appear: {waiting}.");
                }
                return;
            }
            UnitEntityData unit = Kurshu;
            if (unit == null)
            {
                return;
            }
            ApplyScale(unit);
            if (!s_Logged)
            {
                s_Logged = true;
                LogSheet(unit, "first tick since load or spawn");
            }
            if (!kurshu.HiddenCheckDone && !s_Precasting)
            {
                // Spawned but not yet past her before-combat moment (an interrupted spawn or a save in between).
                HiddenCheck(kurshu, unit);
            }
            if (KurshuRules.ExpireSurge(kurshu, now, WardProfile.RoundSeconds))
            {
                Note("Power Surge lapsed unused; the use stays spent.");
            }
            Observe(kurshu, unit, now);
            if (!unit.Descriptor.State.IsDead)
            {
                CheckHold(kurshu, unit);
            }
            if (kurshu.EngagedAt < 0d && unit.IsInCombat)
            {
                kurshu.EngagedAt = now;
                Note($"combat joined at {now:0} (turn-based {TurnBased.Controllers.CombatController.IsInTurnBasedCombat()}).");
            }
            if (s_Precasting && unit.Commands.Empty)
            {
                s_Precasting = false;
                unit.IsBrainActive = true;
                Note($"preparation done; her brain takes over (displacement {Displaced(unit)}).");
            }
            if (s_DirectorCommand && (unit.Commands.Empty || now - s_DirectorIssuedAt > 2 * WardProfile.RoundSeconds))
            {
                if (!unit.Commands.Empty)
                {
                    // A path that never arrives (bodies on the stair, say) is given up rather than holding her still.
                    unit.Commands.InterruptAll(c => c.AiAction == null);
                    if (!string.IsNullOrEmpty(s_DirectorTarget))
                    {
                        s_RetryAfter[s_DirectorTarget] = now + 3 * WardProfile.RoundSeconds;
                    }
                    Note($"gave up on '{s_DirectorWhat}' after two rounds; her brain takes over.");
                }
                s_DirectorCommand = false;
                s_DirectorTarget = null;
                unit.IsBrainActive = true;
            }
            if (!kurshu.Slain && unit.Descriptor.State.IsConscious && kurshu.HiddenCheckDone && !s_Precasting && !s_DirectorCommand)
            {
                Direct(kurshu, unit, now);
            }
        }

        /// <summary>
        /// The party is in A1 and the player has the game back: the token's transfer is over, nothing is loading, the fade is
        /// gone and no dialogue or cutscene holds the screen. Until then she does not exist, so nothing can act under the fade.
        /// </summary>
        private static bool ControlRestored(out string waiting)
        {
            waiting = null;
            if (!Game.Instance.Player.UnlockableFlags.IsUnlocked(ScenarioBlueprints.SanctumEnteredFlag))
            {
                waiting = "arrival not yet committed";
            }
            else if (RefugeTransit.InProgress)
            {
                waiting = "transfer still in progress";
            }
            else if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.IsLoadingScreenActive)
            {
                waiting = "loading";
            }
            else if (FadeCanvas.Instance != null && FadeCanvas.Instance.GetLoadingScreenState() != LoadingScreenState.Hidden)
            {
                waiting = "arrival fade " + FadeCanvas.Instance.GetLoadingScreenState();
            }
            else if (Game.Instance.CurrentMode != GameModeType.Default && Game.Instance.CurrentMode != GameModeType.Pause)
            {
                waiting = "game mode " + Game.Instance.CurrentMode;
            }
            return waiting == null;
        }

        private static void Spawn(KurshuState kurshu, double now)
        {
            Vector3 at = ObstacleAnalyzer.GetNearestNode(Apex).clampedPosition;
            UnitEntityData unit = Game.Instance.EntityCreator.SpawnUnit(KurshuBlueprints.Unit, at, Quaternion.Euler(0f, 0f, 0f), Game.Instance.State.LoadedAreaState.MainState);
            unit.Descriptor.Ensure<KurshuPart>().EncounterId = KurshuProfile.EncounterId;
            unit.IsBrainActive = false;
            kurshu.EntityId = unit.UniqueId;
            kurshu.SpawnedAt = now;
            s_Bound = unit;
            Note($"appears at the apex ({at.x:0.00},{at.y:0.00},{at.z:0.00}) as {unit.UniqueId}, control restored at {now:0}.");
            EnsureNature(kurshu, unit);
            // Once a day; its own use removes it, and nothing gives it back (a reload carries its absence).
            unit.Descriptor.AddBuff(KurshuBlueprints.ImprovedIronWill, unit);
            Reconcile(kurshu, unit);
            LogSheet(unit, "spawned");
            HiddenCheck(kurshu, unit);
        }

        /// <summary>
        /// The donor's model is a Medium figure and the engine scales a view only for a change from the blueprint's size, so
        /// her Large body is drawn at the engine's own one-size step (1 / 0.66). Rules size, reach and weapons are untouched,
        /// and any native size change (enlarge, reduce) still applies on top.
        /// </summary>
        private static void ApplyScale(UnitEntityData unit)
        {
            UnitEntityView view = unit.View;
            if (view == null)
            {
                return;
            }
            Vector3 wanted = view.OriginalScale * (view.GetSizeScale() / 0.66f);
            if ((view.transform.localScale - wanted).sqrMagnitude > 1e-4f)
            {
                view.transform.localScale = wanted;
            }
        }

        /// <summary>Her regeneration and her separation's saved count, applied once; a reload finds them already there.</summary>
        private static void EnsureNature(KurshuState kurshu, UnitEntityData unit)
        {
            if (kurshu.Slain || unit.Descriptor.State.IsDead)
            {
                // A corpse keeps nothing: death removed her buffs natively, and none is put back.
                return;
            }
            if (unit.Buffs.GetBuff(KurshuBlueprints.Regeneration) == null)
            {
                unit.Descriptor.AddBuff(KurshuBlueprints.Regeneration, unit);
            }
            Buff separation = unit.Buffs.GetBuff(KurshuBlueprints.Separation);
            int applied = separation?.SelectComponents<KwrDivineSeparation>().FirstOrDefault()?.Count ?? 0;
            if (applied != kurshu.SeparationLevels)
            {
                separation?.Remove();
                if (kurshu.SeparationLevels > 0)
                {
                    KwrDivineSeparation.PendingCount = kurshu.SeparationLevels;
                    unit.Descriptor.AddBuff(KurshuBlueprints.Separation, unit);
                }
                Note($"Divine Separation now {kurshu.SeparationLevels} negative level(s) (was {applied}).");
            }
        }

        /// <summary>Every daily resource on the unit is brought to the saved ledger; a resource can only ever go down here.</summary>
        private static void Reconcile(KurshuState kurshu, UnitEntityData unit)
        {
            foreach (var pair in KurshuBlueprints.Resources)
            {
                // A resource the unit does not yet carry, or carries at another maximum, says nothing about what was spent.
                int max = pair.Value.GetMaxAmount(unit.Descriptor);
                if (!unit.Descriptor.Resources.ContainsResource(pair.Value) || max != KurshuUses.PerDay[pair.Key])
                {
                    Note($"resource {pair.Key} not reconciled: present {unit.Descriptor.Resources.ContainsResource(pair.Value)}, maximum {max} of {KurshuUses.PerDay[pair.Key]}.");
                    continue;
                }
                int native = unit.Descriptor.Resources.GetResourceAmount(pair.Value);
                int wanted = KurshuRules.Reconcile(kurshu, pair.Key, native);
                if (native != wanted)
                {
                    unit.Descriptor.Resources.Spend(pair.Value, native - wanted);
                    Note($"resource {pair.Key}: {native} -> {wanted} to match the saved uses.");
                }
            }
        }

        /// <summary>
        /// Before combat: does she believe she is hidden? Her Stealth against every arriving character's Perception, native
        /// rolls. If no one beats it she casts Displacement, with a real use and its real duration. If someone does, she does not.
        /// </summary>
        private static void HiddenCheck(KurshuState kurshu, UnitEntityData unit)
        {
            var stealth = Rulebook.Trigger(new RuleSkillCheck(unit, StatType.SkillStealth, 0));
            kurshu.StealthResult = stealth.RollResult;
            kurshu.BestPerception = int.MinValue;
            foreach (UnitEntityData member in Game.Instance.Player.Party.Where(u => !u.Descriptor.State.IsDead))
            {
                var perception = Rulebook.Trigger(new RuleSkillCheck(member, StatType.SkillPerception, kurshu.StealthResult));
                if (perception.RollResult > kurshu.BestPerception)
                {
                    kurshu.BestPerception = perception.RollResult;
                    kurshu.SpottedBy = perception.RollResult >= kurshu.StealthResult ? member.CharacterName : string.Empty;
                }
            }
            kurshu.BelievedHidden = string.IsNullOrEmpty(kurshu.SpottedBy);
            kurshu.HiddenCheckDone = true;
            Note($"hidden check: Stealth {kurshu.StealthResult} vs best Perception {kurshu.BestPerception}: {(kurshu.BelievedHidden ? "she believes she is hidden" : "seen by " + kurshu.SpottedBy)}.");
            if (kurshu.BelievedHidden)
            {
                AbilityData displacement = unit.Descriptor.Abilities.GetAbility(KurshuBlueprints.Displacement)?.Data;
                if (displacement != null && displacement.IsAvailableForCast)
                {
                    s_Precasting = true;
                    unit.Commands.Run(UnitUseAbility.CreateCastCommand(displacement, unit));
                    Note("expecting trouble, she casts Displacement before anyone reaches her.");
                    return;
                }
            }
            unit.IsBrainActive = true;
        }

        private static void Observe(KurshuState kurshu, UnitEntityData unit, double now)
        {
            UnitDescriptor d = unit.Descriptor;
            bool dead = d.State.IsDead;
            foreach (HealthEvent e in KurshuRules.ObserveHealth(kurshu, d.HPLeft, d.MaxHP, d.State.IsConscious, dead, now))
            {
                switch (e)
                {
                    case HealthEvent.SubstantialWound:
                        Note($"substantial wound at {d.HPLeft}/{d.MaxHP}: she speaks.");
                        const string speech = "You may have bled me, mortal, but Krune has mastered the rune of armor and your blades shall never pierce his flesh! Alacrity bestows him with a sixth sense with which he anticipates intentions and actions. Still other runes feed him divine power, strengthening his body and mind, as well as assuring him immortality.";
                        Game.Instance.UI.Bark(unit, speech, Kingmaker.UI.Common.UIUtility.GetBarkDuration(speech));
                        WardRuntime.Say("Kurshu: \"" + speech + "\"");
                        break;
                    case HealthEvent.Defeated:
                        Defeated(kurshu, unit, now);
                        break;
                    case HealthEvent.Slain:
                        Note($"slain at {now:0}; regeneration ended (hp {d.HPLeft}).");
                        WardRuntime.Say("Kurshu the Undying is dead, and does not rise.");
                        break;
                }
            }
        }

        /// <summary>
        /// The first time her hit points run out, whether she dies or lies regenerating: the printed blood message, and the
        /// existing ninety-minute clock, started once by the existing idempotent event.
        /// </summary>
        private static void Defeated(KurshuState kurshu, UnitEntityData unit, double now)
        {
            bool started = RitualRules.DefeatKurshu(State.Ritual, now);
            Note($"DEFEATED at {now:0} ({kurshu.DefeatedHow}); deadline started {started}, due at {State.Ritual.GlobalDeadline:0} ({WardProfile.GlobalDeadlineSeconds / 60d:0} minutes).");
            if (!kurshu.BloodMessageShown)
            {
                kurshu.BloodMessageShown = true;
                const string blood = "Droplets of Kurshu's blood spatter into Thassilonian runes: \"Enjoy your victory, and I shall enjoy mine. In your righteousness you have sacrificed my life to assure the rise of another. May Runelord Krune skin you alive and use your flesh as the canvas for the scriptures of Lissala's second coming!\"";
                WardRuntime.Say(blood);
                WardRuntime.Say("Somewhere beyond the stone, something stirs. You have little more than an hour and a half before Krune wakes.");
                EventBus.RaiseEvent(delegate(IWarningNotificationUIHandler h) { h.HandleWarning("Her blood spells out a message in Thassilonian runes. Krune will wake within 90 minutes."); });
            }
        }

        /// <summary>
        /// The two choices the brain cannot make for her: eating an eligible corpse, and slipping away by her self-only
        /// teleport when she is cornered. Both are native abilities with native costs; each is tried once per round at most.
        /// </summary>
        private static void Direct(KurshuState kurshu, UnitEntityData unit, double now)
        {
            bool turnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
            if (turnBased && TurnBased.Controllers.CombatController.CurrentUnit != unit)
            {
                return;
            }
            if (!unit.Descriptor.State.CanAct || unit.Commands.Raw.Any(c => c != null && c.AiAction == null && !c.IsFinished))
            {
                return;
            }
            if (!unit.IsInCombat)
            {
                return;
            }
            if (!string.IsNullOrEmpty(kurshu.HeldId))
            {
                // While she holds someone, nothing else is chosen for her until she has maintained or let go. In turn-based
                // combat that was decided at her turn's start; in real time it is decided as her standard action comes ready.
                if (!turnBased && unit.CombatState.Cooldown.StandardAction <= 0f && unit.Commands.Empty)
                {
                    DecideHold(unit, "her action is ready");
                }
                return;
            }
            if (TryFeed(unit, turnBased))
            {
                return;
            }
            int round = (int)(now / WardProfile.RoundSeconds);
            if (round == s_TeleportRound || unit.Descriptor.HPLeft >= unit.Descriptor.MaxHP / 2)
            {
                return;
            }
            List<UnitEntityData> foes = Foes(unit).ToList();
            if (foes.Count(f => f.DistanceTo(unit) <= KurshuTactics.ReachMeters + unit.Corpulence + f.Corpulence) < 2)
            {
                return;
            }
            AbilityData teleport = unit.Descriptor.Abilities.GetAbility(KurshuBlueprints.GreaterTeleport)?.Data;
            if (teleport == null || !teleport.IsAvailableForCast || (turnBased && !unit.HasStandardAction()))
            {
                return;
            }
            s_TeleportRound = round;
            Vector3? point = TeleportPoint(unit, foes);
            if (point.HasValue && teleport.CanTarget(new Kingmaker.Utility.TargetWrapper(point.Value)))
            {
                Issue(unit, UnitUseAbility.CreateCastCommand(teleport, new Kingmaker.Utility.TargetWrapper(point.Value)), $"cornered at {unit.Descriptor.HPLeft}/{unit.Descriptor.MaxHP}, teleports herself to ({point.Value.x:0.0},{point.Value.z:0.0})");
            }
            else
            {
                Note("cornered, but no clear point in A1 to teleport to this round.");
            }
        }

        /// <summary>An eligible corpse in reach of a full round: she goes to eat it. Returns true when the command was issued.</summary>
        private static bool TryFeed(UnitEntityData unit, bool turnBased)
        {
            UnitEntityData corpse = FindEligibleCorpse(unit);
            if (corpse == null || (turnBased && !unit.HasFullRoundAction()))
            {
                return false;
            }
            AbilityData feed = unit.Descriptor.Abilities.GetAbility(KurshuBlueprints.FeedAbility)?.Data;
            if (feed == null || !feed.IsAvailableForCast)
            {
                return false;
            }
            Issue(unit, UnitUseAbility.CreateCastCommand(feed, new Kingmaker.Utility.TargetWrapper(corpse.Position)), $"moves to feed on {corpse.CharacterName}", corpse.UniqueId);
            return true;
        }

        /// <summary>
        /// Turn-based: her own turn has begun and nothing has acted yet. Feeding needs the whole turn, so it is decided here,
        /// before her brain spends the standard action.
        /// </summary>
        public static void OnTurnStarted(UnitEntityData unit)
        {
            SanctumState state = State;
            if (state == null || unit == null || unit != Kurshu || state.Kurshu.Slain || !state.Kurshu.HiddenCheckDone || !unit.Descriptor.State.IsConscious || !unit.Descriptor.State.CanAct || s_DirectorCommand)
            {
                return;
            }
            if (!string.IsNullOrEmpty(state.Kurshu.HeldId))
            {
                DecideHold(unit, "her turn begins");
                if (s_DirectorCommand)
                {
                    return;
                }
            }
            TryFeed(unit, turnBased: true);
        }
        private static void Issue(UnitEntityData unit, Kingmaker.UnitLogic.Commands.Base.UnitCommand command, string what, string target = null)
        {
            unit.Commands.InterruptAiCommands();
            unit.IsBrainActive = false;
            s_DirectorCommand = true;
            s_DirectorIssuedAt = WardRuntime.Now;
            s_DirectorWhat = what;
            s_DirectorTarget = target;
            unit.Commands.Run(command);
            Note(what + ".");
        }

        /// <summary>A1's own floor, away from every foe: never through stone into the corridor beyond, never onto the party.</summary>
        private static Vector3? TeleportPoint(UnitEntityData unit, List<UnitEntityData> foes)
        {
            SanctumSpace a1 = SanctumPlan.SpaceAt(SanctumPlan.Arrival.X, SanctumPlan.Arrival.Y);
            Vector3? best = null;
            float bestScore = TeleportClearanceMeters;
            for (float gx = 9f; gx <= 17f; gx += 0.5f)
            {
                for (float gy = 13.5f; gy <= 23f; gy += 0.5f)
                {
                    if (SanctumPlan.SpaceAt(gx, gy) != a1 || !SanctumPlan.HeightAt(gx, gy).HasValue)
                    {
                        continue;
                    }
                    Vector3 node = ObstacleAnalyzer.GetNearestNode(new Vector3(SanctumPlan.ToWorldX(gx), SanctumPlan.HeightAt(gx, gy).Value, SanctumPlan.ToWorldZ(gy))).clampedPosition;
                    float clearance = foes.Count == 0 ? 0f : foes.Min(f => Vector3.Distance(f.Position, node));
                    if (clearance > bestScore && Vector3.Distance(node, unit.Position) > TeleportClearanceMeters)
                    {
                        bestScore = clearance;
                        best = node;
                    }
                }
            }
            return best;
        }

        private static IEnumerable<UnitEntityData> Foes(UnitEntityData unit)
        {
            return Game.Instance.State.Units.Where(u => u.IsInGame && !u.Descriptor.State.IsDead && u.IsEnemy(unit) && u.IsInCombat);
        }

        private static UnitEntityData FindEligibleCorpse(UnitEntityData unit)
        {
            KurshuState kurshu = State.Kurshu;
            return Game.Instance.State.Units
                .Where(u => u != unit && u.IsInGame && u.Descriptor.State.IsDead && u.DistanceTo(unit) <= FeedScanMeters)
                .Where(u => KurshuRules.CanFeed(kurshu, Facts(u)) == FeedVerdict.Eligible)
                .Where(u => !s_RetryAfter.TryGetValue(u.UniqueId, out double after) || WardRuntime.Now >= after)
                .OrderBy(u => u.DistanceTo(unit))
                .FirstOrDefault();
        }

        public static CorpseFacts Facts(UnitEntityData body)
        {
            return new CorpseFacts
            {
                Id = body.UniqueId,
                Dead = body.Descriptor.State.IsDead,
                Outsider = IsOutsider(body),
                HitDice = body.Descriptor.Progression.CharacterLevel,
                Summoned = body.IsSummoned() || body.Descriptor.Get<UnitPartSummonedMonster>() != null,
                StoneHusk = WardFixtures.IdOf(body) != null,
            };
        }

        public static bool IsOutsider(UnitEntityData unit)
        {
            return KurshuBlueprints.OutsiderFeature != null && unit.Descriptor.HasFact(KurshuBlueprints.OutsiderFeature);
        }

        /// <summary>The eligible corpse under a Feed point and within her reach, or null.</summary>
        public static UnitEntityData FeedCorpseAt(UnitEntityData caster, Vector3 point, out FeedVerdict verdict)
        {
            verdict = FeedVerdict.NotDead;
            SanctumState state = State;
            if (caster == null || state == null)
            {
                return null;
            }
            UnitEntityData body = Game.Instance.State.Units
                .Where(u => u != caster && u.IsInGame && u.Descriptor.State.IsDead && Vector3.Distance(u.Position, point) <= FeedPointMeters)
                .OrderBy(u => Vector3.Distance(u.Position, point))
                .FirstOrDefault();
            if (body == null)
            {
                return null;
            }
            verdict = KurshuRules.CanFeed(state.Kurshu, Facts(body));
            return verdict == FeedVerdict.Eligible ? body : null;
        }

        public static void Feed(UnitEntityData caster, Vector3 point)
        {
            UnitEntityData body = FeedCorpseAt(caster, point, out FeedVerdict verdict);
            if (body == null)
            {
                Note($"Feed found nothing to eat at ({point.x:0.0},{point.z:0.0}): {verdict}.");
                return;
            }
            KurshuState kurshu = State.Kurshu;
            CorpseFacts facts = Facts(body);
            int removed = KurshuRules.Feed(kurshu, facts, WardRuntime.Now);
            Note($"FEEDS on {body.CharacterName} ({facts.HitDice} HD outsider): {removed} Divine Separation level(s) removed, {kurshu.SeparationLevels} left; other negative levels untouched.");
            WardRuntime.Say($"Kurshu tears into {body.CharacterName}'s corpse and devours its essence. Her scales darken with strength.");
            EnsureNature(kurshu, caster);
            if (!body.IsPlayerFaction)
            {
                // Devoured: the body is gone from the world. A fallen party member's remains are left, and can still be raised.
                body.IsInGame = false;
            }
        }

        /// <summary>
        /// Her Intelligence drain on one creature, through the native stat-damage rule. Where the engine would kill (a score
        /// below 1), the points it cannot show are held back and the creature lies comatose instead; see <see cref="MindDrainRules"/>.
        /// </summary>
        public static void DrainIntelligence(UnitEntityData unit, UnitEntityData target, DiceFormula dice, string what)
        {
            KwrMindDrained comatose = ComatoseLogic(target);
            if (comatose != null && !comatose.CheckRecovery())
            {
                // A restoration since the last check has woken it: settled first, so the new drain is not erased with the old.
                comatose = ComatoseLogic(target);
            }
            bool markedBefore = target.Descriptor.State.MarkedForDeath;
            ModifiableValueAttributeStat stat = target.Stats.Intelligence;
            var drain = new RuleDealStatDamage(unit, target, StatType.Intelligence, dice, 0) { IsDrain = true };
            Rulebook.Trigger(drain);
            MindDrainState before = comatose != null ? comatose.State : new MindDrainState { Drain = stat.Drain };
            MindDrainState held = MindDrainRules.HoldBack(before, stat.ModifiedValueRaw);
            if (held.Drain == before.Drain || markedBefore)
            {
                Note($"{what} {target.CharacterName}: Intelligence drain {drain.Damage} (immune {drain.Immune}); Intelligence now {stat.ModifiedValue}, drain {stat.Drain}.");
                return;
            }
            stat.Drain = held.Drain;
            target.Descriptor.State.MarkedForDeath = false;
            if (comatose != null)
            {
                // Already at Intelligence 0: the drain still lands, and restoration will have that much more to undo.
                comatose.Pending = held.Pending;
                Note($"{what} {target.CharacterName}, already comatose: Intelligence drain {drain.Damage} held back ({held.Pending} in all).");
                return;
            }
            KwrMindDrained.Pending0 = held.Pending;
            target.Descriptor.AddBuff(KurshuBlueprints.MindDrained, unit, null);
            Note($"{what} {target.CharacterName}: Intelligence drain {drain.Damage} reaches 0: comatose, not dead; {held.Pending} point(s) held back (score shown {stat.ModifiedValue}, drain {stat.Drain}, life {target.Descriptor.State.LifeState}).");
        }

        private static KwrMindDrained ComatoseLogic(UnitEntityData target)
        {
            return target.Buffs.GetBuff(KurshuBlueprints.MindDrained)?.SelectComponents<KwrMindDrained>().FirstOrDefault();
        }

        /// <summary>Printed: grab adds +4 to her grapple checks (in her CMB to grapple); maintaining an existing hold adds +5.</summary>
        private const int MaintainBonus = 5;

        /// <summary>Her offensive spell-like abilities: while any has a use left, her tactics prefer casting to squeezing.</summary>
        private static readonly string[] OffensiveUses = { KurshuUses.Fireball, KurshuUses.LightningBolt, KurshuUses.StinkingCloud, KurshuUses.Slow, KurshuUses.HoldPerson, KurshuUses.MagicMissile, KurshuUses.VampiricTouch, KurshuUses.DispelMagic, KurshuUses.LimitedWish };

        /// <summary>
        /// Freedom of movement however it is granted (spell, scroll, wand or item): the native immunity to being held in place,
        /// or to movement-impairing effects, which her hold carries as its descriptor.
        /// </summary>
        public static bool FreeOfMovement(UnitEntityData unit)
        {
            UnitState state = unit.Descriptor.State;
            if (state.HasConditionImmunity(UnitCondition.CantMove) || state.HasConditionImmunity(UnitCondition.MovementBan))
            {
                return true;
            }
            return unit.Buffs.Enumerable.Cast<Kingmaker.Blueprints.Facts.Fact>().Concat(unit.Descriptor.Progression.Features.Enumerable.Cast<Kingmaker.Blueprints.Facts.Fact>())
                .Any(f => f.SelectComponents<Kingmaker.UnitLogic.FactLogic.BuffDescriptorImmunity>().Any(c => c.Descriptor.HasAnyFlag(Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MovementImpairing)));
        }

        /// <summary>A grapple check with the native maneuver roll. The grappled condition's -2 does not apply to checks to grapple or escape.</summary>
        private static RuleCombatManeuver GrappleCheck(UnitEntityData initiator, UnitEntityData target, int bonus)
        {
            var check = new RuleCombatManeuver(initiator, target, CombatManeuver.Grapple);
            if (initiator.Buffs.GetBuff(KurshuBlueprints.Held) != null || initiator.Buffs.GetBuff(KurshuBlueprints.Grappling) != null)
            {
                check.AddBonus(Grappled.AttackPenalty, null);
            }
            if (bonus != 0)
            {
                check.AddBonus(bonus, null);
            }
            return Rulebook.Trigger(check);
        }

        private static UnitEntityData HeldUnit(KurshuState kurshu)
        {
            return string.IsNullOrEmpty(kurshu.HeldId) ? null : Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == kurshu.HeldId);
        }

        /// <summary>The hold stands while both are up, she can act, the victim is in her reach and not free to move, and both carry the condition.</summary>
        private static bool HoldStands(UnitEntityData unit, UnitEntityData victim, out string why)
        {
            why = null;
            if (victim == null || !victim.IsInGame || victim.Descriptor.State.IsDead)
            {
                why = "the held creature is gone or dead";
            }
            else if (!unit.Descriptor.State.IsConscious || !unit.Descriptor.State.CanAct)
            {
                why = "she can no longer act";
            }
            else if (unit.DistanceTo(victim) > KurshuTactics.ReachMeters + unit.Corpulence + victim.Corpulence + 0.5f)
            {
                why = "the held creature is out of her reach";
            }
            else if (FreeOfMovement(victim))
            {
                why = $"freedom of movement frees {victim.CharacterName}";
            }
            else if (victim.Buffs.GetBuff(KurshuBlueprints.Held) == null || unit.Buffs.GetBuff(KurshuBlueprints.Grappling) == null)
            {
                why = "the grappled condition is gone";
            }
            return why == null;
        }

        /// <summary>A tail hit: grab, if she may. A success holds the target, grapples her too, and constricts once.</summary>
        public static void TryGrab(UnitEntityData unit, UnitEntityData target)
        {
            SanctumState state = State;
            if (state == null)
            {
                return;
            }
            KurshuState kurshu = state.Kurshu;
            GrabVerdict verdict = KurshuRules.CanGrab(kurshu, target.Descriptor.State.Size <= unit.Descriptor.State.Size, FreeOfMovement(target));
            if (verdict != GrabVerdict.Eligible)
            {
                Note($"Tail hit on {target.CharacterName}: no grab ({verdict}{(verdict == GrabVerdict.AlreadyHolding ? ", holding " + kurshu.HeldName : string.Empty)}).");
                return;
            }
            RuleCombatManeuver grab = GrappleCheck(unit, target, 0);
            Note($"Tail grab on {target.CharacterName}: {grab.InitiatorCMValue} (CMB {grab.InitiatorCMB}) vs CMD {grab.TargetCMD}: {(grab.Success ? "held" : "slips free")}.");
            if (!grab.Success)
            {
                return;
            }
            target.Descriptor.AddBuff(KurshuBlueprints.Held, unit, null);
            if (target.Buffs.GetBuff(KurshuBlueprints.Held) == null)
            {
                Note($"{target.CharacterName}'s own immunity refuses the hold.");
                return;
            }
            KurshuRules.Hold(kurshu, target.UniqueId, target.CharacterName, WardRuntime.Now);
            unit.Descriptor.AddBuff(KurshuBlueprints.Grappling, unit, null);
            Constrict(unit, target, false);
        }

        /// <summary>Constrict on a successful grapple check; a maintained hold also deals the tail's damage, as a grapple's damage option.</summary>
        private static void Constrict(UnitEntityData unit, UnitEntityData target, bool withTail)
        {
            var constrict = new PhysicalDamage(new DiceFormula(1, DiceType.D8), PhysicalDamageForm.Bludgeoning);
            constrict.AddBonus(5);
            var bundle = new List<BaseDamage> { constrict };
            if (withTail)
            {
                var tail = new PhysicalDamage(new DiceFormula(1, DiceType.D8), PhysicalDamageForm.Bludgeoning);
                tail.AddBonus(2);
                bundle.Add(tail);
            }
            var rule = Rulebook.Trigger(new RuleDealDamage(unit, target, new DamageBundle(bundle.ToArray())));
            Note($"Constrict{(withTail ? " and tail" : string.Empty)} on {target.CharacterName}: {rule.Damage} bludgeoning.");
        }

        /// <summary>
        /// Her decision while she holds someone, when her standard action is ready (real time) or her turn begins
        /// (turn-based): maintain with that standard action, or let go as a free action and act as she likes.
        /// </summary>
        public static void DecideHold(UnitEntityData unit, string when)
        {
            SanctumState state = State;
            if (state == null || unit == null)
            {
                return;
            }
            KurshuState kurshu = state.Kurshu;
            if (string.IsNullOrEmpty(kurshu.HeldId))
            {
                RemoveHoldBuffs(unit, null);
                return;
            }
            UnitEntityData victim = HeldUnit(kurshu);
            bool stands = HoldStands(unit, victim, out string why);
            bool offensive = OffensiveUses.Any(k => KurshuRules.Remaining(kurshu, k) > 0);
            string name = kurshu.HeldName;
            HoldChoice choice = KurshuRules.DecideHold(kurshu, stands, offensive, out string reason);
            if (choice == HoldChoice.Release)
            {
                RemoveHoldBuffs(unit, victim);
                Note($"{when}: she lets go of {name}: {(why ?? reason)}.");
                return;
            }
            AbilityData maintain = unit.Descriptor.Abilities.GetAbility(KurshuBlueprints.MaintainGrapple)?.Data;
            if (maintain == null)
            {
                return;
            }
            Issue(unit, UnitUseAbility.CreateCastCommand(maintain, victim), $"{when}: no offensive spell-like ability left, so she maintains the hold on {name} (standard action)");
        }

        /// <summary>Maintaining: the grapple check at +5; on a success the tail and constrict damage, on a failure the hold ends.</summary>
        public static void Maintain(UnitEntityData unit)
        {
            SanctumState state = State;
            if (state == null || unit == null)
            {
                return;
            }
            KurshuState kurshu = state.Kurshu;
            UnitEntityData victim = HeldUnit(kurshu);
            if (!HoldStands(unit, victim, out string why))
            {
                ReleaseHold(unit, victim, why);
                return;
            }
            RuleCombatManeuver check = GrappleCheck(unit, victim, MaintainBonus);
            KurshuRules.Maintained(kurshu, check.Success);
            Note($"Maintains the hold on {victim.CharacterName}: {check.InitiatorCMValue} (CMB {check.InitiatorCMB}) vs CMD {check.TargetCMD}: {(check.Success ? "squeezes" : "loses the hold")}.");
            if (check.Success)
            {
                Constrict(unit, victim, true);
            }
            else
            {
                RemoveHoldBuffs(unit, victim);
            }
        }

        /// <summary>The held creature's round: the hold is checked, and then it struggles with its own maneuver roll.</summary>
        public static void OnHeldRound(UnitEntityData victim, UnitEntityData holder)
        {
            SanctumState state = State;
            if (state == null || victim == null)
            {
                return;
            }
            KurshuState kurshu = state.Kurshu;
            if (holder == null || kurshu.HeldId != victim.UniqueId)
            {
                RemoveHoldBuffs(holder, victim);
                return;
            }
            if (!HoldStands(holder, victim, out string why))
            {
                ReleaseHold(holder, victim, why);
                return;
            }
            RuleCombatManeuver escape = GrappleCheck(victim, holder, 0);
            Note($"{victim.CharacterName} struggles against the coils: {escape.InitiatorCMValue} (CMB {escape.InitiatorCMB}) vs CMD {escape.TargetCMD}: {(escape.Success ? "breaks free" : "still held")}.");
            if (escape.Success)
            {
                ReleaseHold(holder, victim, "broke free");
            }
        }

        private static void ReleaseHold(UnitEntityData unit, UnitEntityData victim, string reason)
        {
            SanctumState state = State;
            string name = state?.Kurshu.HeldName;
            if (state != null && KurshuRules.Release(state.Kurshu, reason))
            {
                Note($"The hold on {name} ends: {reason}.");
            }
            RemoveHoldBuffs(unit, victim);
        }

        private static void RemoveHoldBuffs(UnitEntityData unit, UnitEntityData victim)
        {
            unit?.Buffs.GetBuff(KurshuBlueprints.Grappling)?.Remove();
            victim?.Buffs.GetBuff(KurshuBlueprints.Held)?.Remove();
        }

        /// <summary>After a load or each tick: the saved hold and the two conditions agree, and a hold that no longer stands ends at once.</summary>
        private static void CheckHold(KurshuState kurshu, UnitEntityData unit)
        {
            if (string.IsNullOrEmpty(kurshu.HeldId))
            {
                if (unit.Buffs.GetBuff(KurshuBlueprints.Grappling) != null)
                {
                    RemoveHoldBuffs(unit, null);
                }
                foreach (UnitEntityData stray in Game.Instance.Player.Party.Where(u => u.Buffs.GetBuff(KurshuBlueprints.Held) != null))
                {
                    stray.Buffs.GetBuff(KurshuBlueprints.Held).Remove();
                    Note($"{stray.CharacterName}: a hold the saved state does not record is removed.");
                }
                return;
            }
            UnitEntityData victim = HeldUnit(kurshu);
            if (!HoldStands(unit, victim, out string why))
            {
                ReleaseHold(unit, victim, why);
            }
        }

        public static void ArmSurge(UnitEntityData caster)
        {
            SanctumState state = State;
            if (caster == null || state == null)
            {
                return;
            }
            if (!KurshuRules.ArmSurge(state.Kurshu, WardRuntime.Now))
            {
                Note("Power Surge not armed (already armed or no use left).");
                return;
            }
            caster.Descriptor.AddBuff(KurshuBlueprints.Surge, caster, TimeSpan.FromSeconds(WardProfile.RoundSeconds));
            Note($"POWER SURGE armed at {WardRuntime.Now:0}: +2 DC to her next spell-like ability this round; {KurshuRules.Remaining(state.Kurshu, KurshuUses.PowerSurge)} use(s) left.");
        }

        public static void OnSurgeUsed(UnitEntityData caster, AbilityData spell)
        {
            if (KurshuRules.ConsumeSurge(State.Kurshu))
            {
                Note($"Power Surge spent on {spell.Blueprint.name}.");
            }
        }

        public static void OnSurgeEnded(UnitEntityData caster)
        {
            SanctumState state = State;
            if (state != null && state.Kurshu.SurgeArmed && KurshuRules.ExpireSurge(state.Kurshu, WardRuntime.Now + WardProfile.RoundSeconds, WardProfile.RoundSeconds))
            {
                Note("Power Surge's round ended unused; the use stays spent.");
            }
        }

        public static void OnCast(UnitEntityData unit, RuleCastSpell evt)
        {
            SanctumState state = State;
            BlueprintAbility ability = evt.Spell?.Blueprint;
            if (state == null || ability == null)
            {
                return;
            }
            if (KurshuBlueprints.UseKeys.TryGetValue(ability, out string key) && key != KurshuUses.PowerSurge && key != KurshuUses.Feed && evt.Success)
            {
                KurshuRules.Spend(state.Kurshu, key);
            }
            if (ability == KurshuBlueprints.Displacement && !state.Kurshu.DisplacementPrepared && !unit.IsInCombat)
            {
                state.Kurshu.DisplacementPrepared = true;
            }
            if (!string.IsNullOrEmpty(state.Kurshu.HeldId) && ability != KurshuBlueprints.MaintainGrapple && ability.ActionType != Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift && evt.Success)
            {
                // A grapple held past her turn needs that turn's standard action; spending it on a spell ends the hold.
                ReleaseHold(unit, HeldUnit(state.Kurshu), $"she spent her action on {ability.name} instead of maintaining the hold");
            }
            var p = evt.Context?.Params;
            Note($"casts {ability.name} at {evt.Context?.MainTarget?.Unit?.CharacterName ?? evt.Context?.MainTarget?.Point.ToString()}: success {evt.Success}, CL {p?.CasterLevel}, DC {p?.DC}, spell level {p?.SpellLevel}; {(key == null ? "no daily use" : $"{key} left {KurshuRules.Remaining(state.Kurshu, key)}")}; in combat {unit.IsInCombat}.");
        }

        /// <summary>An attack after the round of the grab is an action not spent on maintaining it: the hold ends.</summary>
        public static void OnAttack(UnitEntityData unit, RuleAttackWithWeapon evt)
        {
            SanctumState state = State;
            if (state != null && !string.IsNullOrEmpty(state.Kurshu.HeldId) && WardRuntime.Now - state.Kurshu.HeldSince > 1d && evt.Weapon?.Blueprint != KurshuBlueprints.Tail)
            {
                ReleaseHold(unit, HeldUnit(state.Kurshu), "she attacks instead of maintaining the hold");
            }
        }

        public static void OnDamaged(UnitEntityData unit, RuleDealDamage evt)
        {
            string parts = string.Join(", ", evt.ResultDamage?.Select(v => $"{Describe(v.Source)} {v.ValueWithoutReduction}-{v.Reduction}={v.FinalValue}").ToArray() ?? new string[0]);
            Note($"takes {evt.Damage} from {evt.Initiator?.CharacterName} [{parts}]; hp {unit.Descriptor.HPLeft}/{unit.Descriptor.MaxHP}; regenerating {unit.Descriptor.State.IsRegenerate}.");
        }

        private static string Describe(BaseDamage damage)
        {
            switch (damage)
            {
                case PhysicalDamage physical:
                    return $"physical({physical.Form} {physical.MaterialsMask} +{physical.EnchantmentTotal})";
                case EnergyDamage energy:
                    return "energy(" + energy.EnergyType + ")";
                default:
                    return damage?.Type.ToString() ?? "?";
            }
        }

        private static bool Displaced(UnitEntityData unit)
        {
            return unit.Buffs.Enumerable.Any(b => b.Blueprint.name.StartsWith("Displacement", StringComparison.Ordinal));
        }

        /// <summary>The live field, turned into her tactics' inputs. Only enemies the brain itself has in its lists are counted.</summary>
        public static float Score(KurshuAim aim, bool onTarget, DecisionContext context)
        {
            UnitEntityData unit = context.Unit;
            SanctumState state = State;
            if (state == null || unit.Descriptor.Get<KurshuPart>() == null)
            {
                return 0f;
            }
            List<UnitEntityData> enemies = context.Enemies.Select(t => t.Unit).Where(u => u != null && !u.Descriptor.State.IsDead).ToList();
            if (!onTarget)
            {
                var self = new TacticsSelf
                {
                    HitPointFraction = unit.Descriptor.MaxHP > 0 ? (double)unit.Descriptor.HPLeft / unit.Descriptor.MaxHP : 1d,
                    Displaced = Displaced(unit),
                    Slowed = unit.Descriptor.State.HasCondition(UnitCondition.Slowed),
                    Nauseated = unit.Descriptor.State.HasCondition(UnitCondition.Nauseated),
                    SurgeArmed = state.Kurshu.SurgeArmed,
                    AnySaveAbilityLeft = new[] { KurshuUses.Fireball, KurshuUses.HoldPerson, KurshuUses.LightningBolt, KurshuUses.Slow, KurshuUses.StinkingCloud, KurshuUses.LimitedWish }.Any(k => KurshuRules.Remaining(state.Kurshu, k) > 0),
                    VisibleEnemies = enemies.Count,
                };
                return KurshuTactics.Actor(aim, self);
            }
            UnitEntityData target = context.Target?.Unit;
            if (target == null)
            {
                return 0f;
            }
            if (target == unit)
            {
                // Only her own defenses are aimed at herself; an attack or an area is never centred on her.
                return aim == KurshuAim.Displacement || aim == KurshuAim.CureSelf || aim == KurshuAim.BreakEnchantmentSelf || aim == KurshuAim.PowerSurge ? 1f : 0f;
            }
            Func<float, int> within = feet => enemies.Count(e => Vector3.Distance(e.Position, target.Position) <= feet * 0.3048f);
            var facts = new TacticsTarget
            {
                Distance = unit.DistanceTo(target) - unit.Corpulence - target.Corpulence,
                Outsider = IsOutsider(target),
                Humanoid = !IsOutsider(target) && target.Descriptor.Progression.Race != null,
                Held = target.Descriptor.State.HasCondition(UnitCondition.Paralyzed) || target.Descriptor.State.HasCondition(UnitCondition.Helpless),
                Slowed = target.Descriptor.State.HasCondition(UnitCondition.Slowed),
                Nauseated = target.Descriptor.State.HasCondition(UnitCondition.Nauseated),
                EnemiesWithin10Feet = within(10f),
                EnemiesWithin20Feet = within(20f),
                EnemiesWithin30Feet = within(30f),
                DispellableBuffs = target.Buffs.Enumerable.Count(b => b.Context?.SourceAbility != null && b.Context.SourceAbility.Type == AbilityType.Spell),
                SelfWithin20Feet = unit.DistanceTo(target) <= 20f * 0.3048f + unit.Corpulence,
            };
            return KurshuTactics.Target(aim, facts);
        }

        /// <summary>Read only: the sheet's authored values against what the unit shows, every modifier with its source.</summary>
        public static void LogSheet(UnitEntityData unit, string when)
        {
            Log.Info($"Kurshu sheet [{when}]: {Report(unit)}");
        }

        public static string Report(UnitEntityData unit)
        {
            var sb = new System.Text.StringBuilder();
            UnitDescriptor d = unit.Descriptor;
            sb.Append($"{unit.CharacterName} {unit.UniqueId} size {d.State.Size} HD {d.Progression.CharacterLevel} hp {d.HPLeft}/{d.MaxHP} life {d.State.LifeState} regenerating {d.State.IsRegenerate} brain {unit.IsBrainActive} combat {unit.IsInCombat} pos ({unit.Position.x:0.0},{unit.Position.y:0.0},{unit.Position.z:0.0})\n");
            foreach (StatType type in new[] { StatType.Strength, StatType.Dexterity, StatType.Constitution, StatType.Intelligence, StatType.Wisdom, StatType.Charisma, StatType.BaseAttackBonus, StatType.AC, StatType.SaveFortitude, StatType.SaveReflex, StatType.SaveWill, StatType.Initiative, StatType.SkillPerception, StatType.SkillStealth, StatType.AdditionalAttackBonus, StatType.AdditionalCMB, StatType.AdditionalCMD, StatType.HitPoints, StatType.AttackOfOpportunityCount })
            {
                ModifiableValue stat = unit.Stats.GetStat(type);
                sb.Append($"  {type} {stat.ModifiedValue} (base {stat.BaseValue}) [{string.Join(", ", stat.Modifiers.Select(m => $"{m.ModDescriptor} {m.ModValue:+0;-0}").ToArray())}]\n");
            }
            var cmb = Rulebook.Trigger(new RuleCalculateBaseCMB(unit));
            var cmd = Rulebook.Trigger(new RuleCalculateBaseCMD(unit));
            UnitEntityData foe = Game.Instance.Player.Party.FirstOrDefault();
            string grapple = foe == null ? "?" : Rulebook.Trigger(new RuleCalculateCMB(unit, foe, CombatManeuver.Grapple)).Result.ToString();
            sb.Append($"  AC touch {unit.Stats.AC.Touch} flat-footed {unit.Stats.AC.FlatFooted}; CMB {cmb.Result} (BAB {cmb.ResultBAB} Str {cmb.ResultStrengthBonus} size {cmb.ResultSizeBonus} misc {cmb.ResultMiscBonus}; grapple {grapple}); CMD {cmd.Result} (BAB {cmd.ResultBAB} Str {cmd.ResultStrengthBonus} Dex {cmd.ResultDexterityBonus} size {cmd.ResultSizeBonus} misc {cmd.ResultMiscBonus})\n");
            // The chain from the printed block: the authored value with no separation level, less each level she still has,
            // plus the preset's own Difficulty modifiers, which reach CMB through attack and CMD through touch AC natively.
            int separation = unit.Buffs.GetBuff(KurshuBlueprints.Separation)?.SelectComponents<KwrDivineSeparation>().FirstOrDefault()?.Count ?? 0;
            int presetAttack = unit.Stats.AdditionalAttackBonus.Modifiers.Where(m => m.ModDescriptor == Kingmaker.Enums.ModifierDescriptor.Difficulty).Sum(m => m.ModValue);
            int presetAC = unit.Stats.AC.Modifiers.Where(m => m.ModDescriptor == Kingmaker.Enums.ModifierDescriptor.Difficulty).Sum(m => m.ModValue);
            sb.Append($"  maneuver chain: printed CMB {KurshuProfile.Sheet.First(l => l.Name == "CMB").Printed}/grapple {KurshuProfile.Sheet.First(l => l.Name == "CMB to grapple").Printed}/CMD {KurshuProfile.Sheet.First(l => l.Name == "CMD").Printed}; hard mode {KurshuProfile.Authored("CMB")}/{KurshuProfile.Authored("CMB to grapple")}/{KurshuProfile.Authored("CMD")}; separation now {separation} (authored {KurshuProfile.HardModeNegativeLevels}); preset attack {presetAttack:+0;-0} AC {presetAC:+0;-0}; expected {KurshuProfile.Undrained("CMB") - separation + presetAttack}/{KurshuProfile.Undrained("CMB to grapple") - separation + presetAttack}/{KurshuProfile.Undrained("CMD") - separation + presetAC}; native {cmb.Result}/{grapple}/{cmd.Result}\n");
            KurshuState held = State?.Kurshu;
            sb.Append($"  hold: {(held == null || string.IsNullOrEmpty(held.HeldId) ? "none" : held.HeldName + " since " + held.HeldSince.ToString("0") + (held.MaintainDue ? ", maintenance due" : string.Empty))}; grabs {held?.Grabs} maintains {held?.Maintains}; last release '{held?.LastRelease}'; grappling buff {unit.Buffs.GetBuff(KurshuBlueprints.Grappling) != null}\n");
            sb.Append($"  native feats [{string.Join(", ", KurshuBlueprints.NativeFeats.Select(f => f.name + (d.HasFact(f) ? " present" : " MISSING")).ToArray())}]; Improved Iron Will reroll {(unit.Buffs.GetBuff(KurshuBlueprints.ImprovedIronWill) != null ? "unused" : "spent or absent")}\n");
            sb.Append($"  SR {unit.Descriptor.Get<UnitPartSpellResistance>()?.GetValue(null)}\n");
            foreach (var limb in unit.Body.AdditionalLimbs.Select(l => l.MaybeWeapon).Where(w => w != null))
            {
                var attack = Rulebook.Trigger(new RuleCalculateAttackBonusWithoutTarget(unit, limb, 0));
                var weaponStats = Rulebook.Trigger(new RuleCalculateWeaponStats(unit, limb));
                sb.Append($"  {limb.Blueprint.name}: attack {attack.Result:+0;-0}, damage {weaponStats.DamageDescription.FirstOrDefault()?.Dice}+{weaponStats.DamageDescription.FirstOrDefault()?.Bonus}, secondary {limb.IsSecondary}\n");
            }
            SanctumState state = State;
            foreach (var ability in d.Abilities.Enumerable.Where(a => KurshuBlueprints.UseKeys.ContainsKey(a.Blueprint)))
            {
                AbilityParams p = ability.Data.CalculateParams();
                string key = KurshuBlueprints.UseKeys[ability.Blueprint];
                sb.Append($"  {ability.Blueprint.name}: CL {p.CasterLevel} DC {p.DC} SL {p.SpellLevel} concentration {p.Concentration}; native {d.Resources.GetResourceAmount(KurshuBlueprints.Resources[key])}, ledger {(state == null ? -1 : KurshuRules.Remaining(state.Kurshu, key))}\n");
            }
            sb.Append($"  buffs [{string.Join(", ", unit.Buffs.Enumerable.Select(b => b.Blueprint.name + (b.IsPermanent ? string.Empty : $" {b.TimeLeft.TotalSeconds:0}s")).ToArray())}]\n");
            sb.Append($"  facts [{string.Join(", ", d.Progression.Features.Enumerable.Select(f => f.Blueprint.name).ToArray())}]");
            return sb.ToString();
        }
    }
}
