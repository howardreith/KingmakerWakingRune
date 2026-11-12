using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Controllers.Brain;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Lashmistress
{
    /// <summary>
    /// The A4 defenders at play: placed once the party has the game back, re-bound on every load and never respawned, prepared
    /// by their own native actions, and committed to the printed countdown by a real engagement only.
    /// </summary>
    internal static class LashmistressEncounter
    {
        /// <summary>Page 11: Vandiana west of the altar; the others are placed in the atrium's lobes by this implementation.</summary>
        private static readonly Dictionary<string, GridPoint> Places = new Dictionary<string, GridPoint>
        {
            { DefenderKeys.Vandiana, new GridPoint(13.9f, 9.6f) },
            { DefenderKeys.Agarik, new GridPoint(17.7f, 7.4f) },
            { DefenderKeys.Jorgas, new GridPoint(18.1f, 11.3f) },
            { DefenderKeys.Naga, new GridPoint(18.6f, 9.3f) },
        };

        private static readonly Dictionary<string, UnitEntityData> s_Bound = new Dictionary<string, UnitEntityData>();
        private static readonly Dictionary<string, int> s_DirectedRound = new Dictionary<string, int>();
        private static bool s_Logged;
        private static bool s_ProblemReported;

        private static SanctumState State => WardRuntime.State;

        public static void Note(string text)
        {
            Log.Info($"A4 [{WardRuntime.Now:0.0}]: {text}");
        }

        /// <summary>The live entity for a defender, or null if it was never placed in this save.</summary>
        public static UnitEntityData Find(string key)
        {
            SanctumState state = State;
            if (state == null || state.Lashmistress == null || !state.Lashmistress.Defenders.TryGetValue(key, out DefenderRecord record) || string.IsNullOrEmpty(record.EntityId))
            {
                return null;
            }
            if (!s_Bound.TryGetValue(key, out UnitEntityData unit) || unit == null || unit.UniqueId != record.EntityId || unit.Descriptor == null)
            {
                unit = Game.Instance.State.Units.FirstOrDefault(u => u.UniqueId == record.EntityId)
                    ?? Game.Instance.State.Units.FirstOrDefault(u => u.Descriptor.Get<LashmistressPart>()?.DefenderKey == key);
                s_Bound[key] = unit;
            }
            return unit;
        }

        /// <summary>The canonical Vandiana when the encounter is present (living or dead); null for a save that never had her.</summary>
        public static UnitEntityData Vandiana => State?.Lashmistress != null && State.Lashmistress.IsPresent ? Find(DefenderKeys.Vandiana) : null;

        public static string KeyOf(UnitEntityData unit)
        {
            return unit?.Descriptor.Get<LashmistressPart>()?.DefenderKey;
        }

        public static bool IsDefender(UnitEntityData unit)
        {
            return KeyOf(unit) != null;
        }

        private static IEnumerable<UnitEntityData> Living()
        {
            return DefenderKeys.All.Select(Find).Where(u => u != null && !u.Descriptor.State.IsDead);
        }

        /// <summary>The player's side: the party, their pets and summons.</summary>
        public static bool IsPartySide(UnitEntityData unit)
        {
            if (unit == null)
            {
                return false;
            }
            if (unit.IsPlayerFaction)
            {
                return true;
            }
            UnitEntityData summoner = unit.Get<UnitPartSummonedMonster>()?.Summoner;
            return summoner != null && summoner.IsPlayerFaction;
        }

        public static void AfterLoad(SanctumState state)
        {
            s_Bound.Clear();
            s_DirectedRound.Clear();
            s_Logged = false;
            LashmistressState a4 = state.Lashmistress;
            if (a4.Policy == LashmistressPolicy.AwaitingUpgradeCheck)
            {
                var facts = new UpgradeFacts
                {
                    KurshuEncounterActive = state.Kurshu.Policy == Encounter.KurshuPolicy.Active,
                    PartyOutsideAtrium = Game.Instance.Player.Party.Concat(Game.Instance.Player.Party.Select(u => u.Descriptor.Pet).Where(p => p != null)).All(u => SanctumPlan.SpaceAt(GridX(u.Position), GridY(u.Position)) != SanctumSpace.Atrium),
                    A4DoorEverOpened = state.Ritual.A4DoorHeard,
                    HoundEverNoticed = state.Hound.FirstBarkAt >= 0d,
                    GlyphTriggered = state.Glyph.Triggered,
                    ConfrontationStarted = state.Ritual.ConfrontationStarted,
                    AwakeningDue = state.Ritual.AwakeningDue,
                    EmeraldDestroyed = state.Gem.Destroyed,
                };
                bool upgraded = LashmistressRules.DecideUpgrade(a4, facts);
                Note($"older save checked for the A4 defenders: {(upgraded ? "upgraded" : "left as it was")}: {a4.UpgradeReason}.");
            }
            foreach (string key in DefenderKeys.All)
            {
                UnitEntityData unit = Find(key);
                if (unit != null)
                {
                    // Nothing is refilled, recast or respawned on a load; the brain is the unit's own again.
                    Note($"re-bound {key}: {unit.UniqueId} hp {unit.Descriptor.HPLeft}/{unit.Descriptor.MaxHP} dead {unit.Descriptor.State.IsDead}.");
                }
            }
            RetireFixture(a4);
        }

        public static void Tick(SanctumState state, double now)
        {
            LashmistressState a4 = state.Lashmistress;
            if (a4.Policy == LashmistressPolicy.LegacyNotInserted && !a4.LegacyNoticeShown)
            {
                a4.LegacyNoticeShown = true;
                WardRuntime.Say("This save was made inside the sanctum in a way that makes adding Vandiana's defenders unsafe, so A4 is left as it was. To meet them, load a save from before A4 was reached.");
                Note($"legacy notice shown once ({a4.UpgradeReason}).");
            }
            if (!a4.IsPresent)
            {
                return;
            }
            if (LashmistressBlueprints.Units.Count < DefenderKeys.All.Length)
            {
                if (!s_ProblemReported)
                {
                    s_ProblemReported = true;
                    Log.Error($"The A4 defenders are unavailable: {LashmistressBlueprints.Problem}.");
                }
                return;
            }
            if (DefenderKeys.All.Any(k => string.IsNullOrEmpty(a4.Defender(k).EntityId)))
            {
                if (ControlRestored(out string waiting))
                {
                    Spawn(state, now);
                }
                else if (!s_Logged)
                {
                    s_Logged = true;
                    Note($"waiting to place the defenders: {waiting}.");
                }
                return;
            }
            UnitEntityData naga = Find(DefenderKeys.Naga);
            if (naga != null)
            {
                ApplyScale(naga);
            }
            if (!s_Logged)
            {
                s_Logged = true;
                foreach (string key in DefenderKeys.All)
                {
                    UnitEntityData unit = Find(key);
                    if (unit != null)
                    {
                        Log.Info($"A4 sheet [first tick] {Report(unit)}");
                    }
                }
            }
            ObserveDeaths(state, now);
            DetectEngagement(state, now);
            if (a4.Engaged)
            {
                foreach (string key in LashmistressRules.OnCombat(a4, now).ToList())
                {
                    Note($"combat: {key} requested.");
                }
            }
            Prepare(state, now);
            Direct(state, now);
            if (state.Ritual.AwakeningDue && !a4.AwakeningNoted)
            {
                a4.AwakeningNoted = true;
                Note($"awakening due ({state.Ritual.Reason}, stagger {state.Ritual.AwakeningOpeningStagger}) with {Living().Count()} defender(s) still standing; the A4 fight continues. Krune's own encounter is the next milestone and is not implemented here.");
            }
        }

        /// <summary>The same gate as Kurshu's: nothing is placed under a loading screen, a fade or a dialogue.</summary>
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

        private static void Spawn(SanctumState state, double now)
        {
            LashmistressState a4 = state.Lashmistress;
            foreach (string key in DefenderKeys.All)
            {
                DefenderRecord record = a4.Defender(key);
                if (!string.IsNullOrEmpty(record.EntityId))
                {
                    continue;
                }
                GridPoint place = Places[key];
                Vector3 at = ObstacleAnalyzer.GetNearestNode(World(place)).clampedPosition;
                UnitEntityData unit = Game.Instance.EntityCreator.SpawnUnit(LashmistressBlueprints.Units[key], at, Quaternion.Euler(0f, 90f, 0f), Game.Instance.State.LoadedAreaState.MainState);
                unit.Descriptor.Ensure<LashmistressPart>().DefenderKey = key;
                record.EntityId = unit.UniqueId;
                s_Bound[key] = unit;
                // The day begins with every printed daily use: gear put on after the levels (a headband) raises maximums the
                // level-up's rest had already filled. Done once, here; nothing refills them again.
                var resources = new List<BlueprintScriptableObject>();
                foreach (BlueprintScriptableObject resource in unit.Descriptor.Resources)
                {
                    resources.Add(resource);
                }
                foreach (BlueprintScriptableObject resource in resources)
                {
                    unit.Descriptor.Resources.Restore(resource);
                }
                Calibrate(key, unit);
                if (key != DefenderKeys.Naga)
                {
                    // Vandiana and her two champions of the faith: Lissala's own, whom her glyph does not strike.
                    unit.Descriptor.AddFact(WardBlueprints.LissalaWorshipper);
                }
                Note($"{key} placed at ({place.X:0.0},{place.Y:0.0}) as {unit.UniqueId}.");
            }
            a4.SpawnedAt = now;
            InitializeVandiana(state, now);
            RetireFixture(a4);
            foreach (string key in DefenderKeys.All)
            {
                Log.Info($"A4 sheet [placed] {Report(Find(key))}");
            }
        }

        /// <summary>
        /// The printed sheet's base values through this game's class tables: only the difference is added, once, with the
        /// difficulty preset's own modifiers (and what its ability bonuses add to these) left out, so the preset still applies.
        /// </summary>
        private static void Calibrate(string key, UnitEntityData unit)
        {
            var targets = new Dictionary<StatType, int>();
            switch (key)
            {
                case DefenderKeys.Vandiana:
                    targets[StatType.HitPoints] = LashmistressProfile.VandianaBaseHitPoints;
                    targets[StatType.BaseAttackBonus] = LashmistressProfile.VandianaBaseAttack;
                    targets[StatType.SaveFortitude] = LashmistressProfile.VandianaFortitude;
                    targets[StatType.SaveReflex] = LashmistressProfile.VandianaReflexBase;
                    targets[StatType.SaveWill] = LashmistressProfile.VandianaWill;
                    targets[StatType.SkillPerception] = 12;
                    break;
                case DefenderKeys.Agarik:
                case DefenderKeys.Jorgas:
                    targets[StatType.HitPoints] = LashmistressProfile.AdvancedHitPoints(LashmistressProfile.BlackstrikeBaseHitPoints, LashmistressProfile.BlackstrikeHitDice);
                    targets[StatType.BaseAttackBonus] = LashmistressProfile.BlackstrikeBaseAttack;
                    targets[StatType.SaveFortitude] = LashmistressProfile.BlackstrikeFortitude + 2;
                    targets[StatType.SaveReflex] = LashmistressProfile.BlackstrikeReflex + 2;
                    targets[StatType.SaveWill] = LashmistressProfile.BlackstrikeWill + 2;
                    targets[StatType.SkillPerception] = 12 + 2;
                    targets[StatType.SkillStealth] = 14 + 2;
                    break;
                case DefenderKeys.Naga:
                    targets[StatType.HitPoints] = LashmistressProfile.AdvancedHitPoints(LashmistressProfile.NagaBaseHitPoints, LashmistressProfile.NagaHitDice);
                    targets[StatType.BaseAttackBonus] = 7;
                    targets[StatType.SaveFortitude] = LashmistressProfile.NagaFortitude + 2;
                    targets[StatType.SaveReflex] = LashmistressProfile.NagaReflex + 2;
                    targets[StatType.SaveWill] = LashmistressProfile.NagaWill + 2;
                    targets[StatType.SkillPerception] = 19 + 2;
                    targets[StatType.SkillStealth] = 18 + 2;
                    break;
            }
            var offsets = new Dictionary<StatType, int>();
            foreach (var pair in targets)
            {
                int native = WithoutPreset(unit, pair.Key);
                offsets[pair.Key] = pair.Value - native;
            }
            KwrA4Calibration.Pending = offsets;
            unit.Descriptor.AddBuff(LashmistressBlueprints.Calibration, unit);
            Note($"{key} calibrated once: {string.Join(", ", offsets.Select(o => $"{o.Key} {o.Value:+0;-0} (native {targets[o.Key] - o.Value}, printed {targets[o.Key]})").ToArray())}.");
        }

        /// <summary>A statistic as the unit would show it without the difficulty preset.</summary>
        private static int WithoutPreset(UnitEntityData unit, StatType type)
        {
            int Preset(StatType stat) => unit.Stats.GetStat(stat).Modifiers.Where(m => m.ModDescriptor == ModifierDescriptor.Difficulty).Sum(m => m.ModValue);
            int ModLoss(StatType ability)
            {
                int score = unit.Stats.GetStat(ability).ModifiedValue;
                return Abilities.Mod(score) - Abilities.Mod(score - Preset(ability));
            }
            if (type == StatType.HitPoints)
            {
                return unit.Descriptor.MaxHP - ModLoss(StatType.Constitution) * unit.Descriptor.Progression.CharacterLevel;
            }
            int value = unit.Stats.GetStat(type).ModifiedValue - Preset(type);
            switch (type)
            {
                case StatType.SaveFortitude:
                    return value - ModLoss(StatType.Constitution);
                case StatType.SaveReflex:
                    return value - ModLoss(StatType.Dexterity);
                case StatType.SaveWill:
                case StatType.SkillPerception:
                    return value - ModLoss(StatType.Wisdom);
                case StatType.SkillStealth:
                    return value - ModLoss(StatType.Dexterity);
                default:
                    return value;
            }
        }

        /// <summary>
        /// Once: her two prepared books filled entry for entry, then what she cast before the party arrived spent from them,
        /// with those effects' remaining durations counted from the moment the sanctum was first entered.
        /// </summary>
        private static void InitializeVandiana(SanctumState state, double now)
        {
            LashmistressState a4 = state.Lashmistress;
            UnitEntityData vandiana = Find(DefenderKeys.Vandiana);
            if (vandiana == null || a4.BaselineApplied)
            {
                return;
            }
            Spellbook cleric = vandiana.Descriptor.GetSpellbook(LashmistressBlueprints.ClericClassBlueprint);
            Spellbook wizard = vandiana.Descriptor.GetSpellbook(LashmistressBlueprints.WizardClassBlueprint);
            if (cleric == null || wizard == null)
            {
                Log.Error($"Vandiana has no {(cleric == null ? "cleric" : "wizard")} spellbook; her preparation cannot be applied.", null);
                return;
            }
            Prepare(cleric, VandianaSpells.Cleric, LashmistressBlueprints.VandianaClericSpecial, "cleric");
            Prepare(wizard, VandianaSpells.Wizard, LashmistressBlueprints.VandianaWizardSpecial, "wizard");
            double castAt = state.InitializedAt >= 0d ? state.InitializedAt : now;
            double elapsed = Math.Max(0d, now - castAt);
            foreach (string source in VandianaSpells.CastBeforeArrival)
            {
                Spellbook book = VandianaSpells.Cleric.Any(e => e.Source == source) ? cleric : wizard;
                BlueprintAbility ability = LashmistressBlueprints.PreparedAbilities[source];
                SpellSlot slot = book.GetAllMemorizedSpells().FirstOrDefault(s => s.Spell?.Blueprint == ability && s.Available);
                bool spent = slot != null && book.Spend(slot.Spell);
                Note($"before arrival: {source} spent from her {(book == cleric ? "cleric" : "wizard")} book: {spent}.");
            }
            int arcane = wizard.CasterLevel;
            double hours = arcane * 3600d;
            if (hours - elapsed > 0d)
            {
                vandiana.Descriptor.AddBuff(LashmistressBlueprints.MageArmor, SpellContext(vandiana, LashmistressBlueprints.PreparedAbilities["MageArmor"], arcane, 1), TimeSpan.FromSeconds(hours - elapsed));
                vandiana.Descriptor.AddBuff(LashmistressBlueprints.PreparedFalseLife, SpellContext(vandiana, LashmistressBlueprints.PreparedAbilities["FalseLife"], arcane, 2), TimeSpan.FromSeconds(hours - elapsed));
            }
            vandiana.Descriptor.AddBuff(LashmistressBlueprints.ImprovedGreatFortitude, vandiana);
            a4.BaselineApplied = true;
            a4.BaselineAt = now;
            Note($"baseline applied once: Mage Armor and False Life cast {elapsed:0} s ago at arcane CL {arcane} ({(hours - elapsed) / 60d:0} min left); the hound, alarm and glyph slots spent; divine CL {cleric.CasterLevel}.");
        }

        private static void Prepare(Spellbook book, PreparedEntry[] entries, BlueprintSpellList special, string what)
        {
            Func<Spellbook, int> vandianaStat = b => b.Owner.Stats.GetStat(b.Blueprint.CastingAttribute).ModifiedValue;
            foreach (SpellSlot slot in book.GetAllMemorizedSpells().ToList())
            {
                book.ForgetMemorized(slot);
            }
            book.AddSpecialList(special);
            foreach (PreparedEntry entry in entries)
            {
                BlueprintAbility ability = LashmistressBlueprints.PreparedAbilities[entry.Source];
                if (!book.IsKnown(ability))
                {
                    book.AddKnown(entry.Level, ability);
                }
            }
            // Entries that may also go in the domain or school slot are placed last, so they never take a common slot that an
            // entry with nowhere else to go needs.
            bool Special(PreparedEntry e) => book.IsSpellInSpecialList(LashmistressBlueprints.PreparedAbilities[e.Source], e.Level);
            var failed = new List<string>();
            foreach (PreparedEntry entry in entries.Where(e => !Special(e)).Concat(entries.Where(Special)))
            {
                if (!book.Memorize(new AbilityData(LashmistressBlueprints.PreparedAbilities[entry.Source], book)))
                {
                    failed.Add($"{entry.Name} ({entry.Level})");
                }
            }
            book.Rest();
            Note($"{what} book slots by level (common/special): {string.Join(" ", Enumerable.Range(1, 5).Select(l => $"L{l} {book.CalcSlotsLimit(l, SpellSlotType.Common)}/{book.CalcSlotsLimit(l, SpellSlotType.Favorite)}").ToArray())}; special-eligible [{string.Join(", ", entries.Where(Special).Select(e => e.Name).ToArray())}]; casting stat {book.Blueprint.CastingAttribute} {vandianaStat(book)}.");
            Note($"{what} book CL {book.CasterLevel}: prepared {book.GetAllMemorizedSpells().Count(s => s.Spell != null)} of {entries.Length}{(failed.Count == 0 ? string.Empty : "; NOT placed: " + string.Join(", ", failed.ToArray()))}.");
        }

        private static MechanicsContext SpellContext(UnitEntityData caster, BlueprintAbility spell, int casterLevel, int spellLevel)
        {
            var context = new MechanicsContext(caster, caster.Descriptor, spell);
            Harmony12.AccessTools.Field(typeof(MechanicsContext), "m_Params").SetValue(context, new AbilityParams { CasterLevel = casterLevel, SpellLevel = spellLevel, DC = 10 + spellLevel });
            return context;
        }

        /// <summary>
        /// The hidden support figure that stood in for her before WR-06 is taken out of the game once the real Vandiana exists;
        /// it keeps only its identity so saved effects that name it still resolve. It is never resolved, protected or recreated.
        /// </summary>
        private static void RetireFixture(LashmistressState a4)
        {
            if (!a4.IsPresent || string.IsNullOrEmpty(a4.Defender(DefenderKeys.Vandiana).EntityId))
            {
                return;
            }
            WardFixtures.Live.Remove("Vandiana");
            UnitEntityData fixture = WardFixtures.FindRetired("Vandiana");
            if (fixture != null && fixture.IsInGame)
            {
                fixture.IsInGame = false;
                Note($"support fixture Vandiana {fixture.UniqueId} retired (out of the game; kept for saved contexts only).");
            }
            if (!a4.FixtureRetired)
            {
                a4.FixtureRetired = true;
            }
        }

        private static void ApplyScale(UnitEntityData unit)
        {
            UnitEntityView view = unit.View;
            if (view == null)
            {
                return;
            }
            // The hydra donor is drawn for a Huge body; the naga is Large: one engine size step down.
            Vector3 wanted = view.OriginalScale * view.GetSizeScale() * 0.66f;
            if ((view.transform.localScale - wanted).sqrMagnitude > 1e-4f)
            {
                view.transform.localScale = wanted;
            }
        }

        private static void ObserveDeaths(SanctumState state, double now)
        {
            LashmistressState a4 = state.Lashmistress;
            foreach (string key in DefenderKeys.All)
            {
                UnitEntityData unit = Find(key);
                if (unit == null || !unit.Descriptor.State.IsDead || a4.Defender(key).Slain)
                {
                    continue;
                }
                bool last = LashmistressRules.Slain(a4, key, now);
                Note($"{key} slain at {now:0}.");
                foreach (string prep in Prep.All.Where(p => Prep.Actor(p) == key))
                {
                    LashmistressRules.Abandon(a4, prep, key + " is dead");
                }
                if (key == DefenderKeys.Agarik)
                {
                    LashmistressRules.Abandon(a4, Prep.NagaInvisibilityAgarik, "Agarik is dead");
                }
                if (last && !a4.DefeatReported)
                {
                    a4.DefeatReported = true;
                    WardRuntime.Say("Vandiana and the last of her defenders lie still. Her rite was already complete: the light in the west does not fade.");
                    Note($"ALL A4 DEFENDERS DEFEATED at {now:0}; awakening due {state.Ritual.AwakeningDue} ({state.Ritual.Reason}); the countdown is not cancelled.");
                }
            }
        }

        /// <summary>The first real engagement commits the printed countdown through the one existing commit.</summary>
        public static void Engage(EngagementCause cause, string detail, UnitEntityData by)
        {
            SanctumState state = State;
            if (state?.Lashmistress == null || !state.Lashmistress.IsPresent)
            {
                return;
            }
            if (!LashmistressRules.Engage(state.Lashmistress, cause, detail, WardRuntime.Now))
            {
                return;
            }
            Note($"ENGAGED: {cause} ({detail}).");
            WardRitual.CommitConfrontation(by ?? Game.Instance.Player.MainCharacter.Value);
        }

        /// <summary>An aggressive act against the rite (the altar's tools, the emerald): engages the defenders and the clock.</summary>
        public static void RitualAttacked(UnitEntityData by, string what)
        {
            SanctumState state = State;
            if (state?.Lashmistress != null && state.Lashmistress.IsPresent)
            {
                Engage(EngagementCause.RitualAttacked, what, by);
                return;
            }
            WardRitual.CommitConfrontation(by);
        }

        public static void EmeraldStruck(UnitEntityData by)
        {
            SanctumState state = State;
            if (state?.Lashmistress != null && state.Lashmistress.IsPresent && IsPartySide(by))
            {
                Engage(EngagementCause.RitualAttacked, "the emerald was struck", by);
            }
        }

        private static void DetectEngagement(SanctumState state, double now)
        {
            if (state.Lashmistress.Engaged)
            {
                return;
            }
            foreach (UnitEntityData defender in Living())
            {
                if (!defender.IsInCombat)
                {
                    continue;
                }
                UnitEntityData seen = Game.Instance.Player.Party.FirstOrDefault(u => u.IsInCombat && !u.Descriptor.State.IsDead && (SanctumPlan.SpaceAt(GridX(u.Position), GridY(u.Position)) == SanctumSpace.Atrium || u.HasLOS(defender)));
                if (seen != null)
                {
                    Engage(EngagementCause.CombatJoined, $"{defender.CharacterName} and {seen.CharacterName} are in combat", seen);
                    return;
                }
            }
        }

        /// <summary>A hostile rule between the party's side and a defender, from the global observer.</summary>
        public static void OnHostileRule(UnitEntityData initiator, UnitEntityData target, string what)
        {
            if (initiator == null || target == null || initiator == target)
            {
                return;
            }
            bool partyOnDefender = IsPartySide(initiator) && IsDefender(target);
            bool defenderOnParty = IsDefender(initiator) && IsPartySide(target);
            if (partyOnDefender || defenderOnParty)
            {
                Engage(EngagementCause.HostileAct, $"{what}: {initiator.CharacterName} on {target.CharacterName}", partyOnDefender ? initiator : target);
            }
        }

        public static void OnAwareness(SanctumState state)
        {
            if (state?.Lashmistress == null || !state.Lashmistress.IsPresent)
            {
                return;
            }
            foreach (string key in LashmistressRules.OnAwareness(state.Lashmistress, state.Ritual.DarkvisionPrepared, WardRuntime.Now).ToList())
            {
                Note($"awareness: {key} requested.");
            }
        }

        public static bool OnDoorHeard(SanctumState state)
        {
            if (state?.Lashmistress == null || !state.Lashmistress.IsPresent)
            {
                return false;
            }
            foreach (string key in LashmistressRules.OnDoorHeard(state.Lashmistress, WardRuntime.Now).ToList())
            {
                Note($"east door heard: {key} requested.");
            }
            return true;
        }

        /// <summary>
        /// Requested preparations become native commands from their actors' own potions and slots, one at a time, only when the
        /// actor is free to act (its own turn in turn-based combat). An effect seen on its target marks it performed.
        /// </summary>
        private static void Prepare(SanctumState state, double now)
        {
            LashmistressState a4 = state.Lashmistress;
            foreach (PrepRecord prep in a4.Preparations.Values.Where(p => p.Issued && !p.Performed && !p.Abandoned).ToList())
            {
                if (EffectSeen(prep.Key))
                {
                    LashmistressRules.Performed(a4, prep.Key, "effect observed on its target", now);
                    Note($"preparation {prep.Key} performed.");
                }
            }
            bool turnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
            foreach (string key in DefenderKeys.All)
            {
                UnitEntityData unit = Find(key);
                string next = LashmistressRules.NextFor(a4, key);
                if (unit == null || next == null)
                {
                    continue;
                }
                if (unit.Descriptor.State.IsDead)
                {
                    continue;
                }
                PrepRecord prep = a4.Preparation(next);
                if (!unit.Descriptor.State.CanAct || !unit.Commands.Empty || (turnBased && unit.IsInCombat && TurnBased.Controllers.CombatController.CurrentUnit != unit))
                {
                    continue;
                }
                if (prep.Issued && now - prep.IssuedAt < 2 * WardProfile.RoundSeconds)
                {
                    continue;
                }
                if (prep.Attempts >= 3)
                {
                    LashmistressRules.Abandon(a4, next, "three attempts did not take effect");
                    Note($"preparation {next} abandoned after three attempts.");
                    continue;
                }
                if (EffectSeen(next))
                {
                    LashmistressRules.Performed(a4, next, "already in effect", now);
                    continue;
                }
                UnitCommand command = PrepCommand(next, unit, out string refusal);
                if (command == null)
                {
                    LashmistressRules.Abandon(a4, next, refusal);
                    Note($"preparation {next} cannot happen: {refusal}.");
                    continue;
                }
                unit.Commands.InterruptAiCommands();
                unit.Commands.Run(command);
                LashmistressRules.Issued(a4, next, now);
                Note($"{unit.CharacterName} begins {next} (attempt {prep.Attempts}).");
            }
        }

        private static bool EffectSeen(string key)
        {
            switch (key)
            {
                case Prep.VandianaDarkvision:
                    return Has(DefenderKeys.Vandiana, LashmistressBlueprints.DarkvisionBuff);
                case Prep.VandianaCatsGrace:
                    return HasNamed(DefenderKeys.Vandiana, "CatsGraceBuff");
                case Prep.NagaInvisibilitySelf:
                    return HasNamed(DefenderKeys.Naga, "InvisibilityBuff");
                case Prep.NagaInvisibilityAgarik:
                    return HasNamed(DefenderKeys.Agarik, "InvisibilityBuff");
                case Prep.AgarikBullsStrength:
                    return HasNamed(DefenderKeys.Agarik, "BullsStrengthBuff");
                case Prep.JorgasBullsStrength:
                    return HasNamed(DefenderKeys.Jorgas, "BullsStrengthBuff");
                case Prep.AgarikBlur:
                    return HasNamed(DefenderKeys.Agarik, "BlurBuff");
                case Prep.JorgasBlur:
                    return HasNamed(DefenderKeys.Jorgas, "BlurBuff");
                default:
                    return false;
            }
        }

        private static bool Has(string key, BlueprintBuff buff)
        {
            UnitEntityData unit = Find(key);
            return unit != null && unit.Buffs.GetBuff(buff) != null;
        }

        private static bool HasNamed(string key, string buffName)
        {
            UnitEntityData unit = Find(key);
            return unit != null && unit.Buffs.Enumerable.Any(b => b.Blueprint.name == buffName);
        }

        private static UnitCommand PrepCommand(string key, UnitEntityData unit, out string refusal)
        {
            refusal = null;
            switch (key)
            {
                case Prep.VandianaDarkvision:
                    return CastPrepared(unit, "Darkvision", unit, out refusal);
                case Prep.VandianaCatsGrace:
                    return DrinkPotion(unit, LashmistressBlueprints.PotionCatsGrace, out refusal);
                case Prep.AgarikBullsStrength:
                case Prep.JorgasBullsStrength:
                    return DrinkPotion(unit, LashmistressBlueprints.PotionBullsStrength, out refusal);
                case Prep.AgarikBlur:
                case Prep.JorgasBlur:
                    return DrinkPotion(unit, LashmistressBlueprints.PotionBlur, out refusal);
                case Prep.NagaInvisibilitySelf:
                    return CastNaga(unit, unit, out refusal);
                case Prep.NagaInvisibilityAgarik:
                    {
                        UnitEntityData agarik = Find(DefenderKeys.Agarik);
                        if (agarik == null || agarik.Descriptor.State.IsDead)
                        {
                            refusal = "Agarik is dead";
                            return null;
                        }
                        return CastNaga(unit, agarik, out refusal);
                    }
                default:
                    refusal = "unknown preparation";
                    return null;
            }
        }

        private static UnitCommand CastPrepared(UnitEntityData unit, string source, UnitEntityData target, out string refusal)
        {
            refusal = null;
            BlueprintAbility ability = LashmistressBlueprints.PreparedAbilities[source];
            Spellbook book = unit.Descriptor.Spellbooks.FirstOrDefault(b => b.CanSpend(ability));
            if (book == null)
            {
                refusal = $"no prepared {source} left";
                return null;
            }
            return UnitUseAbility.CreateCastCommand(new AbilityData(ability, book), new TargetWrapper(target));
        }

        private static UnitCommand CastNaga(UnitEntityData unit, UnitEntityData target, out string refusal)
        {
            refusal = null;
            AbilityData data = unit.Descriptor.Abilities.GetAbility(LashmistressBlueprints.NagaAbilities["Invisibility"])?.Data;
            if (data == null || !data.IsAvailableForCast)
            {
                refusal = "no 2nd-level slot left";
                return null;
            }
            return UnitUseAbility.CreateCastCommand(data, new TargetWrapper(target));
        }

        private static UnitCommand DrinkPotion(UnitEntityData unit, string potionGuid, out string refusal)
        {
            refusal = null;
            ItemEntity potion = unit.Body.QuickSlots.Select(s => s.MaybeItem).FirstOrDefault(i => i != null && i.Blueprint.AssetGuid == potionGuid && i.Charges > 0);
            AbilityData data = potion?.Ability?.Data;
            if (data == null)
            {
                refusal = "the potion is gone";
                return null;
            }
            return UnitUseAbility.CreateCastCommand(data, new TargetWrapper(unit));
        }

        /// <summary>
        /// Two choices the brain cannot make: a Blackstrike slipping back into the shadows (hide in plain sight) when no foe is
        /// adjacent, and Vandiana's Dimension Door out of melee when she is hurt and pressed. Each at most once a round.
        /// </summary>
        private static void Direct(SanctumState state, double now)
        {
            bool turnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
            int round = (int)(now / WardProfile.RoundSeconds);
            foreach (string key in DefenderKeys.All)
            {
                UnitEntityData unit = Find(key);
                if (unit == null || unit.Descriptor.State.IsDead || !unit.IsInCombat || !unit.Descriptor.State.CanAct || LashmistressRules.NextFor(state.Lashmistress, key) != null)
                {
                    continue;
                }
                if (turnBased && TurnBased.Controllers.CombatController.CurrentUnit != unit)
                {
                    continue;
                }
                if (s_DirectedRound.TryGetValue(key, out int last) && last == round)
                {
                    continue;
                }
                if (unit.Commands.Raw.Any(c => c != null && c.AiAction == null && !c.IsFinished))
                {
                    continue;
                }
                List<UnitEntityData> foes = Foes(unit).ToList();
                int adjacent = foes.Count(f => f.DistanceTo(unit) - f.Corpulence - unit.Corpulence <= 5f * SanctumPlan.MetersPerFoot + 0.3f);
                if ((key == DefenderKeys.Agarik || key == DefenderKeys.Jorgas) && adjacent == 0 && !unit.Descriptor.State.IsInStealth && (!turnBased || unit.HasMoveAction()))
                {
                    AbilityData hide = unit.Descriptor.Abilities.GetAbility(LashmistressBlueprints.HideInPlainSight)?.Data;
                    if (hide != null && hide.IsAvailableForCast)
                    {
                        s_DirectedRound[key] = round;
                        unit.Commands.Run(UnitUseAbility.CreateCastCommand(hide, unit));
                        Note($"{unit.CharacterName}: no foe adjacent, hides in plain sight.");
                    }
                }
                if (key == DefenderKeys.Vandiana && adjacent >= 1 && unit.Descriptor.HPLeft < unit.Descriptor.MaxHP * 0.6f && (!turnBased || unit.HasStandardAction()))
                {
                    Vector3? point = EscapePoint(unit, foes);
                    BlueprintAbility door = LashmistressBlueprints.PreparedAbilities["DimensionDoorCasterOnly"];
                    Spellbook book = unit.Descriptor.Spellbooks.FirstOrDefault(b => b.CanSpend(door));
                    if (point.HasValue && book != null)
                    {
                        var data = new AbilityData(door, book);
                        if (data.CanTarget(new TargetWrapper(point.Value)))
                        {
                            s_DirectedRound[key] = round;
                            unit.Commands.InterruptAiCommands();
                            unit.Commands.Run(UnitUseAbility.CreateCastCommand(data, new TargetWrapper(point.Value)));
                            Note($"Vandiana, pressed in melee at {unit.Descriptor.HPLeft}/{unit.Descriptor.MaxHP}, casts Dimension Door to ({point.Value.x:0.0},{point.Value.z:0.0}).");
                        }
                    }
                }
            }
        }

        private static IEnumerable<UnitEntityData> Foes(UnitEntityData unit)
        {
            return Game.Instance.State.Units.Where(u => u.IsInGame && !u.Descriptor.State.IsDead && IsPartySide(u) && u.IsEnemy(unit));
        }

        /// <summary>A free point in A4/A5, at least 20 feet from every foe, never through stone.</summary>
        private static Vector3? EscapePoint(UnitEntityData unit, List<UnitEntityData> foes)
        {
            foreach (GridPoint p in SanctumPlan.RingsAround(GridX(unit.Position), GridY(unit.Position), 0.5f, 6f))
            {
                if (SanctumPlan.SpaceAt(p.X, p.Y) != SanctumSpace.Atrium || WardGeometry.InMist(p.X, p.Y))
                {
                    continue;
                }
                Vector3 world = World(p);
                if (foes.All(f => Vector3.Distance(f.Position, world) >= 20f * SanctumPlan.MetersPerFoot) && SanctumPlan.IsValidGround(p.X, p.Y, world.y))
                {
                    return ObstacleAnalyzer.GetNearestNode(world).clampedPosition;
                }
            }
            return null;
        }

        /// <summary>The sting's sleep poison: native Fortitude save with the poison's own context, so immunities and bonuses act.</summary>
        public static void Poison(UnitEntityData naga, UnitEntityData target)
        {
            var context = new MechanicsContext(naga, target.Descriptor, LashmistressBlueprints.NagaSleep);
            int dc = 10 + LashmistressProfile.NagaHitDice / 2 + naga.Stats.Constitution.Bonus;
            Harmony12.AccessTools.Field(typeof(MechanicsContext), "m_Params").SetValue(context, new AbilityParams { CasterLevel = LashmistressProfile.NagaHitDice, DC = dc, SpellLevel = 0 });
            if (target.Descriptor.State.HasConditionImmunity(UnitCondition.Sleeping))
            {
                Note($"Sting on {target.CharacterName}: immune to sleep.");
                return;
            }
            RuleSavingThrow save = context.TriggerRule(new RuleSavingThrow(target, SavingThrowType.Fortitude, dc));
            if (save.IsPassed)
            {
                Note($"Sting on {target.CharacterName}: Fortitude {save.RollResult} vs {dc}: the poison is shaken off (its one save cures it).");
                return;
            }
            int minutes = Rulebook.Trigger(new RuleRollDice(naga, new DiceFormula(2, DiceType.D4))).Result;
            Buff buff = target.Descriptor.AddBuff(LashmistressBlueprints.NagaSleep, context, TimeSpan.FromMinutes(minutes));
            Note($"Sting on {target.CharacterName}: Fortitude {save.RollResult} vs {dc} failed: {(buff != null ? $"asleep for {minutes} minutes" : "the poison was refused natively (immunity)")}.");
        }

        /// <summary>The live field, turned into the A4 tactics' inputs. Only enemies the brain itself has in its lists are counted.</summary>
        public static float Score(A4Aim aim, bool onTarget, DecisionContext context)
        {
            UnitEntityData unit = context.Unit;
            if (State == null || !IsDefender(unit))
            {
                return 0f;
            }
            List<UnitEntityData> enemies = context.Enemies.Select(t => t.Unit).Where(u => u != null && !u.Descriptor.State.IsDead).ToList();
            List<UnitEntityData> allies = Living().Where(u => u != unit).ToList();
            if (!onTarget)
            {
                var self = new A4Self
                {
                    HitPointFraction = unit.Descriptor.MaxHP > 0 ? (double)unit.Descriptor.HPLeft / unit.Descriptor.MaxHP : 1d,
                    VisibleEnemies = enemies.Count,
                    EnemiesAdjacent = enemies.Count(e => e.DistanceTo(unit) - e.Corpulence - unit.Corpulence <= 5f * SanctumPlan.MetersPerFoot + 0.3f),
                    LivingAlliesWithin30Feet = allies.Count(a => a.DistanceTo(unit) <= 30f * SanctumPlan.MetersPerFoot),
                    EnemiesWithin30Feet = enemies.Count(e => e.DistanceTo(unit) <= 30f * SanctumPlan.MetersPerFoot),
                    Displaced = unit.Buffs.Enumerable.Any(b => b.Blueprint.name.StartsWith("Displacement", StringComparison.Ordinal)),
                    InOwnDarkness = unit.Buffs.GetBuff(LashmistressBlueprints.DarknessAura) != null,
                    AlliesSeeInDarkness = allies.Any(a => a.Descriptor.HasFact(LashmistressBlueprints.DarkvisionSense)),
                    SummonedAlready = Game.Instance.State.Units.Any(u => u.Get<UnitPartSummonedMonster>()?.Summoner == unit && !u.Descriptor.State.IsDead),
                    DivineFavorActive = unit.Buffs.Enumerable.Any(b => b.Blueprint.name.StartsWith("DivineFavor", StringComparison.Ordinal)),
                };
                return LashmistressTactics.Actor(aim, self);
            }
            UnitEntityData target = context.Target?.Unit;
            if (target == null)
            {
                return 0f;
            }
            float Feet(UnitEntityData a, UnitEntityData b) => Vector3.Distance(a.Position, b.Position) / SanctumPlan.MetersPerFoot;
            Func<IEnumerable<UnitEntityData>, float, int> within = (group, feet) => group.Count(e => Feet(e, target) <= feet);
            Func<UnitEntityData, Alignment> align = u => u.Descriptor.Alignment.Value;
            List<UnitEntityData> burst15 = enemies.Where(e => Feet(e, target) <= 15f).ToList();
            List<UnitEntityData> burst20 = enemies.Where(e => Feet(e, target) <= 20f).ToList();
            List<UnitEntityData> alliesIn15 = allies.Concat(new[] { unit }).Where(a => Feet(a, target) <= 15f).ToList();
            List<UnitEntityData> alliesIn20 = allies.Concat(new[] { unit }).Where(a => Feet(a, target) <= 20f).ToList();
            bool orders = aim == A4Aim.OrdersWrath, unholy = aim == A4Aim.UnholyBlight;
            var facts = new A4Target
            {
                IsSelf = target == unit,
                IsAlly = allies.Contains(target),
                DistanceFeet = Feet(unit, target),
                Humanoid = target.Descriptor.Progression.Race != null,
                Helpless = target.Descriptor.State.IsHelpless,
                Slowed = target.Descriptor.State.HasCondition(UnitCondition.Slowed),
                Confused = target.Descriptor.State.HasCondition(UnitCondition.Confusion),
                Blinded = target.Descriptor.State.HasCondition(UnitCondition.Blindness),
                Invisible = target.Descriptor.State.HasCondition(UnitCondition.Invisible),
                Polymorphed = target.Buffs.Enumerable.Any(b => b.Blueprint.name.StartsWith("BalefulPolymorph", StringComparison.Ordinal)),
                Hasted = target.Buffs.Enumerable.Any(b => b.Blueprint.name.StartsWith("Haste", StringComparison.Ordinal)),
                Frightened = target.Descriptor.State.HasCondition(UnitCondition.Frightened) || target.Descriptor.State.HasCondition(UnitCondition.Shaken),
                HitPointFraction = target.Descriptor.MaxHP > 0 ? (double)target.Descriptor.HPLeft / target.Descriptor.MaxHP : 1d,
                EnemiesWithin10Feet = within(enemies, 10f),
                EnemiesWithin20Feet = within(enemies, 20f),
                AlliesWithin10Feet = within(allies.Concat(new[] { unit }), 10f),
                AlliesWithin20Feet = orders ? alliesIn15.Count(a => align(a).HasComponent(AlignmentComponent.Chaotic) || !align(a).HasComponent(AlignmentComponent.Lawful))
                    : unholy ? alliesIn20.Count(a => !align(a).HasComponent(AlignmentComponent.Evil))
                    : within(allies.Concat(new[] { unit }), 20f),
                ChaoticWithin15Feet = burst15.Count(e => align(e).HasComponent(AlignmentComponent.Chaotic)),
                NeutralLawWithin15Feet = burst15.Count(e => !align(e).HasComponent(AlignmentComponent.Chaotic) && !align(e).HasComponent(AlignmentComponent.Lawful)),
                GoodWithin20Feet = burst20.Count(e => align(e).HasComponent(AlignmentComponent.Good)),
                NeutralMoralWithin20Feet = burst20.Count(e => !align(e).HasComponent(AlignmentComponent.Good) && !align(e).HasComponent(AlignmentComponent.Evil)),
                DispellableBuffs = target.Buffs.Enumerable.Count(b => b.Context?.SourceAbility != null && b.Context.SourceAbility.Type == AbilityType.Spell),
                Martial = target.Stats.BaseAttackBonus.ModifiedValue >= target.Descriptor.Progression.CharacterLevel * 3 / 4,
                StrengthScore = target.Stats.Strength.ModifiedValue,
                AlliesNotHastedWithin15Feet = allies.Concat(new[] { unit }).Count(a => Feet(a, target) <= 15f && !a.Buffs.Enumerable.Any(b => b.Blueprint.name.StartsWith("Haste", StringComparison.Ordinal))),
            };
            if ((orders || unholy) && facts.AlliesWithin20Feet > 0)
            {
                // Her own allies in the burst who would be harmed: she does not cast it there.
                return 0f;
            }
            return LashmistressTactics.Target(aim, facts);
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

        /// <summary>Read only: one defender's sheet, its source chain values against the live unit, every modifier with its descriptor.</summary>
        public static string Report(UnitEntityData unit)
        {
            if (unit == null)
            {
                return "(none)";
            }
            var sb = new System.Text.StringBuilder();
            UnitDescriptor d = unit.Descriptor;
            string key = KeyOf(unit);
            sb.Append($"{key} {unit.CharacterName} {unit.UniqueId} size {d.State.Size} HD {d.Progression.CharacterLevel} hp {d.HPLeft}/{d.MaxHP} temp {unit.Stats.TemporaryHitPoints.ModifiedValue} life {d.State.LifeState} brain {unit.IsBrainActive} combat {unit.IsInCombat} stealth {d.State.IsInStealth} pos ({GridX(unit.Position):0.0},{GridY(unit.Position):0.0}) alignment {d.Alignment.Value}\n");
            sb.Append($"  classes [{string.Join(", ", d.Progression.Classes.Select(c => c.CharacterClass.name + " " + c.Level).ToArray())}]\n");
            foreach (StatType type in new[] { StatType.Strength, StatType.Dexterity, StatType.Constitution, StatType.Intelligence, StatType.Wisdom, StatType.Charisma, StatType.BaseAttackBonus, StatType.AC, StatType.SaveFortitude, StatType.SaveReflex, StatType.SaveWill, StatType.Initiative, StatType.SkillPerception, StatType.SkillStealth, StatType.AdditionalAttackBonus, StatType.HitPoints, StatType.AttackOfOpportunityCount })
            {
                ModifiableValue stat = unit.Stats.GetStat(type);
                sb.Append($"  {type} {stat.ModifiedValue} (base {stat.BaseValue}) [{string.Join(", ", stat.Modifiers.Select(m => $"{m.ModDescriptor} {m.ModValue:+0;-0}").ToArray())}]\n");
            }
            var cmb = Rulebook.Trigger(new RuleCalculateBaseCMB(unit));
            var cmd = Rulebook.Trigger(new RuleCalculateBaseCMD(unit));
            sb.Append($"  AC touch {unit.Stats.AC.Touch} flat-footed {unit.Stats.AC.FlatFooted}; CMB {cmb.Result}; CMD {cmd.Result}\n");
            foreach (var weapon in new[] { unit.Body.PrimaryHand.MaybeWeapon }.Concat(unit.Body.AdditionalLimbs.Select(l => l.MaybeWeapon)).Where(w => w != null))
            {
                var attack = Rulebook.Trigger(new RuleCalculateAttackBonusWithoutTarget(unit, weapon, 0));
                var weaponStats = Rulebook.Trigger(new RuleCalculateWeaponStats(unit, weapon));
                sb.Append($"  {weapon.Blueprint.name}: attack {attack.Result:+0;-0}, damage {weaponStats.DamageDescription.FirstOrDefault()?.Dice}+{weaponStats.DamageDescription.FirstOrDefault()?.Bonus}, reach {weapon.AttackRange}\n");
            }
            foreach (Spellbook book in d.Spellbooks)
            {
                sb.Append($"  book {book.Blueprint.name} CL {book.CasterLevel} concentration {book.GetConcentration()}:");
                for (int level = 0; level <= 9; level++)
                {
                    var slots = book.GetMemorizedSpells(level).ToList();
                    if (slots.Count > 0)
                    {
                        sb.Append($" L{level}[{string.Join(",", slots.Select(s => (s.Spell?.Blueprint.name ?? "empty") + (s.Available ? string.Empty : "*spent")).ToArray())}]");
                    }
                }
                AbilityData sample = book.GetAllMemorizedSpells().Select(s => s.Spell).FirstOrDefault(s => s != null && s.SpellLevel >= 2);
                if (sample != null)
                {
                    AbilityParams p = sample.CalculateParams();
                    sb.Append($" sample {sample.Blueprint.name} CL {p.CasterLevel} DC {p.DC}");
                }
                sb.Append("\n");
            }
            foreach (var resource in d.Abilities.Enumerable.SelectMany(a => a.Blueprint.GetComponents<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>()).Select(l => l.RequiredResource).Where(r => r != null).Distinct())
            {
                sb.Append($"  resource {resource.name} {d.Resources.GetResourceAmount(resource)}/{resource.GetMaxAmount(d)}\n");
            }
            foreach (var ability in d.Abilities.Enumerable.Where(a => a.Blueprint.Type == AbilityType.Spell || a.Blueprint.name.Contains("Channel") || a.Blueprint.name.Contains("AcidDart") || a.Blueprint.name.Contains("BlastRune") || a.Blueprint.name.StartsWith("RuneDomain")))
            {
                AbilityParams p = ability.Data.CalculateParams();
                sb.Append($"  ability {ability.Blueprint.name}: CL {p.CasterLevel} DC {p.DC} SL {p.SpellLevel} available {ability.Data.IsAvailableForCast}\n");
            }
            sb.Append($"  quick slots [{string.Join(", ", unit.Body.QuickSlots.Select(s => s.MaybeItem).Where(i => i != null).Select(i => i.Blueprint.name + " x" + i.Charges).ToArray())}]\n");
            sb.Append($"  buffs [{string.Join(", ", unit.Buffs.Enumerable.Select(b => b.Blueprint.name + (b.IsPermanent ? string.Empty : $" {b.TimeLeft.TotalSeconds:0}s")).ToArray())}]\n");
            sb.Append($"  facts [{string.Join(", ", d.Progression.Features.Enumerable.Select(f => f.Blueprint.name + (f.Param != null ? "(" + f.Param + ")" : string.Empty)).ToArray())}]");
            return sb.ToString();
        }
    }
}
