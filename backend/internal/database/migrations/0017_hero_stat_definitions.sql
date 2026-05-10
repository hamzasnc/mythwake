ALTER TABLE common.hero_definitions
	ADD COLUMN IF NOT EXISTS max_level integer NOT NULL DEFAULT 100 CHECK (max_level > 0),
	ADD COLUMN IF NOT EXISTS max_ascension integer NOT NULL DEFAULT 10 CHECK (max_ascension >= 0),
	ADD COLUMN IF NOT EXISTS base_attack integer NOT NULL DEFAULT 10 CHECK (base_attack > 0),
	ADD COLUMN IF NOT EXISTS attack_per_level integer NOT NULL DEFAULT 3 CHECK (attack_per_level >= 0),
	ADD COLUMN IF NOT EXISTS attack_per_ascension integer NOT NULL DEFAULT 8 CHECK (attack_per_ascension >= 0),
	ADD COLUMN IF NOT EXISTS base_health integer NOT NULL DEFAULT 100 CHECK (base_health > 0),
	ADD COLUMN IF NOT EXISTS health_per_level integer NOT NULL DEFAULT 20 CHECK (health_per_level >= 0),
	ADD COLUMN IF NOT EXISTS health_per_ascension integer NOT NULL DEFAULT 50 CHECK (health_per_ascension >= 0);

UPDATE common.hero_definitions
SET
	max_level = 100,
	max_ascension = 10,
	base_attack = seed.base_attack,
	attack_per_level = seed.attack_per_level,
	attack_per_ascension = seed.attack_per_ascension,
	base_health = seed.base_health,
	health_per_level = seed.health_per_level,
	health_per_ascension = seed.health_per_ascension
FROM (
	VALUES
		('hero_astra', 18, 5, 11, 150, 28, 70),
		('hero_borin', 10, 3, 8, 230, 42, 55),
		('hero_cyra', 22, 7, 11, 110, 20, 70),
		('hero_dante', 20, 6, 8, 125, 23, 55),
		('hero_elowen', 12, 4, 14, 165, 34, 90)
) AS seed(id, base_attack, attack_per_level, attack_per_ascension, base_health, health_per_level, health_per_ascension)
WHERE common.hero_definitions.id = seed.id;

CREATE OR REPLACE VIEW debug.v_common_hero_definition_overview AS
SELECT
	id,
	display_name,
	sort_order,
	starter_owned,
	max_level,
	max_ascension,
	base_attack,
	attack_per_level,
	attack_per_ascension,
	base_health,
	health_per_level,
	health_per_ascension,
	created_at
FROM common.hero_definitions
ORDER BY sort_order, id;
