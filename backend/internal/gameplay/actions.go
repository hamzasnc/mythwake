package gameplay

const (
	ActionPlayerStateSeed  = "player_state_seed"
	ActionPlayerStateFlush = "player_state_flush"
	ActionStateCacheFlush  = "state_cache_flush"

	ActionCampaignFight     = "campaign_fight"
	ActionDungeonRun        = "dungeon_run"
	ActionGoldDungeonRun    = "gold_dungeon_run"
	ActionEssenceDungeonRun = "essence_dungeon_run"
	ActionGearDungeonRun    = "gear_dungeon_run"
	ActionHeroLevel         = "hero_level"
	ActionHeroAscend        = "hero_ascend"
	ActionEquipmentLevel    = "equipment_level"
	ActionAccessoryEquip    = "accessory_equip"
	ActionAccessoryLevel    = "accessory_level"
	ActionAccessoryFuse     = "accessory_fuse"
	ActionSummonPull        = "summon_pull"
	ActionDailyMissionClaim = "daily_mission_claim"
	ActionBattlePassClaim   = "battle_pass_claim"
)

type ActionDefinition struct {
	ID                  string
	Domain              string
	RequiresIdempotency bool
	MaterializedByFlush bool
}

func ActionCatalog() []ActionDefinition {
	return []ActionDefinition{
		{ID: ActionCampaignFight, Domain: "campaign", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionDungeonRun, Domain: "dungeon", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionGoldDungeonRun, Domain: "dungeon", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionEssenceDungeonRun, Domain: "dungeon", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionGearDungeonRun, Domain: "dungeon", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionHeroLevel, Domain: "hero", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionHeroAscend, Domain: "hero", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionEquipmentLevel, Domain: "equipment", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionAccessoryEquip, Domain: "inventory", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionAccessoryLevel, Domain: "inventory", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionAccessoryFuse, Domain: "inventory", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionSummonPull, Domain: "summon", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionDailyMissionClaim, Domain: "mission", RequiresIdempotency: true, MaterializedByFlush: true},
		{ID: ActionBattlePassClaim, Domain: "battle_pass", RequiresIdempotency: true, MaterializedByFlush: true},
	}
}

func ActionDefinitionByID(actionID string) (ActionDefinition, bool) {
	for _, definition := range ActionCatalog() {
		if definition.ID == actionID {
			return definition, true
		}
	}

	return ActionDefinition{}, false
}
