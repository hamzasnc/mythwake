-- Additive starter grant: existing player progression is merged by hero ID.
-- No player rows or existing hero definitions are replaced.
INSERT INTO common.hero_definitions (
	id, display_name, sort_order, starter_owned, max_level, max_ascension,
	base_attack, attack_per_level, attack_per_ascension,
	base_health, health_per_level, health_per_ascension
) VALUES (
	'hero_kael', 'Kael', 80, true, 100, 10,
	18, 5, 11, 150, 28, 70
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO common.summon_pool_definitions (
	banner_id, hero_id, shard_amount, rotation_order, reward_id
) VALUES (
	'hero_shard_standard', 'hero_kael', 1, 80, 'reward_summon_shards'
)
ON CONFLICT (banner_id, hero_id) DO NOTHING;
