INSERT INTO common.hero_definitions (id, display_name, sort_order, starter_owned) VALUES
	('hero_ravik', 'Ravik', 60, true)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	sort_order = EXCLUDED.sort_order,
	starter_owned = EXCLUDED.starter_owned;

UPDATE common.hero_definitions
SET
	max_level = 100,
	max_ascension = 10,
	base_attack = 24,
	attack_per_level = 7,
	attack_per_ascension = 12,
	base_health = 118,
	health_per_level = 22,
	health_per_ascension = 70
WHERE id = 'hero_ravik';

UPDATE common.summon_pool_definitions
SET
	shard_amount = 1,
	rotation_order = CASE
		WHEN NOT EXISTS (
			SELECT 1
			FROM common.summon_pool_definitions occupied
			WHERE occupied.banner_id = 'hero_shard_standard'
				AND occupied.rotation_order = 60
				AND occupied.hero_id <> 'hero_ravik'
		) THEN 60
		ELSE rotation_order
	END,
	reward_id = 'reward_summon_shards'
WHERE banner_id = 'hero_shard_standard'
	AND hero_id = 'hero_ravik';

INSERT INTO common.summon_pool_definitions (
	banner_id,
	hero_id,
	shard_amount,
	rotation_order,
	reward_id
)
SELECT
	'hero_shard_standard',
	'hero_ravik',
	1,
	60,
	'reward_summon_shards'
WHERE NOT EXISTS (
	SELECT 1
	FROM common.summon_pool_definitions existing
	WHERE existing.banner_id = 'hero_shard_standard'
		AND (
			existing.hero_id = 'hero_ravik'
			OR existing.rotation_order = 60
		)
);
