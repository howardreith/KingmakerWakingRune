using System;
using System.Collections.Generic;
using System.Linq;

// Saved fields: some are only ever written by the native adapters or the JSON reader.
#pragma warning disable CS0649

namespace KingmakerWakingRune.Sanctum.Lashmistress
{
    /// <summary>The four A4 defenders. The scaling sidebar's "Jargas" is the encounter block's Jorgas: one identity.</summary>
    internal static class DefenderKeys
    {
        public const string Vandiana = "A4-Vandiana";
        public const string Agarik = "A4-Agarik";
        public const string Jorgas = "A4-Jorgas";
        public const string Naga = "A4-DarkNaga";

        public static readonly string[] All = { Vandiana, Agarik, Jorgas, Naga };

        /// <summary>The extra-challenge sidebar (page 12): Agarik, Jorgas and the dark naga, never Vandiana.</summary>
        public static readonly string[] Advanced = { Agarik, Jorgas, Naga };

        public static bool IsAdvanced(string key)
        {
            return Advanced.Contains(key);
        }
    }

    /// <summary>The six ability scores in order: Str, Dex, Con, Int, Wis, Cha.</summary>
    internal struct Abilities
    {
        public int Str, Dex, Con, Int, Wis, Cha;

        public Abilities(int str, int dex, int con, int @int, int wis, int cha)
        {
            Str = str;
            Dex = dex;
            Con = con;
            Int = @int;
            Wis = wis;
            Cha = cha;
        }

        public static int Mod(int score)
        {
            return (int)Math.Floor((score - 10) / 2d);
        }

        /// <summary>The advanced simple template, rebuild rules: +4 to every score except an Int of 2 or less.</summary>
        public Abilities Advanced()
        {
            return new Abilities(Str + 4, Dex + 4, Con + 4, Int <= 2 ? Int : Int + 4, Wis + 4, Cha + 4);
        }

        public override string ToString()
        {
            return $"Str {Str}, Dex {Dex}, Con {Con}, Int {Int}, Wis {Wis}, Cha {Cha}";
        }
    }

    /// <summary>
    /// The A4 source-to-runtime sheets (pages 12 and 13, NPC Codex 236, Bestiary 211), as plain numbers the unit tests check:
    /// unbuffed source, then the advanced template's rebuild where it applies, then the printed pre-combat buffs. The native
    /// difficulty preset goes on top in the game and is reported, never folded in here. See docs/WR-06-LASHMISTRESS.md.
    /// </summary>
    internal static class LashmistressProfile
    {
        // Vandiana, Subtier 10-11 (page 13): the printed scores carry Cat's Grace; her base Dex is 10. Int 17 already includes
        // the headband of vast intelligence +2 (it is what gives the printed 4th-level arcane slot count), so the base Int is 15.
        public static readonly Abilities VandianaBase = new Abilities(8, 10, 12, 15, 18, 14);
        public const int VandianaHeadband = 2;
        public const int VandianaClericLevels = 3;
        public const int VandianaWizardLevels = 4;
        public const int VandianaTheurgeLevels = 5;
        public const int VandianaDivineCasterLevel = VandianaClericLevels + VandianaTheurgeLevels;
        public const int VandianaArcaneCasterLevel = VandianaWizardLevels + VandianaTheurgeLevels;
        public const int VandianaBaseAttack = 6;
        public const int VandianaBaseHitPoints = 74;
        public const int VandianaPrintedFalseLife = 14;
        public const int VandianaFortitude = 11;
        public const int VandianaReflexBuffed = 10;
        public const int VandianaReflexBase = 8;
        public const int VandianaWill = 16;
        public const int VandianaChannelUses = 5;
        public const int VandianaChannelDC = 13;
        public const int VandianaAcidDartUses = 6;
        public const int VandianaBlastRuneUses = 7;
        public const int VandianaBlastRuneRounds = 3;
        /// <summary>The armour class she actually has unbuffed: 10 + shield 3 + deflection 2 + natural 1. The printed base AC 18 omits Mage Armor's +4 only from the armour line; see the sheet document.</summary>
        public const int VandianaBaseArmorClass = 16;
        public const int VandianaBuffedArmorClass = 22;

        // The Blackstrike (NPC Codex 236), Agarik and Jorgas alike: the base scores are printed without Bull's Strength.
        public static readonly Abilities BlackstrikeBase = new Abilities(18, 14, 14, 12, 10, 8);
        public const int BlackstrikeFighterLevels = 7;
        public const int BlackstrikeShadowdancerLevels = 2;
        public const int BlackstrikeHitDice = BlackstrikeFighterLevels + BlackstrikeShadowdancerLevels;
        public const int BlackstrikeBaseHitPoints = 86;
        public const int BlackstrikeBaseAttack = 8;
        public const int BlackstrikeFortitude = 9;
        public const int BlackstrikeReflex = 8;
        public const int BlackstrikeWill = 4;
        public const int BlackstrikeArmorClass = 20;

        // The dark naga (Bestiary 211).
        public static readonly Abilities NagaBase = new Abilities(14, 21, 18, 16, 15, 17);
        public const int NagaHitDice = 10;
        public const int NagaBaseHitPoints = 85;
        public const int NagaNaturalArmor = 7;
        public const int NagaFortitude = 7;
        public const int NagaReflex = 10;
        public const int NagaWill = 9;
        public const int NagaArmorClass = 22;
        public const int NagaCasterLevel = 7;
        public const int NagaPoisonBaseDC = 19;
        /// <summary>Sorcerer 7 spells per day before the Charisma bonus, by level 1-3.</summary>
        public static readonly int[] NagaBaseSlots = { 0, 6, 6, 4 };

        /// <summary>The advanced template's rebuild adds +2 natural armor; everything else follows from the +4 scores.</summary>
        public const int AdvancedNaturalArmor = 2;

        /// <summary>Hit points after the template's +4 Constitution: +2 a Hit Die on the printed total.</summary>
        public static int AdvancedHitPoints(int printed, int hitDice)
        {
            return printed + 2 * hitDice;
        }

        /// <summary>A Con-based DC (the naga's poison) after the template: the DC follows the new modifier.</summary>
        public static int AdvancedConDC(int printed, int baseCon)
        {
            return printed + Abilities.Mod(baseCon + 4) - Abilities.Mod(baseCon);
        }

        /// <summary>The bonus spells per day a casting score gives at one spell level (Core Rulebook table 1-3).</summary>
        public static int BonusSpells(int score, int level)
        {
            int mod = Abilities.Mod(score);
            if (level < 1 || mod < level)
            {
                return 0;
            }
            return (mod - level) / 4 + 1;
        }

