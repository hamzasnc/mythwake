package player

import (
	"context"
	"fmt"
	"sort"
	"strings"
	"sync"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

const (
	defaultPlayerID  = "dev-player-1"
	goldDungeonID    = "gold_dungeon"
	essenceDungeonID = "essence_dungeon"
	gearDungeonID    = "gear_dungeon"
	heroBannerID     = "hero_shard_standard"
	weaponID         = "equipment_weapon"
	armorID          = "equipment_armor"
)

type StateStore interface {
	LoadState(ctx context.Context, playerID string) (PersistentState, bool, error)
	SaveState(ctx context.Context, playerID string, state PersistentState, source StateSaveSource) error
}

type ActionResultStore interface {
	LoadActionResult(ctx context.Context, playerID string, idempotencyKey string) (StoredActionResult, bool, error)
}

type ActionResultWriter interface {
	SaveActionResult(ctx context.Context, playerID string, source StateSaveSource) error
}

type StateFlusher interface {
	Flush(ctx context.Context) error
}

type PersistentState struct {
	PlayerState        api.PlayerState
	HeroLevels         map[string]int
	HeroShards         map[string]int
	HeroAscensions     map[string]int
	EquipmentLevels    map[string]int
	AccessoryInventory map[string]int
	AccessoryLevels    map[string]int
	EquippedAccessory  map[string]string
	ClaimedDaily       map[string]bool
	ClaimedBattlePass  map[string]bool
	SummonCount        int
}

type StateSaveSource struct {
	ActionID       string
	RewardID       string
	IdempotencyKey string
	RequestHash    string
	CurrencyDelta  api.Reward
	ActionResult   *api.ActionResult
}

type ActionRequest struct {
	IdempotencyKey string
	RequestHash    string
}

type StoredActionResult struct {
	ActionID     string
	RequestHash  string
	ActionResult api.ActionResult
}

func (request ActionRequest) HasIdempotency() bool {
	return request.IdempotencyKey != "" && request.RequestHash != ""
}

func ClonePersistentState(state PersistentState) PersistentState {
	return PersistentState{
		PlayerState:        state.PlayerState,
		HeroLevels:         cloneIntMap(state.HeroLevels),
		HeroShards:         cloneIntMap(state.HeroShards),
		HeroAscensions:     cloneIntMap(state.HeroAscensions),
		EquipmentLevels:    cloneIntMap(state.EquipmentLevels),
		AccessoryInventory: cloneIntMap(state.AccessoryInventory),
		AccessoryLevels:    cloneIntMap(state.AccessoryLevels),
		EquippedAccessory:  cloneStringMap(state.EquippedAccessory),
		ClaimedDaily:       cloneBoolMap(state.ClaimedDaily),
		ClaimedBattlePass:  cloneBoolMap(state.ClaimedBattlePass),
		SummonCount:        state.SummonCount,
	}
}

type Service struct {
	mu                 sync.Mutex
	playerID           string
	stateStore         StateStore
	actionResultStore  ActionResultStore
	state              api.PlayerState
	heroLevels         map[string]int
	heroShards         map[string]int
	heroAscensions     map[string]int
	equipmentLevels    map[string]int
	accessoryInventory map[string]int
	accessoryLevels    map[string]int
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
		equipmentLevels:    map[string]int{weaponID: 0, armorID: 0},
		accessoryInventory: map[string]int{},
		accessoryLevels:    map[string]int{},
		equippedAccessory:  map[string]string{},
		claimedDaily:       map[string]bool{},
		claimedBattlePass:  map[string]bool{},
	}
}

func (service *Service) UseStateStore(ctx context.Context, store StateStore) error {
	service.mu.Lock()
	defer service.mu.Unlock()

	service.stateStore = store
	if actionResultStore, ok := store.(ActionResultStore); ok {
		service.actionResultStore = actionResultStore
	}
	persistedState, found, err := store.LoadState(ctx, service.playerID)
	if err != nil {
		return err
	}
	if found {
		service.applyPersistentState(persistedState)
		return nil
	}

	return store.SaveState(ctx, service.playerID, service.persistentState(), StateSaveSource{ActionID: gameplay.ActionPlayerStateSeed})
}

func (service *Service) GuestAuth() api.GuestAuthResponse {
	service.mu.Lock()
	defer service.mu.Unlock()

	return api.GuestAuthResponse{
		PlayerID:       service.playerID,
		SessionToken:   "dev-session-token",
		PlayerState:    service.state,
		PlayerSnapshot: service.snapshot(),
	}
}

