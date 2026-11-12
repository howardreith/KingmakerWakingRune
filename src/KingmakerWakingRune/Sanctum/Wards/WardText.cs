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

        private const string PetrifiedTruth = "These are petrified people, not carvings, their bodies built into the supports. Stone to flesh could restore them; nothing here will do it unasked.";

        public static readonly IReadOnlyList<InspectDef> All = Build();

        /// <summary>The cue for a statue slot once its body has changed; null leaves the authored stone description.</summary>
        public static string BodyLine(string slot, StatueOutcome outcome, bool corpse, bool handTaken)
        {
            if (!corpse)
            {
                return null;
            }
            if (!CredentialRules.IsMarked(slot))
            {
                return "The body of a restored cultist lies here.";
            }
            return handTaken
                ? "The body of a restored cultist lies here. The branded hand has been taken."
                : "The body of a restored cultist lies here. The seven-pointed brand is still plain on its palm.";
        }

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

        /// <summary>
        /// The deeper reading, which replaces the identification rather than following it: the ways the rune can be undone,
        /// told as discoveries. Thresholds, DCs and hardness stay in the rules and the diagnostics, never in this text.
        /// </summary>
        public static string RuneUnderstanding(RuneKind kind, bool disabled)
        {
            return RuneOpening(kind, disabled) + " " + RuneMethods(kind);
        }

        private static string RuneOpening(RuneKind kind, bool disabled)
        {
            switch (kind)
            {
                case RuneKind.Life:
                    return disabled
                        ? "This is a spent Thassilonian Rune of Life; it once channeled vitality into Krune, and its inlay is now cold and inert. Closer study shows that forces associated with death could unravel its magic."
                        : "This is an active Thassilonian Rune of Life, apparently channeling vitality into Krune. Closer study reveals that forces associated with death could unravel its magic.";
                case RuneKind.Alacrity:
                    return disabled
                        ? "This is a spent Thassilonian Rune of Alacrity; it once lent Krune unnatural speed and anticipation, and its inlay is now cold and inert. Its magic was vulnerable to effects that impede movement."
                        : "This is an active Thassilonian Rune of Alacrity, apparently lending Krune unnatural speed and anticipation. Its magic is vulnerable to effects that impede movement.";
                case RuneKind.Defense:
                    return disabled
                        ? "This is a spent Thassilonian Rune of Defense; it once shielded Krune against both physical and magical threats, and its inlay is now cold and inert. Its protections could be overwhelmed by a concentrated surge of electricity, magical force, or resonant sound delivered within a single round."
                        : "This is an active Thassilonian Rune of Defense, apparently shielding Krune against both physical and magical threats. Its protections could be overwhelmed by a concentrated surge of electricity, magical force, or resonant sound delivered within a single round.";
                case RuneKind.Armor:
                    return disabled
                        ? "This is a spent Thassilonian Rune of Armor; it once hardened Krune's flesh against injury, and its inlay is now cold and inert. Closer examination shows weaknesses in both its magical bindings and its physical construction."
                        : "This is an active Thassilonian Rune of Armor, apparently hardening Krune's flesh against injury. Closer examination reveals weaknesses in both its magical bindings and its physical construction.";
                default:
                    return disabled
                        ? "This is a spent Thassilonian Rune of Control; it once reinforced Krune's command over his servants, and its inlay is now cold and inert. Its bindings were vulnerable to powerful commands, and to being turned against the very creatures they govern."
                        : "This is an active Thassilonian Rune of Control, apparently reinforcing Krune's command over his servants. Its bindings are vulnerable to powerful commands, and to being turned against the very creatures they govern.";
            }
        }

        private static string RuneMethods(RuneKind kind)
        {
            switch (kind)
            {
                case RuneKind.Life:
                    return "\n\nA concentrated burst of negative energy delivered within a single round could overwhelm it. So could inflicting multiple negative levels, or rapidly damaging a single ability through poison or disease. A death spell of fourth level or higher would strike directly at the enchantment sustaining it.";
                case RuneKind.Alacrity:
                    return "\n\nMagical or alchemical bindings maintained without interruption could bring it to a halt. Sustained staggering or paralysis could achieve the same result more quickly, while an overwhelming burst of cold delivered within a single round could shatter its enchantment. Despite these vulnerabilities, the rune is not a person: magic that binds only humanoids will not hold it.";
                case RuneKind.Defense:
                    return "\n\nAn exceptionally powerful bludgeoning strike could also break its defenses, but the force must come in a single blow. Alternatively, a sufficiently effective dispelling could unravel the powerful enchantment bound into the stone.";
                case RuneKind.Armor:
                    return "\n\nAn overwhelming amount of acid or fire delivered within a single round could destroy it. So could a heavy piercing or slashing blow, provided the damage is delivered in a single strike. Someone with exceptional skill in Trickery might instead sabotage the plate's intricate workings.";
                default:
                    return "\n\nA compulsion spell of fourth level or higher could overturn its authority. A sustained assault by summoned or called creatures, concentrated within a minute, could also tear the enchantment apart. Someone exceptionally skilled in Use Magic Device might instead impose their own command upon the rune.";
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
                string observe = bearer ? "An exceptionally detailed stone figure bearing the ziggurat. " + BearerLines[slot.Id] : "An extraplanar figure, strained and cast in stone, its face set in anger, agony or despair.";
                if (CredentialRules.IsMarked(slot.Id))
                {
                    observe += "\n\nA brand is cut into the palm of one raised hand: a seven-pointed star, crisp and deliberate, every line preserved in the stone. Close enough to touch, it could be copied.";
                }
                defs.Add(new InspectDef
                {
                    Id = slot.Id,
                    Observe = observe,
                    Checks =
                    {
                        bearer
                            ? new InspectCheck { Key = slot.Id, Skills = new[] { WardSkill.Arcana }, DC = 20, Label = "Study the figure", Result = CredentialRules.IsMarked(slot.Id) ? PetrifiedTruth + " The star on the palm is a true Sihedron, a mark of standing among the runelord's servants, burned in before the stone took him." : PetrifiedTruth }
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
                    new InspectCheck { Key = "Door.Inscription", Skills = new[] { WardSkill.Perception }, DC = WardProfile.DoorClueDC, Label = "Examine the inscription closely", Result = "inscription" },
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
