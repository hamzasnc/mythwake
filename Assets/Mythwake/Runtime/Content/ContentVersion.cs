using System.Collections.Generic;
using System.Linq;

namespace Mythwake.Content
{
    /// <summary>
    /// Version of the rules-relevant content (data files and hero definitions; UI strings excluded). Client and
    /// rules service compute it the same way; online play needs identical versions, or predictions would drift.
    /// </summary>
    public static class ContentVersion
    {
        /// <param name="files">Relative path ("Data/afk.json", "Heroes/elara/elara.hero.json") and text of each file.</param>
        public static string Compute(IEnumerable<KeyValuePair<string, string>> files)
        {
            var all = string.Join("\n", files
                .Where(f => !f.Key.StartsWith("Data/strings."))
                .OrderBy(f => f.Key, System.StringComparer.Ordinal)
                .Select(f => f.Key + "\n" + f.Value.Replace("\r\n", "\n")));
            return SaveJson.HashOf(all).Substring(0, 16);
        }
    }
}
