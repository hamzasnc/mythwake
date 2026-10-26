# Hunter's Hut and Hunt Board – concept

Source: design doc, "Building System" (Hunter's Hut: daily missions and monster hunts), "Hunt Board System",
"Shop System" (Hunt Shop) and "Quest System" (Hunt Quests).

## Goals

- A short daily combat loop next to the campaign: a few fights a day with useful rewards, also when the campaign
  is stuck.
- The hut grows with the player (doc: "With higher levels of the hunter's hut the amount of rewards will increase
  as well as difficulty").
- Hunt Tokens give the board its own currency and the Hunt Shop a reason to come back.

## Hunter's Hut

Unlocks after stage 12 (chapter 1 boss + 2), levels 1–10, two hero slots (levels 1 and 5). Heroes working in the hut
raise all hunt rewards (Village Style).

| Hut level | Daily hunts | Weekly hunts | Reward bonus | Enemy level bonus |
|---|---|---|---|---|
| 1 | 3 | 1 | +0 % | +0 |
| 4 | 4 | 1 | +30 % | +1 |
| 6 | 4 | 2 | +50 % | +2 |
| 7 | 5 | 2 | +60 % | +2 |
| 10 | 6 | 2 | +90 % | +3 |

(Reward +10 % per level above 1, enemy level +1 per 3 levels; counts from content.)

## The board

- Every day (same reset as daily quests, 04:00 UTC) the board rolls new **daily hunts**; every Monday new
  **weekly hunts**. The roll is a pure function of player seed + day/week, so a server gets the same board.
- A hunt = a **mission** from the doc's list + a **difficulty** (★ to ★★★). Weekly hunts are always elite (boss
  stats, bigger rewards).
- Each hunt can be won once per period; losing costs nothing and can be retried.
- Enemy level = campaign enemy level at the player's highest cleared stage − 3 / ± 0 / + 3 (★ / ★★ / ★★★)
  + hut bonus; elite +5 with boss stats. Team size follows the campaign.

### Missions (doc examples)

| Mission | Enemies | Main rewards |
|---|---|---|
| Defeat corrupted wolves | any | Gold, Hero EXP |
| Clear a forest path | any | Wood, Stone |
| Protect a caravan | any | Gold |
| Rescue lost villagers | any | Wood, Stone, Hero EXP |
| Collect monster materials | any | Gear materials |
| Defeat a Riftborn enemy | Circle of Aether | Gear materials, Gold |
| Hunt a Hollowed beast (weekly) | The Hollowed | Summon Scroll, Gear materials |

"Gather Fragment Dust" is held back until Fragments exist (the doc lists it; the reward would have no use yet).
Enemies are heroes standing in for monsters until the doc's enemy groups get art.

### Rewards

Each mission's rewards grow with the player's stage; then × (1 + 25 % per ★ above one + hut bonus + hut heroes).
Every hunt also gives **Hunt Tokens**: 10 / 15 / 20 for daily ★ / ★★ / ★★★, 60 for weekly hunts.

## Hunt Shop

| Item | Price | Limit |
|---|---|---|
| 5,000 Gold | 10 Hunt Tokens | 5 per day |
| 3,000 Hero EXP | 10 | 5 per day |
| 50 Gear materials | 20 | 5 per day |
| 1 Summon Scroll | 60 | 3 per week |
| Epic gear piece (random slot and set) | 150 | 1 per week |

## Rules

- `BeginHunt(huntId)` → battle setup (formation from the campaign, village bonuses apply).
- `FinishHunt(auto, toggles, taps)` → re-simulated like campaign battles; on a win: rewards, tokens, the hunt is
  marked done, stats `battleWon` and `huntCompleted` count.
- `BuyHuntItem(itemId)` checks tokens and the daily/weekly limit.

## UI

Hunter's Hut card in Dawnhollow → **Hunt Board** screen: daily and weekly hunts as cards (mission, stars, enemy
power vs team power, rewards, Done stamp), "Jagen" starts the fight with the normal battle screen. A second tab is
the Hunt Shop. A main quest asks to rebuild the hut after the chapter 1 boss.
