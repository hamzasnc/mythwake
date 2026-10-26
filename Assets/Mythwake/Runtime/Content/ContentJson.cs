using Mythwake.Core.Content;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Mythwake.Content
{
    /// <summary>JSON format of all content files: camelCase names, enums as camelCase strings, unknown fields are errors.</summary>
    public static class ContentJson
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) },
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Formatting = Formatting.Indented
        };

        public static HeroDefinition ParseHero(string json) => Parse<HeroDefinition>(json);

        public static T Parse<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);

        public static string Write<T>(T value) => JsonConvert.SerializeObject(value, Settings);
    }
}
