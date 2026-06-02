INSERT INTO common.currency_definitions (id, display_name, is_premium) VALUES
	('awakening_shards', 'Awakening Shards', false)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	is_premium = EXCLUDED.is_premium;

INSERT INTO common.reward_definitions (id, display_name, reward_type, gold, gems, myth_essence, pass_xp) VALUES
	('reward_shard_rift_run', 'Shard Rift Run', 'dungeon', 0, 0, 0, 0),
	('reward_hero_shard_chest', 'Hero Shard Chest', 'chest', 0, 0, 0, 0)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	reward_type = EXCLUDED.reward_type,
	gold = EXCLUDED.gold,
	gems = EXCLUDED.gems,
	myth_essence = EXCLUDED.myth_essence,
	pass_xp = EXCLUDED.pass_xp;

INSERT INTO common.dungeon_definitions (
	id,
	display_name,
	reward_currency_id,
	base_required_power,
	required_power_per_floor,
	base_reward_amount,
	reward_per_floor,
	enemy_base_hp,
	enemy_hp_per_power,
	enemy_hp_per_floor,
	enemy_base_damage,
	enemy_damage_per_floor,
	enemy_damage_power_divisor,
	max_combat_seconds
) VALUES (
	'shard_rift',
	'Shard Rift',
	'awakening_shards',
	125,
	36,
	2,
	1,
	150,
	1,
	55,
	16,
	2,
	60,
	30
)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	reward_currency_id = EXCLUDED.reward_currency_id,
	base_required_power = EXCLUDED.base_required_power,
	required_power_per_floor = EXCLUDED.required_power_per_floor,
	base_reward_amount = EXCLUDED.base_reward_amount,
	reward_per_floor = EXCLUDED.reward_per_floor,
	enemy_base_hp = EXCLUDED.enemy_base_hp,
	enemy_hp_per_power = EXCLUDED.enemy_hp_per_power,
	enemy_hp_per_floor = EXCLUDED.enemy_hp_per_floor,
	enemy_base_damage = EXCLUDED.enemy_base_damage,
	enemy_damage_per_floor = EXCLUDED.enemy_damage_per_floor,
	enemy_damage_power_divisor = EXCLUDED.enemy_damage_power_divisor,
	max_combat_seconds = EXCLUDED.max_combat_seconds;

ALTER TABLE player.player_heroes
	ADD COLUMN IF NOT EXISTS star_level integer NOT NULL DEFAULT 0;

DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1
		FROM pg_constraint
		WHERE conname = 'player_heroes_star_level_check'
	) THEN
		ALTER TABLE player.player_heroes
			ADD CONSTRAINT player_heroes_star_level_check CHECK (star_level >= 0 AND star_level <= 5);
	END IF;
END $$;

CREATE TABLE IF NOT EXISTS player.player_inventory_items (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	item_id text NOT NULL,
	quantity integer NOT NULL CHECK (quantity >= 0),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, item_id)
);

CREATE TABLE IF NOT EXISTS player.player_shard_rift_progress (
	player_id text PRIMARY KEY REFERENCES account.players(id) ON DELETE CASCADE,
	best_enemies_defeated integer NOT NULL DEFAULT 0 CHECK (best_enemies_defeated >= 0),
	total_enemies_defeated integer NOT NULL DEFAULT 0 CHECK (total_enemies_defeated >= 0),
	updated_at timestamptz NOT NULL DEFAULT now()
);

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
	('hero_ascension_any', 'hero', '*', 'awakening_shards', 20, 15, NULL, 'base_amount + current_ascension * amount_per_level'),
	('hero_star_any', 'hero_star', '*', 'hero_shards', 5, 5, 5, 'base_amount + current_star * amount_per_level')
ON CONFLICT (id) DO UPDATE SET
	domain = EXCLUDED.domain,
	target_id = EXCLUDED.target_id,
	cost_currency_id = EXCLUDED.cost_currency_id,
	base_amount = EXCLUDED.base_amount,
	amount_per_level = EXCLUDED.amount_per_level,
	max_level = EXCLUDED.max_level,
	formula = EXCLUDED.formula;

