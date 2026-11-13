using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>How an attack is delivered, for the rules that treat them differently (solid fog).</summary>
    internal enum AttackDelivery
    {
        Melee,
        /// <summary>A bow, crossbow, sling or thrown weapon: an ordinary ranged weapon attack.</summary>
        RangedWeapon,
        /// <summary>A ray or another magical ranged touch.</summary>
        RangedTouch,
    }

    /// <summary>
    /// Solid fog (core rules): a creature in it moves at half its normal speed and cannot take a 5-foot step; it takes -2 on
    /// melee attack and melee damage rolls; the vapors prevent effective ranged weapon attacks (magic rays and the like are
    /// unaffected); it obscures sight as fog cloud. Freedom of movement lets a creature move and attack normally in it.
    /// </summary>
    internal static class SolidFogRules
    {
        public const int MeleePenalty = -2;
        public const double SpeedMultiplier = 0.5d;

        /// <summary>The attack and damage modifier on an attack by a creature standing in the fog.</summary>
        public static int MeleeModifier(AttackDelivery delivery, bool free)
        {
            return delivery == AttackDelivery.Melee && !free ? MeleePenalty : 0;
        }

        /// <summary>An ordinary ranged weapon attack made from, into or through the fog fails; a ray goes through.</summary>
        public static bool RangedAttackFails(AttackDelivery delivery, bool attackerInside, bool targetInside, bool lineCrosses)
        {
            return delivery == AttackDelivery.RangedWeapon && (attackerInside || targetInside || lineCrosses);
        }

        /// <summary>Movement in the fog: half the creature's own speed (a hasted creature still moves at half its hasted speed).</summary>
        public static double SpeedIn(double speed, bool free)
        {
            return free ? speed : speed * SpeedMultiplier;
        }

        /// <summary>Whether the segment a-b passes within r of c, on the floor plane.</summary>
        public static bool SegmentNear(float ax, float ay, float bx, float by, float cx, float cy, float r)
        {
            float dx = bx - ax, dy = by - ay;
            float len = dx * dx + dy * dy;
            float t = len <= 0f ? 0f : Math.Max(0f, Math.Min(1f, ((cx - ax) * dx + (cy - ay) * dy) / len));
            float px = ax + t * dx - cx, py = ay + t * dy - cy;
            return px * px + py * py <= r * r;
        }
    }

    internal enum TentacleResult
    {
        /// <summary>Freedom of movement: the tentacles cannot hold it (and let go of it if they held it).</summary>
        Immune,
        /// <summary>Newly grappled: grappled and 1d6+4.</summary>
        Grappled,
        /// <summary>Already held and the round's check succeeds: 1d6+4.</summary>
        Maintained,
        /// <summary>Not held and the check fails.</summary>
        Missed,
        /// <summary>Held, and the round's check fails: the hold is broken.</summary>
        Released,
    }

    /// <summary>
    /// Black tentacles (core rules): one grapple check for the whole spell each round, at the start of the caster's turn
    /// (including the round it is cast), applied to every creature in the area; a creature entering is attacked with that
    /// round's result. CMB = caster level + 4 (Strength) + 1 (size); +5 against a creature it already grapples. A
    /// successful check grapples (or keeps grappling) and deals 1d6+4. A natural 20 always succeeds and a natural 1 always
    /// fails. Freedom of movement cannot be grappled.
    /// </summary>
    internal static class TentacleRules
    {
        public const int HeldBonus = 5;
        public const double RoundSeconds = 6d;
        /// <summary>A round's start that falls in the same moment as the tentacles' appearance is the same round.</summary>
        public const double SameMomentSeconds = 1d;

        public static int Cmb(int casterLevel)
        {
            return Math.Max(1, casterLevel) + 5;
        }

        public static TentacleResult Resolve(int d20, int cmb, int cmd, bool held, bool free)
        {
            if (free)
            {
                return TentacleResult.Immune;
            }
            int total = d20 + cmb + (held ? HeldBonus : 0);
            bool success = d20 != 1 && (d20 == 20 || total >= cmd);
            if (held)
            {
                return success ? TentacleResult.Maintained : TentacleResult.Released;
            }
            return success ? TentacleResult.Grappled : TentacleResult.Missed;
        }

        public static bool Damages(TentacleResult result)
        {
            return result == TentacleResult.Grappled || result == TentacleResult.Maintained;
        }

        /// <summary>A new round's roll is made unless the current one was made at this same moment (the cast's own round).</summary>
        public static bool NewRoundRoll(double rolledAt, double now)
        {
            return rolledAt < 0d || now < rolledAt || now - rolledAt >= SameMomentSeconds;
        }

        /// <summary>An entering creature meets the current round's roll if there is one; else it starts the round's roll.</summary>
        public static bool EntryNeedsRoll(double rolledAt, double now)
        {
            return rolledAt < 0d || now < rolledAt || now - rolledAt >= RoundSeconds;
        }
    }

    /// <summary>A metamagic option of the rune-carved rod and the spell levels (charges) it adds.</summary>
    internal sealed class RodOption
    {
        public string Feat;
        /// <summary>The game's own metamagic it is applied as; empty when the game has no such mechanic.</summary>
        public string Native;
        public int Cost;
        public string Note;

        public bool Supported => Native.Length > 0;
    }

    /// <summary>
    /// Krune's rune-carved rod (page 16): a greater metamagic rod with seven feats; up to three applied to one spell as it
    /// is cast; 10 charges a day; each costs charges equal to the spell levels it adds. Silent, Still and Widen are not
    /// implemented in this draft: they are listed as unavailable, never applied and never charged. Enlarge is adapted as
    /// the game's own Reach (+1 level, the next range band) rather than Enlarge's doubled range.
    /// </summary>
    internal static class RodRules
    {
        public const int ChargesPerDay = 10;
        public const int MaxPerSpell = 3;

        public static readonly RodOption[] Options =
        {
            new RodOption { Feat = "Empower", Native = "Empower", Cost = 2, Note = "variable numeric effects x1.5" },
            new RodOption { Feat = "Enlarge", Native = "Reach", Cost = 1, Note = "adapted as the game's Reach Spell (the next range band), not Enlarge's doubled range" },
            new RodOption { Feat = "Extend", Native = "Extend", Cost = 1, Note = "double duration" },
            new RodOption { Feat = "Maximize", Native = "Maximize", Cost = 3, Note = "variable numeric effects maximized" },
            new RodOption { Feat = "Silent", Native = string.Empty, Cost = 1, Note = "not implemented in this draft" },
            new RodOption { Feat = "Still", Native = string.Empty, Cost = 1, Note = "not implemented in this draft" },
            new RodOption { Feat = "Widen", Native = string.Empty, Cost = 3, Note = "not implemented in this draft" },
        };

        public static RodOption Option(string native)
        {
            return Options.First(o => o.Native == native);
        }

        /// <summary>
        /// The requested metamagic that is applied: in the order asked, at most three, each legal for the spell (the game's
        /// own list of metamagic the spell accepts), not already on it, and paid for from the charges left.
        /// </summary>
        public static List<RodOption> Apply(IEnumerable<string> requested, ICollection<string> legal, ICollection<string> already, int charges)
        {
            var applied = new List<RodOption>();
            int left = charges;
            foreach (string native in requested.Distinct())
            {
                RodOption option = Options.FirstOrDefault(o => o.Native == native && o.Supported);
                if (option == null || applied.Count >= MaxPerSpell || !legal.Contains(native) || already.Contains(native) || option.Cost > left)
                {
                    continue;
                }
                applied.Add(option);
                left -= option.Cost;
            }
            return applied;
        }

        /// <summary>His most potent offensive spells (page 17): the rod's damage is kept for spells of this level and up.</summary>
        public const int PotentSpellLevel = 5;

        /// <summary>
        /// Krune's choice: one of his most potent damaging spells (5th level and up: cloudkill, acid fog, horrid wilting, his
        /// wish) is maximized and empowered while the charges allow (Maximize first: it costs more and does more); a lesser
        /// damaging spell gets nothing; a summons is extended only while five charges stay in hand for the next potent
        /// spell. Reach is never asked for: his director only chooses targets already in range.
        /// </summary>
        public static List<string> KruneRequest(bool damaging, bool lasting, int charges, int spellLevel)
        {
            if (damaging && spellLevel >= PotentSpellLevel)
            {
                return new List<string> { "Maximize", "Empower" };
            }
            if (!damaging && lasting && charges >= 6)
            {
                return new List<string> { "Extend" };
            }
            return new List<string>();
        }

        /// <summary>
        /// The 24-hour renewal, on a fixed schedule: each whole day since the last renewal advances the schedule by that many
        /// days and restores the rod to 10, whether or not it was full. So a renewal that falls due while the rod is full is
        /// still taken (its date moves on), and a later expenditure is never refunded by a read. No schedule yet, or a date
        /// after now (an inconsistent state): the schedule starts now, with no refill.
        /// </summary>
        public static int Renew(int charges, ref double renewedAt, double now, double daySeconds)
        {
            if (renewedAt < 0d || now < renewedAt)
            {
                renewedAt = now;
                return charges;
            }
            double periods = Math.Floor((now - renewedAt) / daySeconds);
            if (periods < 1d)
            {
                return charges;
            }
            renewedAt += periods * daySeconds;
            return ChargesPerDay;
        }

        /// <summary>What one caster may still draw: the charges less those already promised to other casts pending now.</summary>
        public static int Available(int charges, int reservedByOthers)
        {
            return Math.Max(0, charges - Math.Max(0, reservedByOthers));
        }
    }

    /// <summary>
    /// The dragon-tooth spear's Ego (page 14): a wielder other than Krune makes a DC 22 Will save or is compelled to put it
    /// down. Each attempt after the first has the same result, and the spear strikes the wielder for 1d8+9 first. A wielder
    /// who resists still has to keep it: each round the spear tries to disarm itself with its telekinesis (CMB 20).
    /// </summary>
    internal static class SpearEgoRules
    {
        public const int WillDC = 22;
        public const int TelekinesisCmb = 20;
        public const int StrikeDice = 8;
        public const int StrikeBonus = 9;

        public static bool StrikesFirst(int priorAttempts)
        {
            return priorAttempts >= 1;
        }

        public static bool DisarmSucceeds(int d20, int cmd)
        {
            return d20 != 1 && (d20 == 20 || d20 + TelekinesisCmb >= cmd);
        }
    }

    internal enum DanceEnd
    {
        Continue,
        /// <summary>He grasps it as a free action as it ends: it cannot dance again for 4 rounds.</summary>
        Grasped,
        /// <summary>He cannot take a free action (helpless, held, out of his own control): it drops in his space.</summary>
        Dropped,
        /// <summary>He is dead: it falls with his gear.</summary>
        FallsWithHim,
    }

    /// <summary>
    /// A dancing weapon (core rules): loosed as a standard action, it fights for 4 rounds with the base attack bonus of the
    /// one who loosed it and then drops. It shares his space and goes wherever he goes, by physical or magical movement, and
    /// makes no attacks of opportunity. He may grasp it while it dances as a free action; so retrieved, it cannot dance again
    /// for 4 rounds.
    /// </summary>
    internal static class DanceRules
    {
        public const double DanceSeconds = 24d;
        public const double ReuseSeconds = 24d;

        public static DanceEnd End(double now, double until, bool masterDead, bool masterCanAct)
        {
            if (masterDead)
            {
                return DanceEnd.FallsWithHim;
            }
            if (now < until)
            {
                return DanceEnd.Continue;
            }
            return masterCanAct ? DanceEnd.Grasped : DanceEnd.Dropped;
        }

        public static bool CanLoose(double now, double lockedUntil)
        {
            return lockedUntil < 0d || now >= lockedUntil;
        }

        /// <summary>Whether he acts by his own will (whatever controls him otherwise owns his actions, the spear's pickup included).</summary>
        public static bool OwnMaster(bool canAct, bool confused, bool frightened, bool cowering, bool ownAllegiance)
        {
            return canAct && !confused && !frightened && !cowering && ownAllegiance;
        }
    }

    /// <summary>
    /// What Krune knows of a foe he cannot see now: the place he last saw it, for a short while. A suspicion justifies a
    /// search spell at that place; it never gives him the foe's present position.
    /// </summary>
    internal static class SightRules
    {
        public const double RememberSeconds = 12d;

        public static bool Suspected(double lastSeenAt, double now, bool visibleNow)
        {
            return !visibleNow && lastSeenAt >= 0d && now >= lastSeenAt && now - lastSeenAt <= RememberSeconds;
        }
    }
}
