package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

func (service *Service) PullSummon(bannerID string) api.ActionResult {
	return service.PullSummonWithRequest(context.Background(), ActionRequest{}, bannerID)
}

func (service *Service) PullSummonWithRequest(ctx context.Context, request ActionRequest, bannerID string) api.ActionResult {
	return service.summonActions.PullSummon(ctx, request, bannerID)
}

func (actions summonActions) PullSummon(ctx context.Context, request ActionRequest, bannerID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionSummonPull, func() actionOutcome {
		if bannerID != heroBannerID {
			return actionFailure("invalid_banner", fmt.Sprintf("Unknown banner: %s", bannerID))
		}

		cost, _ := balance.SummonCost(bannerID)
		if failure, ok := service.spendCurrency(economy.CurrencyGems, cost); !ok {
			return failure
		}

		drop, ok := balance.SummonShardReward(bannerID, service.summonCount)
		if !ok {
			return actionFailure("invalid_banner", fmt.Sprintf("Unknown banner: %s", bannerID))
		}

		service.summonCount++
		service.dailySummonCount++
		service.heroShards[drop.HeroID] += drop.Shards
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("Pulled %s shards.", drop.HeroID), drop.Reward)
	})
}

func (service *Service) ClaimDailyMission(missionID string) api.ActionResult {
	return service.ClaimDailyMissionWithRequest(context.Background(), ActionRequest{}, missionID)
}

func (service *Service) ClaimDailyMissionWithRequest(ctx context.Context, request ActionRequest, missionID string) api.ActionResult {
	return service.missionActions.ClaimDailyMission(ctx, request, missionID)
}

func (actions missionActions) ClaimDailyMission(ctx context.Context, request ActionRequest, missionID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionDailyMissionClaim, func() actionOutcome {
		reward, ok := balance.DailyMissionReward(missionID)
		if !ok {
			return actionFailure("invalid_mission", fmt.Sprintf("Unknown daily mission: %s", missionID))
		}
		definition, _ := balance.DailyMissionDefinitionByID(missionID)

		if service.claimedDaily[missionID] {
			return actionFailure("already_claimed", fmt.Sprintf("%s already claimed.", missionID))
		}

		progress := service.dailyProgressFor(definition.ProgressType)
		if progress < definition.Target {
			return actionFailure("not_complete", fmt.Sprintf("%s needs %d/%d progress.", missionID, progress, definition.Target))
		}

		service.claimedDaily[missionID] = true
		economy.Grant(&service.state, reward)
		return actionSuccess(fmt.Sprintf("Claimed %s.", missionID), reward)
	})
}

func (service *Service) ClaimBattlePassReward(rewardID string) api.ActionResult {
	return service.ClaimBattlePassRewardWithRequest(context.Background(), ActionRequest{}, rewardID)
}

func (service *Service) ClaimBattlePassRewardWithRequest(ctx context.Context, request ActionRequest, rewardID string) api.ActionResult {
	return service.missionActions.ClaimBattlePassReward(ctx, request, rewardID)
}

func (actions missionActions) ClaimBattlePassReward(ctx context.Context, request ActionRequest, rewardID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionBattlePassClaim, func() actionOutcome {
		requiredPassXP, ok := balance.BattlePassRequiredXP(rewardID)
		if !ok {
			return actionFailure("invalid_reward", fmt.Sprintf("Unknown battle pass reward: %s", rewardID))
		}

		if service.claimedBattlePass[rewardID] {
			return actionFailure("already_claimed", fmt.Sprintf("%s already claimed.", rewardID))
		}

		if service.state.PassXP < requiredPassXP {
			return actionFailure("not_unlocked", fmt.Sprintf("Need %d Pass XP.", requiredPassXP))
		}

		reward, ok := balance.BattlePassReward(rewardID)
		if !ok {
			return actionFailure("invalid_reward", fmt.Sprintf("Unknown battle pass reward: %s", rewardID))
		}

		service.claimedBattlePass[rewardID] = true
		economy.Grant(&service.state, reward)
		return actionSuccess(fmt.Sprintf("Claimed %s.", rewardID), reward)
	})
}
