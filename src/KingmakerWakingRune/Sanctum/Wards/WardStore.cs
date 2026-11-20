using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Newtonsoft.Json;

namespace KingmakerWakingRune.Sanctum.Wards
{
    /// <summary>
    /// Carries the sanctum's whole save-bound state on the main character, who travels in the cross-scene state. A single
    /// serialized string keeps the native persistence ignorant of the state's shape, and it is written only at save time.
    /// </summary>
    public sealed class SanctumWardPart : UnitPart
    {
        [JsonProperty]
        public string Payload;

        public override void PreSave()
        {
            WardStore.WriteTo(this);
        }
    }

    /// <summary>The one live copy of <see cref="SanctumState"/>, re-read whenever the loaded game's part object changes.</summary>
    internal static class WardStore
    {
        private static SanctumWardPart s_Part;
        private static SanctumState s_State;

        /// <summary>
        /// The state the loaded game already carries, or null if it has none or no main character exists (a load in
        /// progress). Reading never adds anything to a campaign: only <see cref="Create"/> does, and only the sanctum calls it.
        /// </summary>
        public static SanctumState Existing
        {
            get
            {
                UnitEntityData main = Game.Instance.Player.MainCharacter.Value;
                return main == null ? null : Bind(main.Descriptor.Get<SanctumWardPart>());
            }
        }

        /// <summary>The scenario's state, created on the main character if absent. Called only once the sanctum has loaded.</summary>
        public static SanctumState Create()
        {
            UnitEntityData main = Game.Instance.Player.MainCharacter.Value;
            return main == null ? null : Bind(main.Descriptor.Ensure<SanctumWardPart>());
        }

        private static SanctumState Bind(SanctumWardPart part)
        {
            if (part == null)
            {
                return null;
            }
            if (part != s_Part)
            {
                s_Part = part;
                s_State = WardPayload.Read(part.Payload);
            }
            return s_State;
        }

        /// <summary>The state as the save holds it, for the test harness's reports.</summary>
        public static string Dump()
        {
            SanctumState state = Existing;
            return state == null ? "No sanctum state in this game." : WardPayload.Write(state, Formatting.Indented);
        }

        /// <summary>One part of the state as the save holds it, for the test harness's reports.</summary>
        public static string Dump(object part) => WardPayload.Write(part, Formatting.Indented);

        /// <summary>Whether the loaded game carries the part at all, read without creating it.</summary>
        public static bool HasPart => Game.Instance.Player.MainCharacter.Value?.Descriptor.Get<SanctumWardPart>() != null;

        public static void WriteTo(SanctumWardPart part)
        {
            if (part == s_Part && s_State != null)
            {
                part.Payload = WardPayload.Write(s_State);
            }
        }

    }
}
