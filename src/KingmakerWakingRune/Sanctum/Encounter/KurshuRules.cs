using System;
using System.Collections.Generic;
using System.Linq;

// Saved fields: some are only ever written by the native adapters or the JSON reader.
#pragma warning disable CS0649

namespace KingmakerWakingRune.Sanctum.Encounter
{
    /// <summary>One line of the source-to-runtime sheet: the printed Subtier 10-11 value and the explicit extra-challenge change.</summary>
    internal sealed class KurshuStatLine
    {
        public readonly string Name;
        public readonly int Printed;
        public readonly int HardMode;
        public readonly string Why;

        public KurshuStatLine(string name, int printed, int hardMode, string why)
        {
            Name = name;
            Printed = printed;
            HardMode = hardMode;
            Why = why;
        }

        /// <summary>The authored value before the native difficulty preset.</summary>
        public int Authored => Printed + HardMode;
    }

    /// <summary>
    /// Kurshu as printed (Subtier 10-11, page 8) and as authored for the extra challenge (page 9 sidebar): the separately named
    /// Weakness ability is removed (natural armor +2, spell resistance +2) and one of the three printed negative levels is
    /// removed (+1 on the statistics a negative level lowers, +5 hit points, +1 caster level). Nothing else changes.
    /// </summary>
    internal static class KurshuProfile
    {
        public const string EncounterId = "A1-Kurshu";
        public const int PrintedNegativeLevels = 3;
        public const int HardModeNegativeLevels = PrintedNegativeLevels - 1;
        public const int FeedMinimumHitDice = 8;
        public const int PowerSurgeDcBonus = 2;
        public const int Regeneration = 5;
        /// <summary>Caster level with no negative level: printed CL 12 carries three.</summary>
        public const int UndrainedCasterLevel = 12 + PrintedNegativeLevels;
        /// <summary>A limited wish duplicates a spell at the save DC of a 7th-level spell.</summary>
        public const int LimitedWishSpellLevel = 7;
        /// <summary>The source does not quantify "a substantial wound": the first time her hit points fall to three quarters or less.</summary>
        public const double SubstantialWoundFraction = 0.75d;

        public static readonly KurshuStatLine[] Sheet =
        {
            new KurshuStatLine("Armor Class", 28, 2, "Weakness removed: natural armor +2"),
            new KurshuStatLine("Touch AC", 17, 0, string.Empty),
            new KurshuStatLine("Flat-footed AC", 22, 2, "Weakness removed: natural armor +2"),
            new KurshuStatLine("Natural armor", 11, 2, "Weakness removed"),
            new KurshuStatLine("Hit points", 214, 5, "one negative level removed"),
            new KurshuStatLine("Fortitude", 17, 1, "one negative level removed"),
            new KurshuStatLine("Reflex", 8, 1, "one negative level removed"),
            new KurshuStatLine("Will", 13, 1, "one negative level removed"),
            new KurshuStatLine("Damage reduction (cold iron and magic)", 10, 0, string.Empty),
            new KurshuStatLine("Resist cold, electricity, fire, sonic", 30, 0, string.Empty),
            new KurshuStatLine("Spell resistance", 24, 2, "Weakness removed"),
            new KurshuStatLine("Regeneration (acid)", 5, 0, string.Empty),
            new KurshuStatLine("Slam attack", 18, 1, "one negative level removed"),
            new KurshuStatLine("Tail slap attack", 13, 1, "one negative level removed"),
            new KurshuStatLine("CMB", 20, 1, "one negative level removed"),
            new KurshuStatLine("CMB to grapple", 24, 1, "one negative level removed"),
            new KurshuStatLine("CMD", 38, 1, "one negative level removed"),
            new KurshuStatLine("Spell-like caster level", 12, 1, "one negative level removed"),
            new KurshuStatLine("Concentration", 19, 1, "one negative level removed"),
            new KurshuStatLine("Initiative", 10, 0, "printed without the negative levels' penalty; kept as printed"),
            new KurshuStatLine("Perception", 22, 1, "one negative level removed"),
            new KurshuStatLine("Stealth", 19, 1, "one negative level removed"),
            new KurshuStatLine("Spell-like DC (3rd level)", 20, 0, "Charisma-based; negative levels do not lower DCs"),
            new KurshuStatLine("Plane shift DC", 24, 0, string.Empty),
            new KurshuStatLine("Negative levels (divine separation)", 3, -1, "one negative level removed"),
            new KurshuStatLine("Limited wishes left today", 2, 0, "one already used, as printed for Subtier 10-11"),
        };

