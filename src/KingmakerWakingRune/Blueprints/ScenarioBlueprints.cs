using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>
    /// Builds and registers every mod-owned blueprint once, when the native blueprint library
    /// is first loaded (before any save can be resolved). Donor data is cloned, never mutated.
    /// Registration fails closed: if a donor is missing, no story entry is offered.
    /// </summary>
    internal static class ScenarioBlueprints
    {
        private static bool s_Attempted;

        public static bool IsRegistered { get; private set; }
        public static BlueprintAreaPreset Preset { get; private set; }
        public static BlueprintArea Area { get; private set; }
        public static BlueprintAreaEnterPoint EnterPoint { get; private set; }
        public static BlueprintUnit Sheila { get; private set; }
        public static BlueprintUnlockableFlag SessionFlag { get; private set; }
        public static BlueprintUnlockableFlag SetupFlag { get; private set; }
        public static NewGameRoot.StoryEntity StoryEntry { get; private set; }
        public static DlcRoot.DlcData StoryDlcData { get; private set; }

        public static void Register(LibraryScriptableObject library)
        {
            if (s_Attempted)
            {
                return;
            }
            s_Attempted = true;
            try
            {
                LocalizedText.InjectInto(LocalizationManager.CurrentPack);
                BlueprintRoot root = library.Root;
                var all = library.GetAllBlueprints();

                BlueprintAreaPart donorArea = DonorSelection.FindAreaDonor(all);
                BlueprintUnit donorUnit = DonorSelection.FindSheilaDonor(all);
                if (donorArea == null || donorUnit == null || root == null)
                {
                    Log.Error("Registration aborted; the Waking Rune entry will not be offered. " +
                              $"donorArea={Describe(donorArea)} (static scene '{ScenarioIds.DonorStaticScene}'), " +
                              $"donorUnit={Describe(donorUnit)}, blueprintRoot={(root != null)}.");
                    return;
                }

                SessionFlag = Create<BlueprintUnlockableFlag>("KWR_WakingRuneSession", ScenarioIds.SessionFlagGuid);
                SetupFlag = Create<BlueprintUnlockableFlag>("KWR_LodgeInitialSetupDone", ScenarioIds.SetupFlagGuid);
                Area = CreateArea(donorArea);
                EnterPoint = Create<BlueprintAreaEnterPoint>("KWR_MagnimarLodge_Enter", ScenarioIds.EnterPointGuid);
                EnterPoint.Area = Area;
                Sheila = CreateSheila(donorUnit, root);
                Preset = CreatePreset(root);

                foreach (BlueprintScriptableObject bp in new BlueprintScriptableObject[] { SessionFlag, SetupFlag, Area, EnterPoint, Sheila, Preset })
                {
                    library.BlueprintsByAssetId[bp.AssetGuid] = bp;
                    all.Add(bp);
                }

                StoryDlcData = new DlcRoot.DlcData
                {
                    StartGamePreset = Preset,
                    IsRequiredInSaves = false,
                    ImportIntoMainCampaign = new DlcRoot.DlcImportSettings(),
                    ImportFromMainCampaign = new DlcRoot.DlcImportSettings(),
                };
                AccessTools.Field(typeof(DlcRoot.DlcData), "m_Enabled").SetValue(StoryDlcData, (bool?)true);

                StoryEntry = new NewGameRoot.StoryEntity
                {
                    Type = ScenarioIds.StoryType,
                    Title = LocalizedText.Create(LocalizedText.StoryTitle),
                    Description = LocalizedText.Create(LocalizedText.StoryDescription),
                    ComingSoon = false,
                };
                List<NewGameRoot.StoryEntity> stories = root.NewGameSettings.StoryList;
                Log.Info("Native stories before append: " +
                         string.Join(", ", stories.Select(s => $"{s.Type}:'{s.Title}'{(s.ComingSoon ? "(soon)" : "")}").ToArray()));
                if (!stories.Any(s => s.Type == ScenarioIds.StoryType))
                {
                    stories.Add(StoryEntry);
                }

                IsRegistered = true;
                Log.Info($"Registered: preset={Preset.AssetGuid} area={Area.AssetGuid} " +
                         $"(static={Area.StaticScene.SceneName}, mechanics={Area.DynamicScene.SceneName}, " +
                         $"light=[{string.Join(",", Area.TimeOfDayScenes.Select(s => s.SceneName).ToArray())}]) " +
                         $"donorArea={Describe(donorArea)} enter={EnterPoint.AssetGuid} " +
                         $"sheila={Sheila.AssetGuid} donorUnit={Describe(donorUnit)} prefab={Sheila.Prefab?.AssetId} " +
                         $"flags={SessionFlag.AssetGuid},{SetupFlag.AssetGuid} stories={stories.Count}");
            }
            catch (Exception ex)
            {
                IsRegistered = false;
                Log.Error("Registration failed; the Waking Rune entry will not be offered.", ex);
            }
        }

        private static T Create<T>(string name, string guid) where T : BlueprintScriptableObject
        {
            var bp = ScriptableObject.CreateInstance<T>();
            bp.name = name;
            bp.AssetGuid = guid;
            return bp;
        }

        private static BlueprintArea CreateArea(BlueprintAreaPart donor)
        {
            var area = Create<BlueprintArea>("KWR_MagnimarLodge", ScenarioIds.AreaGuid);
            // Geometry, lighting, audio and camera settings come from the donor. Its mechanics scene,
            // changeable dynamics, parts and blueprint components (area scripts) are deliberately not copied.
            foreach (FieldInfo field in typeof(BlueprintAreaPart).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                field.SetValue(area, field.GetValue(donor));
            }
            area.DynamicScene = new SceneReference(ScenarioIds.MechanicsScene);
            AccessTools.Field(typeof(BlueprintAreaPart), "m_ChangeableDynamics").SetValue(area, new BlueprintAreaPart.AdditionalDynamicEntry[0]);
            AccessTools.Field(typeof(BlueprintArea), "m_Parts").SetValue(area, new List<BlueprintAreaPart>());
            area.ComponentsArray = new BlueprintComponent[0];

            if (donor is BlueprintArea donorArea)
            {
                area.CampingSettings = donorArea.CampingSettings;
                area.RandomEncounterSettings = donorArea.RandomEncounterSettings;
                area.ArtSetting = donorArea.ArtSetting;
                area.LoadingScreenSprites = donorArea.LoadingScreenSprites;
                area.MusicTheme = donorArea.MusicTheme;
                area.MusicThemeStop = donorArea.MusicThemeStop;
                area.LootSetting = donorArea.LootSetting;
            }
            area.AreaName = LocalizedText.Create(LocalizedText.AreaName);
            area.CustomUIScene = new SceneReference();
            area.HotAreas = new BlueprintArea[0];
            area.Peaceful = true;
            area.CR = 1;
            return area;
        }

        private static BlueprintUnit CreateSheila(BlueprintUnit donor, BlueprintRoot root)
        {
            // Clone only presentation data; the clone has no components, facts, dialog or companion role.
            var unit = UnityEngine.Object.Instantiate(donor);
            unit.name = "KWR_SheilaHeidmarch";
            unit.AssetGuid = ScenarioIds.SheilaUnitGuid;
            unit.ComponentsArray = new BlueprintComponent[0];
            unit.AddFacts = new BlueprintUnitFact[0];
            unit.CustomizationPreset = null;
            unit.Faction = root.SystemMechanics.FactionNeutrals;
            var name = ScriptableObject.CreateInstance<SharedStringAsset>();
            name.name = "KWR_SheilaHeidmarch_Name";
            name.String = LocalizedText.Create(LocalizedText.SheilaName);
            unit.LocalizedName = name;
            return unit;
        }

        private static BlueprintAreaPreset CreatePreset(BlueprintRoot root)
        {
            var preset = Create<BlueprintAreaPreset>("KWR_WakingRunePreset", ScenarioIds.PresetGuid);
            preset.Area = Area;
            preset.EnterPoint = EnterPoint;
            preset.PlayerCharacter = root.DefaultPlayerCharacter;
            preset.CharGen = true;
            preset.PartyXp = 0;
            preset.MakeAutosave = false;
            preset.DlcCampaign = DlcType.None;
            preset.HasKingdom = false;
            preset.RevealGlobalmap = false;
            preset.StartGameActions = new ActionList { Actions = new GameAction[0] };
            preset.DefaultMechanics = new BlueprintAreaPreset.MechanicsPartsList { ExcludedMechanics = new List<SceneReference>() };
            // Native SetupState() unlocks this save-bound marker before the area loads.
            preset.UnlockedFlags = new List<UnlockValuePair>
            {
                new UnlockValuePair { Flag = SessionFlag, Value = ScenarioIds.SessionVersion },
            };
            return preset;
        }

        private static string Describe(BlueprintScriptableObject bp)
        {
            return bp == null ? "<missing>" : $"{bp.name}/{bp.AssetGuid}";
        }
    }
}
