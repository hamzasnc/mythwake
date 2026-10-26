# Guardian Awakening – concept

Source: design doc, "Guardian Awakening System" (late-game transformation connecting heroes to the ancient Guardians;
requires Guardian Sigils, Fragment materials, boss drops, rare Relics, story progress, special quests, high building
levels; changes artwork, combat effects, passive, ultimate, stat growth, role and story identity; examples Elara,
Kaelen, Veyl, Arion), "Building System" (Guardian Shrine unlocks awakening), combat design
(`output/Mythwake_Kampfsystem/KAMPFSYSTEM.md` §7: awakened forms belong to the Transcendent tier and carry a Trait, a
team-wide aura). Numbers are first-pass balancing.

## Who can awaken

Only heroes with an awakening in `Content/Data/awakenings.json`. At launch these are the doc's four examples:

| Hero | Doc | Awakened passive | Trait (whole team) |
|---|---|---|---|
| Elara | Stronger healer and protector connected to the Celestial Turtle | +25 % healing; after her ultimate the team gets a shield (150 % ATK) | −8 % damage taken |
| Kaelen | Controlled corrupted fire warrior | 12 % lifesteal, +20 % damage below 50 % HP | +6 % damage |
| Veyl | Ruler who creates artificial Guardian constructs | Every 12 s a construct shields the front row (120 % ATK) | +15 % defense for the battle |
| Arion | Mage controlling his fractured mind across multiple realities | +40 % ultimate damage, gets 350 energy back after his ultimate | Team starts with 150 energy |

## Requirements and cost

- Guardian Shrine rebuilt (stage 60; doc: high building levels).
- Campaign stage 60 cleared (doc: story progress).
- Hero at its maximum stars (Legendary: 6).
- 120 Guardian Sigils, 2000 Fragment Dust ("Fragment materials"), 300 Relic materials ("rare Relics" / boss drops)
  and 200 000 Gold.

## What changes

- The hero counts as **Transcendent**: its star cap rises to 7.
- +20 % HP and ATK (stat growth).
- The awakened passive and the Trait above are added in every battle.
- The hero card and detail show the Guardian Awakening.
- Not yet: new artwork, ultimate animation and story (they need art and writing from the team), Guardian Summon,
  "awakened-only" dungeons, a limit of one Transcendent per formation (combat design proposal, not decided).
