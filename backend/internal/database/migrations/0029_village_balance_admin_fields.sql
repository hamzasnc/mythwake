ALTER TABLE common.village_building_definitions
	ADD COLUMN IF NOT EXISTS bonus_label text NOT NULL DEFAULT '',
	ADD COLUMN IF NOT EXISTS bonus_curve text NOT NULL DEFAULT 'linear_per_level',
	ADD COLUMN IF NOT EXISTS upgrade_cost_formula text NOT NULL DEFAULT 'current_level * upgrade_cost_per_level',
	ADD COLUMN IF NOT EXISTS mode_compatibility text NOT NULL DEFAULT 'local_and_server';

UPDATE common.village_building_definitions
SET
	bonus_label = CASE bonus_type
		WHEN 'team_attack' THEN 'Team ATK'
		WHEN 'team_health' THEN 'Team HP'
		WHEN 'afk_gold_rate' THEN 'Gold/s Fast Rewards'
		WHEN 'afk_essence_rate' THEN 'Essence/s Fast Rewards'
		ELSE 'Village bonus'
	END,
	bonus_curve = 'linear_per_level',
	upgrade_cost_formula = 'current_level * upgrade_cost_per_level',
	mode_compatibility = 'local_and_server'
WHERE bonus_label = ''
	OR bonus_curve = ''
	OR upgrade_cost_formula = ''
	OR mode_compatibility = '';

CREATE OR REPLACE VIEW debug.v_common_village_building_balance AS
SELECT
	id,
	slot_index,
	building_option_index,
	display_name,
	texture_name,
	build_cost,
	max_level,
	upgrade_cost_per_level,
	upgrade_cost_formula,
	bonus_label,
	bonus_type,
	bonus_curve,
	bonus_value_per_level,
	ROUND((max_level * bonus_value_per_level)::numeric, 4) AS bonus_cap_value,
	mode_compatibility,
	active
FROM common.village_building_definitions
ORDER BY slot_index, building_option_index, id;
