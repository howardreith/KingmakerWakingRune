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
            new PreparedEntry("Magic Circle against Good", 3, SpellCarry.Deferred, "MagicCircleAgainstGood"),
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
            new PreparedEntry("Sanctuary", 1, SpellCarry.Deferred, "Sanctuary"),
        };

        public static readonly PreparedEntry[] Wizard =
        {
            new PreparedEntry("Baleful Polymorph", 5, SpellCarry.Native, "BalefulPolymorph"),
            new PreparedEntry("Mage's Faithful Hound", 5, SpellCarry.PreCast, "MagesFaithfulHound"),
            new PreparedEntry("Confusion", 4, SpellCarry.Native, "ConfusionSpell"),
            new PreparedEntry("Dimension Door", 4, SpellCarry.Native, "DimensionDoorCasterOnly"),
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
            new PreparedEntry("Chill Touch", 1, SpellCarry.Deferred, "ChillTouch"),
            new PreparedEntry("Chill Touch", 1, SpellCarry.Deferred, "ChillTouch"),
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
            new PreparedEntry("Shield", 1, SpellCarry.Deferred, "Shield"),
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
            switch (aim)
            {
                case A4Aim.CureSelf:
                    return self.HitPointFraction < 0.5d ? 1f : 0f;
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
                default:
                    return 0f;
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
            if (darkvisionPrepared && Request(state, Prep.VandianaDarkvision, "awareness", now))
            {
                yield return Prep.VandianaDarkvision;
            }
            // A Blackstrike who knows where the battle will be drinks Bull's Strength before it.
            foreach (string key in new[] { Prep.AgarikBullsStrength, Prep.JorgasBullsStrength })
            {
                if (Request(state, key, "awareness", now))
                {
                    yield return key;
                }
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
