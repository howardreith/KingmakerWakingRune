using System.Collections.Generic;
using System.Linq;

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal enum WardSkill
    {
        Arcana,
        Religion,
        Nature,
        Perception,
    }

    /// <summary>One study a character can attempt at a fixture, and what a passing result reveals.</summary>
    internal sealed class InspectCheck
    {
        public string Key;
        public WardSkill[] Skills;
        public int DC;
        public string Label;
        public string Result;
    }

    internal sealed class InspectDef
    {
        public string Id;
        public string Observe;
        public List<InspectCheck> Checks = new List<InspectCheck>();
    }

    /// <summary>
    /// The readable investigation content: original wording faithful to the scenario. An authored DC is marked as such in
    /// docs/WR-04-RULES.md; these strings never tell a player what a failed study would have said.
    /// </summary>
    internal static class WardText
    {
        public const string DoorId = "DoorA2";
        public const string GemId = "Gem";
        public const string ApexId = "Apex";
        public const string ToolsId = "Tools";
        public const string LightId = "Light";
        public const string PedestalId = "Pedestal";
        public const string SarcophagusId = "Sarcophagus";
        public const string MistId = "Mist";
        public const string FlailId = "A3d-Flail";

        private static readonly Dictionary<string, string> BearerLines = new Dictionary<string, string>
        {
            { "Support-N1", "A man in travel-worn leathers, arms locked overhead, his head bowed under the weight." },
            { "Support-N2", "A citizen with heavy shoulders, bent as though the stone above were a load he carried willingly." },
            { "Support-N3", "A prosperous merchant, broad hands spread flat against the course above him." },
            { "Support-N4", "A wasted old man, knees trembling in stone that has held the same strain for a century." },
            { "Support-T1", "A noblewoman on the second tier, one arm raised, the other clenched at her side." },
            { "Support-T2", "A hooded figure in cultist's robes, hunched beneath the upper tier." },
            { "Support-W1", "A labourer, arms wide, each palm pressed to the ziggurat's western course." },
            { "Support-E1", "A townswoman, chin lifted, arms high, holding up the eastern course." },
            { "Support-W2", "A priest, spine curved, holding the weight across her back and shoulders." },
            { "Support-E2", "A grieving woman, strain cut deep in her face, knees bent beneath the stone." },
        };

        public static readonly IReadOnlyList<InspectDef> All = Build();

        public static InspectDef Find(string id)
        {
            return All.FirstOrDefault(d => d.Id == id);
        }

        public static string RuneIdentity(RuneFocus focus, RuneFocusState state)
        {
            string status = state.Disabled ? "It is spent; the great inlay is cold and inert." : "It is active.";
            return $"This is a Thassilonian rune of {focus.Kind.ToString().ToLowerInvariant()}. {status} {RuneHint(focus.Kind)}";
        }

        public static string RuneHint(RuneKind kind)
        {
            switch (kind)
            {
                case RuneKind.Life:
                    return "Subtle forces of death could disrupt and destroy it.";
                case RuneKind.Alacrity:
                    return "Whatever would impede a creature's speed and mobility could disrupt and destroy it.";
                case RuneKind.Defense:
                    return "Effects that batter and shock could disrupt and destroy it.";
                case RuneKind.Armor:
                    return "Effects that scorch, tear or sabotage could disrupt and destroy it.";
                default:
                    return "It is vulnerable to servant creatures and to commanding magic.";
            }
        }

        public static string RuneRoutes(RuneKind kind)
        {
            const string pipeline = "\nAfter hardness 8 per hit, the listed energies apply at full value. Physical thresholds count one hit only; energy thresholds gather across the party for one round.";
            switch (kind)
            {
                case RuneKind.Life:
                    return "It fails to 27 negative-energy damage in one round; to two or more negative levels; to 3 points of damage to a single ability score by poison or disease in one round; or to a death spell of 4th level or higher." + pipeline;
                case RuneKind.Alacrity:
                    return "It fails to magical or alchemical entanglement held 10 rounds; to being staggered or paralyzed for 5 rounds (it does not count as a humanoid); or to 35 cold damage in one round." + pipeline;
                case RuneKind.Defense:
                    return "It fails to 35 electricity, force or sonic damage in one round; to 35 bludgeoning damage from a single attack; or to a dispel magic against caster level 14." + pipeline;
                case RuneKind.Armor:
                    return "It fails to 35 acid or fire damage in one round; to a DC 35 Trickery check; or to 35 piercing or slashing damage from a single attack." + pipeline;
                default:
                    return "It fails to a compulsion spell of 4th level or higher; to 50 damage dealt by summoned or called creatures within one minute; or to a DC 35 Use Magic Device check." + pipeline;
            }
        }

        private static List<InspectDef> Build()
        {
            var defs = new List<InspectDef>();
            foreach (RuneFocus focus in KruneProjection.Foci)
            {
                defs.Add(new InspectDef
                {
                    Id = focus.Id,
                    Observe = "A great stone plate, inlaid with blue-green metal, carries one immense rune. Three figures cast in stone frame it, each twisted in anger, agony or despair.",
                    Checks =
                    {
                        new InspectCheck { Key = focus.Id, Skills = new[] { WardSkill.Arcana, WardSkill.Religion }, DC = WardProfile.RuneHintDC, Label = "Study the rune", Result = "rune" },
                    },
                });
            }
            foreach (StatueSlot slot in StatueManifest.Slots)
            {
                bool bearer = BearerManifest.IsBearer(slot.Id);
                defs.Add(new InspectDef
                {
                    Id = slot.Id,
                    Observe = bearer ? "An exceptionally detailed stone figure bearing the ziggurat. " + BearerLines[slot.Id] : "An extraplanar figure, strained and cast in stone, its face set in anger, agony or despair.",
                    Checks =
                    {
                        bearer
                            ? new InspectCheck { Key = slot.Id, Skills = new[] { WardSkill.Arcana }, DC = 20, Label = "Study the figure", Result = "These are petrified people, not carvings, their bodies built into the supports. Stone to flesh could restore them; nothing here will do it unasked." }
                            : new InspectCheck { Key = slot.Id, Skills = new[] { WardSkill.Arcana, WardSkill.Religion }, DC = WardProfile.RuneHintDC, Label = "Study the figure", Result = "Its essence was spent maintaining the rune. Unlike the figures in the outer chamber, this being is long dead." },
                    },
                });
            }
            defs.Add(new InspectDef
            {
                Id = FlailId,
                Observe = "A chain of dull metal is wound about the neck of the bound figure like a noose.",
                Checks = { new InspectCheck { Key = FlailId, Skills = new[] { WardSkill.Perception }, DC = 15, Label = "Look closely at the chain", Result = "It is a flail of true metal, untouched by the stone around it: the bound deva's own weapon." } },
            });
            defs.Add(new InspectDef
            {
                Id = DoorId,
                Observe = "A large stone door carved with a seven-pointed star. A palm-sized blue-green disc is inlaid at each point, with a handprint set at its centre. A band of runes reads: \"Only those who bear the life sign shall pass.\"\nNo hinge or keyhole shows.",
                Checks =
                {
                    new InspectCheck { Key = "Door.Metal", Skills = new[] { WardSkill.Arcana, WardSkill.Nature }, DC = 18, Label = "Study the disks of blue-green metal", Result = "The metal is abyssium, a skymetal tied to sloth and conjuration. It stores great energy, and prolonged exposure sickens those who carry it." },
                    new InspectCheck { Key = "Door.Inscription", Skills = new[] { WardSkill.Perception }, DC = WardProfile.DoorClueDC, Label = "Examine the inscription closely", Result = "One word has been recut, crudely and deeper than its neighbours. It once read: \"Only those who bear my sign shall pass.\"" },
                },
            });
            defs.Add(new InspectDef
            {
                Id = ApexId,
                Observe = "A small mark, carved flush into the apex stone.",
                Checks = { new InspectCheck { Key = ApexId, Skills = new[] { WardSkill.Arcana }, DC = 15, Label = "Study the mark", Result = "Only an ornament: a stylish figure with no meaning in Thassilonian writing. You will know the motif again if you meet it." } },
            });
            defs.Add(new InspectDef
            {
                Id = GemId,
                Observe = "A broad stone altar dedicated to Lissala, with a green gem set into its surface.",
                Checks =
                {
                    new InspectCheck { Key = "Gem.Sight", Skills = new[] { WardSkill.Perception }, DC = 15, Label = "Look closely at the gem", Result = "A flawless emerald, set into the altar by hand." },
                    new InspectCheck { Key = "Gem.Arcana", Skills = new[] { WardSkill.Arcana }, DC = 28, Label = "Study the gem's enchantment", Result = "This is Krune's modified trap the soul focus: his soul is held here, separate from his body." },
                },
            });
            defs.Add(new InspectDef
            {
                Id = ToolsId,
                Observe = "A book of prayers, incense, reagents and the rest of a Lissalan rite lie ordered on the altar.",
                Checks = { new InspectCheck { Key = ToolsId, Skills = new[] { WardSkill.Religion }, DC = 25, Label = "Study the ritual tools", Result = "Together these preserve and then revive a body: the rite on this altar is the one calling Krune back." } },
            });
            defs.Add(new InspectDef
            {
                Id = LightId,
                Observe = "Sickly greenish-purple motes play across the walls, brightening from the room to the west.",
                Checks = { new InspectCheck { Key = LightId, Skills = new[] { WardSkill.Arcana }, DC = 20, Label = "Assess the magic building here", Result = "ritual" } },
            });
            defs.Add(new InspectDef
            {
                Id = PedestalId,
                Observe = "A waist-high stone pedestal holds a shallow bowl of perhaps a dozen crystal shards, each inscribed. Beside it, an iron tile mosaic frames a door-sized recess.",
                Checks = { new InspectCheck { Key = PedestalId, Skills = new[] { WardSkill.Arcana }, DC = 20, Label = "Correlate the shards", Result = "motif" } },
            });
            defs.Add(new InspectDef
            {
                Id = SarcophagusId,
                Observe = "A heavy stone sarcophagus hangs from four chains anchored in the walls. Cracks run through it, seeping bruise-coloured vapor, and brief flashes of energy crawl across its lid.",
            });
            defs.Add(new InspectDef
            {
                Id = MistId,
                Observe = "A dense vapor pools across the floor of the chamber, heavy and strangely still.",
                Checks = { new InspectCheck { Key = MistId, Skills = new[] { WardSkill.Arcana }, DC = 27, Label = "Study the vapor (after it has touched you)", Result = "A stasis vapor: it was brewed to preserve what lies within it, and it will hold anything in it that does not resist, in sleep at the last." } },
            });
            return defs;
        }
    }
}
