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
        public static readonly string[] DataFiles = { "progression", "afk", "campaign", "banners", "start", "buildings", "gear", "quests", "hunts", "arena", "dungeons", "fragments", "relics", "trials", "realms", "awakenings", "bossFragments", "defense", "shops", "skills", "guilds" };

        /// <param name="readData">Returns the text of a data file by name (see <see cref="DataFiles"/>).</param>
        /// <param name="heroJsons">Texts of all *.hero.json files.</param>
        /// <param name="monsterJsons">Texts of all *.monster.json files (optional).</param>
        public static GameContent Load(System.Func<string, string> readData, IEnumerable<string> heroJsons, IEnumerable<string> monsterJsons = null)
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
                Hunts = ContentJson.Parse<HuntConfig>(readData("hunts")),
                Arena = ContentJson.Parse<ArenaConfig>(readData("arena")),
                Dungeons = ContentJson.Parse<DungeonConfig>(readData("dungeons")),
                Fragments = ContentJson.Parse<FragmentConfig>(readData("fragments")),
                Relics = ContentJson.Parse<RelicConfig>(readData("relics")),
                Trials = ContentJson.Parse<TrialConfig>(readData("trials")),
                Realms = ContentJson.Parse<RealmConfig>(readData("realms")),
                Awakening = ContentJson.Parse<AwakeningConfig>(readData("awakenings")),
                BossFragments = ContentJson.Parse<BossFragmentConfig>(readData("bossFragments")),
                Defense = ContentJson.Parse<DefenseConfig>(readData("defense")),
                Shops = ContentJson.Parse<ShopConfig>(readData("shops")),
                Skills = ContentJson.Parse<SkillConfig>(readData("skills")),
                Guilds = ContentJson.Parse<GuildConfig>(readData("guilds"))
            };
            foreach (var json in heroJsons)
            {
                var hero = ContentJson.ParseHero(json);
                if (content.Heroes.ContainsKey(hero.Id))
                    throw new ContentException($"duplicate hero id '{hero.Id}'");
                content.Heroes.Add(hero.Id, hero);
            }

            foreach (var json in monsterJsons ?? Enumerable.Empty<string>())
            {
                var monster = ContentJson.ParseHero(json);
                if (content.Heroes.ContainsKey(monster.Id) || content.Monsters.ContainsKey(monster.Id))
                    throw new ContentException($"duplicate id '{monster.Id}'");
                content.Monsters.Add(monster.Id, monster);
            }

            var errors = ContentValidator.Validate(content.Heroes.Values.Concat(content.Monsters.Values));
            foreach (var m in content.Monsters.Values)
                if (m.Group == EnemyGroup.None)
                    errors.Add($"monster {m.Id}: needs an enemy group");
            foreach (var d in content.Dungeons.Dungeons.Concat(content.Realms.Realms))
            {
                if (!content.Monsters.ContainsKey(d.Boss ?? ""))
                    errors.Add($"dungeons.json/{d.Id}: unknown boss '{d.Boss}'");
                foreach (var id in d.Escort)
                    if (!content.Monsters.ContainsKey(id))
                        errors.Add($"dungeons.json/{d.Id}: unknown escort '{id}'");
            }
            if (content.Trials.Trials.Count > 0)
                foreach (Aspect a in System.Enum.GetValues(typeof(Aspect)))
                {
                    if (!content.Trials.Trials.Any(t => t.Aspect == a))
                        errors.Add($"trials.json: no trial for {a}");
                    if (!content.Monsters.Values.Concat(content.Heroes.Values).Any(h => h.Aspect == a && !h.Boss && h.Role != Role.HeroLeader))
                        errors.Add($"trials.json: no enemies of {a}");
                }
            foreach (var a in content.Awakening.Heroes)
                if (!content.Heroes.ContainsKey(a.HeroId ?? ""))
                    errors.Add($"awakenings.json: unknown hero '{a.HeroId}'");
            foreach (var f in content.BossFragments.Fragments)
                if (!content.Dungeons.Dungeons.Any(d => d.Id == f.Dungeon))
                    errors.Add($"bossFragments.json/{f.Id}: unknown dungeon '{f.Dungeon}'");
            foreach (var shop in content.Shops.Shops)
            {
                if (shop.Building != null && !content.Village.Buildings.Any(b => b.Id == shop.Building))
                    errors.Add($"shops.json/{shop.Id}: unknown building '{shop.Building}'");
                if (shop.Items.Select(i => i.Id).Distinct().Count() != shop.Items.Count)
                    errors.Add($"shops.json/{shop.Id}: duplicate item ids");
            }
            foreach (var id in content.Guilds.Raid.Bosses)
                if (!content.Monsters.TryGetValue(id, out var raidBoss) || !raidBoss.RaidBoss)
                    errors.Add($"guilds.json: '{id}' is not a raid boss monster");
            var relicIds = new HashSet<string>();
            foreach (var r in content.Relics.Relics)
            {
                if (!relicIds.Add(r.Id ?? ""))
                    errors.Add($"relics.json: duplicate relic '{r.Id}'");
                if (!content.Monsters.ContainsKey(r.Source ?? ""))
                    errors.Add($"relics.json/{r.Id}: unknown source boss '{r.Source}'");
                if (r.Components.Count == 0 && r.FragmentBoost <= 0 && r.RewardBonus <= 0)
                    errors.Add($"relics.json/{r.Id}: no effect");
            }
            foreach (var d in content.Dungeons.Dungeons)
                foreach (var id in d.RelicPool)
                    if (!relicIds.Contains(id))
                        errors.Add($"dungeons.json/{d.Id}: unknown relic '{id}'");
            var effectIds = new HashSet<string>();
            foreach (var e in content.Fragments.Effects)
            {
                if (!effectIds.Add(e.Id ?? ""))
                    errors.Add($"fragments.json: duplicate effect '{e.Id}'");
                if (e.Slot == FragmentSlot.Core)
                    errors.Add($"fragments.json/{e.Id}: Core Fragments have no effect");
                if (e.Slot == FragmentSlot.Aspect && e.Aspect == null)
                    errors.Add($"fragments.json/{e.Id}: Aspect effect needs an aspect");
                if (e.Components.Count == 0)
                    errors.Add($"fragments.json/{e.Id}: no components");
            }
            if (content.Fragments.Grades.Count > 0)
            {
                foreach (Aspect a in System.Enum.GetValues(typeof(Aspect)))
                    if (!content.Fragments.Effects.Any(e => e.Slot == FragmentSlot.Aspect && e.Aspect == a))
                        errors.Add($"fragments.json: no Aspect Fragment effect for {a}");
                if (!content.Fragments.Effects.Any(e => e.Slot == FragmentSlot.Echo))
                    errors.Add("fragments.json: no Echo Fragment effect");
            }
            foreach (var c in content.Campaign.Chapters)
            {
                foreach (var id in c.Monsters)
                    if (!content.Monsters.ContainsKey(id))
                        errors.Add($"campaign.json/{c.Id}: unknown monster '{id}'");
                if (c.Boss != null && !content.Monsters.ContainsKey(c.Boss))
                    errors.Add($"campaign.json/{c.Id}: unknown boss '{c.Boss}'");
            }
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
