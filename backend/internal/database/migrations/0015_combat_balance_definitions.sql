ALTER TABLE common.dungeon_definitions
	ADD COLUMN IF NOT EXISTS enemy_base_hp integer NOT NULL DEFAULT 220 CHECK (enemy_base_hp > 0),
	ADD COLUMN IF NOT EXISTS enemy_hp_per_power integer NOT NULL DEFAULT 2 CHECK (enemy_hp_per_power >= 0),
	ADD COLUMN IF NOT EXISTS enemy_hp_per_floor integer NOT NULL DEFAULT 95 CHECK (enemy_hp_per_floor >= 0),
	ADD COLUMN IF NOT EXISTS enemy_base_damage integer NOT NULL DEFAULT 26 CHECK (enemy_base_damage > 0),
	ADD COLUMN IF NOT EXISTS enemy_damage_per_floor integer NOT NULL DEFAULT 3 CHECK (enemy_damage_per_floor >= 0),
	ADD COLUMN IF NOT EXISTS enemy_damage_power_divisor integer NOT NULL DEFAULT 48 CHECK (enemy_damage_power_divisor > 0),
	ADD COLUMN IF NOT EXISTS max_combat_rounds integer NOT NULL DEFAULT 45 CHECK (max_combat_rounds > 0);

ALTER TABLE common.campaign_curve_definitions
	ADD COLUMN IF NOT EXISTS enemy_base_hp integer NOT NULL DEFAULT 180 CHECK (enemy_base_hp > 0),
	ADD COLUMN IF NOT EXISTS enemy_hp_per_power integer NOT NULL DEFAULT 2 CHECK (enemy_hp_per_power >= 0),
	ADD COLUMN IF NOT EXISTS enemy_hp_per_stage_squared integer NOT NULL DEFAULT 6 CHECK (enemy_hp_per_stage_squared >= 0),
	ADD COLUMN IF NOT EXISTS enemy_base_damage integer NOT NULL DEFAULT 18 CHECK (enemy_base_damage > 0),
	ADD COLUMN IF NOT EXISTS enemy_damage_per_stage integer NOT NULL DEFAULT 4 CHECK (enemy_damage_per_stage >= 0),
	ADD COLUMN IF NOT EXISTS enemy_damage_power_divisor integer NOT NULL DEFAULT 50 CHECK (enemy_damage_power_divisor > 0),
	ADD COLUMN IF NOT EXISTS max_combat_rounds integer NOT NULL DEFAULT 45 CHECK (max_combat_rounds > 0);

ALTER TABLE common.campaign_stage_definitions
	ADD COLUMN IF NOT EXISTS enemy_max_hp integer NOT NULL DEFAULT 1 CHECK (enemy_max_hp > 0),
	ADD COLUMN IF NOT EXISTS enemy_damage integer NOT NULL DEFAULT 1 CHECK (enemy_damage > 0),
	ADD COLUMN IF NOT EXISTS max_combat_rounds integer NOT NULL DEFAULT 45 CHECK (max_combat_rounds > 0);

UPDATE common.dungeon_definitions
SET
	enemy_base_hp = 220,
	enemy_hp_per_power = 2,
	enemy_hp_per_floor = 95,
	enemy_base_damage = 26,
	enemy_damage_per_floor = CASE WHEN id = 'gear_dungeon' THEN 4 ELSE 3 END,
	enemy_damage_power_divisor = 48,
	max_combat_rounds = 45
WHERE id IN ('gold_dungeon', 'essence_dungeon', 'gear_dungeon');

UPDATE common.campaign_curve_definitions
SET
	enemy_base_hp = 180,
	enemy_hp_per_power = 2,
	enemy_hp_per_stage_squared = 6,
	enemy_base_damage = 18,
	enemy_damage_per_stage = 4,
	enemy_damage_power_divisor = 50,
	max_combat_rounds = 45
WHERE id = 'main_campaign';

UPDATE common.campaign_stage_definitions
SET
	enemy_max_hp = 180 + ((90 + (stage_number * 46)) * 2) + (stage_number * stage_number * 6),
	enemy_damage = 18 + (stage_number * 4) + ((90 + (stage_number * 46)) / 50),
	max_combat_rounds = 45
WHERE campaign_id = 'main_campaign';

CREATE OR REPLACE VIEW debug.v_common_combat_definition_overview AS
SELECT
	'campaign_stage' AS combat_type,
	stage.id,
	stage.display_name,
	stage.stage_number AS level,
	stage.required_power,
	stage.enemy_max_hp,
	stage.enemy_damage,
	stage.max_combat_rounds,
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
	dungeon.max_combat_rounds,
	dungeon.reward_currency_id,
	CASE WHEN dungeon.reward_currency_id = 'gold' THEN dungeon.base_reward_amount + dungeon.reward_per_floor ELSE 0 END AS gold_reward,
	CASE WHEN dungeon.reward_currency_id = 'myth_essence' THEN dungeon.base_reward_amount + dungeon.reward_per_floor ELSE 0 END AS myth_essence_reward,
	0 AS gems_reward,
	0 AS pass_xp_reward
FROM common.dungeon_definitions dungeon
ORDER BY combat_type, level, id;