        public static int NagaSlots(int level, bool advanced)
        {
            int cha = advanced ? NagaBase.Advanced().Cha : NagaBase.Cha;
            return NagaBaseSlots[level] + BonusSpells(cha, level);
        }
    }

    /// <summary>One prepared entry: the printed spell, and how this game carries it.</summary>
    internal enum SpellCarry
    {
        /// <summary>The game's own spell.</summary>
        Native,
        /// <summary>Built for this encounter from native rules; documented.</summary>
        Local,
        /// <summary>Its slot is held and can be spent only by its own printed casting before the party arrived.</summary>
        PreCast,
        /// <summary>No combat use or no supported treatment: the slot is held, cannot be cast here, and is listed as deferred.</summary>
        Deferred,
    }

    internal sealed class PreparedEntry
    {
        public readonly string Name;
        public readonly int Level;
        public readonly bool Domain;
        public readonly SpellCarry Carry;
        /// <summary>The game blueprint name for a native spell, the local key otherwise.</summary>
        public readonly string Source;

        public PreparedEntry(string name, int level, SpellCarry carry, string source, bool domain = false)
        {
            Name = name;
            Level = level;
            Carry = carry;
            Source = source;
            Domain = domain;
        }
    }

    /// <summary>Her printed Subtier 10-11 prepared lists (page 13), entry for entry, repeats included.</summary>
    internal static class VandianaSpells
    {
        public static readonly PreparedEntry[] Cleric =
        {
            new PreparedEntry("Cure Critical Wounds", 4, SpellCarry.Native, "CureCriticalWoundsCast"),
            new PreparedEntry("Explosive Runes", 4, SpellCarry.Deferred, "ExplosiveRunes", domain: true),
            new PreparedEntry("Order's Wrath", 4, SpellCarry.Local, "OrdersWrath"),
            new PreparedEntry("Unholy Blight", 4, SpellCarry.Local, "UnholyBlight"),
            new PreparedEntry("Blindness/Deafness", 3, SpellCarry.Native, "Blindness"),
            new PreparedEntry("Dispel Magic", 3, SpellCarry.Native, "DispelMagicTarget"),
            new PreparedEntry("Glyph of Warding", 3, SpellCarry.PreCast, "GlyphOfWarding", domain: true),
            new PreparedEntry("Magic Circle against Good", 3, SpellCarry.Local, "MagicCircleAgainstGood"),
            new PreparedEntry("Wind Wall", 3, SpellCarry.Deferred, "WindWall"),
            new PreparedEntry("Darkness", 2, SpellCarry.Local, "Darkness"),
            new PreparedEntry("Detect Thoughts", 2, SpellCarry.Deferred, "DetectThoughts", domain: true),
            new PreparedEntry("Eagle's Splendor", 2, SpellCarry.Native, "EaglesSplendor"),
            new PreparedEntry("Hold Person", 2, SpellCarry.Native, "HoldPerson"),
            new PreparedEntry("Sound Burst", 2, SpellCarry.Native, "SoundBurst"),
            new PreparedEntry("Command", 1, SpellCarry.Local, "Command"),
            new PreparedEntry("Comprehend Languages", 1, SpellCarry.Deferred, "ComprehendLanguages", domain: true),
            new PreparedEntry("Divine Favor", 1, SpellCarry.Native, "DivineFavor"),
            new PreparedEntry("Obscuring Mist", 1, SpellCarry.Native, "ObscuringMist"),
            new PreparedEntry("Remove Fear", 1, SpellCarry.Native, "RemoveFear"),
            new PreparedEntry("Sanctuary", 1, SpellCarry.Local, "Sanctuary"),
        };

        public static readonly PreparedEntry[] Wizard =
        {
            new PreparedEntry("Baleful Polymorph", 5, SpellCarry.Native, "BalefulPolymorph"),
            new PreparedEntry("Mage's Faithful Hound", 5, SpellCarry.PreCast, "MagesFaithfulHound"),
            new PreparedEntry("Confusion", 4, SpellCarry.Native, "ConfusionSpell"),
            new PreparedEntry("Dimension Door", 4, SpellCarry.Native, "DimensionDoorBase"),
            new PreparedEntry("Enervation", 4, SpellCarry.Native, "Enervation"),
            new PreparedEntry("Arcane Sight", 3, SpellCarry.Deferred, "ArcaneSight"),
            new PreparedEntry("Haste", 3, SpellCarry.Native, "Haste"),
            new PreparedEntry("Sleet Storm", 3, SpellCarry.Native, "SleetStorm"),
            new PreparedEntry("Slow", 3, SpellCarry.Native, "Slow"),
            new PreparedEntry("Summon Monster III", 3, SpellCarry.Native, "SummonMonsterIIIBase"),
            new PreparedEntry("Command Undead", 2, SpellCarry.Deferred, "CommandUndead"),
            new PreparedEntry("Darkvision", 2, SpellCarry.Local, "Darkvision"),
            new PreparedEntry("False Life", 2, SpellCarry.PreCast, "FalseLife"),
            new PreparedEntry("Glitterdust", 2, SpellCarry.Native, "Glitterdust"),
            new PreparedEntry("Resist Energy", 2, SpellCarry.Native, "ResistEnergy"),
            new PreparedEntry("Web", 2, SpellCarry.Native, "Web"),
            new PreparedEntry("Alarm", 1, SpellCarry.PreCast, "Alarm"),
            new PreparedEntry("Chill Touch", 1, SpellCarry.Local, "ChillTouch"),
            new PreparedEntry("Chill Touch", 1, SpellCarry.Local, "ChillTouch"),
            new PreparedEntry("Grease", 1, SpellCarry.Native, "Grease"),
            new PreparedEntry("Grease", 1, SpellCarry.Native, "Grease"),
            new PreparedEntry("Mage Armor", 1, SpellCarry.PreCast, "MageArmor"),
        };

        /// <summary>Cast before the party ever arrived (page 13 Before Combat, and the wards the earlier milestones placed).</summary>
        public static readonly string[] CastBeforeArrival = { "FalseLife", "MageArmor", "MagesFaithfulHound", "Alarm", "GlyphOfWarding" };

        public static int Count(PreparedEntry[] book, int level)
        {
            return book.Count(e => e.Level == level);
        }
    }

