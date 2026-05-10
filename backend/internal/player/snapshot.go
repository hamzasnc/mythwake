package player

import (
	"sort"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
)

func (service *Service) snapshot() api.PlayerSnapshot {
	return api.PlayerSnapshot{
		PlayerID:          service.playerID,
		State:             service.state,
		LastAFKClaimUTC:   formatSnapshotTime(service.lastAFKClaimedAt),
		DailyDate:         service.dailyDate,
		DailyProgress:     service.dailyProgressStates(),
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

func (service *Service) dailyProgressStates() []api.DailyProgress {
	definitions := balance.DailyMissionDefinitions()
	states := make([]api.DailyProgress, 0, len(definitions))
	for _, definition := range definitions {
		states = append(states, api.DailyProgress{
			MissionID: definition.ID,
			Progress:  service.dailyProgressFor(definition.ProgressType),
			Target:    definition.Target,
			Claimed:   service.claimedDaily[definition.ID],
		})
	}
	return states
}

func formatSnapshotTime(value time.Time) string {
	if value.IsZero() {
		return ""
	}

	return value.UTC().Format(time.RFC3339)
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