        public static int Authored(string name)
        {
            return Sheet.First(l => l.Name == name).Authored;
        }

        /// <summary>
        /// The value the unit is built with before Divine Separation's penalties: each authored negative level is added back to
        /// the statistics a negative level lowers, and five hit points each. The separation fact then takes them away natively.
        /// </summary>
        public static int Undrained(string name)
        {
            int value = Authored(name);
            switch (name)
            {
                case "Hit points":
                    return value + 5 * HardModeNegativeLevels;
                case "Fortitude":
                case "Reflex":
                case "Will":
                case "Slam attack":
                case "Tail slap attack":
                case "CMB":
                case "CMB to grapple":
                case "CMD":
                case "Spell-like caster level":
                case "Concentration":
                case "Perception":
                case "Stealth":
                    return value + HardModeNegativeLevels;
                default:
                    return value;
            }
        }
    }

    /// <summary>The finite uses printed for her. Everything is per day; she never rests inside this scenario.</summary>
    internal static class KurshuUses
    {
        public const string CureSeriousWounds = "CureSeriousWounds";
        public const string DispelMagic = "DispelMagic";
        public const string Displacement = "Displacement";
        public const string Fireball = "Fireball";
        public const string GreaterTeleport = "GreaterTeleport";
        public const string HoldPerson = "HoldPerson";
        public const string LightningBolt = "LightningBolt";
        public const string LimitedWish = "LimitedWish";
        public const string MagicMissile = "MagicMissile";
        public const string PlaneShift = "PlaneShift";
        public const string Slow = "Slow";
        public const string StinkingCloud = "StinkingCloud";
        public const string VampiricTouch = "VampiricTouch";
        public const string PowerSurge = "PowerSurge";
        public const string Feed = "Feed";

        public static readonly IReadOnlyDictionary<string, int> PerDay = new Dictionary<string, int>
        {
            { CureSeriousWounds, 3 },
            { DispelMagic, 3 },
            { Displacement, 3 },
            { Fireball, 3 },
            { GreaterTeleport, 3 },
            { HoldPerson, 3 },
            { LightningBolt, 3 },
            { LimitedWish, 3 },
            { MagicMissile, 3 },
            { PlaneShift, 3 },
            { Slow, 3 },
            { StinkingCloud, 3 },
            { VampiricTouch, 3 },
            { PowerSurge, 3 },
            { Feed, 1 },
        };

        /// <summary>Spent before the party arrives: one limited wish (Subtier 10-11 tactics).</summary>
        public static readonly IReadOnlyDictionary<string, int> SpentBeforeArrival = new Dictionary<string, int>
        {
            { LimitedWish, 1 },
        };
    }

    internal enum KurshuPolicy
    {
        /// <summary>No decision yet: the state predates the sanctum's first initialization.</summary>
        Unset,
        /// <summary>A fresh arrival: the encounter runs.</summary>
        Active,
        /// <summary>A save that was already inside the sanctum before this encounter existed: nothing is inserted into it.</summary>
        LegacyNotInserted,
    }

    internal enum FeedVerdict
    {
        Eligible,
        AlreadyFedToday,
        NotDead,
        NotOutsider,
        TooFewHitDice,
        Summoned,
        AlreadyConsumed,
        ExhaustedHusk,
        NothingToRestore,
    }

    /// <summary>What a corpse is, as the adapters read it from the game.</summary>
    internal struct CorpseFacts
    {
        public string Id;
        public bool Dead;
        public bool Outsider;
        public int HitDice;
        public bool Summoned;
        public bool StoneHusk;
    }

    /// <summary>Everything the encounter must remember, as plain data saved with the sanctum's state.</summary>
    internal sealed class KurshuState
    {
        public KurshuPolicy Policy = KurshuPolicy.Unset;
        public bool LegacyNoticeShown;

        /// <summary>The one combat entity this encounter ever creates; a reload re-binds to it and never spawns another.</summary>
        public string EntityId = string.Empty;
        public double SpawnedAt = -1d;

        public bool HiddenCheckDone;
        public bool BelievedHidden;
        public int StealthResult;
        public int BestPerception;
        public string SpottedBy = string.Empty;
        public bool DisplacementPrepared;

        public double EngagedAt = -1d;

        public bool WoundSpoken;
        public double WoundSpokenAt = -1d;

