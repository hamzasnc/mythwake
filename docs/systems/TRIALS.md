# Trial of Fragments – concept

Source: design doc, "Trial of Fragments System" (a challenge mode based on the Fragment Aspects; each trial focuses on
one mechanic; like a tower where every monster has the same Aspect; the deployed team needs the matching Aspect;
rewards Fragment Dust, Aspect materials, special Relics, upgrade stones, titles, cosmetics). Mythwake has five Aspects
(docs/DECISIONS.md), so there are five trials. Numbers are first-pass balancing.

## Towers

| Trial | Enemy mechanic (doc) | How the rules do it |
|---|---|---|
| Trial of Ravak | High fire damage, stronger at low HP | Enemies deal +15 % damage and +30 % more below 50 % HP |
| Trial of Somnara | Slow the team, delay cooldowns, apply sleep | Enemy hits may slow, push back the next action, or put to sleep |
| Trial of Envyra | Steal buffs, copy abilities | Enemy hits may steal a buff and drain energy (taking the hero's power for themselves) |
| Trial of Gorath | Regenerate, grow stronger the longer the fight lasts | Enemies regenerate 1.5 % max HP per second, +5 % damage every 10 s (up to +40 %) |
| Trial of Veloria | Charm, confuse, manipulate targeting | Enemy hits may charm or confuse |

- **100 floors per tower.** Each floor is one fight; clearing floor N opens N+1. No daily limit, a lost fight costs
  nothing (like a tower in AFK Arena).
- **Team:** up to the unlocked formation size, **only heroes of the tower's Aspect** (doc). No Hero Leader. The team is
  remembered per tower.
- **Enemies:** monsters and corrupted heroes of the same Aspect; 3 enemies on floors 1–4, 4 on 5–9, 5 from 10.
  Enemy level 25 + 3 per floor, +1 star every 20 floors. Every 10th floor is a boss floor (the strongest enemy of that
  Aspect gets the boss bonus).

## Unlock

Fragment Shrine level 4. The trials sit on the world map as their own pin.

## Rewards

- Every floor (first clear): Fragment Dust (60 + 6 per floor) and Gold (3000 + 400 per floor).
- Every 10th floor: 100 Gems and an **Aspect Fragment of the tower's Aspect** (Rare until floor 29, Epic until 59,
  Legendary from 60). That is how a player can farm the effect they want.
- Not in this step: Aspect materials, special Relics, titles and cosmetics (they need their own systems first).
