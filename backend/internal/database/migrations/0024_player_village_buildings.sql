CREATE TABLE IF NOT EXISTS player.player_village_buildings (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	slot_index integer NOT NULL CHECK (slot_index >= 0 AND slot_index < 12),
	building_id text NOT NULL,
	building_option_index integer NOT NULL CHECK (building_option_index >= 0 AND building_option_index < 3),
	level integer NOT NULL CHECK (level >= 1),
	built_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, slot_index),
	UNIQUE (player_id, building_id)
);

CREATE OR REPLACE VIEW debug.v_player_village_overview AS
SELECT
	player_id,
	slot_index,
	building_id,
	building_option_index,
	level,
	built_at,
	updated_at
FROM player.player_village_buildings
ORDER BY player_id, slot_index;