CREATE OR REPLACE VIEW debug.v_player_hero_overview AS
SELECT
	h.player_id,
	h.hero_id,
	h.level,
	h.ascension,
	COALESCE(s.shards, 0) AS shards,
	h.updated_at,
	h.star_level
FROM player.player_heroes h
LEFT JOIN player.player_hero_shards s
	ON s.player_id = h.player_id
	AND s.hero_id = h.hero_id;

CREATE OR REPLACE VIEW debug.v_player_shard_rift_overview AS
SELECT
	p.id AS player_id,
	COALESCE(c.amount, 0) AS awakening_shards,
	COALESCE(i.quantity, 0) AS hero_shard_chests,
	COALESCE(r.best_enemies_defeated, 0) AS best_enemies_defeated,
	COALESCE(r.total_enemies_defeated, 0) AS total_enemies_defeated,
	COALESCE(r.updated_at, p.updated_at) AS updated_at
FROM account.players p
LEFT JOIN player.player_currencies c
	ON c.player_id = p.id
	AND c.currency_id = 'awakening_shards'
LEFT JOIN player.player_inventory_items i
	ON i.player_id = p.id
	AND i.item_id = 'hero_shard_chest'
LEFT JOIN player.player_shard_rift_progress r
	ON r.player_id = p.id;

CREATE OR REPLACE VIEW debug.v_player_overview AS
SELECT
	p.id AS player_id,
	p.display_name,
	COALESCE(MAX(pc.amount) FILTER (WHERE pc.currency_id = 'gold'), 0) AS gold,
	COALESCE(MAX(pc.amount) FILTER (WHERE pc.currency_id = 'gems'), 0) AS gems,
	COALESCE(MAX(pc.amount) FILTER (WHERE pc.currency_id = 'myth_essence'), 0) AS myth_essence,
	COALESCE(MAX(pc.amount) FILTER (WHERE pc.currency_id = 'pass_xp'), 0) AS pass_xp,
	COALESCE(campaign.current_stage, 1) AS campaign_stage,
	COALESCE(MAX(pd.current_floor) FILTER (WHERE pd.dungeon_id = 'gold_dungeon'), 1) AS gold_dungeon_floor,
	COALESCE(MAX(pd.current_floor) FILTER (WHERE pd.dungeon_id = 'essence_dungeon'), 1) AS essence_dungeon_floor,
	COALESCE(MAX(pd.current_floor) FILTER (WHERE pd.dungeon_id = 'gear_dungeon'), 1) AS gear_dungeon_floor,
	COALESCE(MAX(eq.level) FILTER (WHERE eq.equipment_id = 'equipment_weapon'), 0) AS weapon_level,
	COALESCE(MAX(eq.level) FILTER (WHERE eq.equipment_id = 'equipment_armor'), 0) AS armor_level,
	COALESCE(stats.team_power, 0) AS team_power,
	COALESCE(stats.team_attack, 0) AS team_attack,
	COALESCE(stats.team_health, 0) AS team_health,
	p.created_at,
	p.updated_at,
	COALESCE(MAX(pc.amount) FILTER (WHERE pc.currency_id = 'awakening_shards'), 0) AS awakening_shards,
	COALESCE(r.best_enemies_defeated, 0) AS shard_rift_best,
	COALESCE(r.total_enemies_defeated, 0) AS shard_rift_total
FROM account.players p
LEFT JOIN player.player_currencies pc ON pc.player_id = p.id
LEFT JOIN player.player_campaign_progress campaign ON campaign.player_id = p.id
LEFT JOIN player.player_dungeon_progress pd ON pd.player_id = p.id
LEFT JOIN player.player_equipment_training eq ON eq.player_id = p.id
LEFT JOIN player.player_shard_rift_progress r ON r.player_id = p.id
LEFT JOIN player.player_combat_stats stats ON stats.player_id = p.id
GROUP BY
	p.id,
	p.display_name,
	campaign.current_stage,
	r.best_enemies_defeated,
	r.total_enemies_defeated,
	stats.team_power,
	stats.team_attack,
	stats.team_health,
	p.created_at,
	p.updated_at;