    /// <summary>The dark naga's spells known (Bestiary 211). It casts them spontaneously from its per-level slots.</summary>
    internal static class NagaSpells
    {
        public static readonly PreparedEntry[] Known =
        {
            new PreparedEntry("Displacement", 3, SpellCarry.Native, "Displacement"),
            new PreparedEntry("Lightning Bolt", 3, SpellCarry.Native, "LightningBolt"),
            new PreparedEntry("Cat's Grace", 2, SpellCarry.Native, "CatsGrace"),
            new PreparedEntry("Invisibility", 2, SpellCarry.Native, "Invisibility"),
            new PreparedEntry("Scorching Ray", 2, SpellCarry.Native, "ScorchingRay"),
            new PreparedEntry("Expeditious Retreat", 1, SpellCarry.Native, "ExpeditiousRetreat"),
            new PreparedEntry("Magic Missile", 1, SpellCarry.Native, "MagicMissile"),
            new PreparedEntry("Ray of Enfeeblement", 1, SpellCarry.Native, "RayOfEnfeeblement"),
            new PreparedEntry("Shield", 1, SpellCarry.Native, "MageShield"),
            new PreparedEntry("Silent Image", 1, SpellCarry.Deferred, "SilentImage"),
        };
    }

    internal enum LashmistressPolicy
    {
        /// <summary>No decision yet.</summary>
        Unset,
        /// <summary>A fresh arrival: the defenders are placed with the sanctum.</summary>
        Active,
        /// <summary>A save made inside the sanctum before this encounter existed, waiting for the load-time safety check.</summary>
        AwaitingUpgradeCheck,
        /// <summary>Such a save that passed the check: the defenders were added with the party safely outside A4.</summary>
        Upgraded,
        /// <summary>Unsafe or ambiguous: nothing is added; one notice is shown.</summary>
        LegacyNotInserted,
    }

    /// <summary>What the load-time check can see of an older save. Safety needs every fact positively, never a missing flag.</summary>
    internal struct UpgradeFacts
    {
        public bool KurshuEncounterActive;
        public bool PartyOutsideAtrium;
        public bool A4DoorEverOpened;
        public bool HoundEverNoticed;
        public bool GlyphTriggered;
        public bool ConfrontationStarted;
        public bool AwakeningDue;
        public bool EmeraldDestroyed;
    }

    /// <summary>One defender's saved identity; a reload re-binds to it and nothing is respawned.</summary>
    internal sealed class DefenderRecord
    {
        public string Key = string.Empty;
        public string EntityId = string.Empty;
        public bool Slain;
        public double SlainAt = -1d;
        /// <summary>A charm, domination, confusion or fear has the creature now: the encounter neither commands nor spends for it.</summary>
        public bool Yielded;
        public double YieldedAt = -1d;
        public string YieldReason = string.Empty;
        public int Yields;
    }

    /// <summary>One printed preparation, from its trigger to the native action that performed it.</summary>
    internal sealed class PrepRecord
    {
        public string Key = string.Empty;
        public bool Requested;
        public double RequestedAt = -1d;
        public string RequestedBy = string.Empty;
        /// <summary>A native command was given (a potion or a spell from its owner's own resources).</summary>
        public bool Issued;
        public double IssuedAt = -1d;
        public int Attempts;
        /// <summary>The effect was observed on its target, with the resource it spent.</summary>
        public bool Performed;
        public double PerformedAt = -1d;
        /// <summary>It can no longer happen (its actor is dead, helpless past the moment, or out of the resource).</summary>
        public bool Abandoned;
        public string Outcome = string.Empty;
        /// <summary>Who performed it: the director's command, the actor's own action (its brain, or whoever controlled it), or no one (already in effect).</summary>
        public string PerformedBy = string.Empty;
        /// <summary>The actor's potions or slots for it when first seen after the request, and when it was resolved; -1 unknown.</summary>
        public int ResourceAtRequest = -1;
        public int ResourceAtResolution = -1;
        /// <summary>When the resource was first seen gone without the effect; it is abandoned only if that lasts a round.</summary>
        public double ResourceGoneAt = -1d;
    }

    /// <summary>What the runtime sees of one preparation on a tick, whoever is acting.</summary>
    internal struct PrepObservation
    {
        /// <summary>The actor, and for a spell on another, its subject, are alive.</summary>
        public bool CanStillHappen;
        public string CannotReason;
        public bool EffectSeen;
        /// <summary>Potions of this kind, or slots of this level, the actor holds now; -1 unknown.</summary>
        public int ResourceLeft;
        /// <summary>The director's own command for it is still running.</summary>
        public bool DirectorCommandRunning;
    }

    internal enum PrepVerdict
    {
        None,
        Performed,
        Abandoned,
    }

    /// <summary>
    /// Whether the encounter still controls a defender. Its identity (the saved key) never changes; its allegiance and control
    /// can. A legitimate charm, domination, confusion or fear takes it, and the encounter yields until the effect ends.
    /// </summary>
    internal struct ControlFacts
    {
        public bool Alive;
        public bool OriginalFaction;
        public bool EncounterGroup;
        public bool PlayerFaction;
        public bool Confused;
        /// <summary>Frightened, cowering or panicked: the native fear behaviour moves it.</summary>
        public bool Frightened;
    }

    internal enum ControlChange
    {
        None,
        Yielded,
        Resumed,
    }

    internal static class ControlOwnership
    {
        /// <summary>Null when the encounter controls the creature; otherwise why it does not.</summary>
        public static string Yield(ControlFacts f)
        {
            if (!f.Alive)
            {
                return "dead";
            }
            if (f.PlayerFaction || !f.OriginalFaction)
            {
                return "its allegiance has changed (charm or domination)";
            }
            if (!f.EncounterGroup)
            {
                return "it has left the encounter's group";
            }
            if (f.Confused)
            {
                return "confused";
            }
            if (f.Frightened)
            {
                return "frightened";
            }
            return null;
        }

        /// <summary>Records a change of control once; a repeated tick with the same answer is no change.</summary>
        public static ControlChange Observe(DefenderRecord record, string reason, double now)
        {
            if (reason != null && reason != "dead" && !record.Yielded)
            {
                record.Yielded = true;
                record.YieldedAt = now;
                record.YieldReason = reason;
                record.Yields++;
                return ControlChange.Yielded;
            }
            if (reason == null && record.Yielded)
            {
                record.Yielded = false;
                record.YieldReason = string.Empty;
                return ControlChange.Resumed;
            }
            return ControlChange.None;
        }
    }

    /// <summary>
    /// Vandiana's darkness, as the native concealment entry it adds to: everything that would remove a native partial
    /// concealment removes it too (ignore-all, blindsight in reach, a target that cannot benefit, ignore-partial), and it is
    /// seen through by darkvision within 60 feet and by true seeing, which pierces magical darkness.
    /// </summary>
    internal struct DarknessFacts
    {
        public bool TargetInDarkness;
        public bool AttackerIgnoresAllConcealment;
        public bool AttackerBlindsightReaches;
        public bool TargetCannotBenefit;
        public bool AttackerIgnoresPartial;
        public bool AttackerDarkvisionWithin60;
        public bool AttackerTrueSeeing;
    }

