# Mythwake

Mobile idle RPG prototype built with Unity.

Current prototype:
- Android build profile
- Simple portrait UI
- First core loop: fight enemies, earn Myth Essence, upgrade heroes
- Auto attack while the app is open
- Local save data via PlayerPrefs
- Basic offline Gold and Myth Essence calculation when reopening the app
- Mobile app shell with Home, Battle, Heroes, Summon, and Shop tabs
- Starter hero collection with 5 heroes
- Each starter hero has role-flavored Attack and HP stats
- Team Attack and Team HP are summed from the active hero roster
- Starter roles now affect combat: Warrior and Mage add damage, Tank reduces incoming damage, Support heals, Ranger executes weak enemies
- Gold, Gems, Myth Essence, Pass XP, and Hero Shards are separated
- Hero upgrades use Myth Essence and are saved locally
- Campaign stages now use named enemy data with HP and progression-only clears
- Campaign continues scaling after the starter stages
- Gold Dungeon and Essence Dungeon are endless tower prototypes
- Dungeon floors scale up in enemy HP, enemy damage, and rewards
- Campaign and dungeon fights now simulate win/loss with team HP and enemy damage
- Basic summon flow with Gem cost, rarity rates, hero shards, and saved summon count
- Hero shards add minor Attack and HP immediately
- Hero ascension consumes shards for larger saved stat upgrades
- Daily missions track battles, stage clears, and summons
- Daily mission claims reward Gold, Gems, Myth Essence, and reset by UTC day
- Mission Track XP is earned from daily mission claims
- Mission Track rewards can be claimed in the Shop tab

Development target:
- Android and iOS mobile idle game
- Unity client
- Backend planned separately