        public bool Defeated;
        public double DefeatedAt = -1d;
        public string DefeatedHow = string.Empty;
        public bool BloodMessageShown;
        public bool Slain;
        public double SlainAt = -1d;

        /// <summary>Divine Separation's levels still on her. Only Feed removes them.</summary>
        public int SeparationLevels = KurshuProfile.HardModeNegativeLevels;
        public bool FedToday;
        public double FedAt = -1d;
        public string FedOn = string.Empty;
        public int FedHitDice;
        public List<string> ConsumedCorpses = new List<string>();

        /// <summary>The ledger of spent uses, by <see cref="KurshuUses"/> key. The native resources are reconciled to it.</summary>
        public Dictionary<string, int> Spent = new Dictionary<string, int>();

        public bool SurgeArmed;
        public double SurgeArmedAt = -1d;
        public int SurgesConsumed;
        public int SurgesExpired;

        /// <summary>The one creature her tail holds, or empty. She never holds two.</summary>
        public string HeldId = string.Empty;
        public string HeldName = string.Empty;
        public double HeldSince = -1d;
        /// <summary>She chose to maintain the hold this turn and its standard action has not been spent yet.</summary>
        public bool MaintainDue;
        public int Grabs;
        public int Maintains;
        public string LastRelease = string.Empty;

        public List<string> Log = new List<string>();
    }

    internal enum GrabVerdict
    {
        Eligible,
        AlreadyHolding,
        TooLarge,
        FreedomOfMovement,
    }

    internal enum HoldChoice
    {
        /// <summary>Nothing is held.</summary>
        None,
        /// <summary>A standard action this turn: the grapple check again, and constrict on a success.</summary>
        Maintain,
        /// <summary>A free action: the hold ends and she acts as she likes.</summary>
        Release,
    }

    internal enum HealthEvent
    {
        SubstantialWound,
        Defeated,
        Slain,
    }

    /// <summary>
    /// The encounter's rules, free of the game: who gets the fight, what is spent, what Feed may eat, and the one-time events
    /// of a wound, a defeat and a death. Everything is idempotent, so a reload, a second fight or a repeated callback repeats nothing.
    /// </summary>
    internal static class KurshuRules
    {
        /// <summary>
        /// Decided once, when the sanctum is first initialized: a fresh token arrival gets the fight; a save that had already
        /// entered the sanctum before WR-04 is left as it was.
        /// </summary>
        public static bool DecideOnInitialize(KurshuState state, bool legacyEntered)
        {
            if (state.Policy != KurshuPolicy.Unset)
            {
                return false;
            }
            state.Policy = legacyEntered ? KurshuPolicy.LegacyNotInserted : KurshuPolicy.Active;
            if (state.Policy == KurshuPolicy.Active)
            {
                foreach (var pair in KurshuUses.SpentBeforeArrival)
                {
                    state.Spent[pair.Key] = pair.Value;
                }
            }
            return true;
        }

        /// <summary>A save already inside the sanctum from a build without this encounter: nothing is materialized on top of it.</summary>
        public static bool DecideForExistingSave(KurshuState state)
        {
            if (state.Policy != KurshuPolicy.Unset)
            {
                return false;
            }
            state.Policy = KurshuPolicy.LegacyNotInserted;
            return true;
        }

        public static int Remaining(KurshuState state, string key)
        {
            int max = KurshuUses.PerDay[key];
            state.Spent.TryGetValue(key, out int spent);
            return Math.Max(0, max - spent);
        }

        /// <summary>Records one use. False, and nothing recorded, when none is left.</summary>
        public static bool Spend(KurshuState state, string key)
        {
            if (Remaining(state, key) <= 0)
            {
                return false;
            }
            state.Spent.TryGetValue(key, out int spent);
            state.Spent[key] = spent + 1;
            return true;
        }

        /// <summary>
        /// Brings the ledger and the native resource to the lower of the two: a use the game spent is recorded, and a resource
        /// that came back fuller than the ledger allows (a refill nothing in the scenario grants) is put back down. Returns the
        /// amount the native resource must hold.
        /// </summary>
        public static int Reconcile(KurshuState state, string key, int native)
        {
            int max = KurshuUses.PerDay[key];
            state.Spent.TryGetValue(key, out int spent);
            int nativeSpent = Math.Max(0, max - native);
            if (nativeSpent > spent)
            {
                state.Spent[key] = Math.Min(max, nativeSpent);
            }
            return Remaining(state, key);
        }

