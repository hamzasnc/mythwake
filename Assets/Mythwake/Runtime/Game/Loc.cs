using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Mythwake.Game
{
    /// <summary>UI strings by key (Content/Data/strings.&lt;lang&gt;.json). Missing keys show as the key so they are easy to spot.</summary>
    public static class Loc
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();

        public static void Load(TextAsset file)
        {
            _strings = file == null
                ? new Dictionary<string, string>()
                : JsonConvert.DeserializeObject<Dictionary<string, string>>(file.text) ?? new Dictionary<string, string>();
        }

        public static string T(string key) => _strings.TryGetValue(key, out var s) ? s : key;

        public static string T(string key, params object[] args) => string.Format(T(key), args);

        public static bool Has(string key) => _strings.ContainsKey(key);
    }
}
