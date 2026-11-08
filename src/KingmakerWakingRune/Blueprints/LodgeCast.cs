using System.Collections.Generic;
using System.Linq;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root;
using Kingmaker.Localization;
using KingmakerWakingRune.Lodge;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    internal enum CastRole
    {
        Leader,
        Service,
        Agent,
    }

    /// <summary>One authored NPC: identity, visual donor, portrait and place in the room.</summary>
    internal sealed class CastMember
    {
        public string Id;
        public string NameKey;
        public string NameText;
        public string DonorGuid;
        public string PortraitId;
        public CastRole Role;
        public Vector3 Position;
        public float Yaw;
        public BlueprintUnit Unit;
    }

    /// <summary>
    /// The eleven scenario-authored NPCs. Each is a presentation-only clone of a verified base-game
    /// unit (see docs/RECON.md); donors are never mutated. Sheila is registered by ScenarioBlueprints.
    /// </summary>
    internal static class LodgeCast
    {
        public static readonly List<CastMember> Members = new List<CastMember>
        {
            new CastMember { Id = "ambrus", NameKey = "kwr.npc.ambrus", NameText = "Ambrus Valsin", DonorGuid = "bfae523688f555541b5318f8c709f452", PortraitId = "AmbrusValsin", Role = CastRole.Leader, Position = LodgeLayout.Ambrus, Yaw = LodgeLayout.LeaderYaw },
            new CastMember { Id = "masked", NameKey = "kwr.npc.masked", NameText = "Masked Figure", DonorGuid = "4a91f844d4c422f46b9aa17f3c5550c5", PortraitId = "DecemvirateMember", Role = CastRole.Leader, Position = LodgeLayout.Masked, Yaw = LodgeLayout.LeaderYaw },
            new CastMember { Id = "quartermaster", NameKey = "kwr.npc.quartermaster", NameText = "Quartermaster", DonorGuid = "b4864c5800dd7664d889f934195586ac", PortraitId = "Quartermaster", Role = CastRole.Service, Position = LodgeLayout.Quartermaster, Yaw = LodgeLayout.ServiceYaw },
            new CastMember { Id = "coordinator", NameKey = "kwr.npc.coordinator", NameText = "Society Coordinator", DonorGuid = "59e29674e188f7b4ca06a020e500fc58", PortraitId = "SocietyCoordinator", Role = CastRole.Service, Position = LodgeLayout.Coordinator, Yaw = LodgeLayout.ServiceYaw },
            Agent(1, "a4649b63dff330b49afffad1ec3d7052", LodgeLayout.NorthAgents[0], LodgeLayout.NorthAgentYaw),
            Agent(2, "685baae218ee68e41890342f197a9156", LodgeLayout.NorthAgents[1], LodgeLayout.NorthAgentYaw),
            Agent(3, "62fb33b0803bf094ebbf9c21719477ce", LodgeLayout.NorthAgents[2], LodgeLayout.NorthAgentYaw),
            Agent(4, "e82c477c32b15854d83e548c739abbc1", LodgeLayout.SouthAgents[0], LodgeLayout.SouthAgentYaw),
            Agent(5, "685baae218ee68e41890342f197a9156", LodgeLayout.SouthAgents[1], LodgeLayout.SouthAgentYaw),
            Agent(6, "a4649b63dff330b49afffad1ec3d7052", LodgeLayout.SouthAgents[2], LodgeLayout.SouthAgentYaw),
        };

        private static CastMember Agent(int index, string donorGuid, Vector3 position, float yaw)
        {
            return new CastMember { Id = "agent" + index, NameKey = "kwr.npc.agent", NameText = "Pathfinder Agent", DonorGuid = donorGuid, Role = CastRole.Agent, Position = position, Yaw = yaw };
        }

        public static CastMember Find(string id)
        {
            return Members.First(m => m.Id == id);
        }

        public static IEnumerable<CastMember> Agents => Members.Where(m => m.Role == CastRole.Agent);

        /// <summary>Clones every donor. Returns false (nothing usable) if any verified donor is missing.</summary>
        public static bool Build(BlueprintRoot root, Dictionary<string, BlueprintUnit> units, System.Action<BlueprintScriptableObject> register)
        {
            var portraits = new Dictionary<string, BlueprintPortrait>();
            foreach (CastMember member in Members)
            {
                if (!units.TryGetValue(member.DonorGuid, out BlueprintUnit donor))
                {
                    Log.Error($"Cast donor {member.DonorGuid} for {member.Id} is not in the installed game.");
                    return false;
                }
                LocalizedText.Define(member.NameKey, member.NameText);
                BlueprintPortrait portrait = null;
                if (member.PortraitId != null && !portraits.TryGetValue(member.PortraitId, out portrait))
                {
                    portrait = PortraitAssets.Create(member.PortraitId);
                    portraits[member.PortraitId] = portrait;
                    if (portrait != null)
                    {
                        register(portrait);
                    }
                }
                member.Unit = CloneUnit(donor, root, "KWR_" + member.Id, ScenarioIds.Derive("unit." + member.Id), member.NameKey, portrait);
                register(member.Unit);
            }
            return true;
        }

        /// <summary>Presentation-only clone: no components, facts, dialog or companion role carried over.</summary>
        public static BlueprintUnit CloneUnit(BlueprintUnit donor, BlueprintRoot root, string name, string guid, string nameKey, BlueprintPortrait portrait)
        {
            var unit = UnityEngine.Object.Instantiate(donor);
            unit.name = name;
            unit.AssetGuid = guid;
            unit.ComponentsArray = new BlueprintComponent[0];
            unit.AddFacts = new BlueprintUnitFact[0];
            unit.CustomizationPreset = null;
            unit.Faction = root.SystemMechanics.FactionNeutrals;
            var localizedName = ScriptableObject.CreateInstance<SharedStringAsset>();
            localizedName.name = name + "_Name";
            localizedName.String = LocalizedText.Create(nameKey);
            unit.LocalizedName = localizedName;
            if (portrait != null)
            {
                AccessTools.Field(typeof(BlueprintUnit), "m_Portrait").SetValue(unit, portrait);
            }
            return unit;
        }
    }
}
