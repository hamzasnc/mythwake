package player

import (
	"context"
	"fmt"
	"strings"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

const maxSummonPullCount = 300

func (service *Service) PullSummon(bannerID string) api.ActionResult {
	return service.PullSummonWithRequest(context.Background(), ActionRequest{}, bannerID)
}

func (service *Service) PullSummonWithRequest(ctx context.Context, request ActionRequest, bannerID string) api.ActionResult {
	return service.PullSummonCountWithRequest(ctx, request, bannerID, 1)
}

func (service *Service) PullSummonCountWithRequest(ctx context.Context, request ActionRequest, bannerID string, count int) api.ActionResult {
	return service.summonActions.PullSummon(ctx, request, bannerID, count)
}

func (actions summonActions) PullSummon(ctx context.Context, request ActionRequest, bannerID string, count int) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionSummonPull, func() actionOutcome {
		if bannerID != heroBannerID {
			return actionFailure("invalid_banner", fmt.Sprintf("Unknown banner: %s", bannerID))
		}

		cost, _ := service.balanceCatalog.SummonCost(bannerID)
		count = normalizeSummonPullCount(count)
		if failure, ok := service.spendCurrency(economy.CurrencyGems, summonPackCost(cost, count)); !ok {
			return failure
		}

		dropTotals := map[string]int{}
		dropOrder := make([]string, 0, count)
		for i := 0; i < count; i++ {
			drop, ok := service.balanceCatalog.SummonShardReward(bannerID, service.summonCount)
			if !ok {
				return actionFailure("invalid_banner", fmt.Sprintf("Unknown banner: %s", bannerID))
			}

			if _, exists := dropTotals[drop.HeroID]; !exists {
				dropOrder = append(dropOrder, drop.HeroID)
			}

			service.summonCount++
			service.dailySummonCount++
			service.heroShards[drop.HeroID] += drop.Shards
			dropTotals[drop.HeroID] += drop.Shards
		}

		service.recalculatePower()
		return actionSuccess(formatSummonPullMessage(count, dropOrder, dropTotals), api.Reward{RewardID: balance.RewardSummonShards})
	})
}

func normalizeSummonPullCount(count int) int {
	if count < 1 {
		return 1
	}
	if count > maxSummonPullCount {
		return maxSummonPullCount
	}
	return count
}

func summonPackCost(singleCost int, count int) int {
	count = normalizeSummonPullCount(count)
	if count >= 10 {
		return singleCost * count * 9 / 10
	}
	return singleCost * count
}

func formatSummonPullMessage(count int, dropOrder []string, dropTotals map[string]int) string {
	if count <= 1 && len(dropOrder) > 0 {
		return fmt.Sprintf("Pulled %s shards.", dropOrder[0])
	}

	parts := make([]string, 0, len(dropOrder))
	for _, heroID := range dropOrder {
		parts = append(parts, fmt.Sprintf("%s +%d", heroID, dropTotals[heroID]))
	}

	return fmt.Sprintf("Pulled %d heroes: %s.", count, strings.Join(parts, ", "))
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
		reward, ok := service.balanceCatalog.DailyMissionReward(missionID)
		if !ok {
			return actionFailure("invalid_mission", fmt.Sprintf("Unknown daily mission: %s", missionID))
		}
		definition, _ := service.balanceCatalog.DailyMissionDefinitionByID(missionID)

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
		requiredPassXP, ok := service.balanceCatalog.BattlePassRequiredXP(rewardID)
		if !ok {
			return actionFailure("invalid_reward", fmt.Sprintf("Unknown battle pass reward: %s", rewardID))
		}

		if service.claimedBattlePass[rewardID] {
			return actionFailure("already_claimed", fmt.Sprintf("%s already claimed.", rewardID))
		}

		if service.state.PassXP < requiredPassXP {
			return actionFailure("not_unlocked", fmt.Sprintf("Need %d Pass XP.", requiredPassXP))
		}

		reward, ok := service.balanceCatalog.BattlePassReward(rewardID)
		if !ok {
			return actionFailure("invalid_reward", fmt.Sprintf("Unknown battle pass reward: %s", rewardID))
		}

		service.claimedBattlePass[rewardID] = true
		economy.Grant(&service.state, reward)
		return actionSuccess(fmt.Sprintf("Claimed %s.", rewardID), reward)
	})
}
