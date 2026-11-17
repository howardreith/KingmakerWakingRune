using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Weapons;
using Newtonsoft.Json.Linq;
using UnityModManagerNet;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>One retail line from the optional provider, by its published GUID, and the provider module it belongs to.</summary>
    internal struct ProviderLine
    {
        public readonly string Guid;
        public readonly int Count;
        public readonly string Module;
        public readonly string Label;

        public ProviderLine(string module, string guid, int count, string label)
        {
            Module = module;
            Guid = guid;
            Count = count;
            Label = label;
        }
    }

    /// <summary>
    /// Optional retail stock from Mr. Bard's Tabletop Rules Expansion (UMM id KingmakerGunslinger), checked against its
    /// v0.0.188 catalogue (blueprints.json), and again against v0.0.191 (WR-PLAYTEST-02: every line active). There is no assembly reference: the provider is detected through UMM (installed
    /// and enabled), its own FeatureModules.json (a module switched off contributes nothing), and each GUID resolving in the
    /// library. Its items are referenced, never copied, so its reload, ammunition, misfire and Reliable rules apply unchanged;
    /// its own merchants are not touched. Named and campaign-loot weapons are deliberately absent. Tiers follow this shop's
    /// policy (to +3); Reliable is the provider's own enchantment on its own Reliable items. Weapons are magical only: a weapon
    /// line without an enhancement bonus is refused at assembly, and the plain, masterwork and cold iron lines are retired
    /// (<see cref="Retired"/>). Supplies (powder, ball, cartridges, the kit) stay.
    /// </summary>
    internal static class ProviderStock
    {
        public const string ModId = "KingmakerGunslinger";
        public const string DisplayName = "Tabletop Rules Expansion";

        public static readonly ProviderLine[] Lines =
        {
            // Firearms (module "gunslinger"): one-handed Pistol, two-handed Musket and Blunderbuss, magical only (WR-PLAYTEST-02).
            new ProviderLine("gunslinger", "d0145d0410a34df08d68a67367c1dfc9", 12, "Pistol +1"),
            new ProviderLine("gunslinger", "b767c7389ba64eb19008fa9d66712985", 12, "Pistol +2"),
            new ProviderLine("gunslinger", "e52bc130e64a4000875eaa0f903b5ffe", 12, "Pistol +3"),
            new ProviderLine("gunslinger", "03e9ba98b2954416b93f73b7045daed9", 12, "Reliable Pistol +1"),
            new ProviderLine("gunslinger", "04b5b1bb6e3f47bd8b7b7427ba913a7b", 12, "Reliable Pistol +2"),
            new ProviderLine("gunslinger", "aff91d56deba4542b28ac1a57741a1af", 12, "Reliable Pistol +3"),
            new ProviderLine("gunslinger", "3402fe01de1648b187c192500e370f01", 12, "Musket +1"),
            new ProviderLine("gunslinger", "839dd0d49e7c4fbc92764475e31ee175", 12, "Musket +2"),
            new ProviderLine("gunslinger", "6980d1bfc1d94cd19ea033ad31335c79", 12, "Musket +3"),
            new ProviderLine("gunslinger", "ffd5c4578cce403ebb42efb161a30f43", 12, "Reliable Musket +1"),
            new ProviderLine("gunslinger", "ae4b32641ad847bfb4dc4e257dbd6415", 12, "Reliable Musket +2"),
            new ProviderLine("gunslinger", "e9fe300679ec44dfacf6be958ebf7b19", 12, "Reliable Musket +3"),
            new ProviderLine("gunslinger", "1dc7efe0792040f187a18adfdc54c6e0", 12, "Blunderbuss +1"),
            new ProviderLine("gunslinger", "ec12481bf56a4fa4addeefafdcfb0be7", 12, "Blunderbuss +2"),
            new ProviderLine("gunslinger", "1324e5cb0c374dd6b998546e83509d20", 12, "Blunderbuss +3"),
            new ProviderLine("gunslinger", "8ca37dd6f1c741bdbe7f18570037699c", 12, "Reliable Blunderbuss +1"),
            new ProviderLine("gunslinger", "3d45b093ac204b4ea859c76be35ecf0d", 12, "Reliable Blunderbuss +2"),
            new ProviderLine("gunslinger", "237cdb2b9292418387674526e5aea35d", 12, "Reliable Blunderbuss +3"),
            // Loading supplies: each reload takes a powder charge and a ball, or one paper cartridge when that toggle is on.
            new ProviderLine("gunslinger", "ea966bf998a647cf97b0ed92f71c4b7d", 999, "Black Powder Charge"),
            new ProviderLine("gunslinger", "55c29771445947d685dba9e1ead46a42", 999, "Lead Ball"),
            new ProviderLine("gunslinger", "fea7337cfd06417a853546af9d950f77", 999, "Paper Cartridge"),
            new ProviderLine("gunslinger", "d52aacb753434691b1ed85a16cc87104", 12, "Gunsmith's Kit"),
            // Eastern weapons (module "eastern-weapons"), magical only.
            new ProviderLine("eastern-weapons", "83a507873a518b54793d0da632def246", 12, "Wakizashi +1"),
            new ProviderLine("eastern-weapons", "744ffd16fc1b4bd98bd06eb419c0ec7d", 12, "Wakizashi +2"),
            new ProviderLine("eastern-weapons", "331b4668855841da933887e29adf69d9", 12, "Wakizashi +3"),
            new ProviderLine("eastern-weapons", "87b3d851726a4a9abd0baec6beca957c", 12, "Katana +1"),
            new ProviderLine("eastern-weapons", "8a6b48a90a4743c9a74cd65b6d063094", 12, "Katana +2"),
            new ProviderLine("eastern-weapons", "cc429a9210234467acfeeb16d6859193", 12, "Katana +3"),
            new ProviderLine("eastern-weapons", "38e31ba5cdbdc668f8dcd8985070c0b7", 12, "Nodachi +1"),
            new ProviderLine("eastern-weapons", "be8ad97d3b5f479788ddb73988b4fda8", 12, "Nodachi +2"),
            new ProviderLine("eastern-weapons", "f25d6a89ba364cb0874bb22e559e06bf", 12, "Nodachi +3"),
            // Elven branched spears (module "elven-branched-spears"), magical only; the enchanted cold iron +1 stays.
            new ProviderLine("elven-branched-spears", "66111becd22690a2a19444a5c6bd0c7b", 12, "Elven Branched Spear +1"),
            new ProviderLine("elven-branched-spears", "25d8f6c6f4767b3168f4700a2890954f", 12, "Cold Iron Elven Branched Spear +1"),
            new ProviderLine("elven-branched-spears", "59c1cc59146a42b0bcecd080bf418f2b", 12, "Elven Branched Spear +2"),
            new ProviderLine("elven-branched-spears", "5ee1cb27471943cea19b3e76b3e06e46", 12, "Elven Branched Spear +3"),
        };

        /// <summary>
        /// Lines stocked until 0.10.14 and retired in WR-PLAYTEST-02 (owner's request): the plain, masterwork and cold iron
        /// weapons, which have no enhancement bonus. A shop first opened from now on never has them; a shop saved with them
        /// keeps every copy on its shelf, since the save cannot tell retail from sold-back copies, and never restocks them
        /// (<see cref="QuartermasterStock.PreserveRetiredLines"/>). Never deleted from the library, so old saves and owned
        /// weapons still resolve.
        /// </summary>
        public static readonly string[] Retired =
        {
            "a303d71d244640959827e9464df5a867", // Pistol
            "6c9cdfa2d47e4894847fa85d5319fbd2", // Musket
            "236f3e167f5542bcac22bca72046fb1f", // Blunderbuss
            "b61ee7e62bc9288004eb0121c8f5d37e", // Wakizashi
            "58fd0f272f4523458016dc3656b778c3", // Masterwork Wakizashi
            "1aa01a0528eb595b5cbf19ac7c71a64e", // Cold Iron Wakizashi
            "aba40a9e8302b31e4daa2acf6ab48a46", // Katana
            "37e8c76b2fc196e9f82e1196e918263c", // Masterwork Katana
            "599f8f45f325911ffcbdbd6544ba114f", // Cold Iron Katana
            "35b7082d98ff45ba51dce536a1bc68a1", // Nodachi
            "df5a3b333eab59c04028d88084d7ada9", // Masterwork Nodachi
            "0db026048052031be931b9701b3859ef", // Cold Iron Nodachi
            "6edc216d68810960f85417237748b042", // Elven Branched Spear
            "9c9edabf91f2117fd1b642c4d39b9574", // Masterwork Elven Branched Spear
            "8c0de00a236fe0f532d31711dcaa00a2", // Cold Iron Elven Branched Spear
            "b16c34215cae9d60345042157149a4c0", // Masterwork Cold Iron Elven Branched Spear
        };

        private static readonly HashSet<string> s_Guids = new HashSet<string>(Lines.Select(l => l.Guid));

        /// <summary>Provider-owned scrolls the Wizard audit may meet (Teleport and the Magic Circle family).</summary>
        private static readonly HashSet<string> s_Scrolls = new HashSet<string>
        {
            "2c283c993b233df53b487fc7eeac2ba3",
        };

        public static bool Owns(string guid)
        {
            return s_Guids.Contains(guid) || s_Scrolls.Contains(guid);
        }

        /// <summary>What this run found: version, which modules were on, and why anything was left out.</summary>
        public static string Status { get; private set; } = "not checked";

        /// <summary>The provider's items to stock now, or none when it is absent, disabled, or its content is unavailable.</summary>
        public static List<KeyValuePair<BlueprintItem, int>> Resolve()
        {
            var stock = new List<KeyValuePair<BlueprintItem, int>>();
            UnityModManager.ModEntry mod = UnityModManager.FindMod(ModId);
            if (mod == null)
            {
                Status = "not installed";
                return stock;
            }
            if (!mod.Enabled || !mod.Active)
            {
                Status = $"{mod.Info.Version} installed but not enabled";
                return stock;
            }
            Dictionary<string, bool> modules = ReadModules(mod.Path);
            var missing = new List<string>();
            var nonmagical = new List<string>();
            foreach (ProviderLine line in Lines)
            {
                if (!modules.TryGetValue(line.Module, out bool on) || !on)
                {
                    continue;
                }
                var item = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(line.Guid);
                if (item == null)
                {
                    missing.Add(line.Label);
                    continue;
                }
                if (item is BlueprintItemWeapon weapon && !weapon.IsMagic)
                {
                    // The game's own test (an enchantment with a weapon enhancement bonus); material or masterwork alone is not magic.
                    nonmagical.Add(line.Label);
                    continue;
                }
                stock.Add(new KeyValuePair<BlueprintItem, int>(item, line.Count));
            }
            string off = string.Join(", ", modules.Where(m => !m.Value).Select(m => m.Key).ToArray());
            Status = $"{mod.Info.Version} enabled; {stock.Count} lines" + (off.Length > 0 ? $"; modules off: {off}" : "") +
                     (missing.Count > 0 ? $"; not registered: {string.Join(", ", missing.ToArray())}" : "") +
                     (nonmagical.Count > 0 ? $"; left out as not magical: {string.Join(", ", nonmagical.ToArray())}" : "");
            return stock;
        }

        /// <summary>The provider's own rule: a module key that is absent counts as on; an unreadable file leaves everything off.</summary>
        private static Dictionary<string, bool> ReadModules(string modPath)
        {
            var modules = Lines.Select(l => l.Module).Distinct().ToDictionary(m => m, m => true);
            string path = Path.Combine(modPath, "FeatureModules.json");
            if (!File.Exists(path))
            {
                return modules;
            }
            try
            {
                JObject root = JObject.Parse(File.ReadAllText(path));
                foreach (string key in modules.Keys.ToList())
                {
                    JToken token = root[key];
                    if (token != null)
                    {
                        modules[key] = token.Type == JTokenType.Boolean && token.Value<bool>();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"The provider's {path} could not be read; its stock is left out.", ex);
                return modules.ToDictionary(m => m.Key, m => false);
            }
            return modules;
        }
    }
}
