using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace KingmakerWakingRune.Sanctum.Wards
{
    internal static class WardPayload
    {
        // Create directly so the game's JsonConvert defaults never merge into the ward save format.
        private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            TypeNameHandling = TypeNameHandling.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        });

        public static string Write(object value, Formatting formatting = Formatting.None)
        {
            using (var text = new StringWriter())
            using (var writer = new JsonTextWriter(text) { Formatting = formatting })
            {
                Serializer.Serialize(writer, value);
                return text.ToString();
            }
        }

        public static SanctumState Read(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return new SanctumState();
            using (var reader = new JsonTextReader(new StringReader(payload)))
                return Serializer.Deserialize<SanctumState>(reader) ?? new SanctumState();
        }
    }
}