        /// <summary>Power Surge, a swift action: one use, armed for the rest of this round. False if spent out or already armed.</summary>
        public static bool ArmSurge(KurshuState state, double now)
        {
            if (state.SurgeArmed || !Spend(state, KurshuUses.PowerSurge))
            {
                return false;
            }
            state.SurgeArmed = true;
            state.SurgeArmedAt = now;
            return true;
        }

        /// <summary>The DC bonus a spell-like ability gets now: +2 only while a surge armed this round is still unspent.</summary>
        public static int SurgeBonus(KurshuState state, double now, double roundSeconds)
        {
            return state.SurgeArmed && now - state.SurgeArmedAt < roundSeconds ? KurshuProfile.PowerSurgeDcBonus : 0;
        }

        /// <summary>A spell-like ability went off: an armed surge is used up by it.</summary>
        public static bool ConsumeSurge(KurshuState state)
        {
            if (!state.SurgeArmed)
            {
                return false;
            }
            state.SurgeArmed = false;
            state.SurgesConsumed++;
            return true;
        }

        /// <summary>A surge not used in its round lapses; the spent use is not returned.</summary>
        public static bool ExpireSurge(KurshuState state, double now, double roundSeconds)
        {
            if (!state.SurgeArmed || now - state.SurgeArmedAt < roundSeconds)
            {
                return false;
            }
            state.SurgeArmed = false;
            state.SurgesExpired++;
            return true;
        }

        public static FeedVerdict CanFeed(KurshuState state, CorpseFacts corpse)
        {
            if (state.FedToday || Remaining(state, KurshuUses.Feed) <= 0)
            {
                return FeedVerdict.AlreadyFedToday;
            }
            if (state.ConsumedCorpses.Contains(corpse.Id))
            {
                return FeedVerdict.AlreadyConsumed;
            }
            if (corpse.StoneHusk)
            {
                return FeedVerdict.ExhaustedHusk;
            }
            if (corpse.Summoned)
            {
                return FeedVerdict.Summoned;
            }
            if (!corpse.Dead)
            {
                return FeedVerdict.NotDead;
            }
            if (!corpse.Outsider)
            {
                return FeedVerdict.NotOutsider;
            }
            if (corpse.HitDice < KurshuProfile.FeedMinimumHitDice)
            {
                return FeedVerdict.TooFewHitDice;
            }
            if (state.SeparationLevels <= 0)
            {
                return FeedVerdict.NothingToRestore;
            }
            return FeedVerdict.Eligible;
        }

        /// <summary>
        /// Devours one eligible corpse: one Divine Separation level per Hit Die, never more than she has, and never any other
        /// negative level. Returns the levels removed, or -1 (nothing changed) when the corpse is not eligible.
        /// </summary>
        public static int Feed(KurshuState state, CorpseFacts corpse, double now)
        {
            if (CanFeed(state, corpse) != FeedVerdict.Eligible)
            {
                return -1;
            }
            Spend(state, KurshuUses.Feed);
            int removed = Math.Min(state.SeparationLevels, corpse.HitDice);
            state.SeparationLevels -= removed;
            state.FedToday = true;
            state.FedAt = now;
            state.FedOn = corpse.Id;
            state.FedHitDice = corpse.HitDice;
            state.ConsumedCorpses.Add(corpse.Id);
            return removed;
        }

        /// <summary>
        /// Reads her health once per observation and returns the events that are new. A defeat is her own hit points running
        /// out (unconscious under regeneration, or dead); a hold, sleep or paralysis leaves her hit points alone and is not one.
        /// </summary>
        public static List<HealthEvent> ObserveHealth(KurshuState state, int hitPointsLeft, int maxHitPoints, bool conscious, bool dead, double now)
        {
            var events = new List<HealthEvent>();
            if (!state.WoundSpoken && !dead && conscious && maxHitPoints > 0 && hitPointsLeft <= maxHitPoints * KurshuProfile.SubstantialWoundFraction)
            {
                state.WoundSpoken = true;
                state.WoundSpokenAt = now;
                events.Add(HealthEvent.SubstantialWound);
            }
            bool down = dead || (!conscious && hitPointsLeft <= 0);
            if (down && !state.Defeated)
            {
                state.Defeated = true;
                state.DefeatedAt = now;
                state.DefeatedHow = dead ? "slain" : "unconscious, regenerating";
                events.Add(HealthEvent.Defeated);
            }
            if (dead && !state.Slain)
            {
                state.Slain = true;
                state.SlainAt = now;
                events.Add(HealthEvent.Slain);
            }
            return events;
        }

