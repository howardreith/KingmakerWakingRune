using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
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
        public static BlueprintUnlockableFlag CastSetupFlag { get; private set; }
        public static BlueprintUnlockableFlag ProtagonistXpFlag { get; private set; }
        public static BlueprintUnlockableFlag TreasuryFlag { get; private set; }
        public static BlueprintUnlockableFlag BriefingCompleteFlag { get; private set; }
        public static BlueprintUnlockableFlag TokenGrantedFlag { get; private set; }
        public static BlueprintUnlockableFlag SanctumEnteredFlag { get; private set; }
        public static BlueprintArea Sanctum { get; private set; }
        public static BlueprintAreaEnterPoint SanctumEnter { get; private set; }
        public static BlueprintItemEquipmentUsable Token { get; private set; }
        public static NewGameRoot.StoryEntity StoryEntry { get; private set; }
        public static DlcRoot.DlcData StoryDlcData { get; private set; }

        /// <summary>Main-campaign companion setup (romance flags, camping encounters) run for every new game.</summary>
        public static ActionList RootStartGameActions { get; private set; }

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

                var pending = new List<BlueprintScriptableObject>();
                Action<BlueprintScriptableObject> register = pending.Add;

                SessionFlag = Create<BlueprintUnlockableFlag>("KWR_WakingRuneSession", ScenarioIds.SessionFlagGuid);
                SetupFlag = Create<BlueprintUnlockableFlag>("KWR_LodgeInitialSetupDone", ScenarioIds.SetupFlagGuid);
                CastSetupFlag = Flag("KWR_LodgeCastSetupDone");
                ProtagonistXpFlag = Flag("KWR_ProtagonistXpGranted");
                TreasuryFlag = Flag("KWR_TreasuryInitialized");
                BriefingCompleteFlag = Flag("KWR_BriefingComplete");
                Area = CreateArea(donorArea);
                EnterPoint = Create<BlueprintAreaEnterPoint>("KWR_MagnimarLodge_Enter", ScenarioIds.EnterPointGuid);
                EnterPoint.Area = Area;
                Sheila = CreateSheila(donorUnit, root);
                Preset = CreatePreset(root);
                RootStartGameActions = root.StartGameActions;
                foreach (BlueprintScriptableObject bp in new BlueprintScriptableObject[] { SessionFlag, SetupFlag, CastSetupFlag, ProtagonistXpFlag, TreasuryFlag, BriefingCompleteFlag, Area, EnterPoint, Sheila, Preset })
                {
                    register(bp);
                }

                var units = all.OfType<BlueprintUnit>().GroupBy(u => u.AssetGuid).ToDictionary(g => g.Key, g => g.First());
                if (!LodgeCast.Build(root, units, register))
                {
                    Log.Error("Registration aborted: a cast donor is missing; the Waking Rune entry will not be offered.");
                    return;
                }
                BlueprintPortrait sheilaPortrait = PortraitAssets.Create("SheilaHeidmarch");
                if (sheilaPortrait != null)
                {
                    register(sheilaPortrait);
                    AccessTools.Field(typeof(BlueprintUnit), "m_Portrait").SetValue(Sheila, sheilaPortrait);
                }
                BlueprintAreaPart sanctumDonor = all.OfType<BlueprintArea>().FirstOrDefault(a => a.StaticScene != null && a.StaticScene.SceneName == ScenarioIds.SanctumDonorStaticScene && a.name == "DunswardTomb");
                if (sanctumDonor == null)
                {
                    Log.Error("Registration aborted: the Dunsward Tomb donor is missing; the Waking Rune entry will not be offered.");
                    return;
                }
                TokenGrantedFlag = Flag("KWR_RefugeTokenGranted");
                SanctumEnteredFlag = Flag("KWR_SanctumEntered");
                register(TokenGrantedFlag);
                register(SanctumEnteredFlag);
                Sanctum = CreateSanctumArea(sanctumDonor);
                SanctumEnter = Create<BlueprintAreaEnterPoint>("KWR_Sanctum_Enter", ScenarioIds.Derive("enter.sanctum"));
                SanctumEnter.Area = Sanctum;
                register(Sanctum);
                register(SanctumEnter);
                Token = RefugeTokenBlueprints.Create(register);
                LodgeDialogs.Build(Sheila, register, BriefingCompleteFlag);
                QuartermasterStock.Attach(LodgeCast.Find("quartermaster").Unit, all, register);
                LocalizedText.InjectInto(LocalizationManager.CurrentPack);

                foreach (BlueprintScriptableObject bp in pending)
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

        private static BlueprintUnlockableFlag Flag(string name)
        {
            return Create<BlueprintUnlockableFlag>(name, ScenarioIds.Derive("flag." + name));
        }

        private static BlueprintArea CreateSanctumArea(BlueprintAreaPart donor)
        {
            BlueprintArea area = CreateArea(donor, "KWR_SanctumOfSloth", ScenarioIds.Derive("area.sanctum"), "kwr.area.sanctum");
            LocalizedText.Define("kwr.area.sanctum", "The Sanctum of Sloth");
            // Cyclops is the setting used by the donor's own ancient stone tomb and is matched by Favored Terrain
            // (Underground) but not by Urban, Forest, Plains, Highlands or First World.
            area.LootSetting = LootSetting.Cyclops;
            area.CameraRotation = 0f;
            area.Peaceful = false;
            area.CR = 11;
            return area;
        }

        private static BlueprintArea CreateArea(BlueprintAreaPart donor)
        {
            return CreateArea(donor, "KWR_MagnimarLodge", ScenarioIds.AreaGuid, LocalizedText.AreaName);
        }

        private static BlueprintArea CreateArea(BlueprintAreaPart donor, string name, string guid, string nameKey)
        {
            var area = Create<BlueprintArea>(name, guid);
            // Scenes, lighting and camera settings come from the donor part. Its changeable dynamics,
            // parts and blueprint components (area scripts) are deliberately not copied, and the
            // mechanics scene's entity views are suppressed for this area (see LodgeScope).
            foreach (FieldInfo field in typeof(BlueprintAreaPart).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                field.SetValue(area, field.GetValue(donor));
            }
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
            area.AreaName = LocalizedText.Create(nameKey);
            area.CustomUIScene = new SceneReference();
            area.HotAreas = new BlueprintArea[0];
            area.Peaceful = true;
            area.CR = 1;
            return area;
        }

        private static BlueprintUnit CreateSheila(BlueprintUnit donor, BlueprintRoot root)
        {
            return LodgeCast.CloneUnit(donor, root, "KWR_SheilaHeidmarch", ScenarioIds.SheilaUnitGuid, LocalizedText.SheilaName, null);
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
            // The scenario's own campaign type: saves record it as SaveInfo.CampaignDLC, so they are never
            // offered as main-campaign saves (DLC character import, achievements, DLC import suggestions).
            preset.DlcCampaign = ScenarioIds.StoryType;
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
