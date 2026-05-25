INSERT INTO common.accessory_slot_definitions (id, display_name, sort_order) VALUES
	('headgear', 'Helm', 60)
ON CONFLICT (id) DO UPDATE SET
	display_name = EXCLUDED.display_name,
	sort_order = EXCLUDED.sort_order;

INSERT INTO common.accessory_definitions (
	id,
	slot_id,
	rarity_id,
	attack_per_level,
	health_per_level,
	drop_weight,
	fuse_target_id
) VALUES
	('accessory_headgear_r0', 'headgear', 'r0', 2, 12, 120, 'accessory_headgear_r1'),
	('accessory_headgear_r1', 'headgear', 'r1', 4, 24, 55, 'accessory_headgear_r2'),
	('accessory_headgear_r2', 'headgear', 'r2', 6, 36, 22, 'accessory_headgear_r3'),
	('accessory_headgear_r3', 'headgear', 'r3', 8, 48, 8, 'accessory_headgear_r4'),
	('accessory_headgear_r4', 'headgear', 'r4', 10, 60, 2, NULL)
ON CONFLICT (id) DO UPDATE SET
	slot_id = EXCLUDED.slot_id,
	rarity_id = EXCLUDED.rarity_id,
	attack_per_level = EXCLUDED.attack_per_level,
	health_per_level = EXCLUDED.health_per_level,
	drop_weight = EXCLUDED.drop_weight,
	fuse_target_id = EXCLUDED.fuse_target_id;
