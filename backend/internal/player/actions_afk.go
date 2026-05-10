package player

import (
	"context"
	"fmt"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

func (service *Service) ClaimAFKRewards() api.ActionResult {
	return service.ClaimAFKRewardsWithRequest(context.Background(), ActionRequest{})
}

func (service *Service) ClaimAFKRewardsWithRequest(ctx context.Context, request ActionRequest) api.ActionResult {
	return service.afkActions.ClaimAFKRewards(ctx, request)
}

func (actions afkActions) ClaimAFKRewards(ctx context.Context, request ActionRequest) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionAFKRewardClaim, func() actionOutcome {
		now := service.now().UTC()
		if service.lastAFKClaimedAt.IsZero() || service.lastAFKClaimedAt.After(now) {
			service.lastAFKClaimedAt = now
			return actionSuccess("AFK timer initialized.", api.Reward{RewardID: service.balanceCatalog.RewardAFKClaim()})
		}

		elapsedSeconds := int(now.Sub(service.lastAFKClaimedAt).Seconds())
		reward, claimedSeconds := service.balanceCatalog.AFKReward(service.state.CampaignStage, elapsedSeconds)
		if claimedSeconds <= 0 {
			return actionFailure("afk_not_ready", fmt.Sprintf("AFK rewards need at least %s.", formatAFKDuration(service.balanceCatalog.AFKMinClaimSeconds())))
		}

		economy.Grant(&service.state, reward)
		service.lastAFKClaimedAt = now

		return actionSuccess(fmt.Sprintf("Claimed %s of AFK rewards.", formatAFKDuration(claimedSeconds)), reward)
	})
}

func formatAFKDuration(seconds int) string {
	duration := time.Duration(seconds) * time.Second
	hours := int(duration.Hours())
	minutes := int(duration.Minutes()) % 60
	if hours > 0 {
		return fmt.Sprintf("%dh %dm", hours, minutes)
	}
	if minutes > 0 {
		return fmt.Sprintf("%dm", minutes)
	}

	return fmt.Sprintf("%ds", seconds)
}
