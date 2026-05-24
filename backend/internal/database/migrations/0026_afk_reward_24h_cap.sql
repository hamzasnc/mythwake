UPDATE common.afk_reward_definitions
SET max_claim_seconds = 86400
WHERE id = 'afk_default';

CREATE OR REPLACE VIEW debug.v_player_afk_overview AS
SELECT
	p.player_id,
	p.last_claimed_at,
	GREATEST(0, FLOOR(EXTRACT(EPOCH FROM (now() - p.last_claimed_at))))::integer AS unclaimed_seconds,
	LEAST(86400, GREATEST(0, FLOOR(EXTRACT(EPOCH FROM (now() - p.last_claimed_at)))))::integer AS claimable_seconds_capped,
	p.updated_at
FROM player.player_afk_progress p;
