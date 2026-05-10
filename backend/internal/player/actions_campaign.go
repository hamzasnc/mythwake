package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

func (service *Service) FightCampaign() api.ActionResult {
	return service.FightCampaignWithRequest(context.Background(), ActionRequest{})
}

func (service *Service) FightCampaignWithRequest(ctx context.Context, request ActionRequest) api.ActionResult {
	return service.campaignActions.FightCampaign(ctx, request)
}

func (actions campaignActions) FightCampaign(ctx context.Context, request ActionRequest) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionCampaignFight, func() actionOutcome {
		stage := service.state.CampaignStage
		combat := service.simulateCombat(campaignEnemy(stage))
		service.dailyFightCount++
		label := fmt.Sprintf("Campaign Stage %d", stage)
		if !combat.Won {
			return actionFailureWithCombat("combat_lost", formatCombatMessage(label, combat), combat, true)
		}

		reward := balance.CampaignReward(stage)
		economy.Grant(&service.state, reward)
		service.state.CampaignStage++
		service.dailyStageClears++
		return actionSuccessWithCombat(formatCombatMessage(label, combat), reward, combat)
	})
}
