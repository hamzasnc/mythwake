CREATE TABLE IF NOT EXISTS player.player_accessory_inventory (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	accessory_id text NOT NULL REFERENCES common.accessory_definitions(id),
	copies integer NOT NULL CHECK (copies >= 0),
	level integer NOT NULL CHECK (level >= 0),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, accessory_id)
);

CREATE TABLE IF NOT EXISTS player.player_equipped_accessories (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	slot_id text NOT NULL REFERENCES common.accessory_slot_definitions(id),
	accessory_id text NOT NULL REFERENCES common.accessory_definitions(id),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, slot_id)
);

CREATE OR REPLACE VIEW debug.v_player_accessory_overview AS
SELECT
	i.player_id,
	i.accessory_id,
	def.slot_id,
	def.rarity_id,
	i.copies,
	i.level,
	(equipped.accessory_id IS NOT NULL) AS equipped,
	i.updated_at
FROM player.player_accessory_inventory i
JOIN common.accessory_definitions def ON def.id = i.accessory_id
LEFT JOIN player.player_equipped_accessories equipped
	ON equipped.player_id = i.player_id
	AND equipped.accessory_id = i.accessory_id
ORDER BY i.player_id, def.slot_id, def.rarity_id;
