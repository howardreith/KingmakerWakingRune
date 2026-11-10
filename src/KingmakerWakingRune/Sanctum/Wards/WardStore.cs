using System.IO;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        // Created directly, not through JsonConvert: the game's global defaults (opt-in members, reference ids,
        // its dictionary converter) would otherwise merge into these settings.
        private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            TypeNameHandling = TypeNameHandling.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        });

        private static SanctumWardPart s_Part;
        private static SanctumState s_State;

        /// <summary>The state for the loaded game, or null while no main character exists (a load in progress).</summary>
        public static SanctumState State
        {
            get
            {
                UnitEntityData main = Game.Instance.Player.MainCharacter.Value;
                if (main == null)
                {
                    return null;
                }
                SanctumWardPart part = main.Descriptor.Ensure<SanctumWardPart>();
                if (part != s_Part)
                {
                    s_Part = part;
                    s_State = Read(part);
                }
                return s_State;
            }
        }

        /// <summary>The state as the save holds it, for the test harness's reports.</summary>
        public static string Dump()
        {
            return Write(State, Formatting.Indented);
        }

        public static void WriteTo(SanctumWardPart part)
        {
            if (part == s_Part && s_State != null)
            {
                part.Payload = Write(s_State, Formatting.None);
            }
        }

        private static string Write(SanctumState state, Formatting formatting)
        {
            using (var text = new StringWriter())
            using (var writer = new JsonTextWriter(text) { Formatting = formatting })
            {
                Serializer.Serialize(writer, state);
                return text.ToString();
            }
        }

        private static SanctumState Read(SanctumWardPart part)
        {
            if (string.IsNullOrEmpty(part.Payload))
            {
                return new SanctumState();
            }
            using (var reader = new JsonTextReader(new StringReader(part.Payload)))
            {
                return Serializer.Deserialize<SanctumState>(reader) ?? new SanctumState();
            }
        }
    }
}
