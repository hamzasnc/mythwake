# Arena – concept

Source: design doc, "Arena System" (PvP, automatic battles, attack and defense team, ranks Bronze → Shattered King,
Arena Coins, seasons with rank rewards) and "Shop System" (Arena Shop – not built yet).

## Rules

- **Unlock:** after stage 15.
- **Asynchronous PvP:** the player sets a **defense team** (the current formation, frozen as a snapshot with its
  stats). Others fight against that snapshot while the owner is offline.
- **Attack team:** the current formation at the moment of the fight.
- **Fights:** 5 per day (reset with the daily quests, 04:00 UTC). The server picks the seed, the rules service
  simulates (Auto ultimates, same simulator as everywhere), the client replays the decided fight.
- **Opponents:** three out of the eight closest by rating – one lower, one close, one higher.
- **Rating:** Elo, K = 32, start 1000. Only the attacker's fight count is limited; the defender loses or gains rating
  passively.
- **Ranks (doc):** Bronze 0 · Silver 1100 · Gold 1250 · Platinum 1400 · Mythic 1600 · Guardian 1800 ·
  Shattered King 2000.
- **Rewards per fight:** win 30 Arena Coins + 3,000 Gold, loss 10 Arena Coins. Paid by the server with the
  server-only command `GrantRewards`, which clients cannot send.
- **Seasons:** 14 days, season 1 starts on 2026-01-05. On the first arena visit of a new season the player receives
  the rewards of the old rank (Gems, Arena Coins, Summon Scrolls) and the rating moves halfway back to 1000.
- **Training opponents:** the server keeps 30 bots (teams generated from campaign stages 15–200, ratings 850–1950),
  so a new server is playable from the first day.

## Open points

- Arena Shop (doc lists it; Arena Coins collect until then).
- Separate attack and defense formations in the UI (now: defense = formation at the time it is set).
- Battle history / revenge, anti-collusion checks.
