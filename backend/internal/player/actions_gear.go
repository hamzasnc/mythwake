package player

import (
	"context"
	"fmt"
	"strings"

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
	return service.accessoryActions.LevelAccessory(ctx, request, accessoryID)
}

func (actions accessoryActions) LevelAccessory(ctx context.Context, request ActionRequest, accessoryID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionAccessoryLevel, func() actionOutcome {
		if service.accessoryInventory[accessoryID] <= 0 && !service.accessoryIsEquipped(accessoryID) {
			return actionFailure("missing_item", fmt.Sprintf("Missing accessory: %s.", accessoryID))
		}

		cost := service.balanceCatalog.AccessoryLevelCost(accessoryID, service.accessoryLevels[accessoryID])
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
