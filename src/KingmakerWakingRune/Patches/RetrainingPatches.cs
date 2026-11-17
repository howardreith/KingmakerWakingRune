using System;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.CharSelect;
using KingmakerWakingRune.Party;

namespace KingmakerWakingRune.Patches
{
    /// <summary>The native rebuild starting: an open Coordinator retraining binds to the character named for it. Observation only.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.RespecCompanion))]
    internal static class RetrainingBindPatch
    {
        private static void Prefix(UnitEntityData unit)
        {
            Retraining.BeforeRespec(unit);
        }
    }

    /// <summary>Every finished native respec (the Coordinator's or any other): the scenario's records are carried across.</summary>
    internal sealed class RetrainingRespecEvents : Kingmaker.PubSubSystem.IUnitChangedAfterRespecHandler
    {
        private static RetrainingRespecEvents s_Instance;

        public static void Subscribe()
        {
            if (s_Instance == null)
            {
                s_Instance = new RetrainingRespecEvents();
                Kingmaker.PubSubSystem.EventBus.Subscribe(s_Instance);
            }
        }

        public void HandleUnitChangedAfterRespec(UnitEntityData unit)
        {
            try
            {
                Retraining.AfterAnyRespec(unit);
            }
            catch (Exception ex)
            {
                Log.Error($"Respec of {unit?.CharacterName}: the scenario's records could not be carried across.", ex);
            }
        }
    }

    /// <summary>The native character selector closing (with or without a choice); an open retraining checks which.</summary>
    [HarmonyPatch(typeof(CharSelectWindow), "CloseWindow")]
    internal static class RetrainingSelectorPatch
    {
        private static void Postfix()
        {
            Retraining.OnSelectorClosed();
        }
    }

    /// <summary>
    /// Only during a Coordinator retraining that has started: the pet the native rebuild is about to retire (its owner's new
    /// build has none, or another) first gives its gear to the shared inventory. Every other entity is untouched.
    /// </summary>
    [HarmonyPatch(typeof(EntityDataBase), nameof(EntityDataBase.Destroy))]
    internal static class RetrainingPetPatch
    {
        private static void Prefix(EntityDataBase __instance)
        {
            UnitEntityData pet = Retraining.RetiringPet;
            if (pet == null || !ReferenceEquals(__instance, pet))
            {
                return;
            }
            try
            {
                Retraining.BeforePetRetired(pet);
            }
            catch (Exception ex)
            {
                Log.Error($"Retraining: the former pet {pet.CharacterName}'s gear could not be moved before it was retired.", ex);
            }
        }
    }
}
