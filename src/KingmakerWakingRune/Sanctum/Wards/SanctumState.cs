using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum.Encounter;

// Saved fields: some are only ever written by the native adapters or the JSON reader.
#pragma warning disable CS0649

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal sealed class KnowledgeEntry
    {
        public string Key = string.Empty;
        public string Actor = string.Empty;
        public string Skill = string.Empty;
        public int Rank;
        public int Result;
    }

    internal sealed class AlarmState
    {
        public double ExpiresAt = -1d;
        public bool Disarmed;
        public bool Dispelled;
        public int Alerts;
        public double FirstAlertAt = -1d;
        public double LastAlertAt = -1d;

        public bool IsActive(double now)
        {
            return !Disarmed && !Dispelled && ExpiresAt >= 0d && now < ExpiresAt;
        }
    }

    internal sealed class GlyphState
    {
        public bool Triggered;
        public bool Disarmed;
        public bool Dispelled;
        public double TriggeredAt = -1d;
    }

    internal sealed class DoorWardState
    {
        /// <summary>The physical latch: a barrier on fresh starts, released by a legal opening.</summary>
        public bool Latched = true;
        public bool Destroyed;
        public int HitPoints = WardProfile.DoorHitPoints;
        public FeederState Feeder = new FeederState();
    }

    internal sealed class HoundState
    {
        public double ExpiresAt = -1d;
        public double FirstBarkAt = -1d;
        public double DischargedExpiresAt = -1d;
        public bool Dispelled;
        public double LastBiteAt = -1d;
        public List<string> BarkedAt = new List<string>();

        public bool IsActive(double now)
        {
            if (Dispelled || ExpiresAt < 0d || now >= ExpiresAt)
            {
                return false;
            }
            return DischargedExpiresAt < 0d || now < DischargedExpiresAt;
        }
    }

    internal enum StatueOutcome
    {
        Stone,
        Living,
        Corpse,
        Crumbling,
        Ash,
    }

    internal sealed class StatueState
    {
        public string Id = string.Empty;
        public StatueOutcome Outcome = StatueOutcome.Stone;
        /// <summary>The entity that replaced this figure, so a reload re-binds rather than duplicates it.</summary>
        public string EntityId = string.Empty;
        public double CrumbleStartedAt = -1d;
        public bool Reported;
        public double ReportedAt = -1d;
        /// <summary>The runner's next route leg, advanced only by a real arrival or a completed door action.</summary>
        public int RouteLeg;
        /// <summary>Set once when a marked body gained its Sihedron credential, so a reload never re-grants it.</summary>
        public bool Credentialed;
        /// <summary>
        /// A reporter's faction is known to be the hostile one it flees under. False in saves from before reporters were
        /// hostile, where a neutral body is migrated once, never while an effect holds its faction.
        /// </summary>
        public bool HostileSettled;
    }

    /// <summary>
    /// A player's standing order for one summoned creature to attack one rune plate. Only identities are kept: the summon's
    /// duration, its attacks and the rune's damage stay with the game and the focus window, so a reload restores none of them.
    /// </summary>
    internal sealed class SummonOrderRecord
    {
        public string SummonId = string.Empty;
        public string SummonerId = string.Empty;
        public string SummonerName = string.Empty;
        public string TargetId = string.Empty;
    }

    internal sealed class GemState
    {
        public int HitPoints = WardProfile.GemHitPoints;
        public bool Destroyed;
    }

    /// <summary>
    /// Everything WR-04 must remember, as plain serializable data. It holds no game objects or renderer flags; the adapters
    /// rebuild the world from it on every load, so a reload never rerolls, re-triggers or duplicates anything.
    /// </summary>
    internal sealed class SanctumState
    {
        public const int CurrentVersion = 4;

        public int Version = CurrentVersion;
        public string ProfileSubtier = WardProfile.Subtier;
        public int ProfilePartyBaseline = WardProfile.PartyBaseline;
        public bool ProfileExtraChallenge = WardProfile.ExtraChallenge;
        public bool Initialized;
        public double InitializedAt = -1d;
        /// <summary>True for a save that entered the empty pre-WR-04 sanctum; its wards were armed around the party.</summary>
        public bool MigratedLegacy;
        public bool LegacyNoticeShown;

        public double Clock;
        public List<KnowledgeEntry> Knowledge = new List<KnowledgeEntry>();
        public Dictionary<string, RuneFocusState> Foci = new Dictionary<string, RuneFocusState>();
        public int RuneMask;
        public Dictionary<string, CurseRecord> Curses = new Dictionary<string, CurseRecord>();
        public Dictionary<string, MistRecord> Mist = new Dictionary<string, MistRecord>();
        public Dictionary<string, StatueState> Statues = new Dictionary<string, StatueState>();
        public AlarmState Alarm = new AlarmState();
        public GlyphState Glyph = new GlyphState();
        public DoorWardState Door = new DoorWardState();
        public HoundState Hound = new HoundState();
        public GemState Gem = new GemState();
        public RitualState Ritual = new RitualState();
        public bool FlailAcquired;
        public List<string> AlarmInside = new List<string>();
        public List<string> GlyphInside = new List<string>();
        public string TokenMotif = string.Empty;
        public CredentialState Credentials = new CredentialState();
        /// <summary>Krune's own summons get a typed +4 resistance bonus against the mist; filled by the adapters only.</summary>
        public List<string> KruneSummons = new List<string>();
        /// <summary>Standing summon orders against rune plates, revalidated on every load before any is resumed.</summary>
        public List<SummonOrderRecord> SummonOrders = new List<SummonOrderRecord>();
        /// <summary>Kurshu's A1 encounter (WR-05). A state older than version 4 was already inside the sanctum and does not get it.</summary>
        public KurshuState Kurshu = new KurshuState();

        /// <summary>
        /// Brings an older save forward without resetting anything it recorded. Version 1 predates credentials and route
        /// progress: a runner already under way keeps going from the start of its route, and nothing is re-granted. Version 2
        /// predates saved summon orders and settled reporter hostility; its reporters are checked on their next tick. Version 3
        /// predates Kurshu: a party already inside keeps its sanctum as it was, with no fight inserted and no clock started.
        /// </summary>
        public bool Migrate()
        {
            if (Version >= CurrentVersion)
            {
                return false;
            }
            Credentials = Credentials ?? new CredentialState();
            KruneSummons = KruneSummons ?? new List<string>();
            SummonOrders = SummonOrders ?? new List<SummonOrderRecord>();
            Kurshu = Kurshu ?? new KurshuState();
            if (Initialized)
            {
                KurshuRules.DecideForExistingSave(Kurshu);
            }
            Version = CurrentVersion;
            return true;
        }

        public RuneFocusState Focus(string id)
        {
            if (!Foci.TryGetValue(id, out RuneFocusState focus))
            {
                focus = new RuneFocusState();
                Foci[id] = focus;
            }
            return focus;
        }

        public CurseRecord Curse(string actor)
        {
            if (!Curses.TryGetValue(actor, out CurseRecord record))
            {
                record = new CurseRecord { Actor = actor };
                Curses[actor] = record;
            }
            return record;
        }

        public MistRecord MistOf(string actor)
        {
            if (!Mist.TryGetValue(actor, out MistRecord record))
            {
                record = new MistRecord { Actor = actor };
                Mist[actor] = record;
            }
            return record;
        }

        public StatueState Statue(string id)
        {
            if (!Statues.TryGetValue(id, out StatueState statue))
            {
                statue = new StatueState { Id = id };
                Statues[id] = statue;
            }
            return statue;
        }

        /// <summary>Recomputes the seven-bit mask from each focus's state, the one authoritative source.</summary>
        public int RecomputeMask()
        {
            RuneMask = KruneProjection.Foci.Where(f => Focus(f.Id).Disabled).Aggregate(0, (mask, f) => mask | (1 << f.Bit));
            return RuneMask;
        }

        /// <summary>Starts ward lifetimes once, at the first sanctum initialization. A later load changes nothing.</summary>
        public bool Initialize(double now, bool legacyEntered)
        {
            if (Initialized)
            {
                return false;
            }
            Initialized = true;
            InitializedAt = now;
            Clock = now;
            MigratedLegacy = legacyEntered;
            KurshuRules.DecideOnInitialize(Kurshu, legacyEntered);
            Alarm.ExpiresAt = now + WardProfile.AlarmDurationSeconds;
            Hound.ExpiresAt = now + WardProfile.HoundDurationSeconds;
            if (legacyEntered)
            {
                // A pre-WR-04 save may have walked through this door when it was unlocked; a barrier must not close on it.
                Door.Latched = false;
            }
            return true;
        }
    }

    /// <summary>One roll per character, skill and rank, preserved through saves; a higher rank or new character earns a fresh attempt.</summary>
    internal static class InspectionRules
    {
        public static bool CanAttempt(IEnumerable<KnowledgeEntry> ledger, string key, string actor, string skill, int rank)
        {
            return !ledger.Any(e => e.Key == key && e.Actor == actor && e.Skill == skill && e.Rank >= rank);
        }

        public static void Record(ICollection<KnowledgeEntry> ledger, string key, string actor, string skill, int rank, int result)
        {
            ledger.Add(new KnowledgeEntry { Key = key, Actor = actor, Skill = skill, Rank = rank, Result = result });
        }

        public static int BestResult(IEnumerable<KnowledgeEntry> ledger, string key)
        {
            return ledger.Where(e => e.Key == key).Select(e => e.Result).DefaultIfEmpty(int.MinValue).Max();
        }

        /// <summary>0 for nothing learned, 1 for identity and hint, 2 for the exact routes.</summary>
        public static int RuneTier(int result)
        {
            return result >= WardProfile.RuneExactDC ? 2 : result >= WardProfile.RuneHintDC ? 1 : 0;
        }
    }

    internal enum BearerRole
    {
        Attack,
        Report,
    }

    /// <summary>The authored per-slot response of the ten restored bearers: a balanced five and five, and fixed in this manifest.</summary>
    internal static class BearerManifest
    {
        public static readonly IReadOnlyDictionary<string, BearerRole> Roles = new Dictionary<string, BearerRole>
        {
            { "Support-N1", BearerRole.Report },
            { "Support-N2", BearerRole.Attack },
            { "Support-N3", BearerRole.Report },
            { "Support-N4", BearerRole.Attack },
            { "Support-T1", BearerRole.Report },
            { "Support-T2", BearerRole.Attack },
            { "Support-W1", BearerRole.Attack },
            { "Support-E1", BearerRole.Report },
            { "Support-W2", BearerRole.Report },
            { "Support-E2", BearerRole.Attack },
        };

        public static bool IsBearer(string statueId)
        {
            return statueId.StartsWith("Support-", StringComparison.Ordinal);
        }
    }
}
