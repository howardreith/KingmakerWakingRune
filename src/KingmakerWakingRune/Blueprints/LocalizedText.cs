using System.Collections.Generic;
using Harmony12;
using Kingmaker.Localization;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>Mod-owned display strings, injected into whichever localization pack is loaded.</summary>
    internal static class LocalizedText
    {
        public const string StoryTitle = "kwr.story.title";
        public const string StoryDescription = "kwr.story.description";
        public const string AreaName = "kwr.area.name";
        public const string SheilaName = "kwr.npc.sheila";

        private static readonly Dictionary<string, string> s_Texts = new Dictionary<string, string>
        {
            { StoryTitle, "The Waking Rune" },
            {
                StoryDescription,
                "Pathfinder Society Scenario #4–26: The Waking Rune.\n\n" +
                "Create a new character, receive your briefing in Venture-Captain Sheila Heidmarch's study at the " +
                "Magnimar Lodge, then build a level 11 party and equip it from the Society's stores. The adventure " +
                "does not leave the lodge yet."
            },
            { AreaName, "Magnimar Lodge — Sheila's Study" },
            { SheilaName, "Sheila Heidmarch" },
        };

        public static void Define(string key, string text)
        {
            s_Texts[key] = text;
        }

        public static LocalizedString Create(string key)
        {
            var text = new LocalizedString();
            AccessTools.Field(typeof(LocalizedString), "m_Key").SetValue(text, key);
            return text;
        }

        public static void InjectInto(LocalizationPack pack)
        {
            if (pack == null)
            {
                return;
            }
            foreach (var entry in s_Texts)
            {
                pack.Strings[entry.Key] = entry.Value;
            }
        }
    }
}
