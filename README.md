# Mythwake

Mobile idle RPG prototype built with Unity.

Prototype version: 0.2.1
Local save version: 1

Current prototype:
- Android build profile
- Simple portrait UI
- First core loop: fight enemies, earn Myth Essence, upgrade heroes
- Auto attack while the app is open
- Local save data via PlayerPrefs
- Basic offline Gold and Myth Essence calculation when reopening the app
- Mobile app shell with Home, Battle, Heroes, Gear, Summon, and Shop tabs
- Visible prototype/save version text for quick test builds
- Debug resource buttons for adding small Gold, Gems, Myth Essence, and accessory test amounts
- Starter hero collection with 5 heroes
- Each starter hero has role-flavored Attack and HP stats
- Team Attack and Team HP are summed from the active hero roster
- Starter roles now affect combat: Warrior and Mage add damage, Tank reduces incoming damage, Support heals, Ranger executes weak enemies
- Gold, Gems, Myth Essence, Pass XP, and Hero Shards are separated
- Gold can be spent on starter Weapon and Armor equipment upgrades
- Equipment upgrades are saved locally and add team-wide ATK and HP
- Starter equipment balance is defined through small data structs to prepare for later server/database config
- Core prototype balance is being reshaped into code-side definition rows with stable IDs for later database migration
- Accessory gear system with Ohrringe, Kette, Armband, Handschuhe, and Schuhe slots
- Accessory items have rarity tiers R0-R4, can be equipped, leveled, and saved locally
- Accessory max level starts at 20 for R0 and increases by 10 per rarity tier
- Gear Dungeon drops random accessory copies
- Three copies of the same slot and rarity can be fused into the next rarity
- Hero upgrades use Myth Essence and are saved locally
- Home screen shows a Next Goal hint for the current progression bottleneck
- Campaign stages now use named enemy data with HP and progression-only clears
- Campaign and dungeon scaling now create clearer upgrade walls with recommended power labels
- Campaign milestones reward Gems and Mission Track XP every 5 stages
- Campaign continues scaling after the starter stages
- Gold Dungeon and Essence Dungeon are endless tower prototypes
- Dungeon bonus floors pay extra resources every 5 floors
- Dungeon floors scale up in enemy HP, enemy damage, and rewards
- Campaign and dungeon fights now simulate win/loss with team HP and enemy damage
- Basic summon flow with Gem cost, rarity rates, hero shards, and saved summon count
- Hero shards add minor Attack and HP immediately
- Hero ascension consumes shards for larger saved stat upgrades
- Daily missions track battles, stage clears, and summons
- Daily mission claims reward Gold, Gems, Myth Essence, and reset by UTC day
- Mission Track XP is earned from daily mission claims
- Mission Track rewards can be claimed in the Shop tab

Changelog:
- 0.2.1: Started Batch 2 data shaping with ID-based client definitions for currencies, heroes, stages, dungeons, rewards, missions, accessory slots/rarities, and the summon banner.
- 0.2.0: Added visible prototype/save version UI, Batch 1 debug resource buttons, compact dungeon labels, and a reset path that clears known local prototype save keys before writing a fresh save.

Development target:
- Android and iOS mobile idle game
- Unity client
- Backend planned separately
