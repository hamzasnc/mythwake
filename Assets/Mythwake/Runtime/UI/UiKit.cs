using System;
using System.Collections.Generic;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Small builders so screens read like layout, not like DOM plumbing.</summary>
    public static class UiKit
    {
        public static T With<T>(this T element, params string[] classes) where T : VisualElement
        {
            foreach (var c in classes)
                element.AddToClassList(c);
            return element;
        }

        public static T Into<T>(this T element, VisualElement parent) where T : VisualElement
        {
            parent.Add(element);
            return element;
        }

        public static VisualElement Box(params string[] classes) => new VisualElement().With(classes);

        public static Label Text(string text, params string[] classes) => new Label(text).With(classes);

        public static Button Btn(string text, Action onClick, params string[] classes)
        {
            var b = new Button(onClick) { text = text };
            b.AddToClassList("btn");
            foreach (var c in classes)
                b.AddToClassList(c);
            return b;
        }

        public static string Short(long value)
        {
            if (value >= 10_000_000_000) return (value / 1_000_000_000) + "B";
            if (value >= 10_000_000) return (value / 1_000_000) + "M";
            if (value >= 100_000) return (value / 1_000) + "K";
            return value.ToString("N0");
        }

        public static string Lower(object enumValue)
        {
            var s = enumValue.ToString();
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string GradeClass(Grade g) => Lower(g);

        public static string Stars(int stars) => new string('★', Mathf.Clamp(stars, 0, 9));

        /// <summary>Card with portrait, stars, aspect dot, name and level. Used by lists, formation and summon results.</summary>
        public static VisualElement HeroCard(ContentCatalog catalog, HeroDefinition hero, OwnedHero owned)
        {
            var card = Box("hero-card", "hero-card--" + GradeClass(owned != null && owned.Awakened ? Grade.Transcendent : hero.Grade));
            card.EnableInClassList("hero-card--awakened", owned != null && owned.Awakened);
            var art = catalog.Hero(hero.Id)?.portrait;
            if (art != null)
                Box("hero-card__portrait").Into(card).style.backgroundImage = new StyleBackground(art);
            else
                Text(hero.Name.Substring(0, 1), "hero-card__initial").Into(card);
            Text(Stars(owned?.Stars ?? 1), "hero-card__stars").Into(card);
            Box("aspect-dot", "aspect--" + Lower(hero.Aspect)).Into(card);
            var footer = Box("hero-card__footer").Into(card);
            Text(hero.Name, "hero-card__name").Into(footer);
            if (owned != null)
                Text(Loc.T("heroes.level", owned.Level), "hero-card__level").Into(footer);
            return card;
        }

        public static long Power(PlayerState state, GameContent content, OwnedHero owned) =>
            TeamBuilder.Power(TeamBuilder.StatsOf(Formations.Progress(state, content, owned)));

        public static VisualElement Amount(string currency, long value)
        {
            var row = Box("reward");
            Box("currency__icon", "icon--" + currency).Into(row);
            Text($"{Short(value)}  {Loc.T("currency." + currency)}").Into(row);
            return row;
        }

        public static string CurrencyName(string id) =>
            id.StartsWith("shards.") ? "Hero Shards" : Loc.T("currency." + id);
    }

    /// <summary>Gold / Hero EXP / Gems / Summon Scrolls along the top of most screens.</summary>
    public sealed class CurrencyBar : VisualElement
    {
        private readonly Dictionary<string, Label> _values = new Dictionary<string, Label>();

        public CurrencyBar(params string[] currencies)
        {
            AddToClassList("currency-bar");
            foreach (var id in currencies)
            {
                var chip = UiKit.Box("currency").Into(this);
                UiKit.Box("currency__icon", "icon--" + id).Into(chip);
                _values[id] = UiKit.Text("0", "currency__value").Into(chip);
            }
        }

        public void Refresh(PlayerState state)
        {
            foreach (var kv in _values)
                kv.Value.text = UiKit.Short(state.Get(kv.Key));
        }
    }
}
