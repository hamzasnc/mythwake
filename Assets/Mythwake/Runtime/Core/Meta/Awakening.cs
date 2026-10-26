using System.Collections.Generic;
using Mythwake.Core.Content;

namespace Mythwake.Core.Meta
{
    /// <summary>One hero's Guardian Awakening (docs/systems/AWAKENING.md).</summary>
    public sealed class AwakeningDefinition
    {
        public string HeroId;
        /// <summary>Doc wording of the awakened form, shown in the UI.</summary>
        public string Description;
        /// <summary>Extra HP and ATK, permille.</summary>
        public int StatPermille = 200;
        public List<PassiveDefinition> Passives = new List<PassiveDefinition>();
        /// <summary>Transcendent Trait: team-wide components (Team = true).</summary>
        public List<PassiveDefinition> Trait = new List<PassiveDefinition>();
        public string PassiveText;
        public string TraitText;
    }

    public sealed class AwakeningConfig
    {
        public int MinStage = 60;
        public List<Amount> Cost = new List<Amount>();
        public List<AwakeningDefinition> Heroes = new List<AwakeningDefinition>();
    }

    public static class Awakening
    {
        public static AwakeningDefinition Of(GameContent content, string heroId)
        {
            foreach (var a in content.Awakening.Heroes)
                if (a.HeroId == heroId)
                    return a;
            return null;
        }

        public static int ShrineLevel(PlayerState state, GameContent content)
        {
            foreach (var b in content.Village.Buildings)
                if (b.Effect == BuildingEffect.Awakening)
                    return Village.Level(state, b);
            return 0;
        }

        /// <summary>Why a hero cannot awaken yet (rule code), or null when it can.</summary>
        public static string Blocker(PlayerState state, GameContent content, string heroId)
        {
            var owned = state.FindHero(heroId);
            if (owned == null)
                return "hero_not_owned";
            if (Of(content, heroId) == null)
                return "no_awakening";
            if (owned.Awakened)
                return "already_awakened";
            if (ShrineLevel(state, content) <= 0)
                return "awakening_locked";
            if (state.Campaign.HighestCleared < content.Awakening.MinStage)
                return "awakening_stage";
            if (owned.Stars < Progression.MaxStars(content, content.Hero(heroId).Grade))
                return "awakening_stars";
            foreach (var c in content.Awakening.Cost)
                if (state.Get(c.Id) < c.Value)
                    return "not_enough";
            return null;
        }

        /// <summary>Passive components an awakened hero adds: its own passive plus its Trait.</summary>
        public static List<PassiveDefinition> Passives(GameContent content, OwnedHero owned)
        {
            var list = new List<PassiveDefinition>();
            if (owned == null || !owned.Awakened)
                return list;
            var a = Of(content, owned.HeroId);
            if (a == null)
                return list;
            foreach (var p in a.Passives)
                list.Add(p.Clone());
            foreach (var p in a.Trait)
                list.Add(p.Clone());
            return list;
        }

        public static int StatPermille(GameContent content, OwnedHero owned) =>
            owned != null && owned.Awakened ? Of(content, owned.HeroId)?.StatPermille ?? 0 : 0;
    }

    /// <summary>Guardian Shrine: awakens a hero into its Guardian form.</summary>
    public sealed class AwakenHero : ICommand<bool>
    {
        public string HeroId;

        public bool Apply(CommandContext ctx)
        {
            var blocker = Awakening.Blocker(ctx.State, ctx.Content, HeroId);
            ctx.Require(blocker == null, blocker ?? "", HeroId);
            ctx.Pay(ctx.Content.Awakening.Cost);
            ctx.State.FindHero(HeroId).Awakened = true;
            return true;
        }
    }
}
