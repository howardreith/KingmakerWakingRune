using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>Resolves installed donor blueprints by verified identity; never mutates them.</summary>
    internal static class DonorSelection
    {
        /// <summary>
        /// Human female NPC whose model represents Sheila: Natala Surtova's (CityGatesHub_NatalaSurtova), an upright adult
        /// woman with dark hair in a long green gown, chosen against the approved portrait in a side-by-side lineup in the
        /// lodge (WR-PLAYTEST-02, docs/WR-PLAYTEST-02.md). Until 0.10.14 the bootstrap's first match by name, AdventurerRanger,
        /// a hooded, crouching ranger, stood in.
        /// </summary>
        public const string SheilaDonorGuid = "b12f2eb80731e2743a122ffc796a9a40";

        public static BlueprintAreaPart FindAreaDonor(List<BlueprintScriptableObject> all)
        {
            List<BlueprintAreaPart> matches = all.OfType<BlueprintAreaPart>()
                .Where(p => p.StaticScene != null && p.StaticScene.SceneName == ScenarioIds.DonorStaticScene)
                .ToList();
            foreach (BlueprintAreaPart match in matches)
            {
                Log.Info($"Donor area candidate: {match.GetType().Name} {match.name}/{match.AssetGuid} " +
                         $"mechanics={match.DynamicScene?.SceneName} light=[{string.Join(",", match.TimeOfDayScenes.Select(s => s.SceneName).ToArray())}]");
            }
            return matches.OfType<BlueprintArea>().FirstOrDefault() ?? matches.FirstOrDefault();
        }

        public static BlueprintUnit FindSheilaDonor(List<BlueprintScriptableObject> all)
        {
            if (!string.IsNullOrEmpty(SheilaDonorGuid))
            {
                var unit = all.OfType<BlueprintUnit>().FirstOrDefault(u => u.AssetGuid == SheilaDonorGuid);
                if (unit == null || !IsUsableHumanFemale(unit))
                {
                    Log.Error($"Sheila donor {SheilaDonorGuid} is missing or no longer a fixed-model human female.");
                    return null;
                }
                return unit;
            }
            // Bootstrap only: before a donor is verified, use the first eligible unit by name.
            return HumanFemaleCandidates(all).FirstOrDefault();
        }

        public static IEnumerable<BlueprintUnit> HumanFemaleCandidates(List<BlueprintScriptableObject> all)
        {
            return all.OfType<BlueprintUnit>().Where(IsUsableHumanFemale).OrderBy(u => u.name);
        }

        private static bool IsUsableHumanFemale(BlueprintUnit unit)
        {
            return unit.Gender == Gender.Female
                   && unit.Race != null && unit.Race.RaceId == Race.Human
                   && unit.CustomizationPreset == null
                   && unit.Prefab != null && !string.IsNullOrEmpty(unit.Prefab.AssetId);
        }
    }
}
