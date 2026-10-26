using Mythwake.Core.Meta;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Mythwake.Content
{
    /// <summary>Save-game format. Unlike content, unknown fields are ignored so older builds can read newer saves.</summary>
    public static class SaveJson
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Formatting = Formatting.None
        };

        public static string Write(PlayerState state) => JsonConvert.SerializeObject(state, Settings);

        public static PlayerState Read(string json)
        {
            var state = JsonConvert.DeserializeObject<PlayerState>(json, Settings);
            if (state == null)
                throw new ContentException("empty save");
            Migrate(state);
            return state;
        }

        /// <summary>Upgrades older saves in place. Add one step per version bump.</summary>
        private static void Migrate(PlayerState state)
        {
            if (state.Version > PlayerState.CurrentVersion)
                throw new ContentException($"save version {state.Version} is newer than this build ({PlayerState.CurrentVersion})");
            if (state.Formation == null || state.Formation.Length != 5)
            {
                var fixedSlots = new string[5];
                if (state.Formation != null)
                    System.Array.Copy(state.Formation, fixedSlots, System.Math.Min(5, state.Formation.Length));
                state.Formation = fixedSlots;
            }
            state.Version = PlayerState.CurrentVersion;
        }
    }
}
