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
		Revision:           service.revision,
		UpdatedAt:          service.updatedAt,
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
		LastAFKClaimedAt:   service.lastAFKClaimedAt,
		DailyDate:          service.dailyDate,
		DailyFightCount:    service.dailyFightCount,
		DailyStageClears:   service.dailyStageClears,
		DailySummonCount:   service.dailySummonCount,
	})
}

func (service *Service) applyPersistentState(state PersistentState) {
	service.revision = state.Revision
	if service.revision < 1 {
		service.revision = 1
	}
	service.updatedAt = state.UpdatedAt
	if service.updatedAt.IsZero() {
		service.updatedAt = service.now().UTC()
	}
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
	service.lastAFKClaimedAt = state.LastAFKClaimedAt
	if service.lastAFKClaimedAt.IsZero() {
		service.lastAFKClaimedAt = service.now().UTC()
	}
	service.dailyDate = state.DailyDate
	service.dailyFightCount = state.DailyFightCount
	service.dailyStageClears = state.DailyStageClears
	service.dailySummonCount = state.DailySummonCount
	service.ensureDailyWindow()
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
	heroAttack, heroHealth := service.heroStatTotals()
	equipmentAttack, equipmentHealth, equipmentPower := service.equipmentStatBonuses()
	accessoryAttack, accessoryHealth := service.accessoryStatBonuses()

	teamAttack := heroAttack + equipmentAttack + accessoryAttack
	teamHealth := heroHealth + equipmentHealth + accessoryHealth
	power := teamAttack + (teamHealth / 10) + equipmentPower

	service.state.TeamPower = power
	service.state.TeamAttack = max(1, teamAttack)
	service.state.TeamHealth = max(1, teamHealth)
}

func (service *Service) equipmentStatBonuses() (int, int, int) {
	totalAttack := 0
	totalHealth := 0
	totalPower := 0
	for equipmentID, level := range service.equipmentLevels {
		definition, ok := service.balanceCatalog.EquipmentDefinitionByID(equipmentID)
		if !ok {
			continue
		}
		level = clampEquipmentLevel(level, definition.MaxLevel)
		attack := definition.AttackPerLevel * level
		health := definition.HealthPerLevel * level
		totalAttack += attack
		totalHealth += health
		totalPower += attack + (health / 10)
	}

	return totalAttack, totalHealth, totalPower
}

func (service *Service) heroStatTotals() (int, int) {
	totalAttack := 0
	totalHealth := 0
	for heroID, level := range service.heroLevels {
		definition, ok := service.balanceCatalog.HeroDefinitionByID(heroID)
		if !ok {
			continue
		}
		level = clampHeroLevel(level, definition.MaxLevel)
		ascension := clampHeroAscension(service.heroAscensions[heroID], definition.MaxAscension)
		totalAttack += definition.BaseAttack + ((level - 1) * definition.AttackPerLevel) + (ascension * definition.AttackPerAscension)
		totalHealth += definition.BaseHealth + ((level - 1) * definition.HealthPerLevel) + (ascension * definition.HealthPerAscension)
	}

	return totalAttack, totalHealth
}

func clampHeroLevel(value int, maximum int) int {
	value = max(1, value)
	if maximum <= 0 {
		return value
	}

	return min(value, maximum)
}

func clampHeroAscension(value int, maximum int) int {
	value = max(0, value)
	if maximum <= 0 {
		return value
	}

	return min(value, maximum)
}

func clampEquipmentLevel(value int, maximum int) int {
	value = max(0, value)
	if maximum <= 0 {
		return value
	}

	return min(value, maximum)
}

func (service *Service) accessoryStatBonuses() (int, int) {
	totalAttack := 0
	totalHealth := 0
	for _, accessoryID := range service.equippedAccessory {
		definition, ok := service.balanceCatalog.AccessoryDefinitionByID(accessoryID)
		if !ok {
			continue
		}
		level := max(1, service.accessoryLevels[accessoryID])
		if rarity, ok := service.balanceCatalog.AccessoryRarityDefinitionByID(definition.RarityID); ok && rarity.MaxLevel > 0 {
			level = min(level, rarity.MaxLevel)
		}
		totalAttack += definition.AttackPerLevel * level
		totalHealth += definition.HealthPerLevel * level
	}

	return totalAttack, totalHealth
}
