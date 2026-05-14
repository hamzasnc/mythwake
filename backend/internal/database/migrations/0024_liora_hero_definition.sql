INSERT INTO common.hero_definitions (id, display_name, sort_order, starter_owned) VALUES
	('hero_liora', 'Liora', 70, true)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	sort_order = EXCLUDED.sort_order,
	starter_owned = EXCLUDED.starter_owned;

UPDATE common.hero_definitions
SET
	max_level = 100,
	max_ascension = 10,
	base_attack = 21,
	attack_per_level = 6,
	attack_per_ascension = 13,
	base_health = 142,
	health_per_level = 27,
	health_per_ascension = 82
WHERE id = 'hero_liora';

INSERT INTO common.summon_shard_drops (
	banner_id,
	hero_id,
	shard_amount,
	rotation_order,
	reward_id
) VALUES
	('hero_shard_standard', 'hero_liora', 1, 70, 'reward_summon_shards')
ON CONFLICT (banner_id, hero_id) DO UPDATE SET
	shard_amount = EXCLUDED.shard_amount,
	rotation_order = EXCLUDED.rotation_order,
	reward_id = EXCLUDED.reward_id;
