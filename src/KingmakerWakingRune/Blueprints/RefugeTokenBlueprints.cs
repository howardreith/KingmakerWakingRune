using System;
using System.IO;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerWakingRune.Sanctum;
using UnityEngine;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>The Refuge Token: a mod-owned usable item whose ability hands off to the transfer service.</summary>
    internal static class RefugeTokenBlueprints
    {
        private const float NativeIconPixelsPerUnit = 66.66667f;

        public static BlueprintItemEquipmentUsable Create(Action<BlueprintScriptableObject> register)
        {
            LocalizedText.Define("kwr.token.name", "Refuge Token");
            LocalizedText.Define("kwr.token.description", "Break this rune-marked token to transport your active party from Magnimar Lodge to the Sanctum of Sloth. The expedition sets out ready: wounds are tended and each caster's chosen spells and daily abilities are ready, while protective spells already cast keep their remaining time. Scrolls, potions, wand charges and other consumables stay as they are. Consumed on successful use.");
            LocalizedText.Define("kwr.token.action", "Break Refuge Token");

            Sprite icon = LoadIcon();
            var callback = ScriptableObject.CreateInstance<KwrCallback>();
            callback.name = "KWR_RefugeTokenTransfer";
            callback.Caption = "Refuge Token transfer";
            callback.Body = RefugeTransit.Request;

            var effect = ScriptableObject.CreateInstance<AbilityEffectRunAction>();
            effect.name = "KWR_RefugeTokenEffect";
            effect.SavingThrowType = SavingThrowType.Unknown;
            effect.Actions = new ActionList { Actions = new GameAction[] { callback } };

            var ability = ScriptableObject.CreateInstance<BlueprintAbility>();
            ability.name = "KWR_RefugeTokenAbility";
            ability.AssetGuid = ScenarioIds.Derive("ability.refugetoken");
            ability.Type = AbilityType.Special;
            ability.Range = AbilityRange.Personal;
            ability.CanTargetSelf = true;
            ability.CanTargetFriends = false;
            ability.CanTargetEnemies = false;
            ability.CanTargetPoint = false;
            ability.ActionType = UnitCommand.CommandType.Free;
            ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Self;
            ability.ComponentsArray = new BlueprintComponent[] { effect };
            ability.LocalizedDuration = new LocalizedString();
            ability.LocalizedSavingThrow = new LocalizedString();
            SetFact(ability, "m_DisplayName", LocalizedText.Create("kwr.token.action"));
            SetFact(ability, "m_Description", LocalizedText.Create("kwr.token.description"));
            SetFact(ability, "m_Icon", icon);
            register(ability);

            var item = ScriptableObject.CreateInstance<BlueprintItemEquipmentUsable>();
            item.name = "KWR_RefugeToken";
            item.AssetGuid = ScenarioIds.Derive("item.refugetoken");
            item.Type = UsableItemType.Other;
            item.Ability = ability;
            item.SpendCharges = false;
            item.Charges = 1;
            item.CasterLevel = 1;
            item.SpellLevel = 1;
            item.ComponentsArray = new BlueprintComponent[0];
            SetItem(item, "m_DisplayNameText", LocalizedText.Create("kwr.token.name"));
            SetItem(item, "m_DescriptionText", LocalizedText.Create("kwr.token.description"));
            SetItem(item, "m_FlavorText", new LocalizedString());
            SetItem(item, "m_NonIdentifiedNameText", LocalizedText.Create("kwr.token.name"));
            SetItem(item, "m_NonIdentifiedDescriptionText", LocalizedText.Create("kwr.token.description"));
            SetItem(item, "m_Icon", icon);
            SetItem(item, "m_Cost", 0);
            SetItem(item, "m_IsNotable", true);
            SetItem(item, "m_InventoryPutSound", string.Empty);
            SetItem(item, "m_InventoryTakeSound", string.Empty);
            AccessTools.Field(typeof(BlueprintItemEquipmentUsable), "m_InventoryEquipSound").SetValue(item, string.Empty);
            register(item);
            return item;
        }

        private static void SetFact(BlueprintAbility ability, string field, object value)
        {
            AccessTools.Field(typeof(BlueprintUnitFact), field).SetValue(ability, value);
        }

        private static void SetItem(BlueprintItem item, string field, object value)
        {
            AccessTools.Field(typeof(BlueprintItem), field).SetValue(item, value);
        }

        private static Sprite LoadIcon()
        {
            return PackagedAssets.Sprite("Icons/RefugeToken.png", NativeIconPixelsPerUnit);
        }
    }
}
