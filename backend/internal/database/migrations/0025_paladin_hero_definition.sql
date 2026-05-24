INSERT INTO common.hero_definitions (id, display_name, sort_order, starter_owned) VALUES
	('hero_paladin', 'Paladin', 60, true)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	sort_order = EXCLUDED.sort_order,
	starter_owned = EXCLUDED.starter_owned;

UPDATE common.hero_definitions
SET
	max_level = 100,
	max_ascension = 10,
	base_attack = 17,
	attack_per_level = 5,
	attack_per_ascension = 12,
	base_health = 210,
	health_per_level = 38,
	health_per_ascension = 74
WHERE id = 'hero_paladin';

UPDATE common.hero_definitions
SET sort_order = 70
WHERE id = 'hero_ravik';

UPDATE common.summon_pool_definitions
SET rotation_order = 70
WHERE banner_id = 'hero_shard_standard' AND hero_id = 'hero_ravik';

INSERT INTO common.summon_pool_definitions (
	banner_id,
	hero_id,
	shard_amount,
	rotation_order,
	reward_id
) VALUES
	('hero_shard_standard', 'hero_paladin', 1, 60, 'reward_summon_shards')
ON CONFLICT (banner_id, hero_id) DO UPDATE SET
	shard_amount = EXCLUDED.shard_amount,
	rotation_order = EXCLUDED.rotation_order,
	reward_id = EXCLUDED.reward_id;
