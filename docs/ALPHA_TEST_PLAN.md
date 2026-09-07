# Mythwake Internal Alpha Test Plan

Candidate: Prototype `0.2.177` / Backend `0.2.63` / Android Code `2177`.

Run once in Local Mode and once with the same Email account in Server Mode. Record the visible build version and Player ID for every failure.

1. Account: fresh start, Guest fallback, Email register/login/logout, wrong password, restart, and Login again. The same Email Player ID and progress must return.
2. Campaign: clear the first stage, inspect reward/next goal, enter Formation, stop a local visual fight, and confirm no duplicate reward.
3. Bag: open Bag, switch All/Gear/Armor/Consumables/Materials/Gems, select Hero Shard Chest, try 0, 1, over-owned, and invalid amounts. Counts clamp to owned quantity; rewards appear once and survive refresh/restart.
4. Hero progression: use chest rewards, Star Up a hero, reach Lv. 100, test Awakening, and verify blocked-resource messages and persisted values.
5. Shard Rift: run, claim per-enemy rewards, lose or manually end, restart, and verify earned Awakening Shards/Chests and best/total counters.
6. Tower Server Mode: bootstrap, clear an active floor, inspect server reward, send the same request twice, flush, restart the API/client, logout/login with the same Player ID, and verify one clear/one reward. Also test an intentional defeat and manually stop the client visual before the response; neither may create a second economy mutation.
7. Village/Fast Rewards: build/upgrade one affordable plot, claim Fast Rewards, restart, and verify server/local mode copy and one claim.
8. Language and failures: repeat the key route in EN and DE; turn off the backend or use an invalid session and verify readable errors, no endless loading, and no client-side economy grant.

Pass criteria: no crash/ANR, no blocked visible action, no duplicated reward, no progress rollback, and no server-mode economy change without a successful server action result.
