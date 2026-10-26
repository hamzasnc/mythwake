using System.Linq;
using System.Collections.Generic;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;

namespace Mythwake.Content
{
    /// <summary>Builds <see cref="GameContent"/> from content file texts. File access stays with the caller (Unity, server, tests).</summary>
    public static class GameContentLoader
    {
        /// <summary>Data files under Content/Data, without extension.</summary>
        public static readonly string[] DataFiles = { "progression", "afk", "campaign", "banners", "start", "buildings", "gear", "quests", "hunts" };

        /// <param name="readData">Returns the text of a data file by name (see <see cref="DataFiles"/>).</param>
        /// <param name="heroJsons">Texts of all *.hero.json files.</param>
        public static GameContent Load(System.Func<string, string> readData, IEnumerable<string> heroJsons)
        {
            var content = new GameContent
            {
                Progression = ContentJson.Parse<ProgressionConfig>(readData("progression")),
                Afk = ContentJson.Parse<AfkConfig>(readData("afk")),
                Campaign = ContentJson.Parse<CampaignConfig>(readData("campaign")),
                Banners = ContentJson.Parse<List<BannerDefinition>>(readData("banners")),
                Start = ContentJson.Parse<StartingState>(readData("start")),
                Village = ContentJson.Parse<VillageConfig>(readData("buildings")),
                Gear = ContentJson.Parse<GearConfig>(readData("gear")),
                Quests = ContentJson.Parse<QuestConfig>(readData("quests")),
                Hunts = ContentJson.Parse<HuntConfig>(readData("hunts"))
            };
            foreach (var json in heroJsons)
            {
                var hero = ContentJson.ParseHero(json);
                if (content.Heroes.ContainsKey(hero.Id))
                    throw new ContentException($"duplicate hero id '{hero.Id}'");
                content.Heroes.Add(hero.Id, hero);
            }

            var errors = ContentValidator.Validate(content.Heroes.Values);
            foreach (var id in content.Start.Heroes)
                if (!content.Heroes.ContainsKey(id))
                    errors.Add($"start.json: unknown hero '{id}'");
            foreach (var b in content.Banners)
                foreach (var id in b.Pool)
                    if (!content.Heroes.ContainsKey(id))
                        errors.Add($"banners.json/{b.Id}: unknown hero '{id}'");
            var buildingIds = new HashSet<string>();
            foreach (var b in content.Village.Buildings)
                if (!buildingIds.Add(b.Id))
                    errors.Add($"buildings.json: duplicate building '{b.Id}'");
            foreach (var q in content.Quests.Main)
                if (q.Condition == MainCondition.BuildingLevel && !buildingIds.Contains(q.Target ?? ""))
                    errors.Add($"quests.json/{q.Id}: unknown building '{q.Target}'");
            var stats = new HashSet<string>();
            foreach (var f in typeof(Stat).GetFields())
                stats.Add((string)f.GetValue(null));
            foreach (var t in content.Quests.Daily.Concat(content.Quests.Weekly))
                if (!stats.Contains(t.Stat))
                    errors.Add($"quests.json/{t.Id}: unknown stat '{t.Stat}'");
            if (errors.Count > 0)
                throw new ContentException(string.Join("\n", errors));
            return content;
        }
    }

    public sealed class ContentException : System.Exception
    {
        public ContentException(string message) : base(message) { }
    }
}
