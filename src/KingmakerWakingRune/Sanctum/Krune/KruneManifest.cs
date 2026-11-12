using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>What this increment does with a printed resource.</summary>
    internal enum ManifestStatus
    {
        /// <summary>Created now as a native item, fact or resource with a stable identity.</summary>
        Native,
        /// <summary>Recorded only; the combat increment builds it. Nothing claims it is usable.</summary>
        Deferred,
        /// <summary>Already spent before the party arrived (Mage Armor's slot, cast before hibernation).</summary>
        Spent,
    }

    internal sealed class PreparedSpell
    {
        public int Level;
        public string Spell;
        public string Metamagic = string.Empty;
        public int Count = 1;
        public string Note = string.Empty;
        public ManifestStatus Status = ManifestStatus.Deferred;
    }

    internal sealed class ManifestItem
    {
        public string Key;
        public string Name;
        public string Detail;
        public ManifestStatus Status;
    }

    /// <summary>
    /// Krune's printed preparation and equipment (page 16 and 17) with the extra-challenge adjustments (page 14), as an
    /// auditable list for the combat increment. Data only: the live state of anything created is the native item,
    /// spellbook or resource itself, never a second ledger here.
    /// </summary>
    internal static class KruneManifest
    {
        public static readonly PreparedSpell[] Prepared =
        {
            // 9th. Extra challenge: one of the two quickened summon monster V is replaced by an ordinary summon monster IX.
            S(9, "Foresight"),
            S(9, "Summon Monster V", "Quickened", 1, "printed 2; one replaced by summon monster IX (extra challenge)"),
            S(9, "Summon Monster IX", note: "extra challenge replacement; an ordinary ninth-level spell, not quickened"),
            S(9, "Wall of Stone", "Quickened"),
            S(8, "Summon Monster VI", "Empowered"),
            S(8, "Horrid Wilting", note: "DC 28"),
            S(8, "Black Tentacles", "Quickened"),
            S(8, "Dimension Door", "Quickened"),
            S(8, "Summon Monster IV", "Quickened"),
            S(7, "Cloudkill", "Empowered"),
            S(7, "Greater Teleport"),
            S(7, "Phase Door"),
            S(7, "Dispel Magic", "Quickened"),
            S(7, "Spell Turning"),
            S(7, "Summon Monster VII"),
            S(6, "Acid Fog", count: 2),
            S(6, "Globe of Invulnerability"),
            S(6, "Greater Dispel Magic"),
            S(6, "Planar Binding", note: "DC 28"),
            S(6, "Glitterdust", "Quickened"),
            S(6, "Summon Monster VI"),
            S(6, "True Seeing"),
            S(5, "Cloudkill", note: "DC 27"),
            S(5, "Hostile Juxtaposition", note: "DC 27"),
            S(5, "Mage's Faithful Hound"),
            S(5, "Grease", "Quickened", note: "DC 23"),
            S(5, "Summon Monster V", count: 2),
            S(5, "Wall of Stone"),
            S(5, "Waves of Fatigue"),
            S(4, "Black Tentacles"),
            S(4, "Dimension Door", count: 2),
            S(4, "Fear", note: "DC 24"),
            S(4, "Solid Fog"),
            S(4, "Stoneskin", note: "prepared, not a free pre-buff"),
            S(4, "Summon Monster IV", count: 2),
            S(3, "Dispel Magic", count: 2),
            S(3, "Explosive Runes"),
            S(3, "Fly"),
            S(3, "Protection from Energy"),
            S(3, "Sleet Storm"),
            S(3, "Stinking Cloud", count: 2, note: "DC 25"),
            S(2, "Acid Arrow"),
            S(2, "Blindness/Deafness", count: 2, note: "DC 22"),
            S(2, "Detect Thoughts", note: "DC 22"),
            S(2, "Fog Cloud"),
            S(2, "Glitterdust", count: 2, note: "DC 24"),
            S(2, "Web", count: 2, note: "DC 24"),
            S(1, "Erase"),
            S(1, "Grease", count: 2, note: "DC 23"),
            new PreparedSpell { Level = 1, Spell = "Mage Armor", Note = "cast before hibernation and sustained by the sanctum (page 17): the slot is spent and the effect is on him", Status = ManifestStatus.Spent },
            S(1, "Obscuring Mist"),
            S(1, "Shield"),
            S(1, "True Strike", count: 2),
            S(1, "Unseen Servant"),
            S(0, "Acid Splash"),
            S(0, "Arcane Mark"),
            S(0, "Mage Hand"),
            S(0, "Read Magic"),
        };

        /// <summary>Known beyond the prepared list: wish, cast through his arcane focus (extra challenge, page 14).</summary>
        public static readonly string[] KnownExtra = { "Wish (extra challenge: known, castable through his arcane focus; adjudication deferred)" };

        public static readonly ManifestItem[] Items =
        {
            I("Spear", "Krune's dragon-tooth spear", "major artifact; +3 longspear (page 15); his arcane bond. Native +3 longspear now; dancing, ego, telekinesis, lesser powers and the wield-check deferred", ManifestStatus.Native),
            I("Rod", "Krune's rune-carved rod", "minor artifact; greater metamagic rod of seven feats, up to three per spell, 10 charges a day (page 16). Carried now with its own 10-charge native resource; metamagic application deferred", ManifestStatus.Native),
            I("Belt", "Belt of physical perfection +4", "native item, worn; its +4 is part of the printed Str 18, Dex 18, Con 20", ManifestStatus.Native),
            I("Periapt", "Periapt of proof against poison", "worn; immunity to poison as a native fact on the item", ManifestStatus.Native),
            I("RingFreedom", "Ring of freedom of movement", "worn; the game's own freedom-of-movement enchantment", ManifestStatus.Native),
            I("WandAcidArrow", "Wand of acid arrow", "CL 9, 23 charges (printed remaining charges)", ManifestStatus.Native),
            I("WandSummonMonsterIII", "Wand of summon monster III", "12 charges (printed remaining charges)", ManifestStatus.Native),
            I("Diamonds", "Diamonds (75,000 gp)", "extra challenge: a finite material reserve in his inventory, 15 native diamonds of 5,000 gp; not party gold. Loot policy belongs to the conclusion milestone", ManifestStatus.Native),
            I("Dust", "Diamond and granite dust (1,000 gp)", "20 native diamond dust (50 gp); the game has no granite dust", ManifestStatus.Native),
            I("BootsOfSpeed", "Boots of speed", "deferred: no native item; not a free haste", ManifestStatus.Deferred),
            I("PearlOfPower", "Pearl of power (8th)", "deferred: recovers one spent 8th-level slot; no native item", ManifestStatus.Deferred),
            I("RingSpellTurning", "Ring of spell turning", "deferred: not a free pre-buff; no native spell turning", ManifestStatus.Deferred),
            I("ComponentPouch", "Spell component pouch", "flavour; Eschew Materials is a feat", ManifestStatus.Deferred),
        };

        public static readonly ManifestItem[] Resources =
        {
            I("DimensionalSteps", "Dimensional steps", "510 feet a day: the game's own conjuration-school resource, 17 uses of up to 30 feet; the opening spends one use, once", ManifestStatus.Native),
            I("AcidDart", "Acid dart", "13/day, 1d6+8 (conjuration school); combat use deferred", ManifestStatus.Deferred),
            I("RodCharges", "Rune-carved rod charges", "10 a day, the rod's own pool", ManifestStatus.Native),
            I("Contingency", "Contingency (dispel magic CL 17 on a teleport-impeding effect)", "deferred", ManifestStatus.Deferred),
            I("RunelordsDominion", "Runelord's dominion (DC 25)", "deferred", ManifestStatus.Deferred),
            I("Runesight", "Runesight", "deferred: blindsense to runes within 60 ft", ManifestStatus.Deferred),
            I("ArcaneSight", "Arcane sight (permanent)", "deferred: information only", ManifestStatus.Deferred),
        };

        public static int PreparedCount(int level)
        {
            return Prepared.Where(p => p.Level == level).Sum(p => p.Count);
        }

        public static int CountOf(string spell, string metamagic)
        {
            return Prepared.Where(p => p.Spell == spell && p.Metamagic == metamagic).Sum(p => p.Count);
        }

        public static IEnumerable<string> Lines()
        {
            foreach (PreparedSpell p in Prepared)
            {
                yield return $"L{p.Level} {(p.Metamagic.Length > 0 ? p.Metamagic.ToLowerInvariant() + " " : string.Empty)}{p.Spell}{(p.Count > 1 ? " x" + p.Count : string.Empty)} [{p.Status}]{(p.Note.Length > 0 ? " " + p.Note : string.Empty)}";
            }
            foreach (string known in KnownExtra)
            {
                yield return "known: " + known;
            }
            foreach (ManifestItem item in Items.Concat(Resources))
            {
                yield return $"{item.Key}: {item.Name} [{item.Status}] {item.Detail}";
            }
        }

        private static PreparedSpell S(int level, string spell, string metamagic = "", int count = 1, string note = "")
        {
            return new PreparedSpell { Level = level, Spell = spell, Metamagic = metamagic, Count = count, Note = note };
        }

        private static ManifestItem I(string key, string name, string detail, ManifestStatus status)
        {
            return new ManifestItem { Key = key, Name = name, Detail = detail, Status = status };
        }
    }
}
