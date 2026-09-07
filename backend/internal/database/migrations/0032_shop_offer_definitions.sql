CREATE TABLE IF NOT EXISTS common.shop_offer_definitions (
	tab text NOT NULL CHECK (tab IN ('featured', 'crystals', 'bundles', 'battle_pass')),
	id text NOT NULL,
	display_name text NOT NULL,
	contents text NOT NULL,
	price text NOT NULL,
	icon_key text NOT NULL,
	sort_order integer NOT NULL,
	top_pick boolean NOT NULL DEFAULT false,
	badge_label text NOT NULL DEFAULT '',
	active boolean NOT NULL DEFAULT true,
	created_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (tab, id),
	UNIQUE (tab, sort_order)
);

CREATE INDEX IF NOT EXISTS idx_shop_offer_active_order
	ON common.shop_offer_definitions (tab, active, sort_order, id);

INSERT INTO common.shop_offer_definitions (
	tab, id, display_name, contents, price, icon_key, sort_order, top_pick, badge_label
) VALUES
	('featured', 'starter_pack', 'Starter Pack', '500 Crystals\n25K Gold · 5 Essence', '€2.99', 'icon_gold', 10, false, ''),
	('featured', 'crystal_cache', 'Crystal Cache', '1,100 Crystals\n60K Gold · 5 Essence', '€4.99', 'icon_gems', 20, false, ''),
	('featured', 'adventurer_bundle', 'Adventurer Bundle', '2,200 Crystals\n120K Gold · 15 Essence', '€14.99', 'home_shop_button', 30, false, ''),
	('featured', 'legendary_chest', 'Legendary Chest', '5,000 Crystals\n250K Gold · 25 Essence', '€19.99', 'home_treasure_chest_button', 40, false, ''),
	('crystals', 'crystal_pouch', 'Crystal Pouch', '100 Myth Crystals\nStarter stash', '€0.99', 'shop_icon_crystal_altar', 10, false, ''),
	('crystals', 'crystal_pack', 'Crystal Pack', '500 Myth Crystals\n+ 5% bonus', '€3.99', 'shop_icon_crystal_vault', 20, false, ''),
	('crystals', 'crystal_cache', 'Crystal Cache', '1,100 Myth Crystals\n+ 10% bonus', '€7.99', 'shop_icon_crystal_altar', 30, true, 'BEST VALUE'),
	('crystals', 'crystal_vault', 'Crystal Vault', '2,500 Myth Crystals\n+ 25% bonus', '€14.99', 'shop_icon_crystal_vault', 40, false, ''),
	('crystals', 'crystal_reserve', 'Crystal Reserve', '5,000 Myth Crystals\n+ 35% bonus', '€24.99', 'shop_icon_crystal_altar', 50, false, ''),
	('crystals', 'crystal_treasury', 'Crystal Treasury', '12,000 Myth Crystals\n+ 45% bonus', '€49.99', 'shop_icon_crystal_vault', 60, true, 'BEST VALUE'),
	('crystals', 'crystal_hoard', 'Crystal Hoard', '25,000 Myth Crystals\n+ 55% bonus', '€89.99', 'shop_icon_crystal_altar', 70, false, ''),
	('crystals', 'crystal_relic', 'Ancient Relic Cache', '50,000 Myth Crystals\n+ 70% bonus', '€149.99', 'shop_icon_crystal_vault', 80, false, ''),
	('crystals', 'crystal_ascendant', 'Ascendant Crystals', '100,000 Myth Crystals\n+ 90% bonus', '€249.99', 'shop_icon_crystal_altar', 90, false, ''),
	('crystals', 'crystal_eternal', 'Eternal Crystal Vault', '250,000 Myth Crystals\n+ 120% bonus', '€499.99', 'shop_icon_crystal_vault', 100, false, ''),
	('bundles', 'daily_deal', 'Daily Deal', '250 Crystals\n15K Gold · 2 Essence', '€1.99', 'shop_icon_adventurer_satchel', 10, false, ''),
	('bundles', 'hero_bundle', 'Hero Bundle', '1,000 Crystals\nHero Shard Chest ×2', '€8.99', 'shop_icon_bundle_chest', 20, false, ''),
	('bundles', 'adventurer_bundle', 'Adventurer Bundle', '2,200 Crystals\n120K Gold · 15 Essence', '€14.99', 'shop_icon_adventurer_satchel', 30, true, 'TOP PICK'),
	('bundles', 'legendary_chest', 'Legendary Chest', '5,000 Crystals\n250K Gold · 25 Essence', '€19.99', 'shop_icon_bundle_chest', 40, false, ''),
	('bundles', 'dungeon_expedition', 'Dungeon Expedition', '3,500 Crystals\n150K Gold · 20 Essence', '€24.99', 'shop_icon_adventurer_satchel', 50, false, ''),
	('bundles', 'guild_foundry', 'Guild Foundry Pack', '4,500 Crystals\n200K Gold · 30 Essence', '€34.99', 'shop_icon_bundle_chest', 60, false, ''),
	('bundles', 'royal_war_chest', 'Royal War Chest', '8,000 Crystals\n400K Gold · 45 Essence', '€49.99', 'shop_icon_bundle_chest', 70, true, 'TOP PICK'),
	('bundles', 'mythic_arsenal', 'Mythic Arsenal', '12,000 Crystals\n650K Gold · 60 Essence', '€69.99', 'shop_icon_adventurer_satchel', 80, false, ''),
	('bundles', 'worldbreaker_cache', 'Worldbreaker Cache', '20,000 Crystals\n1M Gold · 90 Essence', '€99.99', 'shop_icon_bundle_chest', 90, false, ''),
	('bundles', 'founder_legacy', 'Founder’s Legacy', '35,000 Crystals\n2M Gold · 120 Essence', '€149.99', 'shop_icon_adventurer_satchel', 100, false, '')
ON CONFLICT (tab, id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	contents = EXCLUDED.contents,
	price = EXCLUDED.price,
	icon_key = EXCLUDED.icon_key,
	sort_order = EXCLUDED.sort_order,
	top_pick = EXCLUDED.top_pick,
	badge_label = EXCLUDED.badge_label,
	active = true;
