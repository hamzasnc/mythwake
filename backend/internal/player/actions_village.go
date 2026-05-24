package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

const (
	villagePlotCount            = 12
	villageBuildingOptionCount  = 3
	villageBuildingMaxLevel     = 20
	villageBuildingBaseCost     = 5
	villageBuildingOptionCost   = 2
	villageBuildingUpgradeScale = 5
)

func (service *Service) BuildVillageBuilding(slotIndex int, buildingOptionIndex int) api.ActionResult {
	return service.BuildVillageBuildingWithRequest(context.Background(), ActionRequest{}, slotIndex, buildingOptionIndex)
}

func (service *Service) BuildVillageBuildingWithRequest(ctx context.Context, request ActionRequest, slotIndex int, buildingOptionIndex int) api.ActionResult {
	return service.villageActions.BuildVillageBuilding(ctx, request, slotIndex, buildingOptionIndex)
}

func (actions villageActions) BuildVillageBuilding(ctx context.Context, request ActionRequest, slotIndex int, buildingOptionIndex int) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionVillageBuild, func() actionOutcome {
		if !validVillageSlot(slotIndex) {
			return actionFailure("invalid_village_slot", fmt.Sprintf("Unknown village slot: %d.", slotIndex))
		}
		if !validVillageBuildingOption(buildingOptionIndex) {
			return actionFailure("invalid_village_building", fmt.Sprintf("Unknown village building option: %d.", buildingOptionIndex))
		}
		if _, exists := service.villageBuildings[slotIndex]; exists {
			return actionFailure("village_slot_occupied", fmt.Sprintf("Village slot %d is already occupied.", slotIndex))
		}

		cost := villageBuildCost(buildingOptionIndex)
		if failure, ok := service.spendCurrency(economy.CurrencyMythEssence, cost); !ok {
			return failure
		}

		service.villageBuildings[slotIndex] = api.VillageBuilding{
			SlotIndex:           slotIndex,
			BuildingID:          villageBuildingID(slotIndex, buildingOptionIndex),
			BuildingOptionIndex: buildingOptionIndex,
			Level:               1,
		}
		return actionSuccess(fmt.Sprintf("Built %s in village slot %d.", villageBuildingID(slotIndex, buildingOptionIndex), slotIndex), api.Reward{})
	})
}

func (service *Service) DemolishVillageBuilding(slotIndex int) api.ActionResult {
	return service.DemolishVillageBuildingWithRequest(context.Background(), ActionRequest{}, slotIndex)
}

func (service *Service) DemolishVillageBuildingWithRequest(ctx context.Context, request ActionRequest, slotIndex int) api.ActionResult {
	return service.villageActions.DemolishVillageBuilding(ctx, request, slotIndex)
}

func (actions villageActions) DemolishVillageBuilding(ctx context.Context, request ActionRequest, slotIndex int) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionVillageDemolish, func() actionOutcome {
		if !validVillageSlot(slotIndex) {
			return actionFailure("invalid_village_slot", fmt.Sprintf("Unknown village slot: %d.", slotIndex))
		}
		if _, exists := service.villageBuildings[slotIndex]; !exists {
			return actionFailure("village_slot_empty", fmt.Sprintf("Village slot %d is already empty.", slotIndex))
		}

		delete(service.villageBuildings, slotIndex)
		return actionSuccess(fmt.Sprintf("Demolished village slot %d.", slotIndex), api.Reward{})
	})
}

func (service *Service) UpgradeVillageBuilding(slotIndex int) api.ActionResult {
	return service.UpgradeVillageBuildingWithRequest(context.Background(), ActionRequest{}, slotIndex)
}

func (service *Service) UpgradeVillageBuildingWithRequest(ctx context.Context, request ActionRequest, slotIndex int) api.ActionResult {
	return service.villageActions.UpgradeVillageBuilding(ctx, request, slotIndex)
}

func (actions villageActions) UpgradeVillageBuilding(ctx context.Context, request ActionRequest, slotIndex int) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionVillageUpgrade, func() actionOutcome {
		if !validVillageSlot(slotIndex) {
			return actionFailure("invalid_village_slot", fmt.Sprintf("Unknown village slot: %d.", slotIndex))
		}

		building, exists := service.villageBuildings[slotIndex]
		if !exists {
			return actionFailure("village_slot_empty", fmt.Sprintf("Village slot %d is empty.", slotIndex))
		}
		if building.Level >= villageBuildingMaxLevel {
			return actionFailure("max_level", fmt.Sprintf("%s is already Lv. %d.", building.BuildingID, building.Level))
		}

		cost := villageUpgradeCost(building.Level)
		if failure, ok := service.spendCurrency(economy.CurrencyMythEssence, cost); !ok {
			return failure
		}

		building.Level++
		if normalized, ok := normalizeVillageBuildingState(slotIndex, building); ok {
			service.villageBuildings[slotIndex] = normalized
		}
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", building.BuildingID, building.Level), api.Reward{})
	})
}

func normalizeVillageBuildingState(slotIndex int, building api.VillageBuilding) (api.VillageBuilding, bool) {
	if !validVillageSlot(slotIndex) {
		slotIndex = building.SlotIndex
	}
	if !validVillageSlot(slotIndex) || !validVillageBuildingOption(building.BuildingOptionIndex) {
		return api.VillageBuilding{}, false
	}

	building.SlotIndex = slotIndex
	if building.BuildingID == "" {
		building.BuildingID = villageBuildingID(slotIndex, building.BuildingOptionIndex)
	}
	building.Level = max(1, building.Level)
	if building.Level > villageBuildingMaxLevel {
		building.Level = villageBuildingMaxLevel
	}
	return building, true
}

func validVillageSlot(slotIndex int) bool {
	return slotIndex >= 0 && slotIndex < villagePlotCount
}

func validVillageBuildingOption(optionIndex int) bool {
	return optionIndex >= 0 && optionIndex < villageBuildingOptionCount
}

func villageBuildCost(buildingOptionIndex int) int {
	return villageBuildingBaseCost + (buildingOptionIndex * villageBuildingOptionCost)
}

func villageUpgradeCost(currentLevel int) int {
	return max(1, currentLevel) * villageBuildingUpgradeScale
}

func villageBuildingID(slotIndex int, buildingOptionIndex int) string {
	return fmt.Sprintf("village_building_%02d_option_%02d", slotIndex+1, buildingOptionIndex+1)
}