    internal static class DarknessRule
    {
        /// <summary>True when, on top of a native result of no concealment, the darkness gives partial concealment.</summary>
        public static bool AddsConcealment(bool nativeIsNone, DarknessFacts f)
        {
            return nativeIsNone && f.TargetInDarkness && !f.AttackerIgnoresAllConcealment && !f.AttackerBlindsightReaches && !f.TargetCannotBenefit
                && !f.AttackerIgnoresPartial && !f.AttackerDarkvisionWithin60 && !f.AttackerTrueSeeing;
        }

        public static string Why(bool nativeIsNone, DarknessFacts f)
        {
            return !nativeIsNone ? "native concealment already applies"
                : !f.TargetInDarkness ? "target not in darkness"
                : f.AttackerIgnoresAllConcealment ? "attacker ignores all concealment"
                : f.AttackerBlindsightReaches ? "attacker's blindsight reaches"
                : f.TargetCannotBenefit ? "target cannot benefit from concealment"
                : f.AttackerIgnoresPartial ? "attacker ignores partial concealment"
                : f.AttackerDarkvisionWithin60 ? "attacker has darkvision within 60 ft"
                : f.AttackerTrueSeeing ? "attacker has true seeing"
                : "darkness: partial concealment";
        }
    }

    /// <summary>
    /// Her +1 whip (Core Rulebook): 1d3 nonlethal, no damage to a creature with an armor bonus of +1 or a natural armor bonus
    /// of +3. Nonlethal damage uses the game's own nonlethal total, which the game already counts toward unconsciousness.
    /// </summary>
    internal static class Nonlethal
    {
        public static bool WhipBlocked(int armorBonus, int naturalArmorBonus)
        {
            return armorBonus >= 1 || naturalArmorBonus >= 3;
        }

        /// <summary>
        /// Adds nonlethal damage. Past the creature's maximum hit points the rest is lethal (Core Rulebook). A creature this
        /// game would kill rather than knock out (it has no unconscious state) stops one short of the threshold instead.
        /// </summary>
        public static void Apply(int nonlethal, int amount, int maxHitPoints, int lethalDamage, bool canFallUnconscious, out int newNonlethal, out int lethalOverflow)
        {
            lethalOverflow = 0;
            int total = nonlethal + Math.Max(0, amount);
            if (!canFallUnconscious)
            {
                newNonlethal = Math.Max(nonlethal, Math.Min(total, maxHitPoints - lethalDamage - 1));
                return;
            }
            newNonlethal = Math.Min(total, maxHitPoints);
            lethalOverflow = total - newNonlethal;
        }

        /// <summary>Healing magic heals the same amount of nonlethal damage.</summary>
        public static int Heal(int nonlethal, int healed)
        {
            return Math.Max(0, nonlethal - Math.Max(0, healed));
        }

        /// <summary>One point per Hit Die per hour of rest or ordinary time.</summary>
        public static int Recover(int nonlethal, int hitDice, int hours)
        {
            return Math.Max(0, nonlethal - Math.Max(1, hitDice) * Math.Max(0, hours));
        }

        /// <summary>
        /// When the game revives the fallen after a fight, the nonlethal damage keeping a creature down is reduced so it stays
        /// up, as the game's own revival does for lethal damage.
        /// </summary>
        public static int AfterCombat(int nonlethal, int maxHitPoints, int lethalDamage)
        {
            return Math.Min(nonlethal, Math.Max(0, maxHitPoints - lethalDamage - 1));
        }
    }

    /// <summary>Sanctuary: each opponent that tries to attack her directly saves once against this casting; a failure bars it.</summary>
    internal static class SanctuaryRule
    {
        /// <summary>The verdict for one attacker: rolled once, on its first attempt, then kept for the spell's duration.</summary>
        public static bool Barred(Dictionary<string, bool> verdicts, string attackerId, Func<bool> saveSucceeds, out bool rolledNow)
        {
            rolledNow = false;
            if (!verdicts.TryGetValue(attackerId, out bool passed))
            {
                passed = saveSucceeds();
                verdicts[attackerId] = passed;
                rolledNow = true;
            }
            return !passed;
        }

        /// <summary>A barred attacker is barred only while it has not passed; one it has never tried to attack is not.</summary>
        public static bool KnownBarred(Dictionary<string, bool> verdicts, string attackerId)
        {
            return verdicts.TryGetValue(attackerId, out bool passed) && !passed;
        }
    }

    /// <summary>Her conjurer's features and bond (page 13; Core Rulebook).</summary>
    internal static class ConjurerRules
    {
        /// <summary>Summoner's charm: summoned creatures last half her wizard level longer (minimum 1 round).</summary>
        public static int SummonersCharmRounds(int wizardLevel)
        {
            return Math.Max(1, wizardLevel / 2);
        }

        /// <summary>Chill Touch: one touch per caster level, until all are used.</summary>
        public static int ChillTouchCharges(int casterLevel)
        {
            return Math.Max(1, casterLevel);
        }

        /// <summary>Disruptive: +4 to the DC of a foe's casting defensively while the Blackstrike threatens it.</summary>
        public const int DisruptiveBonus = 4;

        /// <summary>
        /// Arcane bond, once a day: she casts a spell from her book as if prepared. In this game she recovers one spent wizard
        /// slot as a free action and casts it with its own action: the highest level spent slot whose spell she can cast here,
        /// in the order her tactics value them. Slots spent before the party arrived are not eligible (their spells are cast).
        /// </summary>
        public static string ArcaneBondChoice(IEnumerable<KeyValuePair<PreparedEntry, bool>> wizardSlots)
        {
            string[] preference = { "ConfusionSpell", "BalefulPolymorph", "Haste", "Slow", "Enervation", "DimensionDoorBase", "Glitterdust", "Web", "SleetStorm", "SummonMonsterIIIBase", "Grease", "ChillTouch" };
            return wizardSlots
                .Where(s => !s.Value && (s.Key.Carry == SpellCarry.Native || s.Key.Carry == SpellCarry.Local))
                .Select(s => s.Key)
                .OrderByDescending(e => e.Level)
                .ThenBy(e => Array.IndexOf(preference, e.Source) < 0 ? int.MaxValue : Array.IndexOf(preference, e.Source))
                .Select(e => e.Source)
                .FirstOrDefault();
        }
    }

    /// <summary>The printed preparations and who performs them.</summary>
    internal static class Prep
    {
        public const string VandianaDarkvision = "VandianaDarkvision";
        public const string VandianaCatsGrace = "VandianaCatsGrace";
        public const string NagaInvisibilitySelf = "NagaInvisibilitySelf";
        public const string NagaInvisibilityAgarik = "NagaInvisibilityAgarik";
        public const string AgarikBullsStrength = "AgarikBullsStrength";
        public const string JorgasBullsStrength = "JorgasBullsStrength";
        public const string AgarikBlur = "AgarikBlur";
        public const string JorgasBlur = "JorgasBlur";

