CREATE TABLE IF NOT EXISTS common.hero_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	sort_order integer NOT NULL,
	starter_owned boolean NOT NULL DEFAULT true,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS common.reward_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	reward_type text NOT NULL,
	gold integer NOT NULL DEFAULT 0 CHECK (gold >= 0),
	gems integer NOT NULL DEFAULT 0 CHECK (gems >= 0),
	myth_essence integer NOT NULL DEFAULT 0 CHECK (myth_essence >= 0),
	pass_xp integer NOT NULL DEFAULT 0 CHECK (pass_xp >= 0),
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS common.campaign_curve_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	base_required_power integer NOT NULL,
	required_power_per_stage integer NOT NULL,
	base_myth_essence_reward integer NOT NULL,
	myth_essence_reward_per_stage integer NOT NULL,
	milestone_every_stages integer NOT NULL,
	milestone_base_gems integer NOT NULL,
	milestone_gems_per_stage integer NOT NULL,
	milestone_pass_xp integer NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS common.campaign_stage_definitions (
	id text PRIMARY KEY,
	campaign_id text NOT NULL REFERENCES common.campaign_curve_definitions(id),
	stage_number integer NOT NULL CHECK (stage_number > 0),
	display_name text NOT NULL,
	required_power integer NOT NULL CHECK (required_power >= 0),
	reward_id text NOT NULL REFERENCES common.reward_definitions(id),
	enemy_profile_id text NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now(),
	UNIQUE (campaign_id, stage_number)
);

