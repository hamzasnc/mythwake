package player

import (
	"context"
	"fmt"
	"strings"
	"sync"

	"github.com/hamzasnc/mythwake/backend/internal/api"
)

const (
	defaultPlayerID  = "dev-player-1"
	goldDungeonID    = "gold_dungeon"
	essenceDungeonID = "essence_dungeon"
	gearDungeonID    = "gear_dungeon"
	heroBannerID     = "hero_shard_standard"
)

type StateStore interface {
	LoadState(ctx context.Context, playerID string) (PersistentState, bool, error)
	SaveState(ctx context.Context, playerID string, state PersistentState) error
}

type PersistentState struct {
	PlayerState    api.PlayerState
	HeroLevels     map[string]int
	HeroShards     map[string]int
	HeroAscensions map[string]int
}

type Service struct {
	mu                 sync.Mutex
	playerID           string
	stateStore         StateStore
	state              api.PlayerState
	heroLevels         map[string]int
	heroShards         map[string]int
	heroAscensions     map[string]int
	accessoryInventory map[string]int
	equippedAccessory  map[string]string
	claimedDaily       map[string]bool
	claimedBattlePass  map[string]bool
	summonCount        int
}

func NewService() *Service {
	return &Service{
		playerID: defaultPlayerID,
		state: api.PlayerState{
			SaveVersion:         1,
			Gold:                0,
			Gems:                35,
			MythEssence:         20,
			PassXP:              0,
			CampaignStage:       1,
			GoldDungeonFloor:    1,
			EssenceDungeonFloor: 1,
			GearDungeonFloor:    1,
			TeamPower:           148,
			TeamAttack:          96,
			TeamHealth:          780,
		},
		heroLevels: map[string]int{
			"hero_astra":  1,
			"hero_borin":  1,
			"hero_cyra":   1,
			"hero_dante":  1,
			"hero_elowen": 1,
		},
		heroShards: map[string]int{
			"hero_astra": 0,
		},
		heroAscensions:     map[string]int{},
		accessoryInventory: map[string]int{},
		equippedAccessory:  map[string]string{},
		claimedDaily:       map[string]bool{},
		claimedBattlePass:  map[string]bool{},
	}
}

func (service *Service) UseStateStore(ctx context.Context, store StateStore) error {
	service.mu.Lock()
	defer service.mu.Unlock()

	service.stateStore = store
	persistedState, found, err := store.LoadState(ctx, service.playerID)
	if err != nil {
		return err
	}
	if found {
		service.applyPersistentState(persistedState)
		return nil
	}

	return store.SaveState(ctx, service.playerID, service.persistentState())
}

func (service *Service) GuestAuth() api.GuestAuthResponse {
	service.mu.Lock()
	defer service.mu.Unlock()

	return api.GuestAuthResponse{
		PlayerID:     service.playerID,
		SessionToken: "dev-session-token",
		PlayerState:  service.state,
	}
}

func (service *Service) GetState() api.PlayerState {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.state
}

func (service *Service) FightCampaign() api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	stage := service.state.CampaignStage
	requiredPower := 90 + (stage * 46)
	if service.state.TeamPower < requiredPower {
		return service.result(false, "campaign_fight", "combat_lost", fmt.Sprintf("Campaign Stage %d failed. Required Power %d.", stage, requiredPower), api.Reward{})
	}

	reward := api.Reward{
		RewardID:    fmt.Sprintf("reward_campaign_stage_%03d", stage),
		MythEssence: 7 + (stage * 4),
	}
	if stage%5 == 0 {
		reward.Gems = 12 + stage
		reward.PassXP = 25
	}

	service.grantReward(reward)
	service.state.CampaignStage++
	return service.result(true, "campaign_fight", "", fmt.Sprintf("Campaign Stage %d cleared.", stage), reward)
}

func (service *Service) RunDungeon(dungeonID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	switch dungeonID {
	case goldDungeonID:
		return service.runResourceDungeon(dungeonID, service.state.GoldDungeonFloor, true)
	case essenceDungeonID:
		return service.runResourceDungeon(dungeonID, service.state.EssenceDungeonFloor, false)
	case gearDungeonID:
		return service.runGearDungeon()
	default:
		return service.result(false, "dungeon_run", "invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", dungeonID), api.Reward{})
	}
}