        public static readonly string[] All = { VandianaDarkvision, VandianaCatsGrace, NagaInvisibilitySelf, NagaInvisibilityAgarik, AgarikBullsStrength, JorgasBullsStrength, AgarikBlur, JorgasBlur };

        public static string Actor(string key)
        {
            switch (key)
            {
                case VandianaDarkvision:
                case VandianaCatsGrace:
                    return DefenderKeys.Vandiana;
                case NagaInvisibilitySelf:
                case NagaInvisibilityAgarik:
                    return DefenderKeys.Naga;
                case AgarikBullsStrength:
                case AgarikBlur:
                    return DefenderKeys.Agarik;
                case JorgasBullsStrength:
                case JorgasBlur:
                    return DefenderKeys.Jorgas;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key);
            }
        }
    }

    /// <summary>What committed the A4 confrontation. Scouting, inspection and opening a door are deliberately absent.</summary>
    internal enum EngagementCause
    {
        None,
        /// <summary>A defender and a party member are in combat with each other.</summary>
        CombatJoined,
        /// <summary>A hostile rule (attack, damage, hostile spell) passed between a defender and the party's side.</summary>
        HostileAct,
        /// <summary>An aggressive act against the ritual itself: striking the altar tools or damaging the emerald.</summary>
        RitualAttacked,
    }

    /// <summary>Everything A4 must remember (WR-06), saved with the sanctum's state.</summary>
    internal sealed class LashmistressState
    {
        public LashmistressPolicy Policy = LashmistressPolicy.Unset;
        public bool LegacyNoticeShown;
        public string UpgradeReason = string.Empty;
        public double SpawnedAt = -1d;
        /// <summary>The once-only initialization of her pre-cast spells and ward slots; never repeated by a load or a dispel.</summary>
        public bool BaselineApplied;
        public double BaselineAt = -1d;
        /// <summary>The support fixture that stood in for her before WR-06 is out of the game and no longer resolved.</summary>
        public bool FixtureRetired;
        public Dictionary<string, DefenderRecord> Defenders = new Dictionary<string, DefenderRecord>();
        public Dictionary<string, PrepRecord> Preparations = new Dictionary<string, PrepRecord>();
        public bool Engaged;
        public double EngagedAt = -1d;
        public EngagementCause EngagedBy = EngagementCause.None;
        public string EngagedDetail = string.Empty;
        public bool AllDefeated;
        public double AllDefeatedAt = -1d;
        public bool DefeatReported;
        public bool AwakeningNoted;
        /// <summary>The game time from which the next hour of natural recovery from her whip's nonlethal damage is counted.</summary>
        public double NonlethalHourMark = -1d;

        public DefenderRecord Defender(string key)
        {
            if (!Defenders.TryGetValue(key, out DefenderRecord record))
            {
                record = new DefenderRecord { Key = key };
                Defenders[key] = record;
            }
            return record;
        }

        public PrepRecord Preparation(string key)
        {
            if (!Preparations.TryGetValue(key, out PrepRecord record))
            {
                record = new PrepRecord { Key = key };
                Preparations[key] = record;
            }
            return record;
        }

        public bool IsPresent => Policy == LashmistressPolicy.Active || Policy == LashmistressPolicy.Upgraded;
    }

    /// <summary>The questions the four brains ask of themselves and of one target.</summary>
    public enum A4Aim
    {
        Melee,
        Haste,
        Confusion,
        Slow,
        BalefulPolymorph,
        HoldPerson,
        Enervation,
        OrdersWrath,
        UnholyBlight,
        Blindness,
        DispelMagic,
        Command,
        Glitterdust,
        Web,
        SleetStorm,
        Grease,
        SoundBurst,
        Summon,
        Darkness,
        CureSelf,
        CureAlly,
        RemoveFear,
        DivineFavor,
        ChannelNegative,
        AcidDart,
        BlastRune,
        Displacement,
        LightningBolt,
        ScorchingRay,
        MagicMissile,
        RayOfEnfeeblement,
        BlurPotion,
        StrengthPotion,
        GracePotion,
        HealingPotion,
        MagicCircle,
        Sanctuary,
        ChillTouch,
        ChillTouchStrike,
        ArcaneBond,
        Shield,
    }

    /// <summary>The caster's own situation.</summary>
    internal struct A4Self
    {
        public double HitPointFraction;
        public int VisibleEnemies;
        public int EnemiesAdjacent;
        public int LivingAlliesWithin30Feet;
        public int EnemiesWithin30Feet;
        public bool Hasted;
        public bool Displaced;
        public bool InOwnDarkness;
        public bool AlliesSeeInDarkness;
        public bool SummonedAlready;
        public bool DivineFavorActive;
        public bool Blurred;
        public bool Strengthened;
        public bool Graced;
        public bool MagicCircleActive;
        public int GoodEnemiesVisible;
        public bool SanctuaryActive;
        public bool ChillTouchCharged;
        /// <summary>A wizard slot her bond would recover is spent, and the bond's daily use remains.</summary>
        public bool ArcaneBondWorthwhile;
        public bool ShieldActive;
    }

    /// <summary>One candidate target, as the adapters read it from the field.</summary>
    internal struct A4Target
    {
        public bool IsSelf;
        public bool IsAlly;
        public float DistanceFeet;
        public bool Humanoid;
        public bool Helpless;
        public bool Slowed;
        public bool Confused;
        public bool Blinded;
        public bool Invisible;
        public bool Polymorphed;
        public bool Hasted;
        public bool Frightened;
        public double HitPointFraction;
        public int EnemiesWithin10Feet;
        public int EnemiesWithin20Feet;
        public int AlliesWithin10Feet;
        public int AlliesWithin20Feet;
        /// <summary>Order's Wrath harms chaotic (full) and neutral (half); Unholy Blight harms good (full) and neutral (half).</summary>
        public int ChaoticWithin15Feet;
        public int NeutralLawWithin15Feet;
        public int GoodWithin20Feet;
        public int NeutralMoralWithin20Feet;
        public int DispellableBuffs;
        public bool Martial;
        public int StrengthScore;
        public int AlliesNotHastedWithin15Feet;
    }

    /// <summary>
    /// Her printed tactics (page 13: out of melee, hamper and harm intruders, help allies, darkness when allies can see in it)
    /// and the others' as weights for the native brain. Areas avoid her own allies; the brain still chooses among them.
    /// </summary>
    internal static class LashmistressTactics
    {
        public static float Actor(A4Aim aim, A4Self self)
        {
            if (self.VisibleEnemies <= 0)
            {
                return 0f;
            }
            if (self.SanctuaryActive && self.HitPointFraction < 0.6d && Offensive(aim))
            {
                // Under her sanctuary she recovers first: an attack would end the ward.
                return 0f;
            }
            if (self.ChillTouchCharged && self.EnemiesAdjacent >= 1 && CastsASpell(aim) && aim != A4Aim.CureSelf)
            {
                // A held Chill Touch is spent on the foe at her side: another spell would dissipate it.
                return 0f;
            }
            switch (aim)
            {
                case A4Aim.CureSelf:
                    return self.HitPointFraction < 0.5d ? 1f : 0f;
                case A4Aim.MagicCircle:
                    return self.MagicCircleActive || self.GoodEnemiesVisible < 1 ? 0f : 1f;
                case A4Aim.Sanctuary:
                    // A last resort when wounded and pressed (her Dimension Door comes first; see the director).
                    return self.SanctuaryActive || self.HitPointFraction >= 0.35d || self.EnemiesAdjacent < 1 ? 0f : 1f;
                case A4Aim.ChillTouch:
                    // She keeps out of melee; only a foe at her side makes a touch worth a slot.
                    return self.ChillTouchCharged || self.EnemiesAdjacent < 1 ? 0f : 1f;
                case A4Aim.ChillTouchStrike:
                    return self.ChillTouchCharged && self.EnemiesAdjacent >= 1 ? 1f : 0f;
                case A4Aim.ArcaneBond:
                    return self.ArcaneBondWorthwhile ? 1f : 0f;
                case A4Aim.Shield:
                    return self.ShieldActive ? 0f : 1f;
                case A4Aim.Displacement:
                    return self.Displaced ? 0f : 1f;
                case A4Aim.Summon:
                    return self.SummonedAlready ? 0f : 1f;
                case A4Aim.Darkness:
                    return self.InOwnDarkness || !self.AlliesSeeInDarkness ? 0f : 1f;
                case A4Aim.DivineFavor:
                    return self.DivineFavorActive || self.EnemiesAdjacent == 0 ? 0f : 1f;
                case A4Aim.ChannelNegative:
                    // No Selective Channel: she never channels with a living ally in the burst.
                    return self.LivingAlliesWithin30Feet > 0 || self.EnemiesWithin30Feet < 2 ? 0f : 1f;
                case A4Aim.BlurPotion:
                    // NPC Codex 236: in combat the Blackstrike drinks its blur; a potion is drunk once, so only when not blurred.
                    return self.Blurred ? 0f : 1f;
                case A4Aim.StrengthPotion:
                    return self.Strengthened ? 0f : 1f;
                case A4Aim.GracePotion:
                    return self.Graced ? 0f : 1f;
                case A4Aim.HealingPotion:
                    return self.HitPointFraction < 0.4d ? 1f : 0f;
                default:
                    return 1f;
            }
        }

        public static float Target(A4Aim aim, A4Target t)
        {
            if (t.IsSelf)
            {
                switch (aim)
                {
                    case A4Aim.CureSelf:
                    case A4Aim.Displacement:
                    case A4Aim.Darkness:
                    case A4Aim.DivineFavor:
                    case A4Aim.ChannelNegative:
                    case A4Aim.BlurPotion:
                    case A4Aim.StrengthPotion:
                    case A4Aim.GracePotion:
                    case A4Aim.HealingPotion:
                    case A4Aim.MagicCircle:
                    case A4Aim.Sanctuary:
                    case A4Aim.ChillTouch:
                    case A4Aim.ArcaneBond:
                    case A4Aim.Shield:
                        return 1f;
                    case A4Aim.Haste:
                        return t.AlliesNotHastedWithin15Feet >= 1 ? 1f : 0f;
                    default:
                        return 0f;
                }
            }
            if (t.IsAlly)
            {
                switch (aim)
                {
                    case A4Aim.Haste:
                        return t.Hasted || t.AlliesNotHastedWithin15Feet < 1 ? 0f : Clamp(t.AlliesNotHastedWithin15Feet / 3f);
                    case A4Aim.CureAlly:
                        return t.HitPointFraction < 0.4d ? 1f : 0f;
                    case A4Aim.RemoveFear:
                        return t.Frightened ? 1f : 0f;
                    default:
                        return 0f;
                }
            }
            if (t.Helpless && aim != A4Aim.Melee)
            {
                return 0f;
            }
            switch (aim)
            {
                case A4Aim.Melee:
                    return 1f;
                case A4Aim.Confusion:
                    return t.Confused || t.EnemiesWithin10Feet < 2 || t.AlliesWithin10Feet > 0 ? 0f : Clamp(t.EnemiesWithin10Feet / 3f);
                case A4Aim.Slow:
                    return t.Slowed || t.EnemiesWithin20Feet < 2 ? 0f : Clamp(t.EnemiesWithin20Feet / 4f);
                case A4Aim.BalefulPolymorph:
                    return t.Polymorphed || !t.Martial ? 0f : 1f;
                case A4Aim.HoldPerson:
                    return !t.Humanoid ? 0f : t.Martial ? 1f : 0.7f;
                case A4Aim.Enervation:
                    return 0.8f;
                case A4Aim.OrdersWrath:
                    {
                        float hit = t.ChaoticWithin15Feet + 0.5f * t.NeutralLawWithin15Feet;
                        return t.AlliesWithin20Feet > 0 && hit < 2f ? 0f : hit < 1.5f ? 0f : Clamp(hit / 4f);
                    }
                case A4Aim.UnholyBlight:
                    {
                        float hit = t.GoodWithin20Feet + 0.5f * t.NeutralMoralWithin20Feet;
                        return hit < 1.5f ? 0f : Clamp(hit / 4f);
                    }
                case A4Aim.Blindness:
                    return t.Blinded || !t.Martial ? 0f : 0.9f;
                case A4Aim.DispelMagic:
                    return t.DispellableBuffs < 3 ? 0f : Clamp(t.DispellableBuffs / 6f);
                case A4Aim.Command:
                    return t.DistanceFeet > 30f ? 0f : 0.6f;
                case A4Aim.Glitterdust:
                    return t.Invisible ? 1f : t.Blinded || t.EnemiesWithin10Feet < 2 || t.AlliesWithin10Feet > 0 ? 0f : Clamp(t.EnemiesWithin10Feet / 4f);
                case A4Aim.Web:
                case A4Aim.SleetStorm:
                    return t.EnemiesWithin20Feet < 2 || t.AlliesWithin20Feet > 0 ? 0f : Clamp(t.EnemiesWithin20Feet / 4f);
                case A4Aim.Grease:
                    return t.EnemiesWithin10Feet < 1 || t.AlliesWithin10Feet > 0 ? 0f : 0.5f;
                case A4Aim.SoundBurst:
                    return t.EnemiesWithin10Feet < 2 || t.AlliesWithin10Feet > 0 ? 0f : Clamp(t.EnemiesWithin10Feet / 4f);
                case A4Aim.Summon:
                    return 1f;
                case A4Aim.LightningBolt:
                    return t.EnemiesWithin20Feet < 2 ? 0.5f : Clamp(t.EnemiesWithin20Feet / 3f);
                case A4Aim.ScorchingRay:
                case A4Aim.MagicMissile:
                case A4Aim.AcidDart:
                case A4Aim.BlastRune:
                    return 1f - (float)Math.Min(0.5d, t.HitPointFraction * 0.5d);
                case A4Aim.RayOfEnfeeblement:
                    return t.Martial && t.StrengthScore >= 14 ? 1f : 0f;
                case A4Aim.ChillTouchStrike:
                    return t.DistanceFeet <= 10f ? 1f - (float)Math.Min(0.5d, t.HitPointFraction * 0.5d) : 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>The aims that cast a spell from her books (anything but a weapon, a potion, her channel, her school and domain powers, her bond, or a held touch).</summary>
        public static bool CastsASpell(A4Aim aim)
        {
            switch (aim)
            {
                case A4Aim.Melee:
                case A4Aim.ChannelNegative:
                case A4Aim.AcidDart:
                case A4Aim.BlastRune:
                case A4Aim.BlurPotion:
                case A4Aim.StrengthPotion:
                case A4Aim.GracePotion:
                case A4Aim.HealingPotion:
                case A4Aim.ArcaneBond:
                case A4Aim.ChillTouchStrike:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>The aims that attack or harm a foe, which end her sanctuary.</summary>
        public static bool Offensive(A4Aim aim)
        {
            switch (aim)
            {
                case A4Aim.CureSelf:
                case A4Aim.CureAlly:
                case A4Aim.Haste:
                case A4Aim.RemoveFear:
                case A4Aim.DivineFavor:
                case A4Aim.Darkness:
                case A4Aim.Displacement:
                case A4Aim.BlurPotion:
                case A4Aim.StrengthPotion:
                case A4Aim.GracePotion:
                case A4Aim.HealingPotion:
                case A4Aim.MagicCircle:
                case A4Aim.Sanctuary:
                case A4Aim.ChillTouch:
                case A4Aim.ArcaneBond:
                case A4Aim.Shield:
                    return false;
                default:
                    return true;
            }
        }

        private static float Clamp(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }

    /// <summary>Order's Wrath and Unholy Blight: what one creature takes, by its alignment on the spell's axis (Core Rulebook).</summary>
    internal enum BlightShare
    {
        Unaffected,
        Half,
        Full,
    }

    internal static class AlignedBurst
    {
        /// <summary>1d8 per two caster levels, at most 5d8.</summary>
        public static int Dice(int casterLevel)
        {
            return Math.Max(1, Math.Min(5, casterLevel / 2));
        }

        /// <summary>
        /// The dice one creature takes: an outsider of the full-share alignment (a chaotic outsider for Order's Wrath, a good one
        /// for Unholy Blight) takes 1d6 per caster level, at most 10d6; everyone else takes 1d8 per two levels, at most 5d8.
        /// The half share is half of the ordinary roll, outsider or not.
        /// </summary>
        public static void Roll(int casterLevel, BlightShare share, bool outsider, out int count, out int sides)
        {
            if (share == BlightShare.Full && outsider)
            {
                count = Math.Max(1, Math.Min(10, casterLevel));
                sides = 6;
                return;
            }
            count = Dice(casterLevel);
            sides = 8;
        }

        /// <summary>Order's Wrath: chaotic full, neither lawful nor chaotic half, lawful unaffected.</summary>
        public static BlightShare OrdersWrath(bool lawful, bool chaotic)
        {
            return chaotic ? BlightShare.Full : lawful ? BlightShare.Unaffected : BlightShare.Half;
        }

        /// <summary>Unholy Blight: good full, neither good nor evil half, evil unaffected.</summary>
        public static BlightShare UnholyBlight(bool good, bool evil)
        {
            return good ? BlightShare.Full : evil ? BlightShare.Unaffected : BlightShare.Half;
        }

        /// <summary>The damage a rolled total does: a successful Will save halves it, and a half share halves it again.</summary>
        public static int Damage(int rolled, BlightShare share, bool saved)
        {
            if (share == BlightShare.Unaffected)
            {
                return 0;
            }
            int damage = share == BlightShare.Half ? rolled / 2 : rolled;
            return saved ? damage / 2 : damage;
        }

        /// <summary>Only a creature with the full share that fails its save suffers the rider (dazed, or sickened).</summary>
        public static bool Rider(BlightShare share, bool saved)
        {
            return share == BlightShare.Full && !saved;
        }
    }

    /// <summary>The A4 encounter's decisions, idempotent so a reload, a repeated cause or a second door cannot repeat them.</summary>
    internal static class LashmistressRules
    {
        public static void DecideOnInitialize(LashmistressState state, bool legacyEntered)
        {
            if (state.Policy != LashmistressPolicy.Unset)
            {
                return;
            }
            state.Policy = legacyEntered ? LashmistressPolicy.LegacyNotInserted : LashmistressPolicy.Active;
            if (legacyEntered)
            {
                state.UpgradeReason = "the save entered the sanctum before its wards existed";
            }
        }

        /// <summary>A sanctum state saved before WR-06 already inside: decided at the first load, from what the party can see.</summary>
        public static void MarkForUpgradeCheck(LashmistressState state)
        {
            if (state.Policy == LashmistressPolicy.Unset)
            {
                state.Policy = LashmistressPolicy.AwaitingUpgradeCheck;
            }
        }

        /// <summary>
        /// Adds the defenders to an older save only when every fact says the party has not reached A4: Kurshu's encounter is
        /// the current one, everyone stands outside A4/A5, the east door was never opened, the hound never noticed anyone,
        /// the glyph never went off, and no clock or the emerald has moved. Anything else keeps the save as it was.
        /// </summary>
        public static bool DecideUpgrade(LashmistressState state, UpgradeFacts facts)
        {
            if (state.Policy != LashmistressPolicy.AwaitingUpgradeCheck)
            {
                return state.IsPresent;
            }
            string reason = !facts.KurshuEncounterActive ? "the save predates Kurshu's encounter"
                : !facts.PartyOutsideAtrium ? "someone stands in A4 or A5"
                : facts.A4DoorEverOpened ? "the east door to A4 was already opened"
                : facts.HoundEverNoticed ? "the hound already noticed someone"
                : facts.GlyphTriggered ? "the glyph was already set off"
                : facts.ConfrontationStarted ? "the A4 countdown already began"
                : facts.AwakeningDue ? "Krune's awakening is already due"
                : facts.EmeraldDestroyed ? "the emerald is already destroyed"
                : null;
            state.Policy = reason == null ? LashmistressPolicy.Upgraded : LashmistressPolicy.LegacyNotInserted;
            state.UpgradeReason = reason ?? "the party is safely outside A4 and nothing there has happened yet";
            return reason == null;
        }

        /// <summary>Records a request once; returns true the first time.</summary>
        public static bool Request(LashmistressState state, string key, string by, double now)
        {
            PrepRecord prep = state.Preparation(key);
            if (prep.Requested)
            {
                return false;
            }
            prep.Requested = true;
            prep.RequestedAt = now;
            prep.RequestedBy = by;
            return true;
        }

        /// <summary>What awareness prepares, and what hearing the east door prepares (page 12 and 13 tactics, NPC Codex 236).</summary>
        public static IEnumerable<string> OnAwareness(LashmistressState state, bool darkvisionPrepared, double now)
        {
            // Page 13: the alarm prepares her Darkvision. A Blackstrike's three-minute potion waits for the battle itself (the
            // east door, or combat): a distant warning does not tell it when the fight will come.
            if (darkvisionPrepared && Request(state, Prep.VandianaDarkvision, "awareness", now))
            {
                yield return Prep.VandianaDarkvision;
            }
        }

        public static IEnumerable<string> OnDoorHeard(LashmistressState state, double now)
        {
            foreach (string key in new[] { Prep.VandianaCatsGrace, Prep.NagaInvisibilitySelf, Prep.NagaInvisibilityAgarik, Prep.AgarikBullsStrength, Prep.JorgasBullsStrength })
            {
                if (Request(state, key, "east door heard", now))
                {
                    yield return key;
                }
            }
        }

        /// <summary>In combat, a Blackstrike drinks Blur (and Bull's Strength, if no warning ever came).</summary>
        public static IEnumerable<string> OnCombat(LashmistressState state, double now)
        {
            foreach (string key in new[] { Prep.AgarikBullsStrength, Prep.JorgasBullsStrength, Prep.AgarikBlur, Prep.JorgasBlur })
            {
                if (Request(state, key, "combat", now))
                {
                    yield return key;
                }
            }
        }

        /// <summary>The next preparation an actor still owes, in the printed order, or null.</summary>
        public static string NextFor(LashmistressState state, string actor)
        {
            return Prep.All.FirstOrDefault(key => Prep.Actor(key) == actor && state.Preparations.TryGetValue(key, out PrepRecord p) && p.Requested && !p.Performed && !p.Abandoned);
        }

        public static void Issued(LashmistressState state, string key, double now)
        {
            PrepRecord prep = state.Preparation(key);
            prep.Issued = true;
            prep.IssuedAt = now;
            prep.Attempts++;
        }

        public static void Performed(LashmistressState state, string key, string outcome, double now)
        {
            PrepRecord prep = state.Preparation(key);
            if (prep.Performed)
            {
                return;
            }
            prep.Performed = true;
            prep.PerformedAt = now;
            prep.Outcome = outcome;
        }

        /// <summary>
        /// Settles one requested preparation from what is seen on the field, whoever acted: the director's command, the actor's
        /// own brain in combat, or someone controlling it. Its effect seen is performed (with what was spent); an actor or
        /// subject that can no longer do it, or a resource gone for a full round with no effect, ends it for good. Nothing
        /// here gates any other decision.
        /// </summary>
        public static PrepVerdict Reconcile(LashmistressState state, string key, PrepObservation seen, bool directorActed, double now, double graceSeconds)
        {
            PrepRecord prep = state.Preparation(key);
            if (!prep.Requested || prep.Performed || prep.Abandoned)
            {
                return PrepVerdict.None;
            }
            if (prep.ResourceAtRequest < 0 && seen.ResourceLeft >= 0)
            {
                prep.ResourceAtRequest = seen.ResourceLeft;
            }
            if (seen.EffectSeen)
            {
                bool spent = prep.ResourceAtRequest >= 0 && seen.ResourceLeft >= 0 && seen.ResourceLeft < prep.ResourceAtRequest;
                prep.PerformedBy = directorActed ? "the director's command" : spent ? "its own action (its brain, or whoever controlled it)"
                    : "already in effect when requested (nothing spent after the request)";
                prep.ResourceAtResolution = seen.ResourceLeft;
                Performed(state, key, "effect observed on its target", now);
                return PrepVerdict.Performed;
            }
            if (!seen.CanStillHappen)
            {
                prep.ResourceAtResolution = seen.ResourceLeft;
                Abandon(state, key, seen.CannotReason ?? "it can no longer happen");
                return PrepVerdict.Abandoned;
            }
            if (seen.ResourceLeft == 0 && !seen.DirectorCommandRunning)
            {
                if (prep.ResourceGoneAt < 0d)
                {
                    prep.ResourceGoneAt = now;
                }
                else if (now - prep.ResourceGoneAt >= graceSeconds)
                {
                    prep.ResourceAtResolution = 0;
                    Abandon(state, key, "its potion or slot is gone and the effect never appeared");
                    return PrepVerdict.Abandoned;
                }
            }
            else
            {
                prep.ResourceGoneAt = -1d;
            }
            return PrepVerdict.None;
        }

        public static void Abandon(LashmistressState state, string key, string why)
        {
            PrepRecord prep = state.Preparation(key);
            if (prep.Performed || prep.Abandoned)
            {
                return;
            }
            prep.Abandoned = true;
            prep.Outcome = why;
        }

        /// <summary>The A4 confrontation's first real engagement. Returns true once; the caller then commits the printed clock.</summary>
        public static bool Engage(LashmistressState state, EngagementCause cause, string detail, double now)
        {
            if (state.Engaged || cause == EngagementCause.None)
            {
                return false;
            }
            state.Engaged = true;
            state.EngagedAt = now;
            state.EngagedBy = cause;
            state.EngagedDetail = detail ?? string.Empty;
            return true;
        }

        /// <summary>A defender's death, once. Returns true when this was the last of the four.</summary>
        public static bool Slain(LashmistressState state, string key, double now)
        {
            DefenderRecord record = state.Defender(key);
            if (!record.Slain)
            {
                record.Slain = true;
                record.SlainAt = now;
            }
            if (state.AllDefeated || !DefenderKeys.All.All(k => state.Defenders.TryGetValue(k, out DefenderRecord r) && r.Slain))
            {
                return false;
            }
            state.AllDefeated = true;
            state.AllDefeatedAt = now;
            return true;
        }
    }
}
