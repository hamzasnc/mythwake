# Watchtower – village defense – concept

Source: design doc, "Building System" (Watchtower: unlocks village defense missions; a wave-like monster dungeon to
defend the village) and "Dungeon System" (Endless / Wave Dungeons: push as far as possible for ranking and rewards).
Numbers are first-pass balancing.

## A defense run

- Rebuild the Watchtower (stage 35, up to level 5). **2 runs per day**, reset at 04:00 UTC, no stacking.
- A run is a chain of **waves** (up to 30). Each wave is one fight against monsters attacking Dawnhollow.
- **HP and energy carry over** from wave to wave; survivors recover 10 % of their max HP between waves. Fallen heroes
  stay down for the rest of the run. The run ends when the whole team falls, after wave 30, or when the player
  retreats.
- Enemies: the doc's monsters (no named bosses) at the player's campaign level, +2 levels per wave; 3 enemies, one more
  every 4 waves up to 5; every 5th wave a named boss leads the wave.

## Rewards

- Every cleared wave: Gold, Wood and Stone (village rebuilding materials), growing with the wave and +10 % per
  Watchtower level above 1. Rewards are paid at once, so nothing is lost when the run fails.
- Every 10th wave: 100 Gems.
- The best wave reached is recorded (the doc's ranking needs the server and comes with it).

## UI

Dawnhollow → Watchtower → **Verteidigung**: best wave, runs left, the running wave with each hero's HP and energy,
rewards so far, "Nächste Welle" and "Rückzug".
