using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Owned heroes, strongest first.</summary>
    public sealed class HeroesScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private VisualElement _grid;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.Gold, Ids.HeroExp, Ids.Gems).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            UiKit.Text(Loc.T("heroes.title"), "title").Into(safe);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _grid = UiKit.Box("hero-grid").Into(scroll);
            root.Add(MainNav.Build(Ui, Tab.Heroes));
        }

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _grid.Clear();
            foreach (var owned in Ui.State.Heroes.OrderByDescending(h => UiKit.Power(Ui.State, Ui.Content, h)).ThenBy(h => h.HeroId))
            {
                var id = owned.HeroId;
                var card = UiKit.HeroCard(Ui.Catalog, Ui.Content.Hero(id), owned).Into(_grid);
                card.RegisterCallback<ClickEvent>(_ => Ui.Router.Push(new HeroDetailScreen(id)));
            }
        }
    }

    /// <summary>One hero: art, identity, stats, upgrades and the full kit.</summary>
    public sealed class HeroDetailScreen : UiScreen
    {
        private readonly string _heroId;
        private CurrencyBar _currencies;
        private Label _level;
        private Label _power;
        private VisualElement _stats;
        private Button _levelUp;
        private Button _levelUp5;
        private Label _levelCost;
        private Button _starUp;
        private Label _shards;
        private Label _stars;
        private VisualElement _gear;
        private Label _setBonus;
        private VisualElement _fragments;
        private VisualElement _awakening;
        private VisualElement _targets;

        public HeroDetailScreen(string heroId) => _heroId = heroId;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            var hero = Ui.Content.Hero(_heroId);
            _currencies = new CurrencyBar(Ids.Gold, Ids.HeroExp).Into(root);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(root);
            scroll.style.flexGrow = 1;
            var safe = UiKit.Box("safe").Into(scroll);

            var head = UiKit.Box("row").Into(safe);
            UiKit.Btn("‹", () => Ui.Router.Pop(), "btn--secondary", "btn--small").Into(head);
            var names = UiKit.Box().Into(head);
            UiKit.Text(hero.Name, "title").Into(names);
            UiKit.Text(hero.Title, "subtitle").Into(names);

            var art = UiKit.Box("hero-art").Into(safe);
            var assets = Ui.Catalog.Hero(_heroId);
            var image = assets?.full != null ? assets.full : assets?.portrait;
            if (image != null)
                art.style.backgroundImage = new StyleBackground(image);

            var tags = UiKit.Box("row").Into(safe);
            tags.style.flexWrap = Wrap.Wrap;
            UiKit.Text(Loc.T("grade." + UiKit.Lower(hero.Grade)), "tag", "grade--" + UiKit.GradeClass(hero.Grade)).Into(tags);
            UiKit.Text(Loc.T("aspect." + UiKit.Lower(hero.Aspect)), "tag").Into(tags);
            UiKit.Text(Loc.T("role." + UiKit.Lower(hero.Role)), "tag").Into(tags);
            UiKit.Text(Loc.T("faction." + UiKit.Lower(hero.Faction)), "tag").Into(tags);
            UiKit.Text(Loc.T("race." + UiKit.Lower(hero.Race)), "tag").Into(tags);

            var progress = UiKit.Box("panel").Into(safe);
            var levelRow = UiKit.Box("row").Into(progress);
            _level = UiKit.Text("", "title").Into(levelRow);
            UiKit.Box("spacer").Into(levelRow);
            _stars = UiKit.Text("", "title").Into(levelRow);
            _stars.style.color = new StyleColor(new UnityEngine.Color(1f, 0.73f, 0.23f));
            _power = UiKit.Text("", "subtitle").Into(progress);
            _stats = UiKit.Box().Into(progress);
            _levelCost = UiKit.Text("", "label--small").Into(progress);
            var buttons = UiKit.Box("row").Into(progress);
            _levelUp = UiKit.Btn(Loc.T("hero.levelUp"), () => LevelUp(1)).Into(buttons);
            _levelUp5 = UiKit.Btn(Loc.T("hero.levelUp5"), () => LevelUp(5), "btn--secondary").Into(buttons);
            UiKit.Box("spacer").Into(buttons);
            _starUp = UiKit.Btn(Loc.T("hero.starUp"), StarUp, "btn--secondary").Into(buttons);
            _shards = UiKit.Text("", "label--small").Into(progress);

            var gear = UiKit.Box("panel").Into(safe);
            var gearHead = UiKit.Box("row").Into(gear);
            UiKit.Text(Loc.T("gear.title"), "title").Into(gearHead);
            UiKit.Box("spacer").Into(gearHead);
            _setBonus = UiKit.Text("", "label--good").Into(gearHead);
            _gear = UiKit.Box("gear-row").Into(gear);

            var fragments = UiKit.Box("panel").Into(safe);
            UiKit.Text(Loc.T("fragment.titleRelic"), "title").Into(fragments);
            _fragments = UiKit.Box("gear-row").Into(fragments);

            var target = UiKit.Box("panel").Into(safe);
            UiKit.Text(Loc.T("target.title"), "title").Into(target);
            UiKit.Text(Loc.T("target.hint"), "label--small").Into(target).style.whiteSpace = WhiteSpace.Normal;
            _targets = UiKit.Box("row", "focus-row").Into(target);

            _awakening = UiKit.Box("panel", "awakening").Into(safe);

            var skills = UiKit.Box("panel").Into(safe);
            UiKit.Text(Loc.T("hero.skills"), "title").Into(skills);
            foreach (var skill in hero.Skills())
            {
                var row = UiKit.Box("skill").Into(skills);
                UiKit.Text(Loc.T("slot." + UiKit.Lower(skill.Slot)), "skill__slot").Into(row);
                UiKit.Text(skill.Name, "skill__name").Into(row);
                UiKit.Text(skill.Description, "skill__text").Into(row);
            }
        }

        public override void Refresh()
        {
            var content = Ui.Content;
            var owned = Ui.State.FindHero(_heroId);
            var hero = content.Hero(_heroId);
            _currencies.Refresh(Ui.State);
            if (owned == null)
                return;

            var cap = Progression.LevelCap(content, owned);
            _level.text = $"{Loc.T("heroes.level", owned.Level)} / {cap}";
            _stars.text = UiKit.Stars(owned.Stars);
            var stats = TeamBuilder.StatsOf(Formations.Progress(Ui.State, content, owned));
            _power.text = Loc.T("heroes.power", UiKit.Short(TeamBuilder.Power(stats)));
            _stats.Clear();
            Stat(Loc.T("hero.hp"), stats.Hp);
            Stat(Loc.T("hero.attack"), stats.Attack);
            Stat(Loc.T("hero.defense"), stats.Defense);

            var atCap = owned.Level >= cap;
            var cost = Progression.LevelCost(content, owned.Level, 1);
            _levelCost.text = atCap
                ? Loc.T("hero.maxLevel", owned.Stars)
                : string.Join("   ", cost.Select(c => $"{UiKit.Short(c.Value)} {UiKit.CurrencyName(c.Id)}"));
            var affordable = cost.All(c => Ui.State.Get(c.Id) >= c.Value);
            _levelUp.SetEnabled(!atCap && affordable);
            _levelUp5.SetEnabled(owned.Level + 5 <= cap && Progression.LevelCost(content, owned.Level, 5).All(c => Ui.State.Get(c.Id) >= c.Value));

            RefreshGear();
            FragmentUi.Slots(Ui, _fragments, Root, _heroId, Refresh);
            RefreshAwakening(owned);
            RefreshTargets(owned);
            _fragments.Add(RelicUi.Slot(Ui, Root, _heroId, Refresh));

            var maxStars = Progression.MaxStars(content, owned);
            if (owned.Stars >= maxStars)
            {
                _shards.text = Loc.T("hero.maxStars");
                _starUp.SetEnabled(false);
            }
            else
            {
                var starCost = Progression.StarCost(content, owned.Stars, _heroId);
                var have = Ui.State.Get(starCost.Id);
                _shards.text = Loc.T("hero.shards", have, starCost.Value);
                _starUp.SetEnabled(have >= starCost.Value);
            }
        }

        private void RefreshTargets(OwnedHero owned)
        {
            _targets.Clear();
            foreach (Core.Combat.TargetPriority p in System.Enum.GetValues(typeof(Core.Combat.TargetPriority)))
            {
                var priority = p;
                var chip = UiKit.Btn(Loc.T("target." + UiKit.Lower(p)), () =>
                {
                    if (Ui.Try(new SetTargetPriority { HeroId = _heroId, Priority = priority }, out bool _))
                        Refresh();
                }, "btn--secondary", "btn--small", "focus-chip").Into(_targets);
                chip.EnableInClassList("focus-chip--active", owned.Target == p);
            }
        }

        /// <summary>Guardian Awakening panel: the doc's form, requirements and cost, or the awakened passive and Trait.</summary>
        private void RefreshAwakening(OwnedHero owned)
        {
            _awakening.Clear();
            var def = Awakening.Of(Ui.Content, _heroId);
            _awakening.style.display = def == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (def == null)
                return;
            var head = UiKit.Box("row").Into(_awakening);
            UiKit.Text(Loc.T("awaken.title"), "title").Into(head);
            UiKit.Box("spacer").Into(head);
            if (owned.Awakened)
                UiKit.Text(Loc.T("grade.transcendent"), "tag", "grade--transcendent").Into(head);
            UiKit.Text(def.Description, "skill__text").Into(_awakening);
            UiKit.Text(def.PassiveText, owned.Awakened ? "label--good" : "label--small").Into(_awakening).style.whiteSpace = WhiteSpace.Normal;
            UiKit.Text(def.TraitText, owned.Awakened ? "label--good" : "label--small").Into(_awakening).style.whiteSpace = WhiteSpace.Normal;
            if (owned.Awakened)
            {
                UiKit.Text(Loc.T("awaken.awakened"), "subtitle").Into(_awakening);
                return;
            }
            var content = Ui.Content;
            void Req(bool ok, string text) => UiKit.Text((ok ? "✓ " : "✗ ") + text, ok ? "label--good" : "label--bad").Into(_awakening);
            Req(Awakening.ShrineLevel(Ui.State, content) > 0, Loc.T("awaken.req.shrine"));
            Req(Ui.State.Campaign.HighestCleared >= content.Awakening.MinStage, Loc.T("awaken.req.stage", content.Awakening.MinStage));
            var stars = Progression.MaxStars(content, content.Hero(_heroId).Grade);
            Req(owned.Stars >= stars, Loc.T("awaken.req.stars", stars));
            _awakening.Add(GearUi.CostRow(Ui.State, content.Awakening.Cost));
            var button = UiKit.Btn(Loc.T("awaken.button"), () =>
            {
                if (Ui.Try(new AwakenHero { HeroId = _heroId }, out bool _))
                    Refresh();
            }, "btn--big").Into(_awakening);
            button.SetEnabled(Awakening.Blocker(Ui.State, content, _heroId) == null);
        }

        private void RefreshGear()
        {
            _gear.Clear();
            foreach (GearSlot slot in System.Enum.GetValues(typeof(GearSlot)))
            {
                var worn = Gear.Equipped(Ui.State, _heroId, slot);
                var tile = worn != null ? GearUi.Tile(Ui.Content, worn) : GearUi.EmptyTile(slot);
                var s = slot;
                tile.RegisterCallback<ClickEvent>(_ => GearUi.OpenSlot(Ui, Root, _heroId, s, Refresh));
                _gear.Add(tile);
            }
            var stats = Gear.StatsFor(Ui.State, Ui.Content, _heroId);
            _setBonus.text = stats.SetBonusActive ? Loc.T("gear.setActive") : "";
        }

        private void Stat(string name, long value)
        {
            var row = UiKit.Box("stat-row").Into(_stats);
            UiKit.Text(name).Into(row);
            UiKit.Text(UiKit.Short(value)).Into(row);
        }

        private void LevelUp(int levels)
        {
            if (Ui.Try(new LevelUpHero { HeroId = _heroId, Levels = levels }, out _))
                Refresh();
        }

        private void StarUp()
        {
            if (Ui.Try(new StarUpHero { HeroId = _heroId }, out _))
                Refresh();
        }
    }
}
