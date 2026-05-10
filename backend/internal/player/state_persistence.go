package player

import (
	"context"

	"github.com/hamzasnc/mythwake/backend/internal/api"
)

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
