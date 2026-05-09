CREATE TABLE IF NOT EXISTS player_combat_stats (
	player_id text PRIMARY KEY REFERENCES players(id) ON DELETE CASCADE,
	save_version integer NOT NULL,
	team_power integer NOT NULL,
	team_attack integer NOT NULL,
	team_health integer NOT NULL,
	updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS player_currencies (
	player_id text NOT NULL REFERENCES players(id) ON DELETE CASCADE,
	currency_id text NOT NULL REFERENCES currency_definitions(id),
	amount integer NOT NULL CHECK (amount >= 0),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, currency_id)
);

CREATE TABLE IF NOT EXISTS player_campaign_progress (
	player_id text PRIMARY KEY REFERENCES players(id) ON DELETE CASCADE,
	current_stage integer NOT NULL CHECK (current_stage >= 1),
	updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS player_dungeon_progress (
	player_id text NOT NULL REFERENCES players(id) ON DELETE CASCADE,
	dungeon_id text NOT NULL REFERENCES dungeon_definitions(id),
	current_floor integer NOT NULL CHECK (current_floor >= 1),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, dungeon_id)
);

CREATE TABLE IF NOT EXISTS player_heroes (
	player_id text NOT NULL REFERENCES players(id) ON DELETE CASCADE,
	hero_id text NOT NULL,
	level integer NOT NULL CHECK (level >= 1),
	ascension integer NOT NULL CHECK (ascension >= 0),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, hero_id)
);

CREATE TABLE IF NOT EXISTS player_hero_shards (
	player_id text NOT NULL REFERENCES players(id) ON DELETE CASCADE,
	hero_id text NOT NULL,
	shards integer NOT NULL CHECK (shards >= 0),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, hero_id)
);
