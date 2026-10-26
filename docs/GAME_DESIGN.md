# Mythwake – Game Design (source of truth)

This file is the team's design document (author: Corex [HS], 11.05.2026) copied verbatim.
It is the source of truth for lore, systems, names and terms. Do not add lore terms, names, Fragment Aspects,
races, factions or systems that are not written here; if something is missing, ask the team instead of inventing it.

---

## Lore sneak-peak

Atheris is broken. The old kingdoms have fallen, the Guardians have vanished, and the Fragments—ancient remnants of forgotten power—have left scars across the world. From these wounds, the Shattered Realms bleed into the land, unleashing corrupted beasts, lost magic, and echoes of a past no one truly understands.

In the quiet border village of Dawnhollow, life has always been simple. Far from the wars of empires and the ruins of ancient powers, its people believed they were safe from the world's old disasters.

They were wrong.

When the ground beneath Mossveil Shrine cracks open, a Rift awakens in the forest. Animals twist into monsters, purple light spreads through the trees, and Dawnhollow is swallowed by chaos. In the heart of the disaster, you come into contact with a mysterious Fragment that should have killed you.

Instead, it chooses you.

The Fragment becomes the Mythwake Core, a rare power able to stabilize corruption, awaken forgotten heroes, and call forth Echoes—memories of warriors, kings, mages, and guardians who once fought against the same darkness now rising again.

By dawn, Dawnhollow lies in ruins. The Rift still pulses in the forest. Survivors look to you, not because you are ready, but because you are the only one the Core spared.

Now, you must rebuild what was lost, gather allies from across Atheris, awaken legends buried by time, and uncover the truth behind the Fragments, the Shattered Realms, and the vanished Guardians.

Your village has fallen.
The world is remembering.
And something ancient is waking beneath Atheris.

---

## Auto Battle System

Combat should be automatic with an option to disable it to remain on the current stage.

The player prepares the team before battle, but the heroes fight on their own.

The player's strategy comes from:

- Hero selection
- Team formation
- Faction bonuses
- Race synergies
- Fragment setup
- Gear
- Relics
- Skill upgrades
- Countering enemy mechanics

Battles should be fast, readable, and visually exciting.
The user should think about placement, gear and combinations before going into a big fight or dungeon.

## Team Formation System

Players build teams of up to 5 heroes. [This is just a random number at the moment]

The player need to unlock new formation slots. He starts with 2 or 3 and then it goes up by completing quest/milestones, upgrading the settlement or leveling in general.

Suggested formation:

- 2 frontline positions
- 3 backline positions

Frontline heroes are usually tanks, bruisers, and melee fighters.

Backline heroes are usually mages, healers, supports, assassins, or ranged damage dealers.

Positioning matters because some skills target:

- Front row
- Back row
- Weakest enemy
- Strongest enemy
- Lowest HP ally
- Highest damage enemy
- Specific factions or monster types

We want to make options for heroes to target specific enemy's.
Example: Target mages first, target the weakest units first etc

## Skill and Ultimate System

Each hero has a set of combat abilities.

Suggested structure:

- Basic Attack
- Passive Skill
- Skill 1
- Skill 2
- Ultimate Skill

Ultimate abilities should feel powerful and cinematic.

They can include:

- Short character cut-ins
- Big visual effects
- Special animations
- Unique sound effects
- Strong combat impact

Not every character will have the same amount of attacks or ultimate's. We could also think of changing the character playset by giving them different pieces of equipment to change the playstyle of them.

## Gear System

Heroes can equip gear to increase their stats.
Gear can also change the playstyle of the character. Giving him a shield could make him a tank or giving him a wand would make him a mage.
If we implement it this way we will need to set some limitations for example 3 different varriants for each character so you can customise and combine it how you like. This would also mean we need 3 different combat animations and skills for each character.

Possible gear slots:

- Weapon
- Armor
- Boots
- Accessory
- Relic

Gear can be upgraded, enhanced, refined, crafted, found in dungeons or be obtained from events.

Gear materials come from:

- Idle rewards
- Dungeons
- Bosses
- Hunts
- Events
- Village buildings

Set Bonus:
If a character has all equipment from one set he will get a Set Bonus on all gear stats that he has equiped.
We can scale it here with rarity of the gear.

