using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// The marked palms of A1 and the two items they can yield. Possession is the real inventory item; the saved record
    /// only says where an item came from, so nothing is ever regenerated on a load.
    /// </summary>
    internal static class WardCredentials
    {
        public static BlueprintItem ImpressionItem => WardBlueprints.PalmImpression;

        public static BlueprintItem HandItem => WardBlueprints.BrandedHand;

        private static IEnumerable<ItemEntity> Held(BlueprintItem blueprint)
        {
            return Game.Instance.Player.Inventory.Where(i => i.Blueprint == blueprint);
        }

        public static bool HoldsImpression()
        {
            return Held(ImpressionItem).Any();
        }

        /// <summary>The credential the party would present: an impression first, so the hand is never needlessly used.</summary>
        public static CredentialRecord HeldCredential()
        {
            SanctumState state = WardRuntime.State;
            if (HoldsImpression())
            {
                return state.Credentials.Items.LastOrDefault(r => r.Kind == CredentialKind.Impression);
            }
            return Held(HandItem).Any() ? state.Credentials.Items.FirstOrDefault(r => r.Kind == CredentialKind.Hand) : null;
        }

        /// <summary>Lets a lost impression be remade; held items and every recovered hand stay recorded.</summary>
        public static void Reconcile(SanctumState state)
        {
            // Items carry no identity of their own: an impression record lives exactly as long as an impression is held.
            var held = new HashSet<string>(HoldsImpression() ? state.Credentials.Items.Where(r => r.Kind == CredentialKind.Impression).Select(r => r.ItemId) : Enumerable.Empty<string>());
            CredentialRules.Reconcile(state.Credentials, held);
        }

        public static void NoteMarkSeen(string slot)
        {
            SanctumState state = WardRuntime.State;
            if (state != null && CredentialRules.IsMarked(slot) && !state.Credentials.MarksSeen.Contains(slot))
            {
                state.Credentials.MarksSeen.Add(slot);
            }
        }

        public static bool CanTakeImpression(string slot)
        {
            SanctumState state = WardRuntime.State;
            return state != null && CredentialRules.CanTakeImpression(slot, state.Statue(slot).Outcome, HoldsImpression());
        }

        /// <summary>One minute of copying by the character who reached the figure. A cut-short interval yields nothing.</summary>
        public static void TakeImpression(UnitEntityData actor, string slot)
        {
            SanctumState state = WardRuntime.State;
            string refusal = null;
            if (!CanTakeImpression(slot) || !WardRuntime.CanSpendWork(out refusal))
            {
                WardRuntime.Say(refusal ?? "There is nothing here to copy.");
                return;
            }
            if (!WardRuntime.SpendWork(WardProfile.ImpressionWorkSeconds, $"{actor.CharacterName} taking a palm impression"))
            {
                WardRuntime.Say("The work is cut short; the impression is spoiled.");
                return;
            }
            Game.Instance.Player.Inventory.Add(ImpressionItem);
            string itemId = $"impression:{slot}:{WardRuntime.Now:0}";
            CredentialRules.RecordImpression(state.Credentials, itemId, slot, actor.UniqueId, WardRuntime.Now);
            WardRuntime.Say($"{actor.CharacterName} presses a careful impression of the seven-pointed brand from the stone palm.");
            Log.Info($"Impression of {slot} made by {actor.CharacterName}: record {itemId}; impressions held {Held(ImpressionItem).Count()}.");
        }

        public static bool CanRecoverHand(string slot)
        {
            SanctumState state = WardRuntime.State;
            return state != null && CredentialRules.CanRecoverHand(state.Credentials, slot, WardStatues.BodyIsCorpse(slot));
        }

        public static void RecoverHand(UnitEntityData actor, string slot)
        {
            SanctumState state = WardRuntime.State;
            if (!CanRecoverHand(slot))
            {
                return;
            }
            string itemId = $"hand:{slot}";
            if (!CredentialRules.RecordHand(state.Credentials, itemId, slot, actor.UniqueId, WardRuntime.Now))
            {
                return;
            }
            Game.Instance.Player.Inventory.Add(HandItem);
            WardRuntime.Say($"{actor.CharacterName} takes the branded hand and wraps it for carrying.");
            Log.Info($"Branded hand of {slot} recovered by {actor.CharacterName}; hands held {Held(HandItem).Count()}.");
            WardStatues.ApplyAll(state, WardRuntime.Now);
        }
    }
}
