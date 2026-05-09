CREATE SCHEMA IF NOT EXISTS account;
CREATE SCHEMA IF NOT EXISTS common;
CREATE SCHEMA IF NOT EXISTS player;
CREATE SCHEMA IF NOT EXISTS logs;
CREATE SCHEMA IF NOT EXISTS debug;

DROP VIEW IF EXISTS public.v_player_economy_overview;
DROP VIEW IF EXISTS public.v_player_hero_overview;
DROP VIEW IF EXISTS public.v_player_overview;
DROP VIEW IF EXISTS debug.v_player_economy_overview;
DROP VIEW IF EXISTS debug.v_player_hero_overview;
DROP VIEW IF EXISTS debug.v_player_overview;

ALTER TABLE IF EXISTS public.players SET SCHEMA account;

ALTER TABLE IF EXISTS public.currency_definitions SET SCHEMA common;
ALTER TABLE IF EXISTS public.dungeon_definitions SET SCHEMA common;
ALTER TABLE IF EXISTS public.accessory_slot_definitions SET SCHEMA common;
ALTER TABLE IF EXISTS public.accessory_rarity_definitions SET SCHEMA common;
ALTER TABLE IF EXISTS public.accessory_definitions SET SCHEMA common;

ALTER TABLE IF EXISTS public.player_state_snapshots SET SCHEMA player;
ALTER TABLE IF EXISTS public.player_combat_stats SET SCHEMA player;
ALTER TABLE IF EXISTS public.player_currencies SET SCHEMA player;
ALTER TABLE IF EXISTS public.player_campaign_progress SET SCHEMA player;
ALTER TABLE IF EXISTS public.player_dungeon_progress SET SCHEMA player;
ALTER TABLE IF EXISTS public.player_heroes SET SCHEMA player;
ALTER TABLE IF EXISTS public.player_hero_shards SET SCHEMA player;

ALTER TABLE IF EXISTS public.economy_transactions SET SCHEMA logs;

DROP TABLE IF EXISTS public.schema_migrations;

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
	COALESCE(stats.team_power, 0) AS team_power,
	COALESCE(stats.team_attack, 0) AS team_attack,
	COALESCE(stats.team_health, 0) AS team_health,
	p.created_at,
	p.updated_at
FROM account.players p
LEFT JOIN player.player_currencies pc ON pc.player_id = p.id
LEFT JOIN player.player_campaign_progress campaign ON campaign.player_id = p.id
LEFT JOIN player.player_dungeon_progress pd ON pd.player_id = p.id
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

CREATE OR REPLACE VIEW debug.v_player_hero_overview AS
SELECT
	h.player_id,
	h.hero_id,
	h.level,
	h.ascension,
	COALESCE(s.shards, 0) AS shards,
	h.updated_at
FROM player.player_heroes h
LEFT JOIN player.player_hero_shards s
	ON s.player_id = h.player_id
	AND s.hero_id = h.hero_id;

CREATE OR REPLACE VIEW debug.v_player_economy_overview AS
SELECT
	t.id,
	t.player_id,
	t.action_id,
	t.reward_id,
	t.gold_delta,
	t.gems_delta,
	t.myth_essence_delta,
	t.pass_xp_delta,
	t.created_at
FROM logs.economy_transactions t
ORDER BY t.id DESC;