## Building System

The player starts in a Village that will be destroyed after an event. He needs to rebuild it to get access to different mechanics like crafting and refining from gear and the guild hub

The village becomes the main hub. From here it will be possible to make hunting trips, excavations and so on.

Possible buildings:

**Survivor Camp**
Produces basic resources.
Here you get some basic mats for the start. [Gold,Wood,Stone]

**Broken Forge**
Unlocks gear crafting and upgrades.

**Healer's Tent**
Improves healing, recovery, and support upgrades.

**Hunter's Hut**
Unlocks daily missions and monster hunts.

**Watchtower**
Unlocks village defense missions.
Wave like monster dungeon to defend the village.

**Fragment Shrine**
Unlocks Fragment upgrades and stabilization.
Unlocks relics as well their upgrades and hero awakenings.

**Summoning Gate**
Unlocks hero recruitment.
On later upgrades it will expand the hero recruitment's to get specialized hero's.

**Guardian Shrine**
Unlocks awakening systems.

**Portal Chamber**
Unlocks dungeons, events, and Shattered Realm access.

The village grows as the player progresses.
We also had the idea of managing multiple villages depending on we implement it and how much time the player should spent there.

These are not all buildings or finalized buildings we would add, remove or adjust them on how we need them.
You would need to upgrade the buildings to unlock new and more features [higher equipment grade for crafting]

If we want to expand it we can also expand it into the guild feature to a guild castle with unique features and buildings.

## Relics

Relics are rare magical items connected to boss-monsters, dungeons and events.

Unlike normal gear, Relics can have unique effects.

Examples:

- Increase healing after an ultimate
- Add bonus damage against specific type of monster
- Improve shield strength
- Boost Fragment effects
- Give special starting energy
- Add bonus rewards from certain areas

You will be able to upgrade those relics and we can also add rarity to them here to increase the bonus they give

Each character can equip a relic.

## Idle Progression System

The game should continue progressing while the player is offline.
We can set a time limit for normal player and a time limit for premium with an higher limit and maybe higher reward yield.

Heroes automatically farm resources in the current idle stage or selected expedition area.

The player can choose to push forward to higher stages or stay on the current stage to farm specific resources.

Possible idle rewards:

- Gold
- Hero EXP
- Wood
- Stone
- Gear materials
- Gear depending on the Stage
- Fragment Dust
- Relic materials
- Summon Scrolls
- Guardian Sigils
- Village rebuilding materials in general

The quality and amount of rewards should scale with:

- Current stage
- Region difficulty
- Building upgrades
- Hero power
- Special Relics
- Event bonuses
- VIP or account progression

The idle system should support both casual players and active players.
Casual players collect rewards and upgrade slowly.
Active players optimize stages, builds, heroes and farming locations.

## Hero Collection System

Players can collect heroes from different races, factions and regions of the world.

A hero can have:

- Name
- Race
- Faction
- Role
- Element or Fragment Aspect
- Rarity
- Skills
- Ultimate ability
- Preferred gear types
- Possible playstyle variants
- Awakening path
- Personal story

Heroes can be obtained from:

- Story progression
- Summoning
- Events
- Dungeons
- Boss rewards
- Reputation rewards
- Shops
- Special quests

Not every hero needs to be equally rare.

Some heroes can be common early-game units, while others are legendary characters tied to major story moments, bosses or factions.
There will also be special heros that you can only obtain through event progression. That can be a brand new hero or a reskin/reform of an current hero.

The player should be able to build many different teams and not only rely on one strongest meta team.

## Race / Species System

Heroes and enemies can belong to different races or species.

This gives the world more variety and allows race-based synergies, weaknesses and storylines.

Possible races:

**Humans**
Adaptable and widespread. Good for balanced heroes, leaders, mages, knights, hunters and scholars.

**Veyr**
Mystical long-lived beings connected to memory, moonlight, illusion and ancient magic.

**Drakari**
Dragon-blooded warriors with fire, storm and rage-based abilities.

**Thalassari**
Aquatic island people connected to water, tides, healing and sea magic.

**Stoneborn**
Earthbound beings with stone-like bodies, strong defense, shields and mountain magic.

