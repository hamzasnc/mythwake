INSERT INTO common.reward_definitions (id, display_name, reward_type, gold, gems, myth_essence, pass_xp) VALUES
	('reward_afk_claim', 'AFK Gold and Myth Essence', 'afk', 0, 0, 0, 0)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	reward_type = EXCLUDED.reward_type,
	gold = EXCLUDED.gold,
	gems = EXCLUDED.gems,
	myth_essence = EXCLUDED.myth_essence,
	pass_xp = EXCLUDED.pass_xp;

CREATE TABLE IF NOT EXISTS common.afk_reward_definitions (
	id text PRIMARY KEY,
	reward_id text NOT NULL REFERENCES common.reward_definitions(id),
	display_name text NOT NULL,
	min_claim_seconds integer NOT NULL CHECK (min_claim_seconds > 0),
	max_claim_seconds integer NOT NULL CHECK (max_claim_seconds >= min_claim_seconds),
	tick_seconds integer NOT NULL CHECK (tick_seconds > 0),
	base_myth_essence_per_tick integer NOT NULL CHECK (base_myth_essence_per_tick > 0),
	myth_essence_per_stage integer NOT NULL CHECK (myth_essence_per_stage >= 0),
	gold_per_myth_essence_divisor integer NOT NULL CHECK (gold_per_myth_essence_divisor > 0),
	active boolean NOT NULL DEFAULT true,
	created_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO common.afk_reward_definitions (
	id,
	reward_id,
	display_name,
	min_claim_seconds,
	max_claim_seconds,
	tick_seconds,
	base_myth_essence_per_tick,
	myth_essence_per_stage,
	gold_per_myth_essence_divisor,
	active
) VALUES (
	'afk_default',
	'reward_afk_claim',
	'AFK Gold and Myth Essence',
	60,
	21600,
	60,
	3,
	1,
	2,
	true
)
ON CONFLICT (id) DO UPDATE SET
	reward_id = EXCLUDED.reward_id,
	display_name = EXCLUDED.display_name,
	min_claim_seconds = EXCLUDED.min_claim_seconds,
	max_claim_seconds = EXCLUDED.max_claim_seconds,
	tick_seconds = EXCLUDED.tick_seconds,
	base_myth_essence_per_tick = EXCLUDED.base_myth_essence_per_tick,
	myth_essence_per_stage = EXCLUDED.myth_essence_per_stage,
	gold_per_myth_essence_divisor = EXCLUDED.gold_per_myth_essence_divisor,
	active = EXCLUDED.active;

CREATE OR REPLACE VIEW debug.v_common_afk_reward_definition_overview AS
SELECT
	afk.id,
	afk.reward_id,
	afk.display_name,
	afk.min_claim_seconds,
	afk.max_claim_seconds,
	afk.tick_seconds,
	afk.base_myth_essence_per_tick,
	afk.myth_essence_per_stage,
	afk.gold_per_myth_essence_divisor,
	afk.active,
	afk.created_at
FROM common.afk_reward_definitions afk
ORDER BY afk.id;
