using System.Linq;
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

        /// <summary>An icon of the UI kit by name (Content/UI/Icons/&lt;name&gt;.png, class ui-icon--&lt;name&gt;).</summary>
        public static VisualElement Icon(string name, params string[] classes)
        {
            var icon = Box("ui-icon", "ui-icon--" + name);
            foreach (var c in classes)
                icon.AddToClassList(c);
            icon.pickingMode = PickingMode.Ignore;
            return icon;
        }

        /// <summary>Small pill with an icon in front of the text (grade, Aspect, role, faction, race).</summary>
        public static VisualElement IconTag(string icon, string text, params string[] classes)
        {
            var tag = Box("tag", "tag--icon");
            foreach (var c in classes)
                tag.AddToClassList(c);
            Icon(icon, "tag__icon").Into(tag);
            Text(text).Into(tag);
            return tag;
        }

        /// <summary>Round picture button without text (back, close, info …): "icon-btn icon-btn--&lt;icon&gt;" in Mythwake.uss.</summary>
        public static Button IconBtn(string icon, Action onClick, params string[] classes)
        {
            var b = new Button(onClick);
            b.AddToClassList("icon-btn");
            b.AddToClassList("icon-btn--" + icon);
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

        /// <summary>Row of painted star icons (the UI font has no star glyph).</summary>
        public static VisualElement StarRow(int stars, params string[] classes)
        {
            var row = Box("star-row");
            foreach (var c in classes)
                row.AddToClassList(c);
            row.pickingMode = PickingMode.Ignore;
            SetStars(row, stars);
            return row;
        }

        public static void SetStars(VisualElement row, int stars)
        {
            row.Clear();
            for (var i = 0; i < Mathf.Clamp(stars, 0, 9); i++)
                Icon("star", "star-row__star").Into(row);
        }

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
            StarRow(owned?.Stars ?? 1, "hero-card__stars").Into(card);
            Box("aspect-dot", "aspect--" + Lower(hero.Aspect)).Into(card);
            // grade frame painted over the portrait (Content/UI/Chrome/card<Grade>.png)
            Box("hero-card__frame").Into(card).pickingMode = PickingMode.Ignore;
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

        /// <summary>Round relief icon with a short outlined label, the side buttons of the hubs (docs/UI_STYLE.md).</summary>
        public static VisualElement SideButton(VisualElement column, string icon, string label, Action onClick)
        {
            var b = Box("side-btn").Into(column);
            var ring = Box("side-btn__ring").Into(b);
            Icon(icon, "side-btn__icon").Into(ring);
            Text(label, "side-btn__label").Into(b);
            b.RegisterCallback<ClickEvent>(_ => onClick());
            return b;
        }

        /// <summary>Reward as a square item slot: currency icon, amount bottom right (AFK Arena style, docs/UI_STYLE.md).</summary>
        public static VisualElement Slot(string currency, long value)
        {
            var slot = Box("item-slot");
            Box("item-slot__icon", "currency__icon", "icon--" + currency).Into(slot);
            Text(Short(value), "item-slot__count").Into(slot);
            slot.tooltip = CurrencyName(currency);
            return slot;
        }

        public static string CurrencyName(string id) =>
            id.StartsWith("shards.") ? "Hero Shards" : Loc.T("currency." + id);
    }

    /// <summary>
    /// The one top bar of every screen (AFK style, docs/UI_STYLE.md): the team leader's portrait with the team's power on
    /// the left, up to three currency pills on the right, a "+" next to Gems that opens the shop.
    /// </summary>
    public sealed class CurrencyBar : VisualElement
    {
        /// <summary>Set once at boot; the bar reads the team and opens the shop through it.</summary>
        public static UiContext Context;

        private readonly Dictionary<string, Label> _values = new Dictionary<string, Label>();
        private readonly VisualElement _portrait;
        private readonly Label _power;

        public CurrencyBar(params string[] currencies)
        {
            AddToClassList("currency-bar");
            var avatar = UiKit.Box("top-avatar").Into(this);
            _portrait = UiKit.Box("top-avatar__portrait").Into(avatar);
            UiKit.Box("top-avatar__ring").Into(avatar);
            var power = UiKit.Box("top-power").Into(this);
            UiKit.Icon("power", "top-power__icon").Into(power);
            _power = UiKit.Text("", "top-power__value").Into(power);
            UiKit.Box("spacer").Into(this);
            foreach (var id in currencies.Take(3))
            {
                var chip = UiKit.Box("currency").Into(this);
                UiKit.Box("currency__icon", "icon--" + id).Into(chip);
                _values[id] = UiKit.Text("0", "currency__value").Into(chip);
                if (id == Ids.Gems)
                {
                    var plus = UiKit.Box("currency__plus").Into(chip);
                    plus.RegisterCallback<ClickEvent>(_ => Context?.Router.Push(new ShopScreen()));
                }
            }
        }

        public void Refresh(PlayerState state)
        {
            foreach (var kv in _values)
                kv.Value.text = UiKit.Short(state.Get(kv.Key));
            if (Context?.Authority == null)
                return;
            var lead = state.Formation.FirstOrDefault(id => id != null);
            var art = lead != null ? Context.Catalog.Hero(lead)?.portrait : null;
            _portrait.style.backgroundImage = art != null ? new StyleBackground(art) : StyleKeyword.None;
            var team = TeamBuilder.Build(Formations.ToProgress(state, Context.Content, state.Formation));
            _power.text = UiKit.Short(team.Sum(u => TeamBuilder.Power(u.Stats)));
        }
    }
}
