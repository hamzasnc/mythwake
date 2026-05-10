package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

func (service *Service) EquipAccessory(accessoryID string) api.ActionResult {
	return service.EquipAccessoryWithRequest(context.Background(), ActionRequest{}, accessoryID)
}

func (service *Service) EquipAccessoryWithRequest(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	return service.accessoryActions.EquipAccessory(ctx, request, accessoryID)
}

func (actions accessoryActions) EquipAccessory(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionAccessoryEquip, func() actionOutcome {
		definition, ok := service.balanceCatalog.AccessoryDefinitionByID(accessoryID)
		if !ok || definition.SlotID == "" {
			return actionFailure("invalid_accessory", fmt.Sprintf("Unknown accessory: %s.", accessoryID))
		}

		if service.accessoryInventory[accessoryID] <= 0 {
			return actionFailure("missing_item", fmt.Sprintf("Missing accessory: %s", accessoryID))
		}

		if current := service.equippedAccessory[definition.SlotID]; current != "" {
			service.accessoryInventory[current]++
		}

		service.accessoryInventory[accessoryID]--
		if service.accessoryLevels[accessoryID] <= 0 {
			service.accessoryLevels[accessoryID] = 1
		}
		service.equippedAccessory[definition.SlotID] = accessoryID
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("Equipped %s.", accessoryID), api.Reward{})
	})
}

func (service *Service) LevelAccessory(accessoryID string) api.ActionResult {
	return service.LevelAccessoryWithRequest(context.Background(), ActionRequest{}, accessoryID)
}

func (service *Service) LevelAccessoryWithRequest(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	return service.accessoryActions.LevelAccessory(ctx, request, accessoryID)
}

func (actions accessoryActions) LevelAccessory(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionAccessoryLevel, func() actionOutcome {
		definition, ok := service.balanceCatalog.AccessoryDefinitionByID(accessoryID)
		if !ok {
			return actionFailure("invalid_accessory", fmt.Sprintf("Unknown accessory: %s.", accessoryID))
		}
		rarity, ok := service.balanceCatalog.AccessoryRarityDefinitionByID(definition.RarityID)
		if !ok {
			return actionFailure("invalid_accessory_rarity", fmt.Sprintf("Unknown accessory rarity: %s.", definition.RarityID))
		}

		if service.accessoryInventory[accessoryID] <= 0 && !service.accessoryIsEquipped(accessoryID) {
			return actionFailure("missing_item", fmt.Sprintf("Missing accessory: %s.", accessoryID))
		}

		currentLevel := service.accessoryLevels[accessoryID]
		if currentLevel >= rarity.MaxLevel {
			return actionFailure("max_level", fmt.Sprintf("%s is already Lv. %d.", accessoryID, currentLevel))
		}

		cost := service.balanceCatalog.AccessoryLevelCost(accessoryID, currentLevel)
		if failure, ok := service.spendCurrency(economy.CurrencyGold, cost); !ok {
			return failure
		}

		service.accessoryLevels[accessoryID]++
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", accessoryID, service.accessoryLevels[accessoryID]), api.Reward{})
	})
}

func (service *Service) FuseAccessory(accessoryID string) api.ActionResult {
	return service.FuseAccessoryWithRequest(context.Background(), ActionRequest{}, accessoryID)
}

func (service *Service) FuseAccessoryWithRequest(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	return service.accessoryActions.FuseAccessory(ctx, request, accessoryID)
}

func (actions accessoryActions) FuseAccessory(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionAccessoryFuse, func() actionOutcome {
		definition, ok := service.balanceCatalog.AccessoryDefinitionByID(accessoryID)
		if !ok {
			return actionFailure("invalid_accessory", fmt.Sprintf("Unknown accessory: %s.", accessoryID))
		}
		rarity, ok := service.balanceCatalog.AccessoryRarityDefinitionByID(definition.RarityID)
		if !ok {
			return actionFailure("invalid_accessory_rarity", fmt.Sprintf("Unknown accessory rarity: %s.", definition.RarityID))
		}
		if definition.FuseTargetID == "" {
			return actionFailure("max_rarity", fmt.Sprintf("%s cannot be fused further.", accessoryID))
		}
		if _, ok := service.balanceCatalog.AccessoryDefinitionByID(definition.FuseTargetID); !ok {
			return actionFailure("invalid_fuse_target", fmt.Sprintf("Unknown fuse target: %s.", definition.FuseTargetID))
		}

		copyCost := max(1, rarity.FuseCopyCost)
		if service.accessoryInventory[accessoryID] < copyCost {
			return actionFailure("missing_items", fmt.Sprintf("Need %d copies of %s.", copyCost, accessoryID))
		}

		service.accessoryInventory[accessoryID] -= copyCost
		fusedID := definition.FuseTargetID
		service.accessoryInventory[fusedID]++
		return actionSuccess(fmt.Sprintf("Fused into %s.", fusedID), api.Reward{})
	})
}

func (service *Service) accessoryIsEquipped(accessoryID string) bool {
	for _, equippedAccessoryID := range service.equippedAccessory {
		if equippedAccessoryID == accessoryID {
			return true
		}
	}
	return false
}
