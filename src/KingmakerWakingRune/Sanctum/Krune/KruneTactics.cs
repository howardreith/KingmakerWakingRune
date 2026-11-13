using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>Krune's tactical phase (page 17): fight; when overwhelmed withdraw elsewhere in the sanctum; recover; return.</summary>
    internal enum KrunePhase
    {
        Fight,
        Recover,
    }

    /// <summary>What a chosen action is aimed at; the native layer turns it into a real target.</summary>
    internal enum KruneAim
    {
        Self,
        /// <summary>The point that catches the most foes in the spell's area (and no ally, for harmful clouds).</summary>
        FoeCluster,
        NearestFoe,
        /// <summary>The foe whose magic most protects it (for dispelling) or that casts most (for blinding).</summary>
        FoeCaster,
        /// <summary>Where an unseen foe is believed to be.</summary>
        UnseenFoe,
        /// <summary>A line between him and the foes closing on him.</summary>
        Barrier,
        /// <summary>Beside him, toward the foes: where his summons appear.</summary>
        SummonPoint,
        /// <summary>A legal sanctum floor point far from every foe.</summary>
        Retreat,
        /// <summary>The foe linked for hostile juxtaposition: one standing near him.</summary>
        LinkFoe,
    }

    internal sealed class KruneChoice
    {
        public string Spell;
        public string Metamagic = string.Empty;
        public KruneAim Aim;
        public string Reason;

        public string Key => KruneTactics.Key(Spell, Metamagic);
    }

    /// <summary>What the director sees when the native brain gives him a decision. Read from the game; nothing invented.</summary>
    internal sealed class KruneView
    {
        public double HitPointFraction = 1d;
        public int AdjacentFoes;
        public int FoesWithin30;
        /// <summary>The most living foes one 20-ft-radius area would catch without catching an ally of his.</summary>
        public int BestCluster;
        public int FoeCasters;
        public bool UnseenFoeSuspected;
        public int SummonsAlive;
        public bool HasForesight;
        public bool HasStoneskin;
        public bool HasSpellTurning;
        public bool HasGlobe;
        public bool HasTrueSeeing;
        public bool HasShield;
        public bool WallStanding;
        public bool TentaclesActive;
        public bool CloudActive;
        public bool FoeHasStrongBuffs;
        public int RoundsInCombat;
        public KrunePhase Phase = KrunePhase.Fight;
        /// <summary>Ready slots by key (see <see cref="KruneTactics.Key"/>) with how many are left.</summary>
        public Dictionary<string, int> Ready = new Dictionary<string, int>();
    }

    /// <summary>
    /// The printed tactics (page 17) as a priority list over what is really left. "Combines quickened spells, summoning
    /// spells, and defensive spells to grant himself a tactical advantage before using his most potent offensive spells.
    /// Divides his enemies with conjured creatures and wall of stone; does not hesitate to cast cloudkill in close quarters.
    /// If overwhelmed, uses phase door or another movement ability to move elsewhere in his sanctum, casts more spells to
    /// augment himself and summons a small army. Does not surrender or retreat from the sanctum; fights to the death."
    /// It chooses only; every choice is cast with a native command and resolves natively.
    /// </summary>
    internal static class KruneTactics
    {
        public const double OverwhelmedHitPoints = 0.35d;
        public const double RecoveredHitPoints = 0.6d;

        public static string Key(string spell, string metamagic)
        {
            return metamagic.Length > 0 ? metamagic + " " + spell : spell;
        }

        public static bool Overwhelmed(KruneView v)
        {
            return v.HitPointFraction < OverwhelmedHitPoints && v.AdjacentFoes > 0 || v.AdjacentFoes >= 3;
        }

        /// <summary>
        /// The phase after this decision: withdraw when overwhelmed and able to (and stay withdrawing while still pressed, so
        /// the phase never flickers with foes at his side); once clear, return when recovered, found again, or well guarded.
        /// </summary>
        public static KrunePhase NextPhase(KruneView v)
        {
            if (Overwhelmed(v) && CanRelocate(v))
            {
                return KrunePhase.Recover;
            }
            if (v.Phase == KrunePhase.Fight)
            {
                return KrunePhase.Fight;
            }
            return v.HitPointFraction >= RecoveredHitPoints || v.AdjacentFoes > 0 || v.SummonsAlive >= 3 ? KrunePhase.Fight : KrunePhase.Recover;
        }

        public static bool CanRelocate(KruneView v)
        {
            return Has(v, "Phase Door") || Has(v, "Quickened Dimension Door") || Has(v, "Dimension Door") || Has(v, "Greater Teleport");
        }

        /// <summary>The swift action: a quickened spell, if one fits the moment.</summary>
        public static KruneChoice Swift(KruneView v)
        {
            return SwiftOptions(v).FirstOrDefault();
        }

        /// <summary>Every quickened option that fits, best first; the native layer takes the first it can really aim.</summary>
        public static List<KruneChoice> SwiftOptions(KruneView v)
        {
            var options = new List<KruneChoice>();
            if (Overwhelmed(v) && Has(v, "Quickened Dimension Door"))
            {
                options.Add(C("Dimension Door", "Quickened", KruneAim.Retreat, "overwhelmed: a quickened step away"));
            }
            if (!v.WallStanding && v.FoesWithin30 >= 2 && Has(v, "Quickened Wall of Stone"))
            {
                options.Add(C("Wall of Stone", "Quickened", KruneAim.Barrier, "divide the foes closing on him"));
            }
            if (v.UnseenFoeSuspected && Has(v, "Quickened Glitterdust"))
            {
                options.Add(C("Glitterdust", "Quickened", KruneAim.UnseenFoe, "reveal an unseen foe"));
            }
            if (!v.TentaclesActive && v.BestCluster >= 2 && Has(v, "Quickened Black Tentacles"))
            {
                options.Add(C("Black Tentacles", "Quickened", KruneAim.FoeCluster, "pin a cluster of foes"));
            }
            if (v.SummonsAlive < 4 && Has(v, "Quickened Summon Monster V"))
            {
                options.Add(C("Summon Monster V", "Quickened", KruneAim.SummonPoint, "summons to divide the party"));
            }
            if (v.SummonsAlive < 4 && Has(v, "Quickened Summon Monster IV"))
            {
                options.Add(C("Summon Monster IV", "Quickened", KruneAim.SummonPoint, "summons to divide the party"));
            }
            if (v.FoeHasStrongBuffs && Has(v, "Quickened Dispel Magic"))
            {
                options.Add(C("Dispel Magic", "Quickened", KruneAim.FoeCaster, "strip a foe's protection"));
            }
            if (v.AdjacentFoes > 0 && Has(v, "Quickened Grease"))
            {
                options.Add(C("Grease", "Quickened", KruneAim.FoeCluster, "trip the foes at his side without slicking his own footing"));
            }
            return options;
        }

        /// <summary>The standard action (or full round, for an unquickened summons).</summary>
        public static KruneChoice Standard(KruneView v)
        {
            return StandardOptions(v).FirstOrDefault();
        }

        /// <summary>Every standard-action option that fits, best first; the native layer takes the first it can really aim.</summary>
        public static List<KruneChoice> StandardOptions(KruneView v)
        {
            var options = new List<KruneChoice>();
            if (Overwhelmed(v))
            {
                options.AddRange(new[] { Relocate(v) }.Where(c => c != null));
            }
            options.AddRange(Defenses(v).Concat(Summons(v)).Concat(Offense(v)).Where(c => c != null));
            return options;
        }

        private static KruneChoice Relocate(KruneView v)
        {
            if (Has(v, "Phase Door"))
            {
                return C("Phase Door", string.Empty, KruneAim.Retreat, "overwhelmed: through the wall to another part of the sanctum");
            }
            if (Has(v, "Greater Teleport"))
            {
                return C("Greater Teleport", string.Empty, KruneAim.Retreat, "overwhelmed: elsewhere in the sanctum");
            }
            if (Has(v, "Dimension Door"))
            {
                return C("Dimension Door", string.Empty, KruneAim.Retreat, "overwhelmed: away from the foes");
            }
            return null;
        }

        private static IEnumerable<KruneChoice> Defenses(KruneView v)
        {
            bool early = v.RoundsInCombat <= 3 || v.Phase == KrunePhase.Recover;
            if (!v.HasForesight && Has(v, "Foresight"))
            {
                yield return C("Foresight", string.Empty, KruneAim.Self, "his first protection");
            }
            if (!v.HasSpellTurning && v.FoeCasters > 0 && Has(v, "Spell Turning"))
            {
                yield return C("Spell Turning", string.Empty, KruneAim.Self, "foes cast at him");
            }
            if (v.UnseenFoeSuspected && !v.HasTrueSeeing && Has(v, "True Seeing"))
            {
                yield return C("True Seeing", string.Empty, KruneAim.Self, "an unseen foe");
            }
            if (early && !v.HasStoneskin && Has(v, "Stoneskin"))
            {
                yield return C("Stoneskin", string.Empty, KruneAim.Self, "protection before the foes reach him");
            }
            if (early && !v.HasGlobe && v.FoeCasters > 0 && Has(v, "Globe of Invulnerability"))
            {
                yield return C("Globe of Invulnerability", string.Empty, KruneAim.Self, "foes' lesser spells");
            }
            if (v.AdjacentFoes > 0 && !v.HasShield && Has(v, "Shield"))
            {
                yield return C("Shield", string.Empty, KruneAim.Self, "a foe at his side");
            }
        }

        private static IEnumerable<KruneChoice> Summons(KruneView v)
        {
            if (v.SummonsAlive >= 6)
            {
                yield break;
            }
            foreach (string summon in new[] { "Summon Monster IX", "Empowered Summon Monster VI", "Summon Monster VII", "Summon Monster VI", "Summon Monster V" })
            {
                if (Has(v, summon) && (v.SummonsAlive < 2 || v.Phase == KrunePhase.Recover))
                {
                    string metamagic = summon.StartsWith("Empowered ") ? "Empowered" : string.Empty;
                    yield return C(summon.Replace("Empowered ", string.Empty), metamagic, KruneAim.SummonPoint, v.Phase == KrunePhase.Recover ? "a small army while he recovers" : "summons to divide the party");
                }
            }
        }

        private static IEnumerable<KruneChoice> Offense(KruneView v)
        {
            if (v.Phase == KrunePhase.Recover)
            {
                yield break;
            }
            if (!v.WallStanding && v.FoesWithin30 >= 2 && Has(v, "Wall of Stone"))
            {
                yield return C("Wall of Stone", string.Empty, KruneAim.Barrier, "divide the foes");
            }
            if (v.BestCluster >= 3 && Has(v, "Horrid Wilting"))
            {
                yield return C("Horrid Wilting", string.Empty, KruneAim.FoeCluster, "his most potent spell on a cluster");
            }
            if (!v.CloudActive && v.BestCluster >= 2)
            {
                foreach (string cloud in new[] { "Empowered Cloudkill", "Cloudkill", "Acid Fog", "Stinking Cloud" })
                {
                    if (Has(v, cloud))
                    {
                        yield return C(cloud.Replace("Empowered ", string.Empty), cloud.StartsWith("Empowered ") ? "Empowered" : string.Empty, KruneAim.FoeCluster, "a cloud he is immune to, in close quarters");
                    }
                }
            }
            if (!v.TentaclesActive && v.BestCluster >= 2 && Has(v, "Black Tentacles"))
            {
                yield return C("Black Tentacles", string.Empty, KruneAim.FoeCluster, "pin a cluster");
            }
            if (v.AdjacentFoes > 0 && Has(v, "Hostile Juxtaposition"))
            {
                yield return C("Hostile Juxtaposition", string.Empty, KruneAim.LinkFoe, "turn a foe's blows onto another");
            }
            if (v.FoeHasStrongBuffs && Has(v, "Greater Dispel Magic"))
            {
                yield return C("Greater Dispel Magic", string.Empty, KruneAim.FoeCaster, "strip a foe's protections");
            }
            if (v.UnseenFoeSuspected && Has(v, "Glitterdust"))
            {
                yield return C("Glitterdust", string.Empty, KruneAim.UnseenFoe, "reveal an unseen foe");
            }
            if (v.BestCluster >= 2)
            {
                foreach (string area in new[] { "Waves of Fatigue", "Fear", "Sleet Storm", "Web", "Glitterdust", "Solid Fog" })
                {
                    if (Has(v, area))
                    {
                        yield return C(area, string.Empty, KruneAim.FoeCluster, "control a cluster");
                    }
                }
            }
            foreach (string single in new[] { "Blindness/Deafness", "Acid Arrow", "Dispel Magic" })
            {
                if (Has(v, single) && (single != "Dispel Magic" || v.FoeHasStrongBuffs))
                {
                    yield return C(single, string.Empty, single == "Acid Arrow" ? KruneAim.NearestFoe : KruneAim.FoeCaster, "a single foe");
                }
            }
        }

        private static bool Has(KruneView v, string key)
        {
            return v.Ready.TryGetValue(key, out int n) && n > 0;
        }

        private static KruneChoice C(string spell, string metamagic, KruneAim aim, string reason)
        {
            return new KruneChoice { Spell = spell, Metamagic = metamagic, Aim = aim, Reason = reason };
        }
    }
}