        /// <summary>
        /// Grab, on a tail hit: a creature her size or smaller, while her tail holds no one else. Freedom of movement wins any
        /// grapple check to resist, so the attempt fails outright.
        /// </summary>
        public static GrabVerdict CanGrab(KurshuState state, bool targetNoLarger, bool targetFreeOfMovement)
        {
            if (!string.IsNullOrEmpty(state.HeldId))
            {
                return GrabVerdict.AlreadyHolding;
            }
            if (!targetNoLarger)
            {
                return GrabVerdict.TooLarge;
            }
            return targetFreeOfMovement ? GrabVerdict.FreedomOfMovement : GrabVerdict.Eligible;
        }

        /// <summary>A successful grab: she now holds this one creature, and is grappling it herself.</summary>
        public static bool Hold(KurshuState state, string id, string name, double now)
        {
            if (!string.IsNullOrEmpty(state.HeldId))
            {
                return false;
            }
            state.HeldId = id;
            state.HeldName = name;
            state.HeldSince = now;
            state.MaintainDue = false;
            state.Grabs++;
            return true;
        }

        /// <summary>
        /// Her decision at the start of each of her turns while she holds someone. A grapple lasts only while she spends a
        /// standard action each round to maintain it, and her tactics favor spells: she lets go to cast while she has an
        /// offensive spell-like ability left, and keeps squeezing only when she has none. A maintenance chosen last turn but
        /// never made, or a hold that no longer stands (the victim gone, out of reach or free to move), ends it.
        /// </summary>
        public static HoldChoice DecideHold(KurshuState state, bool holdStillStands, bool offensiveSpellsLeft, out string reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(state.HeldId))
            {
                return HoldChoice.None;
            }
            if (!holdStillStands)
            {
                reason = "the hold no longer stands";
            }
            else if (state.MaintainDue)
            {
                reason = "last turn's maintenance was never made";
            }
            else if (offensiveSpellsLeft)
            {
                reason = "she lets go to cast";
            }
            if (reason != null)
            {
                Release(state, reason);
                return HoldChoice.Release;
            }
            state.MaintainDue = true;
            return HoldChoice.Maintain;
        }

        /// <summary>The maintenance check was made: a success keeps the hold; a failure ends it.</summary>
        public static void Maintained(KurshuState state, bool success)
        {
            state.MaintainDue = false;
            state.Maintains++;
            if (!success)
            {
                Release(state, "the maintenance check failed");
            }
        }