func (service *Service) GetState() api.PlayerState {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.state
}

func (service *Service) GetSnapshot() api.PlayerSnapshot {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.snapshot()
}

func (service *Service) FlushState(ctx context.Context) error {
	service.mu.Lock()
	store := service.stateStore
	if store == nil {
		service.mu.Unlock()
		return nil
	}

	playerID := service.playerID
	state := service.persistentState()
	service.mu.Unlock()

	if err := store.SaveState(ctx, playerID, state, StateSaveSource{ActionID: gameplay.ActionPlayerStateFlush}); err != nil {
		return err
	}

	flusher, ok := store.(StateFlusher)
	if !ok {
		return nil
	}

	return flusher.Flush(ctx)
}

type actionOutcome struct {
	success   bool
	errorCode string
	message   string
	reward    api.Reward
}

func actionSuccess(message string, reward api.Reward) actionOutcome {
	return actionOutcome{
		success: true,
		message: message,
		reward:  reward,
	}
}

func actionFailure(errorCode string, message string) actionOutcome {
	return actionOutcome{
		errorCode: errorCode,
		message:   message,
	}
}

func (service *Service) executeAction(ctx context.Context, request ActionRequest, actionID string, run func() actionOutcome) api.ActionResult {
	if result, handled := service.replayedActionResult(ctx, request, actionID); handled {
		return result
	}

	beforeState := service.persistentState()
	outcome := run()
	result := service.newActionResult(outcome.success, actionID, request.IdempotencyKey, false, outcome.errorCode, outcome.message, outcome.reward)
	if !outcome.success {
		return result
	}

	delta := currencyDelta(beforeState.PlayerState, service.state)
	if err := service.saveState(ctx, request, actionID, outcome.reward, delta, result); err != nil {
		service.applyPersistentState(beforeState)
		return service.newActionResult(false, actionID, request.IdempotencyKey, false, "persistence_failed", fmt.Sprintf("Action could not be saved and was rolled back: %v", err), api.Reward{})
	}

	return result
}

func (service *Service) replayedActionResult(ctx context.Context, request ActionRequest, actionID string) (api.ActionResult, bool) {
	if !request.HasIdempotency() || service.actionResultStore == nil {
		return api.ActionResult{}, false
	}

	record, found, err := service.actionResultStore.LoadActionResult(ctx, service.playerID, request.IdempotencyKey)
	if err != nil {
		return service.newActionResult(false, actionID, request.IdempotencyKey, false, "idempotency_lookup_failed", fmt.Sprintf("Could not verify idempotency key: %v", err), api.Reward{}), true
	}
	if !found {
		return api.ActionResult{}, false
	}
	if record.ActionID != actionID || record.RequestHash != request.RequestHash {
		return service.newActionResult(false, actionID, request.IdempotencyKey, false, "idempotency_conflict", "Idempotency key was already used for a different action request.", api.Reward{}), true
	}

	result := record.ActionResult
	result.IdempotencyKey = request.IdempotencyKey
	result.Replay = true
	return result, true
}

func (service *Service) newActionResult(success bool, actionID string, idempotencyKey string, replay bool, errorCode string, message string, reward api.Reward) api.ActionResult {
	return api.ActionResult{
		Success:        success,
		ActionID:       actionID,
		IdempotencyKey: idempotencyKey,
		Replay:         replay,
		ErrorCode:      errorCode,
		Message:        message,
		PlayerState:    service.state,
		PlayerSnapshot: service.snapshot(),
		Reward:         reward,
	}
}

func (service *Service) snapshot() api.PlayerSnapshot {
	return api.PlayerSnapshot{
		PlayerID:          service.playerID,
		State:             service.state,
		Heroes:            heroStates(service.heroLevels, service.heroAscensions),
		HeroShards:        heroShardStates(service.heroShards),
		Equipment:         equipmentStates(service.equipmentLevels),
		Accessories:       accessoryStates(service.accessoryInventory, service.accessoryLevels),
		EquippedAccessory: equippedAccessoryStates(service.equippedAccessory),
		DailyClaims:       claimStates(service.claimedDaily),
		BattlePassClaims:  claimStates(service.claimedBattlePass),
		SummonCount:       service.summonCount,
	}
}

func (service *Service) FightCampaign() api.ActionResult {
	return service.FightCampaignWithRequest(context.Background(), ActionRequest{})
}

