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
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using KingmakerWakingRune.Blueprints;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// The dragon-tooth spear dances (page 15, a +3 dancing longspear; DanceRules). As a standard action Krune looses it: the
    /// same spear leaves his hand and fights on its own for 4 rounds with his base attack bonus (+8, so its iterative attack
    /// too) and its own +3. Only his base attack bonus: his other bonuses (the Rune of Control's competence among them) are
    /// on his own attack rolls, not the weapon's. It shares his space and goes wherever he goes, walking or teleporting
    /// (its form is kept at his side every tick), reaches 10 feet as a longspear, makes no attacks of opportunity, and is
    /// attended by him: never a target of its own. As the 4 rounds end he grasps it (a free action); so retrieved it cannot
    /// dance again for 4 rounds. If he cannot act then, it drops in his space (a loot bag there), and he takes it up with a
    /// move action if he is still beside it. If he dies while it dances it falls with his gear. His hand is empty while it
    /// dances (no Wish or spear powers): there is never a second spear. The form is a draft body (a floating light carrying
    /// the spear), not new art.
    /// </summary>
    internal static class KruneDancing
    {
        // The dropped spear whose withheld pickup was last noted (one note per drop).
        private static string s_WithheldFor;

        private static string Control(UnitEntityData krune)
        {
            UnitState s = krune.Descriptor.State;
            return $"can act {s.CanAct}, confused {s.HasCondition(UnitCondition.Confusion)}, frightened {s.HasCondition(UnitCondition.Frightened)}, "
                + $"cowering {s.HasCondition(UnitCondition.Cowering)}, faction {krune.Descriptor.Faction?.name}";
        }

        public const double DanceSeconds = DanceRules.DanceSeconds;
        public static BlueprintUnit Form { get; private set; }
        public static BlueprintAbility Loose { get; private set; }
        public static BlueprintAbility Grasp { get; private set; }

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
            // His base attack bonus in place of its own (it has none): as a base attack bonus, so it brings the iterative
            // attack. It keeps to his space (it does not move by itself) and makes no attacks of opportunity.
            BlueprintFeature wielder = KruneBlueprints.Feature("KWR_KruneDancingWielder", "Dancing", "It fights in its master's space with his base attack bonus (+8), reaching 10 feet; it makes no attacks of opportunity and is attended by him (not a target of its own). An artifact: it cannot be broken.",
                Stat(StatType.BaseAttackBonus, 8), Condition(UnitCondition.CantMove), Condition(UnitCondition.DisableAttacksOfOpportunity));
            form.AddFacts = new BlueprintUnitFact[] { wielder };
            KruneBlueprints.Register(form);
            Form = form;

            Loose = KruneBlueprints.Create<BlueprintAbility>("KWR_KruneLooseDancingSpear");
            KruneBlueprints.Name(Loose, "Loose the Dancing Spear", "As a standard action, Krune looses his dancing spear: it fights on its own in his space for 4 rounds with his base attack bonus, then he grasps it again (as a free action). Once grasped, it cannot dance again for 4 rounds.");
            Loose.Type = AbilityType.Supernatural;
            Loose.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard;
            Loose.Range = AbilityRange.Personal;
            Loose.CanTargetSelf = true;
            Loose.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            Loose.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            var run = KruneBlueprints.Component<AbilityEffectRunAction>("KWR_KruneLooseRun");
            run.Actions = new ActionList { Actions = new GameAction[] { ScriptableObject.CreateInstance<KwrLooseSpear>() } };
            Loose.ComponentsArray = new BlueprintComponent[] { run, KruneBlueprints.Component<KwrSpearPossessed>("KWR_KruneLooseHeld"), KruneBlueprints.Component<KwrDanceReady>("KWR_KruneLooseReady") };
            KruneBlueprints.Register(Loose);

            Grasp = KruneBlueprints.Create<BlueprintAbility>("KWR_KruneGraspDancingSpear");
            KruneBlueprints.Name(Grasp, "Take Up the Dragon-Tooth Spear", "As a move action, Krune takes up his spear from the floor of his space, where it dropped when its dance ended.");
            Grasp.Type = AbilityType.Extraordinary;
            Grasp.ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move;
            Grasp.Range = AbilityRange.Personal;
            Grasp.CanTargetSelf = true;
            Grasp.LocalizedDuration = new Kingmaker.Localization.LocalizedString();
            Grasp.LocalizedSavingThrow = new Kingmaker.Localization.LocalizedString();
            var take = KruneBlueprints.Component<AbilityEffectRunAction>("KWR_KruneGraspRun");
            take.Actions = new ActionList { Actions = new GameAction[] { ScriptableObject.CreateInstance<KwrGraspSpear>() } };
            Grasp.ComponentsArray = new BlueprintComponent[] { take, KruneBlueprints.Component<KwrDroppedSpearBeside>("KWR_KruneGraspBeside") };
            KruneBlueprints.Register(Grasp);
        }

        private static AddStatBonus Stat(StatType stat, int value)
        {
            var bonus = KruneBlueprints.Component<AddStatBonus>("KWR_KruneDancing_" + stat);
            bonus.Stat = stat;
            bonus.Value = value;
            bonus.Descriptor = ModifierDescriptor.UntypedStackable;
            return bonus;
        }

        private static AddCondition Condition(UnitCondition condition)
        {
            var add = KruneBlueprints.Component<AddCondition>("KWR_KruneDancing_" + condition);
            add.Condition = condition;
            return add;
        }

        public static UnitEntityData FormOf(KruneState krune)
        {
            return string.IsNullOrEmpty(krune.DancingFormId) ? null : Game.Instance.State.Units.All.FirstOrDefault(u => u.UniqueId == krune.DancingFormId);
        }

        public static void Start(UnitEntityData krune)
        {
            KruneState state = WardRuntime.State?.Krune;
            ItemEntity spear = krune.Body.PrimaryHand.MaybeItem;
            if (state == null || !KruneLoot.IsSpear(spear) || FormOf(state) != null || !DanceRules.CanLoose(WardRuntime.Now, state.DanceLockedUntil))
            {
                return;
            }
            UnitEntityData form = Game.Instance.EntityCreator.SpawnUnit(Form, SpaceOf(krune), Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            form.GroupId = krune.GroupId;
            Attend(form);
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
            state.DancingUntil = WardRuntime.Now + DanceRules.DanceSeconds;
            KruneAwakening.Note($"the dragon-tooth spear dances in his space (form {form.UniqueId}) until {state.DancingUntil:0}; his hand is empty.");
        }

        /// <summary>A point of his own space just behind him (the form never stands on him: the two would push each other).</summary>
        private static Vector3 SpaceOf(UnitEntityData krune)
        {
            return ObstacleAnalyzer.GetNearestNode(krune.Position - krune.OrientationDirection * 0.6f).clampedPosition;
        }

        /// <summary>Attended by its master: no target of its own (a form saved by an earlier build is brought up to this).</summary>
        private static void Attend(UnitEntityData form)
        {
            if (!form.Descriptor.State.IsUntargetable)
            {
                form.Descriptor.State.IsUntargetable.Retain();
            }
            if (form.View?.AgentASP != null)
            {
                form.View.AgentASP.AvoidanceDisabled = true;
            }
        }

        /// <summary>Every tick: it keeps to his space while it dances; after 4 rounds he grasps it, or it drops, or it falls with him.</summary>
        public static void Tick(SanctumState sanctum, double now)
        {
            KruneState state = sanctum.Krune;
            if (string.IsNullOrEmpty(state.DancingFormId))
            {
                // Out of combat his brain is not consulted, so nothing would ever pick a dropped spear up: with no fight
                // on and his hands free he simply takes it up (in combat the director spends a move action on it). Only by
                // his own will: while dominated, charmed, confused or frightened it stays where it lies until that ends.
                UnitEntityData master = KruneAwakening.Actor;
                if (!string.IsNullOrEmpty(state.DroppedSpearBagId) && master != null && !master.Descriptor.State.IsDead && !master.IsInCombat
                    && DroppedBeside(master))
                {
                    if (KruneDirector.OwnMaster(master))
                    {
                        KruneAwakening.Note("out of combat, he takes up his dropped spear.");
                        TakeUp(master);
                    }
                    else if (s_WithheldFor != state.DroppedSpearBagId)
                    {
                        s_WithheldFor = state.DroppedSpearBagId;
                        KruneAwakening.Note($"out of combat, his spear lies at his feet, but he is not his own master ({Control(master)}): it stays there.");
                    }
                }
                return;
            }
            UnitEntityData form = FormOf(state);
            UnitEntityData krune = KruneAwakening.Actor;
            bool dead = krune == null || krune.Descriptor.State.IsDead;
            DanceEnd end = form == null || form.Descriptor.State.IsDead ? (dead ? DanceEnd.FallsWithHim : DanceEnd.Grasped) : DanceRules.End(now, state.DancingUntil, dead, KruneDirector.OwnMaster(krune));
            if (end == DanceEnd.Continue)
            {
                Attend(form);
                if (Vector3.Distance(form.Position, krune.Position) > 1.2f)
                {
                    form.Position = SpaceOf(krune);
                    form.View?.StopMoving();
                }
                return;
            }
            ItemEntity spear = form?.Inventory.Items.FirstOrDefault(KruneLoot.IsSpear);
            ItemSlot.IgnoreLock.Retain();
            try
            {
                if (spear != null && end == DanceEnd.Grasped)
                {
                    form.Inventory.Transfer(spear, krune.Inventory);
                    krune.Body.PrimaryHand.InsertItem(spear);
                    state.DanceLockedUntil = now + DanceRules.ReuseSeconds;
                    KruneAwakening.Note($"he grasps the dragon-tooth spear as its dance ends (a free action); it cannot dance again until {state.DanceLockedUntil:0}.");
                }
                else if (spear != null && end == DanceEnd.Dropped)
                {
                    state.DroppedSpearBagId = Drop(form, spear);
                    KruneAwakening.Note("its dance over, the dragon-tooth spear drops in his space: he could not grasp it.");
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

        private static string Drop(UnitEntityData form, ItemEntity spear)
        {
            Vector3 at = ObstacleAnalyzer.GetNearestNode(form.Position).clampedPosition;
            DroppedLoot bag = Game.Instance.EntityCreator.SpawnEntityView(BlueprintRoot.Instance.Prefabs.DroppedLootBag, at, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            bag.Loot = new ItemsCollection();
            form.Inventory.Transfer(spear, bag.Loot);
            return bag.Data?.UniqueId ?? string.Empty;
        }

        /// <summary>The bag the spear dropped into, if it is still there with the spear in it.</summary>
        public static DroppedLoot DroppedBag(KruneState state)
        {
            if (string.IsNullOrEmpty(state?.DroppedSpearBagId))
            {
                return null;
            }
            DroppedLoot bag = Game.Instance.State.MapObjects.All.FirstOrDefault(m => m.UniqueId == state.DroppedSpearBagId)?.View as DroppedLoot;
            return bag != null && bag.Loot != null && bag.Loot.Items.Any(KruneLoot.IsSpear) ? bag : null;
        }

        public static bool DroppedBeside(UnitEntityData krune)
        {
            DroppedLoot bag = DroppedBag(WardRuntime.State?.Krune);
            return bag != null && krune.Body.PrimaryHand.MaybeItem == null && Vector3.Distance(bag.transform.position, krune.Position) <= 1.5f + krune.Corpulence;
        }

        public static void TakeUp(UnitEntityData krune)
        {
            KruneState state = WardRuntime.State?.Krune;
            DroppedLoot bag = DroppedBag(state);
            ItemEntity spear = bag?.Loot.Items.FirstOrDefault(KruneLoot.IsSpear);
            if (spear == null || !DroppedBeside(krune))
            {
                return;
            }
            ItemSlot.IgnoreLock.Retain();
            try
            {
                bag.Loot.Transfer(spear, krune.Inventory);
                krune.Body.PrimaryHand.InsertItem(spear);
            }
            finally
            {
                ItemSlot.IgnoreLock.Release();
            }
            state.DroppedSpearBagId = string.Empty;
            KruneAwakening.Note("he takes up the dragon-tooth spear from the floor (a move action).");
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

    public sealed class KwrGraspSpear : Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
    {
        public override string GetCaption()
        {
            return "Take up the dropped spear";
        }

        public override void RunAction()
        {
            if (Context.MaybeCaster != null)
            {
                KruneDancing.TakeUp(Context.MaybeCaster);
            }
        }
    }

    /// <summary>Not while it dances, and not for 4 rounds after he grasped it from a dance.</summary>
    public sealed class KwrDanceReady : BlueprintComponent, IAbilityCasterChecker
    {
        public bool CorrectCaster(UnitEntityData caster)
        {
            KruneState state = WardRuntime.State?.Krune;
            return state != null && string.IsNullOrEmpty(state.DancingFormId) && DanceRules.CanLoose(WardRuntime.Now, state.DanceLockedUntil);
        }

        public string GetReason()
        {
            return "The spear has only just been grasped from its dance.";
        }
    }

    /// <summary>The spear lies dropped in his own space and his hand is free.</summary>
    public sealed class KwrDroppedSpearBeside : BlueprintComponent, IAbilityCasterChecker
    {
        public bool CorrectCaster(UnitEntityData caster)
        {
            return KruneDancing.DroppedBeside(caster);
        }

        public string GetReason()
        {
            return "The spear is not lying beside him.";
        }
    }
}
