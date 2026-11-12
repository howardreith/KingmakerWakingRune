using System;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal enum RuneKind
    {
        Life,
        Alacrity,
        Defense,
        Armor,
        Control,
    }

    internal sealed class RuneFocus
    {
        public readonly string Id;
        public readonly RuneKind Kind;
        public readonly int Bit;

        public RuneFocus(string id, RuneKind kind, int bit)
        {
            Id = id;
            Kind = kind;
            Bit = bit;
        }
    }

    /// <summary>Krune's rune benefits at one instant, always recomputed from the immutable baseline.</summary>
    internal struct KruneProjectionResult
    {
        public int MaximumHP;
        public int LifeFastHealing;
        public int NaturalArmor;
        public int DodgeAC;
        public int InitiativeInsight;
        public int DeflectionAC;
        public int ResistanceSaves;
        public int SpearCompetence;
        public bool ConjuredCreatureProtection;
        public int CasterLevel;
        public int WeakeningStage;
    }

    /// <summary>
    /// Subtier 10-11, six PCs, extra challenge: what the seven-bit disabled mask leaves of Krune's rune benefits.
    /// A pure function of the mask, so nothing can be subtracted twice by a reload or a repeat call.
    /// </summary>
    internal static class KruneProjection
    {
        public const int BaseCasterLevel = 17;
        public const int FullListedHP = 293;
        public const int FullMask = 127;

        private const int LifeHPActive = 40, LifeHPDisabled = 20;
        private const int LifeFastHealingActive = 1, LifeFastHealingDisabled = 0;

        private static readonly int[] CasterLevelLoss = { 0, 1, 1, 2, 2, 2, 2, 3 };
        private static readonly int[] WeakeningStages = { 0, 1, 1, 2, 2, 3, 3, 4 };

        /// <summary>Bit order is the printed order, A3a through A3g, and matches the authoritative mask in saves.</summary>
        public static readonly RuneFocus[] Foci =
        {
            new RuneFocus("A3a", RuneKind.Life, 0),
            new RuneFocus("A3b", RuneKind.Alacrity, 1),
            new RuneFocus("A3c", RuneKind.Defense, 2),
            new RuneFocus("A3d", RuneKind.Life, 3),
            new RuneFocus("A3e", RuneKind.Armor, 4),
            new RuneFocus("A3f", RuneKind.Control, 5),
            new RuneFocus("A3g", RuneKind.Life, 6),
        };

        public static RuneFocus Focus(string id)
        {
            return Foci.First(f => f.Id == id);
        }

        public static KruneProjectionResult Project(int disabledMask)
        {
            if (disabledMask < 0 || disabledMask > FullMask)
            {
                throw new ArgumentOutOfRangeException(nameof(disabledMask));
            }
            int disabledCount = Foci.Count(f => IsDisabled(disabledMask, f));
            int disabledLife = Foci.Count(f => f.Kind == RuneKind.Life && IsDisabled(disabledMask, f));
            int activeLife = 3 - disabledLife;
            bool armor = KindDisabled(disabledMask, RuneKind.Armor);
            bool alacrity = KindDisabled(disabledMask, RuneKind.Alacrity);
            bool defense = KindDisabled(disabledMask, RuneKind.Defense);
            bool control = KindDisabled(disabledMask, RuneKind.Control);
            return new KruneProjectionResult
            {
                // The listed HP already includes all three active contributions.
                MaximumHP = FullListedHP - disabledLife * (LifeHPActive - LifeHPDisabled),
                LifeFastHealing = activeLife * LifeFastHealingActive + disabledLife * LifeFastHealingDisabled,
                NaturalArmor = Halved(6, armor),
                DodgeAC = Halved(4, alacrity),
                InitiativeInsight = Halved(4, alacrity),
                DeflectionAC = Halved(5, defense),
                ResistanceSaves = Halved(5, defense),
                SpearCompetence = Halved(3, control),
                ConjuredCreatureProtection = !control,
                CasterLevel = BaseCasterLevel - CasterLevelLoss[disabledCount],
                WeakeningStage = WeakeningStages[disabledCount],
            };
        }

        private static bool IsDisabled(int mask, RuneFocus focus)
        {
            return (mask & (1 << focus.Bit)) != 0;
        }

        private static bool KindDisabled(int mask, RuneKind kind)
        {
            return Foci.Any(f => f.Kind == kind && IsDisabled(mask, f));
        }

        /// <summary>Halved rounding down, the high-tier result of disabling the focus.</summary>
        private static int Halved(int active, bool disabled)
        {
            return disabled ? active / 2 : active;
        }
    }
}
