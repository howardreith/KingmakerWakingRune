using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerWakingRune.Sanctum.Wards;

// Saved fields: some are only ever written by the native adapters or the JSON reader.
#pragma warning disable CS0649

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// The save-bound awakening lifecycle. A due awakening is not an actor, and an actor is not a finished entrance: each
    /// stage is recorded only once what it names has actually happened, so a reload resumes rather than repeats.
    /// </summary>
    internal enum AwakeningStage
    {
        /// <summary>The ritual has not come due; the hidden receiver stands in for Krune.</summary>
        Dormant,
        /// <summary>Due and committed: cause, time and the revival mask are fixed. No actor yet (or a partial one to reconcile).</summary>
        Committed,
        /// <summary>The real actor exists, is initialized and owns the feeder protection; the receiver is retired.</summary>
        ActorReady,
        /// <summary>The soul has returned and he has stepped out of the coffin onto the A5 floor.</summary>
        Emerged,
        /// <summary>The opening (parley, battle bark or unobserved waking) is over and his hostility is settled.</summary>
        OpeningResolved,
        /// <summary>The combat handoff: everything the combat increment needs, and nothing it must recreate.</summary>
        CombatReady,
    }

    internal enum OpeningMode
    {
        None,
        /// <summary>No fight under way and he perceives intruders: the short exchange in the native dialogue.</summary>
        Parley,
        /// <summary>The A4 fight is under way: brief barks only, and the hostile handoff at once.</summary>
        BattleBark,
        /// <summary>Nobody he can perceive: he wakes alone, and the party is told nothing it could not know.</summary>
        Unobserved,
    }

    /// <summary>Why he turned hostile, as observable events. A cancelled cursor, a hover or the inventory is none of these.</summary>
    internal enum HostilityCause
    {
        None,
        BattleUnderWay,
        PartyAttacked,
        PartyCastSpell,
        Challenged,
        ParleyEnded,
        Unobserved,
        LegacyAdoption,
    }

    internal enum LegacyAwakening
    {
        /// <summary>Not an older save with a due awakening: nothing to decide.</summary>
        None,
        /// <summary>An older build recorded the awakening as due but never made Krune; the owner must choose once.</summary>
        AwaitingChoice,
        /// <summary>Chosen: the new awakening is initialized from the saved cause, time and resources.</summary>
        Adopted,
        /// <summary>Chosen: kept as a legacy exploration save, with no Krune added.</summary>
        KeptLegacy,
    }

    /// <summary>Everything the awakening must remember. Plain data; the adapters rebuild the world from it on every load.</summary>
    internal sealed class KruneState
    {
        public AwakeningStage Stage = AwakeningStage.Dormant;
        public AwakeningReason Cause = AwakeningReason.None;
        /// <summary>The ritual's own due time. Presentation never moves it.</summary>
        public double EffectiveAt = -1d;
        public double CommittedAt = -1d;
        /// <summary>The disabled-rune mask at commit, for the revival-dependent caster-level penalty only. -1 until committed.</summary>
        public int RevivalMask = -1;
        /// <summary>True when the snapshot is the saved mask at migration, because the build that recorded the awakening kept no history.</summary>
        public bool RevivalMaskFromMigration;
        /// <summary>The one opening stagger, owed only for an early release by the emerald.</summary>
        public bool OpeningStaggerOwed;
        public bool OpeningStaggerApplied;
        public double OpeningStaggerAppliedAt = -1d;
        /// <summary>Turn-based play: his first completed turn has spent the stagger.</summary>
        public bool OpeningStaggerTurnServed;

        /// <summary>The real actor's stable identity. Once set it is authoritative, living or dead.</summary>
        public string EntityId = string.Empty;
        public double ActorCreatedAt = -1d;
        public bool BaselineApplied;
        public bool FeederHandedOff;
        public int FeederHandedOffAmount;
        public string ReceiverId = string.Empty;
        public bool ReceiverRetired;

        public double EmergedAt = -1d;
        public float EmergedX = -1f;
        public float EmergedY = -1f;
        public int StepsFeetSpent;

        public OpeningMode Opening = OpeningMode.None;
        public bool ParleyStarted;
        public bool ParleyFinished;
        public bool Identified;
        public bool ToldOfFall;
        public bool Calculated;
        public bool Hostile;
        public HostilityCause HostileCause = HostilityCause.None;
        public string HostileDetail = string.Empty;
        public double HostileAt = -1d;
        public bool RevivalStudyAttempted;

        public bool CheckpointReached;
        public double CheckpointAt = -1d;
        /// <summary>Set only by the labelled test harness: the diagnostic release of the prototype checkpoint.</summary>
        public bool DiagnosticRelease;

        public int InitializationAttempts;
        public string LastFailure = string.Empty;
        public LegacyAwakening Legacy = LegacyAwakening.None;
        public bool LegacyNoticeShown;
        /// <summary>The director's tactical phase (WR-FINAL): fighting, or withdrawn elsewhere in the sanctum to recover.</summary>
        public KrunePhase DirectorPhase = KrunePhase.Fight;
        /// <summary>When his combat began (the handoff), for his round count; -1 until then.</summary>
        public double CombatStartedAt = -1d;
        /// <summary>A WR-07A save that reached the development checkpoint, brought into the real encounter once (WR-FINAL).</summary>
        public bool PrototypeRetired;
        /// <summary>His book, director brain and combat gear were set up once (WR-FINAL); never refilled.</summary>
        public bool CombatInitialized;
        public string CombatInitializedNote = string.Empty;
        /// <summary>The dancing spear's form while it dances, and when the dance ends (WR-FINAL).</summary>
        public string DancingFormId = string.Empty;
        public double DancingUntil = -1d;
    }

    /// <summary>Krune's linked and revival-dependent values at one instant. Before the native difficulty preset.</summary>
    internal struct KruneSheet
    {
        public KruneProjectionResult Linked;
        public int CasterLevel;
        public int CasterLevelLoss;
        public bool RevivalPenaltyActive;
        public int BaseHitPoints;
        public int MaximumHitPoints;
    }

    /// <summary>A creature or obstacle a placement must not overlap, in plan squares.</summary>
    internal struct Occupant
    {
        public float X;
        public float Y;
        public float Radius;
    }

    internal static class KruneRules
    {
        /// <summary>Printed 293 less the three active Life runes' 40 each: the hit points that come from Krune himself.</summary>
        public const int BaseHitPointsWithoutLife = KruneProjection.FullListedHP - 3 * 40;
        /// <summary>The revival's personal-magic expenditure lasts 24 hours from the awakening (source page 18).</summary>
        public const double RevivalPenaltySeconds = WardProfile.DaySeconds;
        /// <summary>Dimensional steps: 510 feet a day (page 16), kept as a native resource in 5-foot units.</summary>
        public const int StepsFeetPerDay = 510;
        /// <summary>The game's conjuration school gives the 510 feet as 17 uses of up to 30 feet each.</summary>
        public const int StepsFeetPerUse = 30;
        public const double EmergenceDelaySeconds = 3d;
        /// <summary>His footprint in plan squares (a Medium creature's half-width plus a little room).</summary>
        public const float FootprintSquares = 0.45f;

        /// <summary>
        /// Commits a due awakening once: cause and time from the ritual, the revival mask from the current runes, and the
        /// opening stagger only for an emerald release. A repeat call, a load or a second trigger changes nothing.
        /// </summary>
        public static bool Commit(KruneState krune, RitualState ritual, int currentMask, double now)
        {
            if (krune.Stage != AwakeningStage.Dormant || !ritual.AwakeningDue)
            {
                return false;
            }
            krune.Stage = AwakeningStage.Committed;
            krune.Cause = ritual.Reason;
            krune.EffectiveAt = ritual.AwakeningAt >= 0d ? ritual.AwakeningAt : now;
            krune.CommittedAt = now;
            krune.RevivalMask = currentMask;
            // The flag alone is not trusted: a build before this one set it even for an emerald broken after a deadline.
            krune.OpeningStaggerOwed = ritual.Reason == AwakeningReason.EmeraldDestroyed;
            return true;
        }

        /// <summary>Caster levels lost to the revival: the commit's mask, for 24 hours from the awakening; none after.</summary>
        public static int RevivalCasterLevelLoss(KruneState krune, double now)
        {
            if (krune.RevivalMask < 0 || !RevivalPenaltyActive(krune, now))
            {
                return 0;
            }
            return KruneProjection.BaseCasterLevel - KruneProjection.Project(krune.RevivalMask).CasterLevel;
        }

        public static bool RevivalPenaltyActive(KruneState krune, double now)
        {
            return krune.RevivalMask >= 0 && krune.EffectiveAt >= 0d && now < krune.EffectiveAt + RevivalPenaltySeconds;
        }

        /// <summary>
        /// The sheet the actor must show: still-linked rune benefits from the current mask, caster level from the revival
        /// snapshot while its 24 hours last. Later sabotage changes the links; it never restarts the revival.
        /// </summary>
        public static KruneSheet Compose(KruneState krune, int currentMask, double now)
        {
            KruneProjectionResult linked = KruneProjection.Project(currentMask);
            int loss = RevivalCasterLevelLoss(krune, now);
            return new KruneSheet
            {
                Linked = linked,
                CasterLevelLoss = loss,
                CasterLevel = KruneProjection.BaseCasterLevel - loss,
                RevivalPenaltyActive = RevivalPenaltyActive(krune, now),
                BaseHitPoints = BaseHitPointsWithoutLife,
                MaximumHitPoints = BaseHitPointsWithoutLife + LifeHitPoints(currentMask),
            };
        }

        /// <summary>The Life runes' share of maximum hit points under a mask: 40 for each lit rune, 20 for each dark one.</summary>
        public static int LifeHitPoints(int mask)
        {
            return KruneProjection.Project(mask).MaximumHP - BaseHitPointsWithoutLife;
        }

        /// <summary>
        /// The native damage-preservation policy: the game keeps damage taken, not hit points left, so a lower maximum
        /// leaves the same damage and fewer hit points, and nothing is healed. A maximum at or below the damage is fatal,
        /// as any loss of maximum hit points is.
        /// </summary>
        public static int HitPointsLeft(int maximum, int damage)
        {
            return maximum - Math.Max(0, damage);
        }

        /// <summary>
        /// Feeder protection at the handoff: the receiver's consumption already reconciled into the ledger, elapsed grants
        /// expired at their own times, and what is left exposed with its original expiry. Nothing is rolled or extended.
        /// </summary>
        public static int FeederHandoff(FeederState feeder, double now, out double expiresAt)
        {
            FeederRules.Expire(feeder, now);
            int amount = FeederRules.ExposedTemporaryHP(feeder, now);
            FeederGrant strongest = feeder.Grants.Where(g => now < g.ExpiresAt && g.Remaining == amount && amount > 0).OrderByDescending(g => g.ExpiresAt).FirstOrDefault();
            expiresAt = strongest?.ExpiresAt ?? -1d;
            return amount;
        }

        /// <summary>Feet of dimensional steps the emergence spends: the straight step out of the coffin, in whole 5-foot squares.</summary>
        public static int StepFeet(float fromX, float fromY, float toX, float toY)
        {
            double squares = Math.Sqrt((toX - fromX) * (toX - fromX) + (toY - fromY) * (toY - fromY));
            return Math.Max(1, (int)Math.Ceiling(squares - 1e-4)) * 5;
        }

        /// <summary>Uses of the native dimensional-steps resource a step of this length takes: one per 30 feet or part.</summary>
        public static int StepUses(int feet)
        {
            return Math.Max(1, (feet + StepsFeetPerUse - 1) / StepsFeetPerUse);
        }

        /// <summary>
        /// The first candidate whose whole footprint stands on walkable floor and overlaps nobody. Deterministic: the
        /// candidates' order decides, and an occupied preferred point only moves him, never onto anyone.
        /// </summary>
        public static GridPoint? ChoosePlacement(IEnumerable<GridPoint> candidates, IList<Occupant> occupied, Func<float, float, bool> walkable, float footprint = FootprintSquares)
        {
            foreach (GridPoint p in candidates)
            {
                if (!walkable(p.X, p.Y))
                {
                    continue;
                }
                bool clear = true;
                for (int i = 0; i < 8 && clear; i++)
                {
                    double angle = i * Math.PI / 4d;
                    clear = walkable(p.X + footprint * (float)Math.Cos(angle), p.Y + footprint * (float)Math.Sin(angle));
                }
                if (!clear)
                {
                    continue;
                }
                if (occupied.Any(o => Distance(p.X, p.Y, o.X, o.Y) < footprint + o.Radius))
                {
                    continue;
                }
                return p;
            }
            return null;
        }

        /// <summary>
        /// The placement check again at the moment he steps out: the point chosen when the hidden actor was made is kept if
        /// it is still clear; if someone has moved onto it since, the same policy chooses another; null when nowhere is clear.
        /// </summary>
        public static GridPoint? ConfirmPlacement(GridPoint chosen, IList<Occupant> occupied, Func<float, float, bool> walkable)
        {
            GridPoint? still = ChoosePlacement(new[] { chosen }, occupied, walkable);
            return still ?? ChoosePlacement(EmergenceCandidates(), occupied, walkable);
        }

        /// <summary>
        /// Preferred emergence points: east of the hanging coffin facing the altar, then rings around it. The coffin's own
        /// footprint is never a candidate.
        /// </summary>
        public static IEnumerable<GridPoint> EmergenceCandidates()
        {
            GridPoint coffin = SanctumPlan.SarcophagusCenter;
            var preferred = new GridPoint(coffin.X + 1.55f, coffin.Y);
            yield return preferred;
            foreach (GridPoint p in SanctumPlan.RingsAround(preferred.X, preferred.Y, 0.25f, 4.5f))
            {
                if (!InCoffin(p.X, p.Y))
                {
                    yield return p;
                }
            }
        }

        public static bool InCoffin(float x, float y)
        {
            return SanctumPlan.Inside(SanctumPlan.SarcophagusShadow, x, y);
        }

        /// <summary>
        /// A4/A5 floor that is walkable and not under the coffin. Placement keeps to the west chamber, where he wakes.
        /// </summary>
        public static bool A5Floor(float x, float y)
        {
            return x < 12.6f && SanctumPlan.SpaceAt(x, y) == SanctumSpace.Atrium && SanctumPlan.HeightAt(x, y).HasValue && !InCoffin(x, y);
        }

        /// <summary>
        /// The one-time decision for a save whose ritual already came due under an older build. Nothing is reset; a choice
        /// is offered once, and a save already carrying the new lifecycle is never asked.
        /// </summary>
        public static bool MarkLegacy(KruneState krune, RitualState ritual)
        {
            if (krune.Stage != AwakeningStage.Dormant || krune.Legacy != LegacyAwakening.None || !ritual.AwakeningDue)
            {
                return false;
            }
            krune.Legacy = LegacyAwakening.AwaitingChoice;
            return true;
        }

        /// <summary>Adopting keeps the recorded cause and time; the current saved mask becomes the revival snapshot, labelled as such.</summary>
        public static bool AdoptLegacy(KruneState krune, RitualState ritual, int currentMask, double now)
        {
            if (krune.Legacy != LegacyAwakening.AwaitingChoice)
            {
                return false;
            }
            krune.Legacy = LegacyAwakening.Adopted;
            Commit(krune, ritual, currentMask, now);
            krune.RevivalMaskFromMigration = true;
            return true;
        }

        public static bool KeepLegacy(KruneState krune)
        {
            if (krune.Legacy != LegacyAwakening.AwaitingChoice)
            {
                return false;
            }
            krune.Legacy = LegacyAwakening.KeptLegacy;
            return true;
        }

        /// <summary>Whether the lifecycle may advance: never while a legacy choice is pending or was declined.</summary>
        public static bool MayAdvance(KruneState krune)
        {
            return krune.Legacy == LegacyAwakening.None || krune.Legacy == LegacyAwakening.Adopted;
        }

        /// <summary>The first hostile observation wins; the parley never resumes once he is hostile.</summary>
        public static bool TurnHostile(KruneState krune, HostilityCause cause, string detail, double now)
        {
            if (krune.Hostile)
            {
                return false;
            }
            krune.Hostile = true;
            krune.HostileCause = cause;
            krune.HostileDetail = detail ?? string.Empty;
            krune.HostileAt = now;
            return true;
        }

        /// <summary>Advances one stage only from the stage before it, so a repeated callback cannot skip or replay one.</summary>
        public static bool Advance(KruneState krune, AwakeningStage from, AwakeningStage to)
        {
            if (krune.Stage != from || (int)to != (int)from + 1)
            {
                return false;
            }
            krune.Stage = to;
            return true;
        }

        /// <summary>The prototype boundary holds the whole simulation once the handoff is reached, unless the harness released it.</summary>
        public static bool CheckpointHolds(KruneState krune)
        {
            return krune != null && krune.CheckpointReached && !krune.DiagnosticRelease && !krune.PrototypeRetired;
        }

        private static float Distance(float ax, float ay, float bx, float by)
        {
            return (float)Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
        }
    }
}