**Hollowed**
Corrupted beings changed by Fragment energy. They use mutation, regeneration and unstable power.

**Aetherials**
Rare beings touched by portal energy. They use teleportation, summons, void magic and reality effects.

Race can affect:

- Passive bonuses
- Team synergies
- Enemy weaknesses
- Questlines
- Dialogue
- Visual identity
- Special equipment restrictions or bonuses

Those races are not final and are used as suggestions and idea templates to built from.
There will also be a race synergy where heros from the same race/country or specialization can get bonuses from fighting in the same formation.

## Faction System

Factions define what a hero believes in and where they belong politically or culturally.

A hero's race and faction do not always need to match.

For example:
A Human could belong to the Order of the Veil.
A Drakari could join the Guilds of the Hunt.
A Hollowed could become a playable anti-hero.

Main factions:

**Order of the Veil**
Scholars, priests and mages who want to understand the Fragments.

**Tribe of the Crimson Fang**
Warrior clans who believe strength proves worth before the Guardians.

**Empire of the Shattered Sky**
A powerful empire that wants to control Fragment power and restore order through force.

**Guilds of the Hunt**
Monster hunters, mercenaries and scavengers who protect settlements and hunt corrupted beasts.

**Circle of Aether**
Portal researchers and forbidden mages who study the Shattered Realms.

**The Hollowed**
Corrupted beings and monsters consumed by Fragment energy.

Faction bonuses can activate when multiple heroes from the same faction are used in one team.

Possible faction bonus examples:

- 2 heroes from same faction: small stat bonus
- 3 heroes from same faction: stronger bonus
- 5 heroes from same faction: special team effect

