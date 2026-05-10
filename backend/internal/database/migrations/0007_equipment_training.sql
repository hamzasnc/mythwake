CREATE TABLE IF NOT EXISTS player.player_equipment_training (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	equipment_id text NOT NULL,
	level integer NOT NULL CHECK (level >= 0),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, equipment_id)
);

CREATE OR REPLACE VIEW debug.v_player_equipment_overview AS
SELECT
	player_id,
	equipment_id,
	level,
	updated_at
FROM player.player_equipment_training
ORDER BY player_id, equipment_id;

DROP VIEW IF EXISTS debug.v_player_overview;

CREATE VIEW debug.v_player_overview AS
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
	p.updated_at
FROM account.players p
LEFT JOIN player.player_currencies pc ON pc.player_id = p.id
LEFT JOIN player.player_campaign_progress campaign ON campaign.player_id = p.id
LEFT JOIN player.player_dungeon_progress pd ON pd.player_id = p.id
LEFT JOIN player.player_equipment_training eq ON eq.player_id = p.id
LEFT JOIN player.player_combat_stats stats ON stats.player_id = p.id
GROUP BY
	p.id,
	p.display_name,
	campaign.current_stage,
	stats.team_power,
	stats.team_attack,
	stats.team_health,
	p.created_at,
	p.updated_at;