func (service *Service) FightCampaignWithRequest(ctx context.Context, request ActionRequest) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionCampaignFight, func() actionOutcome {
		stage := service.state.CampaignStage
		requiredPower := 90 + (stage * 46)
		if service.state.TeamPower < requiredPower {
			return actionFailure("combat_lost", fmt.Sprintf("Campaign Stage %d failed. Required Power %d.", stage, requiredPower))
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
		return actionSuccess(fmt.Sprintf("Campaign Stage %d cleared.", stage), reward)
	})
}

func (service *Service) RunDungeon(dungeonID string) api.ActionResult {
	return service.RunDungeonWithRequest(context.Background(), ActionRequest{}, dungeonID)
}

func (service *Service) RunDungeonWithRequest(ctx context.Context, request ActionRequest, dungeonID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, dungeonActionID(dungeonID), func() actionOutcome {
		switch dungeonID {
		case goldDungeonID:
			return service.runResourceDungeon(dungeonID, service.state.GoldDungeonFloor, true)
		case essenceDungeonID:
			return service.runResourceDungeon(dungeonID, service.state.EssenceDungeonFloor, false)
		case gearDungeonID:
			return service.runGearDungeon()
		default:
			return actionFailure("invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", dungeonID))
		}
	})
}

func dungeonActionID(dungeonID string) string {
	switch dungeonID {
	case goldDungeonID:
		return gameplay.ActionGoldDungeonRun
	case essenceDungeonID:
		return gameplay.ActionEssenceDungeonRun
	case gearDungeonID:
		return gameplay.ActionGearDungeonRun
	default:
		return gameplay.ActionDungeonRun
	}
}

func (service *Service) LevelHero(heroID string) api.ActionResult {
	return service.LevelHeroWithRequest(context.Background(), ActionRequest{}, heroID)
}

func (service *Service) LevelHeroWithRequest(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionHeroLevel, func() actionOutcome {
		level, ok := service.heroLevels[heroID]
		if !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID))
		}

		cost := 14 + (level * 6)
		if service.state.MythEssence < cost {
			return actionFailure("insufficient_currency", fmt.Sprintf("Need %d Myth Essence.", cost))
		}

		service.state.MythEssence -= cost
		service.heroLevels[heroID] = level + 1
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", heroID, level+1), api.Reward{})
	})
}

func (service *Service) AscendHero(heroID string) api.ActionResult {
	return service.AscendHeroWithRequest(context.Background(), ActionRequest{}, heroID)
}

func (service *Service) AscendHeroWithRequest(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionHeroAscend, func() actionOutcome {
		if _, ok := service.heroLevels[heroID]; !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID))
		}

		cost := 20 + (service.heroAscensions[heroID] * 15)
		if service.heroShards[heroID] < cost {
			return actionFailure("insufficient_shards", fmt.Sprintf("Need %d shards.", cost))
		}

		service.heroShards[heroID] -= cost
		service.heroAscensions[heroID]++
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s ascended.", heroID), api.Reward{})
	})
}

func (service *Service) LevelEquipment(equipmentID string) api.ActionResult {
	return service.LevelEquipmentWithRequest(context.Background(), ActionRequest{}, equipmentID)
}

func (service *Service) LevelEquipmentWithRequest(ctx context.Context, request ActionRequest, equipmentID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionEquipmentLevel, func() actionOutcome {
		level, ok := service.equipmentLevels[equipmentID]
		if !ok {
			return actionFailure("invalid_equipment", fmt.Sprintf("Unknown equipment: %s", equipmentID))
		}

		var baseCost int
		switch equipmentID {
		case weaponID:
			baseCost = 80
		case armorID:
			baseCost = 75
		}

		cost := baseCost + (level * 35)
		if service.state.Gold < cost {
			return actionFailure("insufficient_currency", fmt.Sprintf("Need %d Gold.", cost))
		}

		service.state.Gold -= cost
		service.equipmentLevels[equipmentID] = level + 1
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", equipmentID, level+1), api.Reward{})
	})
}

func (service *Service) EquipAccessory(accessoryID string) api.ActionResult {
	return service.EquipAccessoryWithRequest(context.Background(), ActionRequest{}, accessoryID)
}

func (service *Service) EquipAccessoryWithRequest(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionAccessoryEquip, func() actionOutcome {
		if service.accessoryInventory[accessoryID] <= 0 {
			return actionFailure("missing_item", fmt.Sprintf("Missing accessory: %s", accessoryID))
		}

		slotID := accessorySlot(accessoryID)
		if current := service.equippedAccessory[slotID]; current != "" {
			service.accessoryInventory[current]++
		}

		service.accessoryInventory[accessoryID]--
		service.equippedAccessory[slotID] = accessoryID
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("Equipped %s.", accessoryID), api.Reward{})
	})
}

