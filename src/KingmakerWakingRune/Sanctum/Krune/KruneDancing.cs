using System;
using System.Linq;
using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Utility;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.View;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// The dragon-tooth spear dances (page 15, a +3 dancing longspear). As a standard action Krune looses it: the same spear
    /// leaves his hand for a floating form that fights on its own for 4 rounds with his base attack bonus (+8), its own +3
    /// and Control's competence while that rune is lit, then returns to his hand. His hand is empty meanwhile: there is never
    /// a second spear. If he dies while it dances it falls where it fights, into his loot; if its form is destroyed, the
    /// spear falls the same way. The form is a draft body (a floating light carrying the spear), not new art.
    /// </summary>
    internal static class KruneDancing
    {
        public const double DanceSeconds = 24d;
        public static BlueprintUnit Form { get; private set; }
        public static BlueprintAbility Loose { get; private set; }

        public static void Build(BlueprintRoot root, System.Collections.Generic.List<BlueprintScriptableObject> all)
        {
            BlueprintUnit donor = all.OfType<BlueprintUnit>().First(u => u.AssetGuid == "a8225dd5b4d32454eac4c91f25520cc1");
            const string name = "KWR_KruneDancingSpear";
            LocalizedText.Define("kwr.unit." + name, "Dancing Dragon-Tooth Spear");
            BlueprintUnit form = LodgeCast.CloneUnit(donor, root, name, ScenarioIds.Derive("unit." + name), "kwr.unit." + name, null);
            form.Faction = root.SystemMechanics.FactionMobs;
            form.Strength = 10;
            form.Dexterity = 10;
            form.Constitution = 10;
            form.Intelligence = 10;
            form.Wisdom = 17;
            form.Charisma = 17;
            form.MaxHP = 200;
            form.Speed = 30.Feet();
            form.StartingInventory = new Kingmaker.Blueprints.Items.BlueprintItem[0];
            form.Body = new BlueprintUnit.UnitBody { QuickSlots = new Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentUsable[0] };
            var attack = (BlueprintAiAction)ScriptableObject.CreateInstance(AccessTools.TypeByName("Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAttack"));
            attack.name = "KWR_KruneDancingSpearAttack";
            attack.AssetGuid = ScenarioIds.Derive("krune." + attack.name);
            attack.BaseScore = 1f;
            attack.ActorConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[0];
            attack.TargetConsiderations = new Kingmaker.Controllers.Brain.Blueprints.Considerations.Consideration[0];
            KruneBlueprints.Register(attack);
            BlueprintBrain brain = KruneBlueprints.Create<BlueprintBrain>("KWR_KruneDancingSpearBrain");
            brain.Actions = new[] { attack };
            KruneBlueprints.Register(brain);
            form.Brain = brain;
            // His base attack bonus in place of its own (it has none); the spear's +3 is the weapon's own.
            BlueprintFeature wielder = KruneBlueprints.Feature("KWR_KruneDancingWielder", "Dancing", "It fights with its master's base attack bonus (+8). An artifact: it cannot be broken.",
                Stat(StatType.AdditionalAttackBonus, 8));
            form.AddFacts = new BlueprintUnitFact[] { wielder };
            KruneBlueprints.Register(form);
            Form = form;

            Loose = KruneBlueprints.Create<BlueprintAbility>("KWR_KruneLooseDancingSpear");
            KruneBlueprints.Name(Loose, "Loose the Dancing Spear", "As a standard action, Krune looses his dancing spear: it fights on its own for 4 rounds with his base attack bonus, then returns to his hand.");
            Loose.Type = AbilityType.Supernatural;
            Loose.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
            Loose.Range = AbilityRange.Personal;
            Loose.CanTargetSelf = true;
            Loose.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            Loose.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            var run = KruneBlueprints.Component<AbilityEffectRunAction>("KWR_KruneLooseRun");
            run.Actions = new ActionList { Actions = new GameAction[] { ScriptableObject.CreateInstance<KwrLooseSpear>() } };
            Loose.ComponentsArray = new BlueprintComponent[] { run, KruneBlueprints.Component<KwrSpearPossessed>("KWR_KruneLooseHeld") };
            KruneBlueprints.Register(Loose);
        }

        private static AddStatBonus Stat(StatType stat, int value)
        {
            var bonus = KruneBlueprints.Component<AddStatBonus>("KWR_KruneDancing_" + stat);
            bonus.Stat = stat;
            bonus.Value = value;
            bonus.Descriptor = ModifierDescriptor.UntypedStackable;
            return bonus;
        }

        public static UnitEntityData FormOf(KruneState krune)
        {
            return string.IsNullOrEmpty(krune.DancingFormId) ? null : Game.Instance.State.Units.All.FirstOrDefault(u => u.UniqueId == krune.DancingFormId);
        }

        public static void Start(UnitEntityData krune)
        {
            KruneState state = WardRuntime.State?.Krune;
            ItemEntity spear = krune.Body.PrimaryHand.MaybeItem;
            if (state == null || !KruneLoot.IsSpear(spear) || FormOf(state) != null)
            {
                return;
            }
            Vector3 at = ObstacleAnalyzer.GetNearestNode(krune.Position + krune.OrientationDirection * 1.5f).clampedPosition;
            UnitEntityData form = Game.Instance.EntityCreator.SpawnUnit(Form, at, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            form.GroupId = krune.GroupId;
            ItemSlot.IgnoreLock.Retain();
            try
            {
                krune.Inventory.Transfer(spear, form.Inventory);
                form.Body.PrimaryHand.InsertItem(spear);
            }
            finally
            {
                ItemSlot.IgnoreLock.Release();
            }
            state.DancingFormId = form.UniqueId;
            state.DancingUntil = WardRuntime.Now + DanceSeconds;
            KruneAwakening.Note($"the dragon-tooth spear dances (form {form.UniqueId}) until {state.DancingUntil:0}; his hand is empty.");
        }

        /// <summary>Every tick: the dance ends after 4 rounds, or when its form is gone; the spear goes back to him, or down with his gear.</summary>
        public static void Tick(SanctumState sanctum, double now)
        {
            KruneState state = sanctum.Krune;
            UnitEntityData form = FormOf(state);
            if (string.IsNullOrEmpty(state.DancingFormId))
            {
                return;
            }
            UnitEntityData krune = KruneAwakening.Actor;
            bool over = now >= state.DancingUntil || form == null || form.Descriptor.State.IsDead || krune == null || krune.Descriptor.State.IsDead;
            if (!over)
            {
                return;
            }
            ItemEntity spear = form?.Inventory.Items.FirstOrDefault(KruneLoot.IsSpear);
            ItemSlot.IgnoreLock.Retain();
            try
            {
                if (spear != null && krune != null && !krune.Descriptor.State.IsDead)
                {
                    form.Inventory.Transfer(spear, krune.Inventory);
                    krune.Body.PrimaryHand.InsertItem(spear);
                    KruneAwakening.Note("the dragon-tooth spear returns to his hand.");
                }
                else if (spear != null && krune != null)
                {
                    // He is dead: the spear joins the gear he dropped (KruneLoot puts his inventory in the bag).
                    form.Inventory.Transfer(spear, krune.Inventory);
                    KruneAwakening.Note("the dancing spear falls with its master.");
                }
            }
            finally
            {
                ItemSlot.IgnoreLock.Release();
            }
            if (form != null && form.IsInGame)
            {
                form.IsInGame = false;
            }
            state.DancingFormId = string.Empty;
            state.DancingUntil = -1d;
        }
    }

    public sealed class KwrLooseSpear : Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
    {
        public override string GetCaption()
        {
            return "Loose the dancing spear";
        }

        public override void RunAction()
        {
            if (Context.MaybeCaster != null)
            {
                KruneDancing.Start(Context.MaybeCaster);
            }
        }
    }
}
