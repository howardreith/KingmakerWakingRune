using System.Linq;
using Kingmaker;
using Kingmaker.UnitLogic;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using KingmakerWakingRune.Sanctum.Wards;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Krune's gear after his death. Before his body is consumed, every lootable item he holds or wears (the two artifacts,
    /// his remaining wand charges, unspent diamonds and the rest) is moved, as the same item instances, into one native
    /// ground loot bag where he fell, saved with the area. Nothing is copied, refilled or spawned "if missing"; spent
    /// diamonds stay spent. The artifacts' identities are recorded for the return and the handover.
    /// </summary>
    internal static class KruneLoot
    {
        public static bool IsSpear(ItemEntity item)
        {
            return item != null && item.Blueprint == KruneBlueprints.Spear;
        }

        public static bool IsRod(ItemEntity item)
        {
            return item != null && item.Blueprint == KruneBlueprints.Rod;
        }

        public static bool IsArtifact(ItemEntity item)
        {
            return IsSpear(item) || IsRod(item);
        }

        /// <summary>Moves his remaining gear into a stable loot bag before his body goes. Idempotent: once a bag exists, nothing more.</summary>
        public static void Secure(SanctumState state, UnitEntityData actor)
        {
            ConclusionState c = state.Conclusion;
            if (c.LootContainerId.Length > 0 || actor == null)
            {
                return;
            }
            var items = actor.Inventory.Items.Where(i => i.IsLootable).ToList();
            ItemEntity spear = items.FirstOrDefault(IsSpear);
            ItemEntity rod = items.FirstOrDefault(IsRod);
            // Items carry no saved identity in this game; each artifact blueprint exists once, on Krune: it is the identity.
            c.SpearId = spear?.Blueprint.name ?? string.Empty;
            c.RodId = rod?.Blueprint.name ?? string.Empty;
            if (items.Count == 0)
            {
                c.LootContainerId = "none";
                KruneAwakening.Note("his body held nothing to recover.");
                return;
            }
            Vector3 at = ObstacleAnalyzer.GetNearestNode(actor.Position).clampedPosition;
            DroppedLoot bag = Game.Instance.EntityCreator.SpawnEntityView(BlueprintRoot.Instance.Prefabs.DroppedLootBag, at, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
            bag.Loot = new ItemsCollection();
            ItemSlot.IgnoreLock.Retain();
            try
            {
                foreach (ItemEntity item in items)
                {
                    actor.Inventory.Transfer(item, bag.Loot);
                }
            }
            finally
            {
                ItemSlot.IgnoreLock.Release();
            }
            c.LootContainerId = bag.Data?.UniqueId ?? "bag";
            KruneAwakening.Note($"his gear set down where he fell ({bag.Loot.Items.Count} items: {string.Join(", ", bag.Loot.Items.Select(i => i.Name + (i.Count > 1 ? " x" + i.Count : string.Empty)).ToArray())}); spear {c.SpearId}, rod {c.RodId}.");
        }
    }
}

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>The two quest artifacts cannot be dropped (the game's only way to discard an item): they are carried home or left in their bag.</summary>
    [Harmony12.HarmonyPatch(typeof(ItemsCollection), nameof(ItemsCollection.DropItem))]
    internal static class ArtifactDropPatch
    {
        private static bool Prefix(ItemEntity item, ref bool __result)
        {
            if (!KruneLoot.IsArtifact(item))
            {
                return true;
            }
            __result = false;
            Kingmaker.PubSubSystem.EventBus.RaiseEvent(delegate(Kingmaker.PubSubSystem.IWarningNotificationUIHandler h) { h.HandleWarning("The Society asked for this artifact: it is not something to throw away."); });
            return false;
        }
    }
}