func (service *Service) LevelAccessory(accessoryID string) api.ActionResult {
	return service.LevelAccessoryWithRequest(context.Background(), ActionRequest{}, accessoryID)
}

func (service *Service) LevelAccessoryWithRequest(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionAccessoryLevel, func() actionOutcome {
		if service.accessoryInventory[accessoryID] <= 0 && !service.accessoryIsEquipped(accessoryID) {
			return actionFailure("missing_item", fmt.Sprintf("Missing accessory: %s.", accessoryID))
		}

		if service.state.Gold < 35 {
			return actionFailure("insufficient_currency", "Need 35 Gold.")
		}

		service.state.Gold -= 35
		service.accessoryLevels[accessoryID]++
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", accessoryID, service.accessoryLevels[accessoryID]), api.Reward{})
	})
}

func (service *Service) FuseAccessory(accessoryID string) api.ActionResult {
	return service.FuseAccessoryWithRequest(context.Background(), ActionRequest{}, accessoryID)
}

func (service *Service) FuseAccessoryWithRequest(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionAccessoryFuse, func() actionOutcome {
		if service.accessoryInventory[accessoryID] < 3 {
			return actionFailure("missing_items", fmt.Sprintf("Need 3 copies of %s.", accessoryID))
		}

		service.accessoryInventory[accessoryID] -= 3
		fusedID, ok := accessoryFuseTarget(accessoryID)
		if !ok {
			return actionFailure("max_rarity", fmt.Sprintf("%s cannot be fused further.", accessoryID))
		}

		service.accessoryInventory[fusedID]++
		return actionSuccess(fmt.Sprintf("Fused into %s.", fusedID), api.Reward{})
	})
}

func (service *Service) PullSummon(bannerID string) api.ActionResult {
	return service.PullSummonWithRequest(context.Background(), ActionRequest{}, bannerID)
}

func (service *Service) PullSummonWithRequest(ctx context.Context, request ActionRequest, bannerID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionSummonPull, func() actionOutcome {
		if bannerID != heroBannerID {
			return actionFailure("invalid_banner", fmt.Sprintf("Unknown banner: %s", bannerID))
		}

		if service.state.Gems < 35 {
			return actionFailure("insufficient_currency", "Need 35 Gems.")
		}

		heroes := []string{"hero_astra", "hero_borin", "hero_cyra", "hero_dante", "hero_elowen"}
		heroID := heroes[service.summonCount%len(heroes)]
		service.summonCount++
		service.state.Gems -= 35
		service.heroShards[heroID] += 7
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("Pulled %s shards.", heroID), api.Reward{RewardID: "reward_summon_shards"})
	})
}

func (service *Service) ClaimDailyMission(missionID string) api.ActionResult {
	return service.ClaimDailyMissionWithRequest(context.Background(), ActionRequest{}, missionID)
}

func (service *Service) ClaimDailyMissionWithRequest(ctx context.Context, request ActionRequest, missionID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionDailyMissionClaim, func() actionOutcome {
		if service.claimedDaily[missionID] {
			return actionFailure("already_claimed", fmt.Sprintf("%s already claimed.", missionID))
		}

		reward := api.Reward{RewardID: "reward_" + missionID, Gold: 40, Gems: 5, MythEssence: 70, PassXP: 40}
		service.claimedDaily[missionID] = true
		service.grantReward(reward)
		return actionSuccess(fmt.Sprintf("Claimed %s.", missionID), reward)
	})
}

func (service *Service) ClaimBattlePassReward(rewardID string) api.ActionResult {
	return service.ClaimBattlePassRewardWithRequest(context.Background(), ActionRequest{}, rewardID)
}

func (service *Service) ClaimBattlePassRewardWithRequest(ctx context.Context, request ActionRequest, rewardID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionBattlePassClaim, func() actionOutcome {
		if service.claimedBattlePass[rewardID] {
			return actionFailure("already_claimed", fmt.Sprintf("%s already claimed.", rewardID))
		}

		if service.state.PassXP < 40 {
			return actionFailure("not_unlocked", "Need 40 Pass XP.")
		}

		reward := api.Reward{RewardID: rewardID, Gold: 100, Gems: 10}
		service.claimedBattlePass[rewardID] = true
		service.grantReward(reward)
		return actionSuccess(fmt.Sprintf("Claimed %s.", rewardID), reward)
	})
}

