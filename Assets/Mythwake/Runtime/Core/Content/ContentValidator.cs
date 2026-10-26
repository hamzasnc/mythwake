using System.Collections.Generic;

namespace Mythwake.Core.Content
{
    /// <summary>Checks content for mistakes the rules would otherwise silently accept. Run by tests and the editor import.</summary>
    public static class ContentValidator
    {
        public static List<string> Validate(IEnumerable<HeroDefinition> heroes)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var h in heroes)
            {
                var where = h?.Id ?? "<null>";
                if (h == null || string.IsNullOrEmpty(h.Id))
                {
                    errors.Add("hero without id");
                    continue;
                }
                if (!ids.Add(h.Id))
                    errors.Add($"{where}: duplicate id");
                if (h.BaseStats == null || h.BaseStats.Hp <= 0)
                    errors.Add($"{where}: base HP must be positive");
                if (h.BaseStats != null && h.BaseStats.AttackIntervalMs < 400)
                    errors.Add($"{where}: attack interval below 400 ms");
                if (h.Role != Role.HeroLeader && h.Normal == null)
                    errors.Add($"{where}: fighting hero without Normal Skill");

                foreach (var s in h.Skills())
                    ValidateSkill(errors, where, s);
            }
            return errors;
        }

        private static void ValidateSkill(List<string> errors, string hero, SkillDefinition s)
        {
            var where = $"{hero}.{s.Slot}";
            if (s.Slot == SkillSlot.Passive)
            {
                foreach (var p in s.Passives)
                {
                    if (p.Kind == PassiveKind.None)
                        errors.Add($"{where}: passive component without kind");
                    var needsEffect = p.Kind == PassiveKind.OnHit || p.Kind == PassiveKind.OnHitTaken || p.Kind == PassiveKind.OnKill ||
                                      p.Kind == PassiveKind.Periodic || p.Kind == PassiveKind.BattleStart;
                    if (needsEffect && p.Effect == null)
                        errors.Add($"{where}: {p.Kind} needs an effect");
                    if (p.Kind == PassiveKind.Periodic && p.IntervalMs < 1000)
                        errors.Add($"{where}: periodic interval below 1 s");
                }
                return;
            }
            if (s.Hits < 1)
                errors.Add($"{where}: hits must be at least 1");
            if (s.HitTimesMs == null || s.HitTimesMs.Length != s.Hits)
                errors.Add($"{where}: {s.Hits} hits but {s.HitTimesMs?.Length ?? 0} hit times");
            else
                for (var i = 0; i < s.HitTimesMs.Length; i++)
                {
                    if (s.HitTimesMs[i] < 0 || s.HitTimesMs[i] > s.DurationMs)
                        errors.Add($"{where}: hit {i} at {s.HitTimesMs[i]} ms is outside the {s.DurationMs} ms action");
                    if (i > 0 && s.HitTimesMs[i] < s.HitTimesMs[i - 1])
                        errors.Add($"{where}: hit times are not ascending");
                }
            if (s.Effects.Count == 0)
                errors.Add($"{where}: skill without effects");
            if (s.Slot == SkillSlot.Ultimate && s.Trigger.Kind != TriggerKind.Energy)
                errors.Add($"{where}: ultimate must use the energy trigger");
            if (s.Trigger.Kind == TriggerKind.Cooldown && s.Trigger.CooldownMs < 1000)
                errors.Add($"{where}: cooldown below 1 s");
            if (s.Trigger.Kind == TriggerKind.ChanceOnAttack && (s.Trigger.ChancePermille <= 0 || s.Trigger.ChancePermille > 1000))
                errors.Add($"{where}: proc chance must be 1..1000 permille");
            foreach (var e in s.Effects)
            {
                if (e.Kind == EffectKind.ApplyStatus && e.Status == null)
                    errors.Add($"{where}: applyStatus without status");
                if ((e.Kind == EffectKind.Damage || e.Kind == EffectKind.Heal || e.Kind == EffectKind.Shield) && e.Multiplier <= 0)
                    errors.Add($"{where}: {e.Kind} with multiplier {e.Multiplier}");
            }
        }
    }
}