func (service *Service) LevelHero(heroID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	level, ok := service.heroLevels[heroID]
	if !ok {
		return service.result(false, "hero_level", "invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID), api.Reward{})
	}

	cost := 14 + (level * 6)
	if service.state.MythEssence < cost {
		return service.result(false, "hero_level", "insufficient_currency", fmt.Sprintf("Need %d Myth Essence.", cost), api.Reward{})
	}

	service.state.MythEssence -= cost
	service.heroLevels[heroID] = level + 1
	service.recalculatePower()
	return service.result(true, "hero_level", "", fmt.Sprintf("%s reached Lv. %d.", heroID, level+1), api.Reward{})
}

func (service *Service) AscendHero(heroID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	if _, ok := service.heroLevels[heroID]; !ok {
		return service.result(false, "hero_ascend", "invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID), api.Reward{})
	}

	cost := 20 + (service.heroAscensions[heroID] * 15)
	if service.heroShards[heroID] < cost {
		return service.result(false, "hero_ascend", "insufficient_shards", fmt.Sprintf("Need %d shards.", cost), api.Reward{})
	}

	service.heroShards[heroID] -= cost
	service.heroAscensions[heroID]++
	service.recalculatePower()
	return service.result(true, "hero_ascend", "", fmt.Sprintf("%s ascended.", heroID), api.Reward{})
}

func (service *Service) LevelEquipment(equipmentID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	var cost int
	switch equipmentID {
	case "equipment_weapon":
		cost = 80
	case "equipment_armor":
		cost = 75
	default:
		return service.result(false, "equipment_level", "invalid_equipment", fmt.Sprintf("Unknown equipment: %s", equipmentID), api.Reward{})
	}

	if service.state.Gold < cost {
		return service.result(false, "equipment_level", "insufficient_currency", fmt.Sprintf("Need %d Gold.", cost), api.Reward{})
	}

	service.state.Gold -= cost
	service.recalculatePower()
	return service.result(true, "equipment_level", "", fmt.Sprintf("%s leveled.", equipmentID), api.Reward{})
}

func (service *Service) EquipAccessory(accessoryID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	if service.accessoryInventory[accessoryID] <= 0 {
		return service.result(false, "accessory_equip", "missing_item", fmt.Sprintf("Missing accessory: %s", accessoryID), api.Reward{})
	}

	slotID := accessorySlot(accessoryID)
	if current := service.equippedAccessory[slotID]; current != "" {
		service.accessoryInventory[current]++
	}

	service.accessoryInventory[accessoryID]--
	service.equippedAccessory[slotID] = accessoryID
	service.recalculatePower()
	return service.result(true, "accessory_equip", "", fmt.Sprintf("Equipped %s.", accessoryID), api.Reward{})
}

func (service *Service) LevelAccessory(accessoryID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	if service.state.Gold < 35 {
		return service.result(false, "accessory_level", "insufficient_currency", "Need 35 Gold.", api.Reward{})
	}

	service.state.Gold -= 35
	service.recalculatePower()
	return service.result(true, "accessory_level", "", fmt.Sprintf("Leveled %s.", accessoryID), api.Reward{})
}

func (service *Service) FuseAccessory(accessoryID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	if service.accessoryInventory[accessoryID] < 3 {
		return service.result(false, "accessory_fuse", "missing_items", fmt.Sprintf("Need 3 copies of %s.", accessoryID), api.Reward{})
	}

	service.accessoryInventory[accessoryID] -= 3
	fusedID := accessoryID + "_fused"
	service.accessoryInventory[fusedID]++
	return service.result(true, "accessory_fuse", "", fmt.Sprintf("Fused into %s.", fusedID), api.Reward{})
}

func (service *Service) PullSummon(bannerID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	if bannerID != heroBannerID {
		return service.result(false, "summon_pull", "invalid_banner", fmt.Sprintf("Unknown banner: %s", bannerID), api.Reward{})
	}

	if service.state.Gems < 35 {
		return service.result(false, "summon_pull", "insufficient_currency", "Need 35 Gems.", api.Reward{})
	}

	heroes := []string{"hero_astra", "hero_borin", "hero_cyra", "hero_dante", "hero_elowen"}
	heroID := heroes[service.summonCount%len(heroes)]
	service.summonCount++
	service.state.Gems -= 35
	service.heroShards[heroID] += 7
	service.recalculatePower()
	return service.result(true, "summon_pull", "", fmt.Sprintf("Pulled %s shards.", heroID), api.Reward{RewardID: "reward_summon_shards"})
}

func (service *Service) ClaimDailyMission(missionID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	if service.claimedDaily[missionID] {
		return service.result(false, "daily_mission_claim", "already_claimed", fmt.Sprintf("%s already claimed.", missionID), api.Reward{})
	}

	reward := api.Reward{RewardID: "reward_" + missionID, Gold: 40, Gems: 5, MythEssence: 70, PassXP: 40}
	service.claimedDaily[missionID] = true
	service.grantReward(reward)
	return service.result(true, "daily_mission_claim", "", fmt.Sprintf("Claimed %s.", missionID), reward)
}

func (service *Service) ClaimBattlePassReward(rewardID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	if service.claimedBattlePass[rewardID] {
		return service.result(false, "battle_pass_claim", "already_claimed", fmt.Sprintf("%s already claimed.", rewardID), api.Reward{})
	}

	if service.state.PassXP < 40 {
		return service.result(false, "battle_pass_claim", "not_unlocked", "Need 40 Pass XP.", api.Reward{})
	}

	reward := api.Reward{RewardID: rewardID, Gold: 100, Gems: 10}
	service.claimedBattlePass[rewardID] = true
	service.grantReward(reward)
	return service.result(true, "battle_pass_claim", "", fmt.Sprintf("Claimed %s.", rewardID), reward)
}

func (service *Service) runResourceDungeon(dungeonID string, floor int, isGold bool) api.ActionResult {
	requiredPower := 100 + (floor * 50)
	if service.state.TeamPower < requiredPower {
		return service.result(false, dungeonID+"_run", "combat_lost", fmt.Sprintf("Floor %d failed. Required Power %d.", floor, requiredPower), api.Reward{})
	}

	reward := api.Reward{RewardID: fmt.Sprintf("reward_%s_floor_%d", dungeonID, floor)}
	if isGold {
		reward.Gold = 95 + (floor * 34)
		service.state.GoldDungeonFloor++
	} else {
		reward.MythEssence = 110 + (floor * 40)
		service.state.EssenceDungeonFloor++
	}

	service.grantReward(reward)
	return service.result(true, dungeonID+"_run", "", fmt.Sprintf("%s floor %d cleared.", dungeonID, floor), reward)
}

func (service *Service) runGearDungeon() api.ActionResult {
	floor := service.state.GearDungeonFloor
	requiredPower := 120 + (floor * 56)
	if service.state.TeamPower < requiredPower {
		return service.result(false, "gear_dungeon_run", "combat_lost", fmt.Sprintf("Floor %d failed. Required Power %d.", floor, requiredPower), api.Reward{})
	}

	accessoryID := "accessory_earrings_r0"
	service.accessoryInventory[accessoryID]++
	service.state.GearDungeonFloor++
	return service.result(true, "gear_dungeon_run", "", fmt.Sprintf("Dropped %s.", accessoryID), api.Reward{RewardID: "reward_gear_drop"})
}

func (service *Service) grantReward(reward api.Reward) {
	service.state.Gold += reward.Gold
	service.state.Gems += reward.Gems
	service.state.MythEssence += reward.MythEssence
	service.state.PassXP += reward.PassXP
}

func (service *Service) result(success bool, actionID string, errorCode string, message string, reward api.Reward) api.ActionResult {
	if success {
		if err := service.saveState(); err != nil {
			return api.ActionResult{
				Success:     false,
				ActionID:    actionID,
				ErrorCode:   "persistence_failed",
				Message:     fmt.Sprintf("Action succeeded locally but could not be saved: %v", err),
				PlayerState: service.state,
				Reward:      api.Reward{},
			}
		}
	}

	return api.ActionResult{
		Success:     success,
		ActionID:    actionID,
		ErrorCode:   errorCode,
		Message:     message,
		PlayerState: service.state,
		Reward:      reward,
	}
}

func (service *Service) saveState() error {
	if service.stateStore == nil {
		return nil
	}

	return service.stateStore.SaveState(context.Background(), service.playerID, service.persistentState())
}

func (service *Service) persistentState() PersistentState {
	return PersistentState{
		PlayerState:    service.state,
		HeroLevels:     cloneIntMap(service.heroLevels),
		HeroShards:     cloneIntMap(service.heroShards),
		HeroAscensions: cloneIntMap(service.heroAscensions),
	}
}

func (service *Service) applyPersistentState(state PersistentState) {
	service.state = state.PlayerState
	service.heroLevels = mergeIntMaps(service.heroLevels, state.HeroLevels)
	service.heroShards = mergeIntMaps(service.heroShards, state.HeroShards)
	service.heroAscensions = mergeIntMaps(service.heroAscensions, state.HeroAscensions)
	service.recalculatePower()
}

func cloneIntMap(values map[string]int) map[string]int {
	cloned := make(map[string]int, len(values))
	for key, value := range values {
		cloned[key] = value
	}
	return cloned
}

func mergeIntMaps(defaults map[string]int, persisted map[string]int) map[string]int {
	merged := cloneIntMap(defaults)
	for key, value := range persisted {
		merged[key] = value
	}
	return merged
}

func (service *Service) recalculatePower() {
	power := 148
	for _, level := range service.heroLevels {
		power += level * 8
	}
	for _, ascension := range service.heroAscensions {
		power += ascension * 45
	}
	service.state.TeamPower = power
	service.state.TeamAttack = 96 + (power / 8)
	service.state.TeamHealth = 780 + (power * 2)
}

func accessorySlot(accessoryID string) string {
	switch {
	case strings.HasPrefix(accessoryID, "accessory_earrings"):
		return "earrings"
	case strings.HasPrefix(accessoryID, "accessory_necklace"):
		return "necklace"
	case strings.HasPrefix(accessoryID, "accessory_bracelet"):
		return "bracelet"
	case strings.HasPrefix(accessoryID, "accessory_gloves"):
		return "gloves"
	case strings.HasPrefix(accessoryID, "accessory_shoes"):
		return "shoes"
	default:
		return "unknown"
	}
}
