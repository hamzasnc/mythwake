CREATE TABLE IF NOT EXISTS common.equipment_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	sort_order integer NOT NULL,
	starter_owned boolean NOT NULL DEFAULT true,
	max_level integer NOT NULL DEFAULT 100 CHECK (max_level > 0),
	attack_per_level integer NOT NULL DEFAULT 0 CHECK (attack_per_level >= 0),
	health_per_level integer NOT NULL DEFAULT 0 CHECK (health_per_level >= 0),
	created_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO common.equipment_definitions (
	id,
	display_name,
	sort_order,
	starter_owned,
	max_level,
	attack_per_level,
	health_per_level
) VALUES
	('equipment_weapon', 'Weapon', 10, true, 100, 7, 0),
	('equipment_armor', 'Armor', 20, true, 100, 0, 65)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	sort_order = EXCLUDED.sort_order,
	starter_owned = EXCLUDED.starter_owned,
	max_level = EXCLUDED.max_level,
	attack_per_level = EXCLUDED.attack_per_level,
	health_per_level = EXCLUDED.health_per_level;

CREATE OR REPLACE VIEW debug.v_common_equipment_definition_overview AS
SELECT
	id,
	display_name,
	sort_order,
	starter_owned,
	max_level,
	attack_per_level,
	health_per_level,
	created_at
FROM common.equipment_definitions
ORDER BY sort_order, id;
