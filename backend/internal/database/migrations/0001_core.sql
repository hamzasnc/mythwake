CREATE TABLE IF NOT EXISTS players (
	id text PRIMARY KEY,
	display_name text NOT NULL DEFAULT 'Guest',
	created_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS player_state_snapshots (
	player_id text PRIMARY KEY REFERENCES players(id) ON DELETE CASCADE,
	state jsonb NOT NULL,
	updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS currency_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	is_premium boolean NOT NULL DEFAULT false,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS dungeon_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	reward_currency_id text REFERENCES currency_definitions(id),
	base_required_power integer NOT NULL,
	required_power_per_floor integer NOT NULL,
	base_reward_amount integer NOT NULL,
	reward_per_floor integer NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS accessory_slot_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	sort_order integer NOT NULL
);

CREATE TABLE IF NOT EXISTS accessory_rarity_definitions (
	id text PRIMARY KEY,
	rarity_index integer NOT NULL UNIQUE,
	display_name text NOT NULL,
	max_level integer NOT NULL,
	fuse_copy_cost integer NOT NULL DEFAULT 3
);

CREATE TABLE IF NOT EXISTS accessory_definitions (
	id text PRIMARY KEY,
	slot_id text NOT NULL REFERENCES accessory_slot_definitions(id),
	rarity_id text NOT NULL REFERENCES accessory_rarity_definitions(id),
	attack_per_level integer NOT NULL,
	health_per_level integer NOT NULL,
	drop_weight integer NOT NULL,
	fuse_target_id text,
	UNIQUE (slot_id, rarity_id)
);

CREATE TABLE IF NOT EXISTS economy_transactions (
	id bigserial PRIMARY KEY,
	player_id text NOT NULL REFERENCES players(id) ON DELETE CASCADE,
	action_id text NOT NULL,
	reward_id text,
	gold_delta integer NOT NULL DEFAULT 0,
	gems_delta integer NOT NULL DEFAULT 0,
	myth_essence_delta integer NOT NULL DEFAULT 0,
	pass_xp_delta integer NOT NULL DEFAULT 0,
	created_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO currency_definitions (id, display_name, is_premium) VALUES
	('gold', 'Gold', false),
	('gems', 'Gems', true),
	('myth_essence', 'Myth Essence', false),
	('pass_xp', 'Pass XP', false),
	('hero_shards', 'Hero Shards', false)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	is_premium = EXCLUDED.is_premium;

INSERT INTO dungeon_definitions (
	id,
	display_name,
	reward_currency_id,
	base_required_power,
	required_power_per_floor,
	base_reward_amount,
	reward_per_floor
) VALUES
	('gold_dungeon', 'Gold Dungeon', 'gold', 100, 50, 95, 34),
	('essence_dungeon', 'Essence Dungeon', 'myth_essence', 100, 50, 110, 40),
	('gear_dungeon', 'Gear Dungeon', NULL, 120, 56, 0, 0)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	reward_currency_id = EXCLUDED.reward_currency_id,
	base_required_power = EXCLUDED.base_required_power,
	required_power_per_floor = EXCLUDED.required_power_per_floor,
	base_reward_amount = EXCLUDED.base_reward_amount,
	reward_per_floor = EXCLUDED.reward_per_floor;

INSERT INTO accessory_slot_definitions (id, display_name, sort_order) VALUES
	('earrings', 'Ohrringe', 10),
	('necklace', 'Kette', 20),
	('bracelet', 'Armband', 30),
	('gloves', 'Handschuhe', 40),
	('shoes', 'Schuhe', 50)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	sort_order = EXCLUDED.sort_order;

INSERT INTO accessory_rarity_definitions (id, rarity_index, display_name, max_level, fuse_copy_cost) VALUES
	('r0', 0, 'R0', 20, 3),
	('r1', 1, 'R1', 30, 3),
	('r2', 2, 'R2', 40, 3),
	('r3', 3, 'R3', 50, 3),
	('r4', 4, 'R4', 60, 3)
ON CONFLICT (id) DO UPDATE SET
	rarity_index = EXCLUDED.rarity_index,
	display_name = EXCLUDED.display_name,
	max_level = EXCLUDED.max_level,
	fuse_copy_cost = EXCLUDED.fuse_copy_cost;

INSERT INTO accessory_definitions (
	id,
	slot_id,
	rarity_id,
	attack_per_level,
	health_per_level,
	drop_weight,
	fuse_target_id
) VALUES
	('accessory_earrings_r0', 'earrings', 'r0', 2, 8, 120, 'accessory_earrings_r1'),
	('accessory_earrings_r1', 'earrings', 'r1', 4, 16, 55, 'accessory_earrings_r2'),
	('accessory_earrings_r2', 'earrings', 'r2', 7, 28, 22, 'accessory_earrings_r3'),
	('accessory_earrings_r3', 'earrings', 'r3', 11, 44, 8, 'accessory_earrings_r4'),
	('accessory_earrings_r4', 'earrings', 'r4', 16, 64, 2, NULL),
	('accessory_necklace_r0', 'necklace', 'r0', 1, 12, 120, 'accessory_necklace_r1'),
	('accessory_necklace_r1', 'necklace', 'r1', 3, 24, 55, 'accessory_necklace_r2'),
	('accessory_necklace_r2', 'necklace', 'r2', 5, 40, 22, 'accessory_necklace_r3'),
	('accessory_necklace_r3', 'necklace', 'r3', 8, 64, 8, 'accessory_necklace_r4'),
	('accessory_necklace_r4', 'necklace', 'r4', 12, 92, 2, NULL),
	('accessory_bracelet_r0', 'bracelet', 'r0', 2, 10, 120, 'accessory_bracelet_r1'),
	('accessory_bracelet_r1', 'bracelet', 'r1', 4, 20, 55, 'accessory_bracelet_r2'),
	('accessory_bracelet_r2', 'bracelet', 'r2', 6, 34, 22, 'accessory_bracelet_r3'),
	('accessory_bracelet_r3', 'bracelet', 'r3', 10, 54, 8, 'accessory_bracelet_r4'),
	('accessory_bracelet_r4', 'bracelet', 'r4', 14, 78, 2, NULL),
	('accessory_gloves_r0', 'gloves', 'r0', 3, 6, 120, 'accessory_gloves_r1'),
	('accessory_gloves_r1', 'gloves', 'r1', 5, 12, 55, 'accessory_gloves_r2'),
	('accessory_gloves_r2', 'gloves', 'r2', 9, 22, 22, 'accessory_gloves_r3'),
	('accessory_gloves_r3', 'gloves', 'r3', 13, 36, 8, 'accessory_gloves_r4'),
	('accessory_gloves_r4', 'gloves', 'r4', 18, 52, 2, NULL),
	('accessory_shoes_r0', 'shoes', 'r0', 1, 14, 120, 'accessory_shoes_r1'),
	('accessory_shoes_r1', 'shoes', 'r1', 3, 26, 55, 'accessory_shoes_r2'),
	('accessory_shoes_r2', 'shoes', 'r2', 5, 44, 22, 'accessory_shoes_r3'),
	('accessory_shoes_r3', 'shoes', 'r3', 8, 70, 8, 'accessory_shoes_r4'),
	('accessory_shoes_r4', 'shoes', 'r4', 12, 100, 2, NULL)
ON CONFLICT (id) DO UPDATE SET
	slot_id = EXCLUDED.slot_id,
	rarity_id = EXCLUDED.rarity_id,
	attack_per_level = EXCLUDED.attack_per_level,
	health_per_level = EXCLUDED.health_per_level,
	drop_weight = EXCLUDED.drop_weight,
	fuse_target_id = EXCLUDED.fuse_target_id;