func (service *Service) runResourceDungeon(dungeonID string, floor int, isGold bool) actionOutcome {
	requiredPower := 100 + (floor * 50)
	if service.state.TeamPower < requiredPower {
		return actionFailure("combat_lost", fmt.Sprintf("Floor %d failed. Required Power %d.", floor, requiredPower))
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
	return actionSuccess(fmt.Sprintf("%s floor %d cleared.", dungeonID, floor), reward)
}

func (service *Service) runGearDungeon() actionOutcome {
	floor := service.state.GearDungeonFloor
	requiredPower := 120 + (floor * 56)
	if service.state.TeamPower < requiredPower {
		return actionFailure("combat_lost", fmt.Sprintf("Floor %d failed. Required Power %d.", floor, requiredPower))
	}

	accessoryID := "accessory_earrings_r0"
	service.accessoryInventory[accessoryID]++
	service.state.GearDungeonFloor++
	return actionSuccess(fmt.Sprintf("Dropped %s.", accessoryID), api.Reward{RewardID: "reward_gear_drop"})
}

func (service *Service) grantReward(reward api.Reward) {
	service.state.Gold += reward.Gold
	service.state.Gems += reward.Gems
	service.state.MythEssence += reward.MythEssence
	service.state.PassXP += reward.PassXP
}

func currencyDelta(before api.PlayerState, after api.PlayerState) api.Reward {
	return api.Reward{
		Gold:        after.Gold - before.Gold,
		Gems:        after.Gems - before.Gems,
		MythEssence: after.MythEssence - before.MythEssence,
		PassXP:      after.PassXP - before.PassXP,
	}
}

func (service *Service) saveState(ctx context.Context, request ActionRequest, actionID string, reward api.Reward, delta api.Reward, result api.ActionResult) error {
	if service.stateStore == nil {
		return nil
	}

	source := StateSaveSource{
		ActionID:       actionID,
		RewardID:       reward.RewardID,
		IdempotencyKey: request.IdempotencyKey,
		RequestHash:    request.RequestHash,
		CurrencyDelta:  delta,
	}
	if request.HasIdempotency() {
		source.ActionResult = &result
	}

	return service.stateStore.SaveState(ctx, service.playerID, service.persistentState(), source)
}

func (service *Service) persistentState() PersistentState {
	return ClonePersistentState(PersistentState{
		PlayerState:        service.state,
		HeroLevels:         service.heroLevels,
		HeroShards:         service.heroShards,
		HeroAscensions:     service.heroAscensions,
		EquipmentLevels:    service.equipmentLevels,
		AccessoryInventory: service.accessoryInventory,
		AccessoryLevels:    service.accessoryLevels,
		EquippedAccessory:  service.equippedAccessory,
		ClaimedDaily:       service.claimedDaily,
		ClaimedBattlePass:  service.claimedBattlePass,
		SummonCount:        service.summonCount,
	})
}

func (service *Service) applyPersistentState(state PersistentState) {
	service.state = state.PlayerState
	service.heroLevels = mergeIntMaps(service.heroLevels, state.HeroLevels)
	service.heroShards = mergeIntMaps(service.heroShards, state.HeroShards)
	service.heroAscensions = mergeIntMaps(service.heroAscensions, state.HeroAscensions)
	service.equipmentLevels = mergeIntMaps(service.equipmentLevels, state.EquipmentLevels)
	service.accessoryInventory = mergeIntMaps(service.accessoryInventory, state.AccessoryInventory)
	service.accessoryLevels = mergeIntMaps(service.accessoryLevels, state.AccessoryLevels)
	service.equippedAccessory = mergeStringMaps(service.equippedAccessory, state.EquippedAccessory)
	service.claimedDaily = mergeBoolMaps(service.claimedDaily, state.ClaimedDaily)
	service.claimedBattlePass = mergeBoolMaps(service.claimedBattlePass, state.ClaimedBattlePass)
	service.summonCount = state.SummonCount
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

func cloneStringMap(values map[string]string) map[string]string {
	cloned := make(map[string]string, len(values))
	for key, value := range values {
		cloned[key] = value
	}
	return cloned
}

func mergeStringMaps(defaults map[string]string, persisted map[string]string) map[string]string {
	merged := cloneStringMap(defaults)
	for key, value := range persisted {
		merged[key] = value
	}
	return merged
}

func cloneBoolMap(values map[string]bool) map[string]bool {
	cloned := make(map[string]bool, len(values))
	for key, value := range values {
		cloned[key] = value
	}
	return cloned
}

func mergeBoolMaps(defaults map[string]bool, persisted map[string]bool) map[string]bool {
	merged := cloneBoolMap(defaults)
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
	power += service.equipmentLevels[weaponID] * 18
	power += service.equipmentLevels[armorID] * 16
	service.state.TeamPower = power
	service.state.TeamAttack = 96 + (power / 8) + (service.equipmentLevels[weaponID] * 7)
	service.state.TeamHealth = 780 + (power * 2) + (service.equipmentLevels[armorID] * 65)
}

func (service *Service) accessoryIsEquipped(accessoryID string) bool {
	for _, equippedAccessoryID := range service.equippedAccessory {
		if equippedAccessoryID == accessoryID {
			return true
		}
	}
	return false
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

func accessoryFuseTarget(accessoryID string) (string, bool) {
	switch {
	case strings.HasSuffix(accessoryID, "_r0"):
		return strings.TrimSuffix(accessoryID, "_r0") + "_r1", true
	case strings.HasSuffix(accessoryID, "_r1"):
		return strings.TrimSuffix(accessoryID, "_r1") + "_r2", true
	case strings.HasSuffix(accessoryID, "_r2"):
		return strings.TrimSuffix(accessoryID, "_r2") + "_r3", true
	case strings.HasSuffix(accessoryID, "_r3"):
		return strings.TrimSuffix(accessoryID, "_r3") + "_r4", true
	default:
		return "", false
	}
}

func heroStates(levels map[string]int, ascensions map[string]int) []api.HeroState {
	heroIDs := sortedKeys(levels)
	states := make([]api.HeroState, 0, len(heroIDs))
	for _, heroID := range heroIDs {
		states = append(states, api.HeroState{
			HeroID:    heroID,
			Level:     levels[heroID],
			Ascension: ascensions[heroID],
		})
	}
	return states
}

func heroShardStates(shards map[string]int) []api.HeroShardState {
	heroIDs := sortedKeys(shards)
	states := make([]api.HeroShardState, 0, len(heroIDs))
	for _, heroID := range heroIDs {
		states = append(states, api.HeroShardState{
			HeroID: heroID,
			Shards: shards[heroID],
		})
	}
	return states
}

func equipmentStates(levels map[string]int) []api.EquipmentState {
	equipmentIDs := sortedKeys(levels)
	states := make([]api.EquipmentState, 0, len(equipmentIDs))
	for _, equipmentID := range equipmentIDs {
		states = append(states, api.EquipmentState{
			EquipmentID: equipmentID,
			Level:       levels[equipmentID],
		})
	}
	return states
}

func accessoryStates(inventory map[string]int, levels map[string]int) []api.AccessoryState {
	seen := map[string]bool{}
	for accessoryID := range inventory {
		seen[accessoryID] = true
	}
	for accessoryID := range levels {
		seen[accessoryID] = true
	}

	accessoryIDs := sortedBoolKeys(seen)
	states := make([]api.AccessoryState, 0, len(accessoryIDs))
	for _, accessoryID := range accessoryIDs {
		states = append(states, api.AccessoryState{
			AccessoryID: accessoryID,
			Copies:      inventory[accessoryID],
			Level:       levels[accessoryID],
		})
	}
	return states
}

func equippedAccessoryStates(equipped map[string]string) []api.EquippedAccessory {
	slotIDs := sortedKeys(equipped)
	states := make([]api.EquippedAccessory, 0, len(slotIDs))
	for _, slotID := range slotIDs {
		states = append(states, api.EquippedAccessory{
			SlotID:      slotID,
			AccessoryID: equipped[slotID],
		})
	}
	return states
}

func claimStates(claims map[string]bool) []api.ClaimState {
	claimIDs := sortedKeys(claims)
	states := make([]api.ClaimState, 0, len(claimIDs))
	for _, claimID := range claimIDs {
		states = append(states, api.ClaimState{
			ClaimID: claimID,
			Claimed: claims[claimID],
		})
	}
	return states
}

func sortedKeys[T any](values map[string]T) []string {
	keys := make([]string, 0, len(values))
	for key := range values {
		keys = append(keys, key)
	}
	sort.Strings(keys)
	return keys
}

func sortedBoolKeys(values map[string]bool) []string {
	keys := make([]string, 0, len(values))
	for key := range values {
		keys = append(keys, key)
	}
	sort.Strings(keys)
	return keys
}
