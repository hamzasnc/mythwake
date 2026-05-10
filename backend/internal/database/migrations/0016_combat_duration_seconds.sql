ALTER TABLE common.dungeon_definitions
	ADD COLUMN IF NOT EXISTS max_combat_seconds integer NOT NULL DEFAULT 30 CHECK (max_combat_seconds > 0);

ALTER TABLE common.campaign_curve_definitions
	ADD COLUMN IF NOT EXISTS max_combat_seconds integer NOT NULL DEFAULT 30 CHECK (max_combat_seconds > 0);

ALTER TABLE common.campaign_stage_definitions
	ADD COLUMN IF NOT EXISTS max_combat_seconds integer NOT NULL DEFAULT 30 CHECK (max_combat_seconds > 0);

UPDATE common.dungeon_definitions
SET max_combat_seconds = 30
WHERE id IN ('gold_dungeon', 'essence_dungeon', 'gear_dungeon');

UPDATE common.campaign_curve_definitions
SET max_combat_seconds = 30
WHERE id = 'main_campaign';

UPDATE common.campaign_stage_definitions
SET max_combat_seconds = 30
WHERE campaign_id = 'main_campaign';

DROP VIEW IF EXISTS debug.v_common_combat_definition_overview;

ALTER TABLE common.dungeon_definitions
	DROP COLUMN IF EXISTS max_combat_rounds;

ALTER TABLE common.campaign_curve_definitions
	DROP COLUMN IF EXISTS max_combat_rounds;

ALTER TABLE common.campaign_stage_definitions
	DROP COLUMN IF EXISTS max_combat_rounds;

CREATE OR REPLACE VIEW debug.v_common_combat_definition_overview AS
SELECT
	'campaign_stage' AS combat_type,
	stage.id,
	stage.display_name,
	stage.stage_number AS level,
	stage.required_power,
	stage.enemy_max_hp,
	stage.enemy_damage,
	stage.max_combat_seconds,
	NULL::text AS reward_currency_id,
	reward.gold AS gold_reward,
	reward.myth_essence AS myth_essence_reward,
	reward.gems AS gems_reward,
	reward.pass_xp AS pass_xp_reward
FROM common.campaign_stage_definitions stage
JOIN common.reward_definitions reward ON reward.id = stage.reward_id
UNION ALL
SELECT
	'dungeon' AS combat_type,
	dungeon.id,
	dungeon.display_name,
	1 AS level,
	dungeon.base_required_power + dungeon.required_power_per_floor AS required_power,
	dungeon.enemy_base_hp + ((dungeon.base_required_power + dungeon.required_power_per_floor) * dungeon.enemy_hp_per_power) + dungeon.enemy_hp_per_floor AS enemy_max_hp,
	dungeon.enemy_base_damage + dungeon.enemy_damage_per_floor + ((dungeon.base_required_power + dungeon.required_power_per_floor) / dungeon.enemy_damage_power_divisor) AS enemy_damage,
	dungeon.max_combat_seconds,
	dungeon.reward_currency_id,
	CASE WHEN dungeon.reward_currency_id = 'gold' THEN dungeon.base_reward_amount + dungeon.reward_per_floor ELSE 0 END AS gold_reward,
	CASE WHEN dungeon.reward_currency_id = 'myth_essence' THEN dungeon.base_reward_amount + dungeon.reward_per_floor ELSE 0 END AS myth_essence_reward,
	0 AS gems_reward,
	0 AS pass_xp_reward
FROM common.dungeon_definitions dungeon
ORDER BY combat_type, level, id;