namespace KingmakerWakingRune.Sanctum.Krune
{
    /// <summary>
    /// Wielding the dragon-tooth spear is not carrying it (page 14; SpearEgoRules). Carrying it home needs no check. A party
    /// member who takes it in hand faces its Ego each time: a Will save against DC 22, or it compels them to put it down
    /// (it leaves the hand for the shared inventory). From the second attempt on (the count is kept in the save, so a reload
    /// gives no fresh first try) it first strikes them for 1d8+9. A wielder who masters it must still keep it: each round it
    /// tries to disarm itself with its telekinesis (CMB 20 against their CMD). Its powers serve Krune alone.
    /// </summary>
    [Harmony12.HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.OnDidEquipped))]
    internal static class SpearEgoPatch
    {
        private static void Postfix(ItemEntity __instance, Kingmaker.UnitLogic.UnitDescriptor wielder)
        {
            if (!KruneLoot.IsSpear(__instance) || wielder?.Unit == null || !wielder.Unit.IsPlayerFaction || wielder.Unit == KruneAwakening.Actor || !(__instance.HoldingSlot is Kingmaker.Items.Slots.HandSlot))
            {
                return;
            }
            UnitEntityData unit = wielder.Unit;
            ConclusionState conclusion = WardStore.Existing?.Conclusion;
            System.Collections.Generic.Dictionary<string, int> attempts = conclusion?.SpearWieldAttempts ?? new System.Collections.Generic.Dictionary<string, int>();
            attempts.TryGetValue(unit.UniqueId, out int prior);
            attempts[unit.UniqueId] = prior + 1;
            if (SpearEgoRules.StrikesFirst(prior))
            {
                int strike = Kingmaker.RuleSystem.RulebookEvent.Dice.D(new Kingmaker.RuleSystem.DiceFormula(1, Kingmaker.RuleSystem.DiceType.D8)) + SpearEgoRules.StrikeBonus;
                var damage = new Kingmaker.RuleSystem.Rules.Damage.PhysicalDamage(new Kingmaker.RuleSystem.DiceFormula(0, Kingmaker.RuleSystem.DiceType.Zero), Kingmaker.Enums.Damage.PhysicalDamageForm.Piercing);
                damage.AddBonus(strike);
                Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage(unit, unit, new Kingmaker.RuleSystem.Rules.Damage.DamageBundle(damage)));
                WardRuntime.Say($"The dragon-tooth spear strikes {unit.CharacterName} for the insolence of trying again.");
                Log.Info($"Dragon-tooth spear: attempt {prior + 1} by {unit.CharacterName}: it strikes for {strike} (1d8+9) before its Ego.");
            }
            Kingmaker.RuleSystem.Rules.RuleSavingThrow save = Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.RuleSavingThrow(unit, Kingmaker.EntitySystem.Stats.SavingThrowType.Will, SpearEgoRules.WillDC));
            Log.Info($"Dragon-tooth spear: {unit.CharacterName} takes it in hand (attempt {prior + 1}): Will {save.RollResult} vs DC {SpearEgoRules.WillDC} ({(save.IsPassed ? "mastered" : "compelled to put it down")}).");
            if (save.IsPassed)
            {
                WardRuntime.Say($"The dragon-tooth spear yields to {unit.CharacterName}, muttering about rash, spontaneous hands, and pulls against the grip.");
                if (KruneGear.SpearResists != null && !unit.Buffs.HasFact(KruneGear.SpearResists))
                {
                    unit.Descriptor.AddBuff(KruneGear.SpearResists, unit, null);
                }
                return;
            }
            WardRuntime.Say($"The dragon-tooth spear will not serve {unit.CharacterName}: its will compels them to set it down.");
            PutDown(__instance);
        }

        public static void PutDown(ItemEntity spear)
        {
            Kingmaker.Items.Slots.ItemSlot slot = spear.HoldingSlot;
            Kingmaker.Items.Slots.ItemSlot.IgnoreLock.Retain();
            try
            {
                slot?.RemoveItem();
            }
            finally
            {
                Kingmaker.Items.Slots.ItemSlot.IgnoreLock.Release();
            }
        }
    }

    /// <summary>On a wielder who mastered the spear's Ego: each round its telekinesis tries to disarm it (SpearEgoRules).</summary>
    public sealed class KwrSpearResistsWielder : Kingmaker.UnitLogic.Buffs.Components.BuffLogic, Kingmaker.Controllers.Units.ITickEachRound
    {
        public void OnNewRound()
        {
            UnitEntityData unit = Owner.Unit;
            ItemEntity spear = unit.Body.HandsEquipmentSets.SelectMany(s => new[] { s.PrimaryHand.MaybeItem, s.SecondaryHand.MaybeItem }).FirstOrDefault(KruneLoot.IsSpear);
            if (spear == null)
            {
                Buff.Remove();
                return;
            }
            int d20 = Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.RuleRollD20(unit)).Result;
            int cmd = Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.RuleCalculateCMD(unit, unit, Kingmaker.RuleSystem.Rules.CombatManeuver.Disarm)).Result;
            bool disarmed = SpearEgoRules.DisarmSucceeds(d20, cmd);
            Log.Info($"Dragon-tooth spear: its telekinesis tries to disarm {unit.CharacterName}: d20 {d20} + {SpearEgoRules.TelekinesisCmb} vs CMD {cmd}: {(disarmed ? "disarmed" : "held")}.");
            if (!disarmed)
            {
                return;
            }
            WardRuntime.Say($"The dragon-tooth spear twists itself out of {unit.CharacterName}'s grip.");
            SpearEgoPatch.PutDown(spear);
            Buff.Remove();
        }
    }
}
