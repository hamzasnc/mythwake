# Mythwake

Mobile idle RPG prototype built with Unity.

Prototype version: 0.2.8
Local save version: 2

Current prototype:
- Android build profile
- Simple portrait UI
- First core loop: fight enemies, earn Myth Essence, upgrade heroes
- Auto attack while the app is open
- Local save data via a versioned JSON blob stored in PlayerPrefs
- Legacy PlayerPrefs scalar keys are migrated into the JSON save on load
- Currency spend/grant actions now go through local economy boundary methods for backend migration
- Shared service contracts now define player state, reward, action result, economy, battle, summon, and inventory boundaries
- Campaign fights, dungeon runs, and summons now return local action-result DTOs
- Accessory equip, level, and fuse actions now return local action-result DTOs
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
- Accessory slot/rarity pairs now have item-like definitions with stable IDs, level caps, stat scaling, drop weights, and fuse targets
- Accessory items have rarity tiers R0-R4, can be equipped, leveled, and saved locally
- Accessory max level starts at 20 for R0 and increases by 10 per rarity tier
- Gear Dungeon drops random accessory copies
- Three copies of the same slot and rarity can be fused into the next rarity
- Hero upgrades use Myth Essence and are saved locally
- Home screen shows a Next Goal hint for the current progression bottleneck
- Campaign stages now use named enemy data with HP and progression-only clears
- Campaign and dungeon scaling now create clearer upgrade walls with recommended power labels
- First balance pass tunes Campaign pressure, dungeon rewards, summon pacing, daily missions, and offline reward caps for the early loop
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
- 0.2.8: Routed accessory equip, level, and fusion through inventory service action-result methods for backend-ready item validation.
- 0.2.7: Routed campaign fights, dungeon runs, and summons through local action-result methods that match the future backend response shape.
- 0.2.6: Added shared service contracts and made the prototype controller expose player state/economy services for the future backend boundary.
- 0.2.5: Started backend-readiness by routing currency spends, reward grants, dungeon rewards, offline rewards, debug rewards, and summons through centralized economy methods.
- 0.2.4: Cleaned the local save into one versioned JSON save blob with legacy PlayerPrefs migration, preparing the client for service/backend boundaries.
- 0.2.3: Tuned early Campaign, dungeon, summon, daily mission, gear drop, and offline reward pacing for the first Balance Pass.
- 0.2.2: Added item-like accessory definitions while keeping the current copy-count inventory, so drops, stats, and fusion now have stable accessory IDs for future item instances.
- 0.2.1: Started Batch 2 data shaping with ID-based client definitions for currencies, heroes, stages, dungeons, rewards, missions, accessory slots/rarities, and the summon banner.
- 0.2.0: Added visible prototype/save version UI, Batch 1 debug resource buttons, compact dungeon labels, and a reset path that clears known local prototype save keys before writing a fresh save.

Development target:
- Android and iOS mobile idle game
- Unity client
- Backend planned separately
