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

        /// <summary>The lodge's leaders and staff carry no weapon: the donor's hand items are left off (agents keep theirs).</summary>
        public bool Unarmed => Role != CastRole.Agent;
        public Vector3 Position;
        public float Yaw;
        public BlueprintUnit Unit;
    }

    /// <summary>
    /// The eleven scenario-authored NPCs. Each is a presentation-only clone of a verified base-game
    /// unit (see docs/RECON.md); donors are never mutated. Sheila is registered by ScenarioBlueprints.
    /// WR-PLAYTEST-02 (owner's request: the dolls should resemble their portraits): the Masked Figure, the Quartermaster and
    /// the Coordinator take new donors chosen against their portraits in a side-by-side lineup in the lodge; Ambrus keeps
    /// his, already the closest. Blueprint identities are unchanged, so existing saves show the new models at their next load.
    /// </summary>
    internal static class LodgeCast
    {
        public static readonly List<CastMember> Members = new List<CastMember>
        {
            new CastMember { Id = "ambrus", NameKey = "kwr.npc.ambrus", NameText = "Ambrus Valsin", DonorGuid = "bfae523688f555541b5318f8c709f452", PortraitId = "AmbrusValsin", Role = CastRole.Leader, Position = LodgeLayout.Ambrus, Yaw = LodgeLayout.LeaderYaw },
            new CastMember { Id = "masked", NameKey = "kwr.npc.masked", NameText = "Masked Figure", DonorGuid = "2a842f99acd10834f9c8016ee11a0141", PortraitId = "DecemvirateMember", Role = CastRole.Leader, Position = LodgeLayout.Masked, Yaw = LodgeLayout.LeaderYaw },
            new CastMember { Id = "quartermaster", NameKey = "kwr.npc.quartermaster", NameText = "Quartermaster", DonorGuid = "a6d9da12672a1bc4ebb0f7a027b95d58", PortraitId = "Quartermaster", Role = CastRole.Service, Position = LodgeLayout.Quartermaster, Yaw = LodgeLayout.ServiceYaw },
            new CastMember { Id = "coordinator", NameKey = "kwr.npc.coordinator", NameText = "Society Coordinator", DonorGuid = "2ea2114e81009db4ba4356f338973224", PortraitId = "SocietyCoordinator", Role = CastRole.Service, Position = LodgeLayout.Coordinator, Yaw = LodgeLayout.ServiceYaw },
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
                member.Unit = CloneUnit(donor, root, "KWR_" + member.Id, ScenarioIds.Derive("unit." + member.Id), member.NameKey, portrait, member.Unarmed);
                register(member.Unit);
            }
            return true;
        }

        /// <summary>Presentation-only clone: no components, facts, dialog or companion role carried over; optionally no weapons.</summary>
        public static BlueprintUnit CloneUnit(BlueprintUnit donor, BlueprintRoot root, string name, string guid, string nameKey, BlueprintPortrait portrait, bool unarmed = false)
        {
            var unit = UnityEngine.Object.Instantiate(donor);
            if (unarmed)
            {
                unit.Body = WithoutHands(donor.Body);
            }
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

        /// <summary>A copy of a donor's body with nothing in its hands; the donor's own body is not touched.</summary>
        private static BlueprintUnit.UnitBody WithoutHands(BlueprintUnit.UnitBody body)
        {
            var copy = (BlueprintUnit.UnitBody)typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(body, null);
            copy.PrimaryHand = null;
            copy.SecondaryHand = null;
            copy.PrimaryHandAlternative1 = null;
            copy.SecondaryHandAlternative1 = null;
            copy.PrimaryHandAlternative2 = null;
            copy.SecondaryHandAlternative2 = null;
            copy.PrimaryHandAlternative3 = null;
            copy.SecondaryHandAlternative3 = null;
            return copy;
        }
    }
}