CREATE TABLE IF NOT EXISTS common.progression_cost_definitions (
	id text PRIMARY KEY,
	domain text NOT NULL,
	target_id text NOT NULL,
	cost_currency_id text NOT NULL REFERENCES common.currency_definitions(id),
	base_amount integer NOT NULL CHECK (base_amount >= 0),
	amount_per_level integer NOT NULL DEFAULT 0 CHECK (amount_per_level >= 0),
	max_level integer CHECK (max_level IS NULL OR max_level >= 0),
	formula text NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS common.summon_banner_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	cost_currency_id text NOT NULL REFERENCES common.currency_definitions(id),
	cost_amount integer NOT NULL CHECK (cost_amount >= 0),
	resolution_mode text NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS common.summon_pool_definitions (
	banner_id text NOT NULL REFERENCES common.summon_banner_definitions(id) ON DELETE CASCADE,
	hero_id text NOT NULL REFERENCES common.hero_definitions(id),
	shard_amount integer NOT NULL CHECK (shard_amount > 0),
	rotation_order integer NOT NULL,
	reward_id text NOT NULL REFERENCES common.reward_definitions(id),
	created_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (banner_id, hero_id),
	UNIQUE (banner_id, rotation_order)
);

CREATE TABLE IF NOT EXISTS common.mission_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	reset_group text NOT NULL,
	progress_type text NOT NULL,
	target integer NOT NULL CHECK (target > 0),
	reward_id text NOT NULL REFERENCES common.reward_definitions(id),
	active boolean NOT NULL DEFAULT true,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS common.battle_pass_track_definitions (
	id text PRIMARY KEY,
	display_name text NOT NULL,
	required_pass_xp integer NOT NULL CHECK (required_pass_xp >= 0),
	reward_id text NOT NULL REFERENCES common.reward_definitions(id),
	sort_order integer NOT NULL,
	active boolean NOT NULL DEFAULT true,
	created_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO common.hero_definitions (id, display_name, sort_order, starter_owned) VALUES
	('hero_astra', 'Astra', 10, true),
	('hero_borin', 'Borin', 20, true),
	('hero_cyra', 'Cyra', 30, true),
	('hero_dante', 'Dante', 40, true),
	('hero_elowen', 'Elowen', 50, true)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	sort_order = EXCLUDED.sort_order,
	starter_owned = EXCLUDED.starter_owned;

INSERT INTO common.campaign_curve_definitions (
	id,
	display_name,
	base_required_power,
	required_power_per_stage,
	base_myth_essence_reward,
	myth_essence_reward_per_stage,
	milestone_every_stages,
	milestone_base_gems,
	milestone_gems_per_stage,
	milestone_pass_xp
) VALUES (
	'main_campaign',
	'Main Campaign',
	90,
	46,
	7,
	4,
	5,
	12,
	1,
	25
)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	base_required_power = EXCLUDED.base_required_power,
	required_power_per_stage = EXCLUDED.required_power_per_stage,
	base_myth_essence_reward = EXCLUDED.base_myth_essence_reward,
	myth_essence_reward_per_stage = EXCLUDED.myth_essence_reward_per_stage,
	milestone_every_stages = EXCLUDED.milestone_every_stages,
	milestone_base_gems = EXCLUDED.milestone_base_gems,
	milestone_gems_per_stage = EXCLUDED.milestone_gems_per_stage,
	milestone_pass_xp = EXCLUDED.milestone_pass_xp;

INSERT INTO common.reward_definitions (id, display_name, reward_type, gold, gems, myth_essence, pass_xp) VALUES
	('reward_summon_shards', 'Hero Shards', 'summon', 0, 0, 0, 0),
	('reward_gear_drop', 'Gear Dungeon Accessory Drop', 'gear_drop', 0, 0, 0, 0),
	('reward_daily_battles_15', 'Daily Battles Reward', 'daily_mission', 40, 5, 70, 40),
	('reward_daily_stage_clears_3', 'Daily Stage Clears Reward', 'daily_mission', 70, 10, 110, 40),
	('reward_daily_summon_1', 'Daily Summon Reward', 'daily_mission', 35, 20, 55, 40),
	('reward_mission_track_01', 'Mission Track Reward 01', 'battle_pass', 100, 10, 0, 0),
	('reward_mission_track_02', 'Mission Track Reward 02', 'battle_pass', 125, 15, 120, 0),
	('reward_mission_track_03', 'Mission Track Reward 03', 'battle_pass', 175, 20, 0, 0),
	('reward_mission_track_04', 'Mission Track Reward 04', 'battle_pass', 225, 25, 180, 0),
	('reward_mission_track_05', 'Mission Track Reward 05', 'battle_pass', 350, 40, 300, 0)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	reward_type = EXCLUDED.reward_type,
	gold = EXCLUDED.gold,
	gems = EXCLUDED.gems,
	myth_essence = EXCLUDED.myth_essence,
	pass_xp = EXCLUDED.pass_xp;

INSERT INTO common.reward_definitions (id, display_name, reward_type, gold, gems, myth_essence, pass_xp)
SELECT
	format('reward_campaign_stage_%s', lpad(stage_number::text, 3, '0')),
	format('Campaign Stage %s Reward', stage_number),
	'campaign_stage',
	0,
	CASE WHEN stage_number % 5 = 0 THEN 12 + stage_number ELSE 0 END,
	7 + (stage_number * 4),
	CASE WHEN stage_number % 5 = 0 THEN 25 ELSE 0 END
FROM generate_series(1, 60) AS stage(stage_number)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	reward_type = EXCLUDED.reward_type,
	gold = EXCLUDED.gold,
	gems = EXCLUDED.gems,
	myth_essence = EXCLUDED.myth_essence,
	pass_xp = EXCLUDED.pass_xp;

INSERT INTO common.campaign_stage_definitions (
	id,
	campaign_id,
	stage_number,
	display_name,
	required_power,
	reward_id,
	enemy_profile_id
)
SELECT
	format('campaign_stage_%s', lpad(stage_number::text, 3, '0')),
	'main_campaign',
	stage_number,
	format('Rift Echo %s', stage_number),
	90 + (stage_number * 46),
	format('reward_campaign_stage_%s', lpad(stage_number::text, 3, '0')),
	'rift_echo'
FROM generate_series(1, 60) AS stage(stage_number)
ON CONFLICT (id) DO UPDATE SET
	campaign_id = EXCLUDED.campaign_id,
	stage_number = EXCLUDED.stage_number,
	display_name = EXCLUDED.display_name,
	required_power = EXCLUDED.required_power,
	reward_id = EXCLUDED.reward_id,
	enemy_profile_id = EXCLUDED.enemy_profile_id;

INSERT INTO common.progression_cost_definitions (
	id,
	domain,
	target_id,
	cost_currency_id,
	base_amount,
	amount_per_level,
	max_level,
	formula
) VALUES
	('hero_level_any', 'hero', '*', 'myth_essence', 14, 6, NULL, 'base_amount + current_level * amount_per_level'),
	('hero_ascension_any', 'hero', '*', 'hero_shards', 20, 15, NULL, 'base_amount + current_ascension * amount_per_level'),
	('equipment_weapon_level', 'equipment', 'equipment_weapon', 'gold', 80, 35, NULL, 'base_amount + current_level * amount_per_level'),
	('equipment_armor_level', 'equipment', 'equipment_armor', 'gold', 75, 35, NULL, 'base_amount + current_level * amount_per_level'),
	('accessory_level_any', 'accessory', '*', 'gold', 35, 0, NULL, 'flat base_amount')
ON CONFLICT (id) DO UPDATE SET
	domain = EXCLUDED.domain,
	target_id = EXCLUDED.target_id,
	cost_currency_id = EXCLUDED.cost_currency_id,
	base_amount = EXCLUDED.base_amount,
	amount_per_level = EXCLUDED.amount_per_level,
	max_level = EXCLUDED.max_level,
	formula = EXCLUDED.formula;

INSERT INTO common.summon_banner_definitions (
	id,
	display_name,
	cost_currency_id,
	cost_amount,
	resolution_mode
) VALUES (
	'hero_shard_standard',
	'Standard Hero Shards',
	'gems',
	35,
	'deterministic_rotation'
)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	cost_currency_id = EXCLUDED.cost_currency_id,
	cost_amount = EXCLUDED.cost_amount,
	resolution_mode = EXCLUDED.resolution_mode;

INSERT INTO common.summon_pool_definitions (
	banner_id,
	hero_id,
	shard_amount,
	rotation_order,
	reward_id
) VALUES
	('hero_shard_standard', 'hero_astra', 7, 10, 'reward_summon_shards'),
	('hero_shard_standard', 'hero_borin', 7, 20, 'reward_summon_shards'),
	('hero_shard_standard', 'hero_cyra', 7, 30, 'reward_summon_shards'),
	('hero_shard_standard', 'hero_dante', 7, 40, 'reward_summon_shards'),
	('hero_shard_standard', 'hero_elowen', 7, 50, 'reward_summon_shards')
ON CONFLICT (banner_id, hero_id) DO UPDATE SET
	shard_amount = EXCLUDED.shard_amount,
	rotation_order = EXCLUDED.rotation_order,
	reward_id = EXCLUDED.reward_id;

INSERT INTO common.mission_definitions (
	id,
	display_name,
	reset_group,
	progress_type,
	target,
	reward_id,
	active
) VALUES
	('daily_battles_15', 'Battle 15 times', 'daily', 'fight', 15, 'reward_daily_battles_15', true),
	('daily_stage_clears_3', 'Clear 3 stages', 'daily', 'stage_clear', 3, 'reward_daily_stage_clears_3', true),
	('daily_summon_1', 'Summon 1 hero', 'daily', 'summon', 1, 'reward_daily_summon_1', true)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	reset_group = EXCLUDED.reset_group,
	progress_type = EXCLUDED.progress_type,
	target = EXCLUDED.target,
	reward_id = EXCLUDED.reward_id,
	active = EXCLUDED.active;

INSERT INTO common.battle_pass_track_definitions (
	id,
	display_name,
	required_pass_xp,
	reward_id,
	sort_order,
	active
) VALUES
	('mission_track_reward_01', 'Mission Track Reward 01', 40, 'reward_mission_track_01', 10, true),
	('mission_track_reward_02', 'Mission Track Reward 02', 80, 'reward_mission_track_02', 20, true),
	('mission_track_reward_03', 'Mission Track Reward 03', 120, 'reward_mission_track_03', 30, true),
	('mission_track_reward_04', 'Mission Track Reward 04', 180, 'reward_mission_track_04', 40, true),
	('mission_track_reward_05', 'Mission Track Reward 05', 240, 'reward_mission_track_05', 50, true)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	required_pass_xp = EXCLUDED.required_pass_xp,
	reward_id = EXCLUDED.reward_id,
	sort_order = EXCLUDED.sort_order,
	active = EXCLUDED.active;

CREATE OR REPLACE VIEW debug.v_common_reward_overview AS
SELECT
	id,
	display_name,
	reward_type,
	gold,
	gems,
	myth_essence,
	pass_xp,
	created_at
FROM common.reward_definitions
ORDER BY reward_type, id;

CREATE OR REPLACE VIEW debug.v_common_progression_cost_overview AS
SELECT
	id,
	domain,
	target_id,
	cost_currency_id,
	base_amount,
	amount_per_level,
	max_level,
	formula,
	created_at
FROM common.progression_cost_definitions
ORDER BY domain, target_id, id;

CREATE OR REPLACE VIEW debug.v_common_meta_definition_overview AS
SELECT
	'daily_mission' AS definition_type,
	m.id,
	m.display_name,
	m.reward_id,
	r.gold,
	r.gems,
	r.myth_essence,
	r.pass_xp
FROM common.mission_definitions m
JOIN common.reward_definitions r ON r.id = m.reward_id
UNION ALL
SELECT
	'battle_pass' AS definition_type,
	b.id,
	b.display_name,
	b.reward_id,
	r.gold,
	r.gems,
	r.myth_essence,
	r.pass_xp
FROM common.battle_pass_track_definitions b
JOIN common.reward_definitions r ON r.id = b.reward_id
ORDER BY definition_type, id;