        /// <summary>Ends the hold; false when nothing was held.</summary>
        public static bool Release(KurshuState state, string reason)
        {
            if (string.IsNullOrEmpty(state.HeldId))
            {
                return false;
            }
            state.LastRelease = $"{state.HeldName}: {reason}";
            state.HeldId = string.Empty;
            state.HeldName = string.Empty;
            state.HeldSince = -1d;
            state.MaintainDue = false;
            return true;
        }
    }

    /// <summary>
    /// Intelligence drain to zero. In the source a creature at Intelligence 0 is comatose, not dead; the engine kills any creature
    /// whose ability score falls below 1. Only her drain is changed: the engine keeps the score at 1, the points it cannot show
    /// are held back as pending drain, and the victim lies comatose until restoration has removed at least that much drain.
    /// </summary>
    internal static class MindDrainRules
    {
        /// <summary>The points of drain held back so the engine's score stays at 1, given the score the drain would leave.</summary>
        public static int Excess(int scoreAfterDrain)
        {
            return scoreAfterDrain >= 1 ? 0 : 1 - scoreAfterDrain;
        }

        /// <summary>Awake again once restoration has removed at least as much drain as was held back.</summary>
        public static bool Recovered(int drainWhenHeldBack, int drainNow, int pending)
        {
            return drainWhenHeldBack - drainNow >= pending;
        }

        /// <summary>On waking, the held-back points become real drain, so the score shows what restoration truly left.</summary>
        public static int DrainOnWaking(int drainNow, int pending)
        {
            return drainNow + pending;
        }
    }

    /// <summary>The questions her tactics ask of one target or the field, in a form the game adapters fill in.</summary>
    public enum KurshuAim
    {
        Displacement,
        CureSelf,
        BreakEnchantmentSelf,
        PowerSurge,
        Fireball,
        LightningBolt,
        StinkingCloud,
        Slow,
        HoldPerson,
        HoldMonster,
        ConeOfCold,
        DispelMagic,
        GreaterDispelMagic,
        MagicMissile,
        VampiricTouch,
        Melee,
    }

    internal struct TacticsTarget
    {
        public float Distance;
        public bool Outsider;
        public bool Humanoid;
        public bool Held;
        public bool Slowed;
        public bool Nauseated;
        public int EnemiesWithin10Feet;
        public int EnemiesWithin20Feet;
        public int EnemiesWithin30Feet;
        public int DispellableBuffs;
        public bool SelfWithin20Feet;
    }

    internal struct TacticsSelf
    {
        public double HitPointFraction;
        public bool Displaced;
        public bool Slowed;
        public bool Nauseated;
        public bool SurgeArmed;
        public bool AnySaveAbilityLeft;
        public int VisibleEnemies;
    }

    /// <summary>
    /// Her printed tactics as scores from 0 to 1 that the native brain multiplies into each action: ranged spells where they
    /// pay, outsiders first, defenses when they matter. Target choice lives here; the game's own commands carry it out.
    /// </summary>
    internal static class KurshuTactics
    {
        public const float ReachMeters = 3.05f;
        public const float OutsiderPreference = 1f;
        public const float OtherPreference = 0.6f;

        public static float Actor(KurshuAim aim, TacticsSelf self)
        {
            if (self.VisibleEnemies <= 0)
            {
                return 0f;
            }
            switch (aim)
            {
                case KurshuAim.Displacement:
                    return self.Displaced ? 0f : 1f;
                case KurshuAim.CureSelf:
                    return self.HitPointFraction < 0.35d ? 1f : 0f;
                case KurshuAim.BreakEnchantmentSelf:
                    return self.Slowed || self.Nauseated ? 1f : 0f;
                case KurshuAim.PowerSurge:
                    return !self.SurgeArmed && self.AnySaveAbilityLeft ? 1f : 0f;
                default:
                    return 1f;
            }
        }

        public static float Target(KurshuAim aim, TacticsTarget t)
        {
            float preference = t.Outsider ? OutsiderPreference : OtherPreference;
            switch (aim)
            {
                case KurshuAim.Fireball:
                    return t.SelfWithin20Feet || t.EnemiesWithin20Feet < 2 ? 0f : Clamp(t.EnemiesWithin20Feet / 4f) * Blend(preference);
                case KurshuAim.ConeOfCold:
                    return t.EnemiesWithin20Feet < 3 ? 0f : Clamp(t.EnemiesWithin20Feet / 5f) * Blend(preference);
                case KurshuAim.StinkingCloud:
                    return t.Nauseated || t.SelfWithin20Feet || t.EnemiesWithin20Feet < 2 ? 0f : Clamp(t.EnemiesWithin20Feet / 4f) * Blend(preference);
                case KurshuAim.Slow:
                    return t.Slowed || t.EnemiesWithin30Feet < 2 ? 0f : Clamp(t.EnemiesWithin30Feet / 5f) * Blend(preference);
                case KurshuAim.LightningBolt:
                    return Clamp(0.5f + t.EnemiesWithin10Feet / 6f) * preference;
                case KurshuAim.HoldPerson:
                    return !t.Humanoid || t.Held ? 0f : t.Distance <= ReachMeters * 2f ? 1f : 0.8f;
                case KurshuAim.HoldMonster:
                    return t.Humanoid || t.Held ? 0f : preference;
                case KurshuAim.DispelMagic:
                    return t.DispellableBuffs < 2 ? 0f : Clamp(t.DispellableBuffs / 5f) * Blend(preference);
                case KurshuAim.GreaterDispelMagic:
                    return t.DispellableBuffs < 4 ? 0f : Clamp(t.DispellableBuffs / 8f) * Blend(preference);
                case KurshuAim.MagicMissile:
                    return preference;
                case KurshuAim.VampiricTouch:
                    return (t.Distance <= ReachMeters ? 1f : 0.3f) * preference;
                case KurshuAim.Melee:
                    return (t.Distance <= ReachMeters ? 1f : 0.4f) * preference;
                default:
                    return 1f;
            }
        }

        /// <summary>For area spells the field matters most; an outsider in it still tips the choice of centre.</summary>
        private static float Blend(float preference)
        {
            return 0.8f + 0.2f * preference;
        }

        private static float Clamp(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }
    }
}