Faction reputation can also unlock heroes, quests, shops and cosmetics. [cosmetics if we decide to implement visual ones for hero's]

Also those factions are not set. I took them from the story we where thinking of using.

## Fragment System

They are one of the most important progression systems in the game.

Fragments can give heroes special powers, but they can also be dangerous and unstable.

Each hero could equip different Fragment types depending on how deep we want the system to be.

Possible Fragment slots:

**Core Fragment**
Main Fragment that gives the biggest stat or power bonus.

**Aspect Fragment**
Changes the combat behavior or adds a special effect.

**Echo Fragment**
Adds passive bonuses, team synergies or special interactions.

Fragments can be:

- Found in dungeons
- Dropped by bosses
- Earned from events
- Crafted or stabilized in the Fragment Shrine
- Upgraded with Fragment Dust
- Fused into higher rarity versions

Fragments should not only increase stats.

They should also create build choices.

Example:
One Fragment could make a hero more defensive.
Another could make the same hero stronger at low HP.
Another could improve his ultimate.
Another could give bonus damage against Hollowed.

We can also add drawbacks to those fragments. They could lower defense or the damage of the selected hero while giving another a boost.

## Hero Upgrade System

Heroes should have multiple layers of progression.

Possible upgrade types:

**Level**
Basic stat growth through Hero EXP.

**Rank**
Unlocks stronger stats, skill upgrades and higher level limits.

**Stars**
Increases rarity and long-term power, possibly using duplicates or hero shards.

**Skills**
Improves basic attacks, passives, normal skills and ultimates.

**Gear**
Improves stats and can change the hero's role or playstyle.

**Fragments**
Adds special effects and build customization.

**Relics**
Adds unique effects from bosses, dungeons or events.

**Awakening**
Unlocks advanced forms, new passives or stronger ultimate effects.

The goal is to give players both short-term and long-term progression.

Short-term:
Level up, equip better gear, upgrade skills.

Long-term:
Awaken heroes, improve rare Fragments, complete gear sets, upgrade Relics.

We can also combine something here if we want to simplify it.

## Guardian Awakening System

Guardian Awakening is a late-game transformation system.

It connects heroes to the ancient Guardians and gives them stronger forms.

Awakening can require:

- Guardian Sigils
- Fragment materials
- Boss drops
- Rare Relics
- Story progress
- Special quests
- High building levels

Awakening can change:

- Character artwork
- Combat effects
- Passive skill
- Ultimate animation
- Stat growth
- Role identity
- Story identity

It could also unlock a new story for the character or a special dungeon where you need awakened hero's

Examples:

Elara could awaken into a stronger healer and protector connected to the Celestial Turtle.

Kaelen could awaken into a controlled corrupted fire warrior.

Veyl could awaken into a ruler who creates artificial Guardian constructs.

Arion could awaken into a mage who controls his fractured mind across multiple realities.

Awakening should feel like a major milestone for each character you field and not just a small stat upgrade.

## Summoning System

Players recruit heroes through a summoning system.

Possible summon types:

**Normal Summon**
Basic heroes, shards and materials.

**Faction Summon**
Higher chance for heroes from a selected faction.

**Fragment Summon**
Higher chance for heroes connected to a specific Fragment Aspect.

**Event Summon**
Limited-time heroes connected to story events, portals or seasonal content.

**Guardian Summon**
Rare summon type for awakened or Guardian-linked heroes.

Summoning can use:

- Summon Scrolls
- Gems
- Event currency
- Special tickets

The system should also include some form of pity or guaranteed reward after a certain number of summons.

## Dungeon System

Dungeons are PvE challenge areas with multiple stages or floors.
They are used for farming stronger rewards and fighting special bosses.

Dungeon types could include:

**Story Dungeons**
Connected to the campaign.

**Material Dungeons**
Focused on specific upgrade materials.

**Gear Dungeons**
Used to farm gear and set pieces.

**Fragment Dungeons**
Used to farm Fragment Dust and Fragment materials.

**Relic Dungeons**
Used to farm Relics and Relic upgrade materials.

**Endless Dungeons / Wave Dungeons**
The player pushes as far as possible for ranking and rewards.

Dungeons can have special rules.
Examples:

- Healing is reduced
- Fire damage is increased
- Only certain factions receive bonuses
- Enemies regenerate over time
- Ultimates charge slower
- Bosses become stronger every few floors

Dungeons should make the player think about team setup before entering.
We can also limit the dungeon try and link it in a daily or weekly challenge to get x points or get the a specific stage. Those limited dungeons would give needed rewards we want to limit.

## Shattered Realm System

The Shattered Realms are cracks and portals into broken dimensions.
They could appear as a form of limited dungeon that opens twice a day or where you can get a key to open it and try it for the day. The key would come from a shop or daily reward system.

Shattered Realms can be used for:

- Story missions
- Event areas
- Special dungeons
- High-level challenges
- Rare material farming
- Corrupted Guardian encounters

Each Shattered Realm can have its own rules, visuals and enemies.

Examples:

**Realm of Ash**
Fire damage is stronger, healing is weaker.

**Realm of Stillness**
Enemies slow the team and delay skills.

**Realm of Reflection**
Enemies copy buffs and reflect damage.

**Realm of Hunger**
Enemies regenerate and become stronger over time.

This system allows us to create many different game modes using the same core combat.

## Arena System

The Arena is the PvP mode.
Players fight against other players' teams in automatic battles.

The player can prepare:

- Attack team
- Defense team
- Formation
- Gear setup
- Fragment setup
- Relics
- Targeting priorities

Possible Arena ranks:

- Bronze
- Silver
- Gold
- Platinum
- Mythic
- Guardian
- Shattered King

Arena rewards can include:

- Gems
- Arena Coins
- Hero shards
- Gear materials
- Fragment materials
- Cosmetics
- Ranking rewards

Arena seasons can reset after a certain time and give rewards based on final rank.

## Guild System

Guilds should support social and cooperative gameplay.

Possible guild features:

- Guild chat
- Guild missions
- Guild donations
- Guild shop
- Guild ranking
- Shared raid progress
- Guild research
- Guild buffs
- Cooperative events

Guilds can later be expanded into a Guild Castle system.

The Guild Castle could have its own buildings and upgrades.

Possible Guild Castle features:

- Guild Forge
- Raid Hall
- Guild Shrine
- Guild Market
- Training Grounds
- War Room

This depends on how much time we want the player to spend in guild management.

You would get special bonuses from those buildings in regard of stats/gear/resources.
The resources needed to upgrade those buildings would be higher since many player can contribute to it.

## Guardian Raid System

Guardian Raids are large boss battles fought by guilds.

Players fight massive corrupted Guardians over multiple attempts.

The goal is to deal as much damage as possible and help the guild defeat the boss.

Raid rewards can be based on:

- Individual damage
- Guild damage
- Boss level
- Participation
- Guild ranking

Possible Guardian bosses:

- Celestial Turtle
- Dragon of the Sky
- Hollowed Leviathan
- Six-Legged Worldbearer
- Craterborn Guardian

Rewards can include:

- Guardian Sigils
- Legendary Relics
- Awakening materials
- Guild Coins
- Exclusive cosmetics
- Rare Fragment materials

Raids should require preparation and team optimization.

## Hunt Board System

The Hunt Board gives the player daily and weekly monster-hunting tasks.

It can be unlocked through the Hunter's Hut.
With higher levels of the hunter's hut the amount of rewards will increase as well as difficulty to make it more challenging.

Mission examples:

- Defeat corrupted wolves
- Clear a forest path
- Hunt a Hollowed beast
- Protect a caravan
- Gather Fragment Dust
- Defeat a Riftborn enemy
- Rescue lost villagers
- Collect monster materials

Rewards can include:

- Gold
- Hero EXP
- Gear materials
- Hunt Tokens
- Village resources
- Summon Scrolls
- Fragment Dust

The Hunt Board helps guide daily gameplay and gives the player clear short-term goals.

## Event Portal System

Event Portals are limited-time content.

A portal opens for a specific time and introduces a special event.

Each Event Portal can include:

- Short event story
- Special enemies
- Event stages
- Event boss
- Event currency
- Event shop
- Limited rewards
- Limited hero or skin

Example:

**Portal of the Drowned Moon**

A Thalassia-themed event with sea monsters, water Relics, moonlight magic and a special ocean boss.

We could use this mechanic to add short or longterm events for example chinese new-year that will get it's own icon and will have it's own page/dungeon there.

## Trial of Fragments System

The Trial of Fragments is a challenge mode based on the seven Fragment Aspects.

Each trial focuses on one mechanic/attribute.

Examples:

**Trial of Ravak**
Enemies deal high fire damage and become stronger at low HP.

**Trial of Somnara**
Enemies slow the team, delay cooldowns and apply sleep.

**Trial of Envyra**
Enemies steal buffs and copy abilities.

**Trial of Gorath**
Enemies regenerate and grow stronger the longer the fight lasts.

**Trial of Veloria**
Enemies charm, confuse and manipulate targeting.

Rewards can include:

- Fragment Dust
- Aspect materials
- Special Relics
- Upgrade stones
- Titles
- Cosmetics

This dungeon would be to conquer and master different themed things. We could make them like a tower where each monster has the same element/attribute. You would also need the same matching element/attribute in your deployed team to go into the tower.

## Shop System

Possible shops:

**General Shop**
Basic resources, gear materials and daily items.

**Arena Shop**
PvP rewards, hero shards and competitive items.

**Guild Shop**
Raid materials, Relics and guild rewards.

**Event Shop**
Limited-time rewards from Event Portals.

**Fragment Shop**
Fragment materials and Aspect resources.

**Village Shop**
Rebuilding materials, tools and cosmetic village upgrades.

**Hunt Shop**
Monster materials and hunter-related rewards.

Shops can refresh daily, weekly or after events.

Some shops can be unlocked through buildings or reputation.

The cash shop is not included here bcs it will be it's own whole system with more icons for offers.

## Quest System

Quests guide the player through the game.

Quest types:

**Main Quests**
Advance the campaign story.

**Side Quests**
Explore local characters, regions and smaller stories.

**Daily Quests**
Give short daily goals.

**Weekly Quests**
Give bigger goals with better rewards.

**Village Quests**
Help rebuild Dawnhollow and unlock buildings.

**Faction Quests**
Increase reputation with factions.

**Hunt Quests**
Monster-hunting tasks from the Hunt Board.

**Event Quests**
Limited-time objectives connected to Event Portals.

Quest rewards can include:

- Gold
- EXP
- Gems
- Gear materials
- Fragment Dust
- Village resources
- Hero shards
- Relic materials

## Enemy System

Enemies are divided into groups.
We can expand and categories them for regions and maybe adjust bonuses there.
The strength and hp of those enemy should increase as the player progresses.

Enemy types:

**Wild Beasts**
Normal animals and early enemies.

**Corrupted Beasts**
Animals changed by Fragment energy.

**Hollowed**
Humans, creatures or heroes corrupted by Fragments.

**Crimson Wraiths**
Powerful corrupted mages and elite enemies.

**Riftborn**
Creatures from the Shattered Realms.

**Fragment Constructs**
Magical beings created from unstable Fragment energy.

**Corrupted Guardians**
Major bosses connected to the lost Guardians.

Each enemy type can have:

- Strengths
- Weaknesses
- Special mechanics
- Preferred regions
- Drop materials
- Boss variants

## Boss System

Bosses are major progression challenges.
They should have unique visuals, mechanics and phases.

You will find them at marked points in regions every x steps we can use 5 or 10 steps for a boss to show up.
They will also be the end of normal dungeon when you finish the boss you end the dungeon.

Early bosses:

- **Hollowtusk Alpha** – First corrupted forest boss.
- **Riftborn Treant** – Boss of the corrupted Mossveil Woods.
- **Crimson Wraith Adept** – First humanoid corrupted mage boss.

Mid-game bosses:

- Hollow Spire Warden
- Obsidian Vault Sentinel
- Fragment Devourer

Late-game bosses:

- Corrupted Guardian of the Deep
- Dragon Echo of the Sky
- False Guardian Construct

Bosses can drop:

- Gear
- Relics
- Fragment Dust
- Awakening materials
- Boss-specific materials
- Hero shards
- Village upgrade materials

Boss fights should make preparation important.

## Region Map System

The world map is divided into playable regions.
The player will unlock regions after completing a dungeon/mission or the region before it.

Each region map can show:

- Main locations
- Dungeons
- Cities
- Rifts
- Craters
- Faction borders
- Boss locations
- Event locations

Region maps help the player understand where they are in the world.
They also make the world feel larger and more connected.

Each region should have:

- Visual identity
- Enemy types
- Local resources
- Local faction influence / can be used for the hero recruiting pool
- Main dungeon
- Main boss
- Story purpose

## Monetization Systems

Possible monetization systems:

**Hero Summoning**
Premium summons for heroes.

**Battle Pass**
A seasonal reward track, possibly called a Guild Contract.

**Monthly Card**
Daily Gems or resources.

**Cosmetic Skins**
Alternate hero outfits, awakened forms or seasonal skins.

**Event Packs**
Extra event currency, tickets or materials.

**Idle Reward Boosts**
Temporary boosts to offline rewards.

**Fragment Packs**
Materials for Fragment upgrades.

**Starter Packs**
Early progression bundles.

**Village Packs**
Optional building materials or cosmetic village upgrades.

Important:
Monetization should not break the lore.

We would add pack options and bundle offers here for players to buy. Like a starter pack/hero pack or AD-Skip. We will focus on other features first and will decide if we will use multiple currency's on it.

It should feel connected to guild contracts, relic shipments, summon scrolls, faction supplies and expedition rewards.

## Boss Fragments

After the player defeats a main boss, I would use dungeon bosses for now they can refight them to gain the fragment of this Boss.
We can make it like a gacha system where you get chests from those defeated bosses where you can get them.

You can upgrade those fragments and equip one at the time. It will give you a pissve bonus or an map wide effect like healing/shielding or an damaging ability. It will have a cooldown so you can cast it more than one time in a fight. It should feel powerful and needed for late game fights.

This would be locked to an global daily limit for the player so if he has 4 dungeons he could refight and has 7 entries that he needs to choose where to use them.
The entries would reset at a fixed time and would not stack and go up if you don't use them.

## Hero Leaders

This is a special type of hero that will not fight for you.
It will grant your hero's a buff depending on his level/rarity and equipment.

We could also add an ability for them like war cry or deafening scream to boost your formation or stun enemies for time.

## Village Style

Each building will have slots to fit your heros in them. They will not only fight but also give you bonuses and advantages in your village.

For example:
Your mage is placed in your healers tent and will boost your global healing effects for 4% so all received healing will be stronger.

Or your tank is placed in the forge and for that your chance to upgrade gear is higher or you have a better chance to get rarer gear or even unlock a special kind of rarity.

So you can place hero's there that you have no use in your current formation.
Hero's placed in the village will also fight in the formation so you can place a hero in both places
