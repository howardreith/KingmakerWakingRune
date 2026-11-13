using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>How a printed preparation exists in Krune's real book.</summary>
    internal enum PrepKind
    {
        /// <summary>The game's own spell, prepared and spent in his book.</summary>
        Native,
        /// <summary>An encounter-local spell built for this scenario (no native one exists), prepared and spent in his book.</summary>
        Local,
        /// <summary>Held: it occupies its printed slot but has no use in this fight or this game (named, never silently dropped).</summary>
        Held,
        /// <summary>Spent before the party arrived (Mage Armor, cast before hibernation).</summary>
        Spent,
    }

    /// <summary>One prepared slot of Krune's book: what fills it, with which metamagic, at which level.</summary>
    internal sealed class BookEntry
    {
        public int Level;
        public string Spell;
        public string Blueprint;
        public string Variant = string.Empty;
        public string Metamagic = string.Empty;
        public PrepKind Kind;
        public string Note = string.Empty;

        /// <summary>The level the base spell is known at: the slot level less the metamagic's adjustment.</summary>
        public int BaseLevel => Level - KruneBook.MetamagicCost(Metamagic);
    }

    /// <summary>
    /// Krune's printed preparation (page 16, KruneManifest) turned into book slots, one entry per slot. Pure: the native layer
    /// fills his wizard book from this list once, and his director casts only what is actually in a slot.
    /// </summary>
    internal static class KruneBook
    {
        private sealed class Source
        {
            public string Blueprint;
            public PrepKind Kind;
            public string Variant;
            public string Note;
        }

        private static readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>
        {
            { "Foresight", N("Foresight") },
            { "Summon Monster IV", N("SummonMonsterIVBase") },
            { "Summon Monster V", N("SummonMonsterVBase") },
            { "Summon Monster VI", N("SummonMonsterVIBase") },
            { "Summon Monster VII", N("SummonMonsterVIIBase") },
            { "Summon Monster IX", N("SummonMonsterIXBase") },
            { "Horrid Wilting", N("HorridWilting") },
            { "Dimension Door", N("DimensionDoorBase", "DimensionDoorCasterOnly") },
            { "Cloudkill", N("Cloudkill") },
            { "Dispel Magic", N("DispelMagic", "DispelMagicTarget") },
            { "Greater Dispel Magic", N("DispelMagicGreater", "DispelMagicGreaterTarget") },
            { "Acid Fog", N("AcidFog") },
            { "Glitterdust", N("Glitterdust") },
            { "True Seeing", N("TrueSeeingCast") },
            { "Grease", N("Grease") },
            { "Waves of Fatigue", N("WavesOfFatigue") },
            { "Fear", N("Fear") },
            { "Stoneskin", N("Stoneskin") },
            { "Protection from Energy", N("ProtectionFromEnergy") },
            { "Sleet Storm", N("SleetStorm") },
            { "Stinking Cloud", N("StinkingCloud") },
            { "Acid Arrow", N("AcidArrow") },
            { "Blindness/Deafness", N("Blindness", note: "the game's Blindness") },
            { "Web", N("Web") },
            { "Mage Armor", new Source { Blueprint = "MageArmor", Kind = PrepKind.Spent, Note = "cast before hibernation" } },
            { "Obscuring Mist", N("ObscuringMist") },
            { "Shield", N("MageShield") },
            { "True Strike", N("TrueStrike") },
            { "Acid Splash", N("AcidSplash") },
            { "Wall of Stone", L("KWR_KruneWallOfStone") },
            { "Black Tentacles", L("KWR_KruneBlackTentacles") },
            { "Greater Teleport", L("KWR_KruneGreaterTeleport", "within his sanctum only") },
            { "Phase Door", L("KWR_KrunePhaseDoor", "his own passage through one wall") },
            { "Spell Turning", L("KWR_KruneSpellTurning") },
            { "Globe of Invulnerability", L("KWR_KruneGlobeOfInvulnerability") },
            { "Hostile Juxtaposition", L("KWR_KruneHostileJuxtaposition") },
            { "Solid Fog", L("KWR_KruneSolidFog") },
            { "Fog Cloud", L("KWR_KruneFogCloud") },
            { "Planar Binding", H("ten minutes to cast; no use once the fight has begun") },
            { "Mage's Faithful Hound", H("a guard for an empty sanctum; he has his summons") },
            { "Explosive Runes", H("runes on a written text; no use in a fight") },
            { "Fly", H("no flight in this game") },
            { "Detect Thoughts", H("information the game's AI already has") },
            { "Erase", H("no use in a fight") },
            { "Unseen Servant", H("no use in a fight") },
            { "Arcane Mark", H("no use in a fight") },
            { "Mage Hand", H("no use in a fight") },
            { "Read Magic", H("no use in a fight") },
        };

        public static int MetamagicCost(string metamagic)
        {
            switch (metamagic)
            {
                case "Quickened":
                    return 4;
                case "Empowered":
                    return 2;
                default:
                    return 0;
            }
        }

        public static bool Known(string spell)
        {
            return Sources.ContainsKey(spell);
        }

        /// <summary>One entry per printed slot, in the manifest's order.</summary>
        public static List<BookEntry> Plan()
        {
            var plan = new List<BookEntry>();
            foreach (PreparedSpell p in KruneManifest.Prepared)
            {
                Source source = Sources[p.Spell];
                for (int i = 0; i < p.Count; i++)
                {
                    plan.Add(new BookEntry
                    {
                        Level = p.Level,
                        Spell = p.Spell,
                        Blueprint = source.Kind == PrepKind.Held ? HeldName(p.Spell) : source.Blueprint,
                        Variant = source.Variant ?? string.Empty,
                        Metamagic = p.Metamagic,
                        Kind = source.Kind,
                        Note = source.Note ?? string.Empty,
                    });
                }
            }
            return plan;
        }

        /// <summary>The printed number of slots at a level: the book is sized to exactly this, school slot included.</summary>
        public static int Slots(int level)
        {
            return KruneManifest.PreparedCount(level);
        }

        public static string HeldName(string spell)
        {
            return "KWR_KruneHeld_" + new string(spell.Where(char.IsLetter).ToArray());
        }

        public static IEnumerable<string> HeldSpells()
        {
            return Sources.Where(s => s.Value.Kind == PrepKind.Held).Select(s => s.Key);
        }

        public static string HeldReason(string spell)
        {
            return Sources[spell].Note;
        }

        public static IEnumerable<string> LocalBlueprints()
        {
            return Sources.Values.Where(s => s.Kind == PrepKind.Local).Select(s => s.Blueprint).Distinct();
        }

        private static Source N(string blueprint, string variant = null, string note = null)
        {
            return new Source { Blueprint = blueprint, Kind = PrepKind.Native, Variant = variant, Note = note };
        }

        private static Source L(string blueprint, string note = null)
        {
            return new Source { Blueprint = blueprint, Kind = PrepKind.Local, Note = note };
        }

        private static Source H(string reason)
        {
            return new Source { Kind = PrepKind.Held, Note = reason };
        }
    }
}
