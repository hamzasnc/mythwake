package player

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

var errTestPersistenceFailed = errors.New("test persistence failed")

func TestServicePersistsSuccessfulActionWhenStoreIsAttached(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.FightCampaign()
	if !result.Success {
		t.Fatalf("expected campaign fight to succeed, got %#v", result)
	}

	if store.saved.PlayerState.CampaignStage != 2 {
		t.Fatalf("expected saved campaign stage 2, got %d", store.saved.PlayerState.CampaignStage)
	}
}

func TestAccessoryFuseTargetUsesNextRarityID(t *testing.T) {
	target, ok := accessoryFuseTarget("accessory_earrings_r0")
	if !ok {
		t.Fatal("expected r0 accessory to have a fuse target")
	}

	if target != "accessory_earrings_r1" {
		t.Fatalf("expected accessory_earrings_r1, got %s", target)
	}
}

func TestAccessoryFuseTargetStopsAtR4(t *testing.T) {
	if target, ok := accessoryFuseTarget("accessory_earrings_r4"); ok {
		t.Fatalf("expected r4 accessory to have no fuse target, got %s", target)
	}
}

func TestDungeonActionIDUsesSpecificActionForKnownDungeons(t *testing.T) {
	if actionID := dungeonActionID(goldDungeonID); actionID != gameplay.ActionGoldDungeonRun {
		t.Fatalf("expected gold dungeon action id, got %s", actionID)
	}
	if actionID := dungeonActionID(essenceDungeonID); actionID != gameplay.ActionEssenceDungeonRun {
		t.Fatalf("expected essence dungeon action id, got %s", actionID)
	}
	if actionID := dungeonActionID(gearDungeonID); actionID != gameplay.ActionGearDungeonRun {
		t.Fatalf("expected gear dungeon action id, got %s", actionID)
	}
	if actionID := dungeonActionID("unknown"); actionID != gameplay.ActionDungeonRun {
		t.Fatalf("expected generic dungeon action id, got %s", actionID)
	}
}

func TestSummonCountPersistsAfterPull(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.PullSummon(heroBannerID)
	if !result.Success {
		t.Fatalf("expected summon to succeed, got %#v", result)
	}

	if store.saved.SummonCount != 1 {
		t.Fatalf("expected saved summon count 1, got %d", store.saved.SummonCount)
	}
}

func TestMissionClaimsPersist(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.ClaimDailyMission("daily_battles_15")
	if !result.Success {
		t.Fatalf("expected daily claim to succeed, got %#v", result)
	}

	if !store.saved.ClaimedDaily["daily_battles_15"] {
		t.Fatalf("expected daily_battles_15 claim to be saved")
	}
}

func TestAFKClaimGrantsGoldAndEssence(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	service.now = func() time.Time { return now }
	service.lastAFKClaimedAt = now.Add(-2 * time.Hour)

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.ClaimAFKRewards()
	if !result.Success {
		t.Fatalf("expected afk claim to succeed, got %#v", result)
	}
	if result.ActionID != gameplay.ActionAFKRewardClaim {
		t.Fatalf("expected afk action id, got %s", result.ActionID)
	}
	if result.Reward.Gold <= 0 || result.Reward.MythEssence <= 0 {
		t.Fatalf("expected gold and essence reward, got %#v", result.Reward)
	}
	if !store.saved.LastAFKClaimedAt.Equal(now) {
		t.Fatalf("expected saved afk claim time %s, got %s", now, store.saved.LastAFKClaimedAt)
	}
}

func TestAFKClaimRequiresMinimumWindow(t *testing.T) {
	service := NewService()
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	service.now = func() time.Time { return now }
	service.lastAFKClaimedAt = now.Add(-(balance.AFKMinClaimSeconds - 1) * time.Second)
	before := service.GetState()

	result := service.ClaimAFKRewards()
	if result.Success || result.ErrorCode != "afk_not_ready" {
		t.Fatalf("expected afk_not_ready, got %#v", result)
	}

	after := service.GetState()
	if after.Gold != before.Gold || after.MythEssence != before.MythEssence {
		t.Fatalf("early afk claim mutated state: before=%#v after=%#v", before, after)
	}
}

func TestAFKClaimIsCapped(t *testing.T) {
	service := NewService()
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	service.now = func() time.Time { return now }
	service.lastAFKClaimedAt = now.Add(-24 * time.Hour)

	result := service.ClaimAFKRewards()
	if !result.Success {
		t.Fatalf("expected capped afk claim to succeed, got %#v", result)
	}

	expected, _ := balance.AFKReward(1, balance.AFKMaxClaimSeconds)
	if result.Reward.Gold != expected.Gold || result.Reward.MythEssence != expected.MythEssence {
		t.Fatalf("expected capped reward %#v, got %#v", expected, result.Reward)
	}
}

func TestUnknownMissionClaimIsRejected(t *testing.T) {
	service := NewService()

	before := service.GetState()
	result := service.ClaimDailyMission("daily_fake_claim")
	if result.Success || result.ErrorCode != "invalid_mission" {
		t.Fatalf("expected invalid mission, got %#v", result)
	}

	after := service.GetState()
	if after.Gold != before.Gold || after.Gems != before.Gems || after.MythEssence != before.MythEssence || after.PassXP != before.PassXP {
		t.Fatalf("invalid mission mutated state: before=%#v after=%#v", before, after)
	}
}

func TestBattlePassUsesDefinitionRewards(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()
	service.state.PassXP = 240

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.ClaimBattlePassReward("mission_track_reward_05")
	if !result.Success {
		t.Fatalf("expected battle pass claim to succeed, got %#v", result)
	}

	if result.Reward.Gold != 350 || result.Reward.Gems != 40 || result.Reward.MythEssence != 300 {
		t.Fatalf("expected reward 05 definition, got %#v", result.Reward)
	}
	if !store.saved.ClaimedBattlePass["mission_track_reward_05"] {
		t.Fatalf("expected mission_track_reward_05 claim to be saved")
	}
}

func TestUnknownBattlePassRewardIsRejected(t *testing.T) {
	service := NewService()
	service.state.PassXP = 999

	before := service.GetState()
	result := service.ClaimBattlePassReward("mission_track_fake")
	if result.Success || result.ErrorCode != "invalid_reward" {
		t.Fatalf("expected invalid battle pass reward, got %#v", result)
	}

	after := service.GetState()
	if after.Gold != before.Gold || after.Gems != before.Gems || after.MythEssence != before.MythEssence || after.PassXP != before.PassXP {
		t.Fatalf("invalid battle pass reward mutated state: before=%#v after=%#v", before, after)
	}
}

func TestEquipmentLevelPersistsAndRaisesPower(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()
	service.state.Gold = 1000
	beforePower := service.state.TeamPower

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.LevelEquipment(weaponID)
	if !result.Success {
		t.Fatalf("expected equipment level to succeed, got %#v", result)
	}

	if store.saved.EquipmentLevels[weaponID] != 1 {
		t.Fatalf("expected saved weapon level 1, got %d", store.saved.EquipmentLevels[weaponID])
	}
	if result.PlayerState.TeamPower <= beforePower {
		t.Fatalf("expected team power to increase from %d, got %d", beforePower, result.PlayerState.TeamPower)
	}
}

func TestFlushStateQueuesCurrentStateAndFlushesStore(t *testing.T) {
	store := &flushableStateStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}
	result := service.FightCampaign()
	if !result.Success {
		t.Fatalf("expected campaign fight to succeed, got %#v", result)
	}

	if err := service.FlushState(context.Background()); err != nil {
		t.Fatalf("flush state: %v", err)
	}

	if store.flushCount != 1 {
		t.Fatalf("expected one flush, got %d", store.flushCount)
	}
	if store.saved.PlayerState.CampaignStage != 2 {
		t.Fatalf("expected saved campaign stage 2, got %d", store.saved.PlayerState.CampaignStage)
	}
	if store.source.ActionID != gameplay.ActionPlayerStateFlush {
		t.Fatalf("expected flush source, got %#v", store.source)
	}
}

func TestIdempotencyReplayDoesNotApplyActionTwice(t *testing.T) {
	store := newIdempotentStateStore()
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	request := ActionRequest{IdempotencyKey: "campaign-key-1", RequestHash: "campaign-hash-1"}
	first := service.FightCampaignWithRequest(context.Background(), request)
	if !first.Success {
		t.Fatalf("expected first campaign fight to succeed, got %#v", first)
	}

	replay := service.FightCampaignWithRequest(context.Background(), request)
	if !replay.Success || !replay.Replay {
		t.Fatalf("expected idempotent replay, got %#v", replay)
	}

	if stage := service.GetState().CampaignStage; stage != 2 {
		t.Fatalf("expected campaign stage to stay at 2 after replay, got %d", stage)
	}
}

func TestIdempotencyConflictDoesNotMutateState(t *testing.T) {
	store := newIdempotentStateStore()
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	request := ActionRequest{IdempotencyKey: "campaign-key-1", RequestHash: "campaign-hash-1"}
	if result := service.FightCampaignWithRequest(context.Background(), request); !result.Success {
		t.Fatalf("expected first campaign fight to succeed, got %#v", result)
	}

	conflict := service.FightCampaignWithRequest(context.Background(), ActionRequest{IdempotencyKey: "campaign-key-1", RequestHash: "different-hash"})
	if conflict.Success || conflict.ErrorCode != "idempotency_conflict" {
		t.Fatalf("expected idempotency conflict, got %#v", conflict)
	}

	if stage := service.GetState().CampaignStage; stage != 2 {
		t.Fatalf("expected campaign stage to stay at 2 after conflict, got %d", stage)
	}
}

func TestPersistenceFailureRollsBackHotState(t *testing.T) {
	store := &failingAfterSeedStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.FightCampaign()
	if result.Success || result.ErrorCode != "persistence_failed" {
		t.Fatalf("expected persistence failure, got %#v", result)
	}

	if stage := service.GetState().CampaignStage; stage != 1 {
		t.Fatalf("expected campaign stage rollback to 1, got %d", stage)
	}
}

type fakeStateStore struct {
	saved PersistentState
}

func (store *fakeStateStore) LoadState(context.Context, string) (PersistentState, bool, error) {
	return PersistentState{}, false, nil
}

func (store *fakeStateStore) SaveState(_ context.Context, _ string, state PersistentState, _ StateSaveSource) error {
	store.saved = state
	return nil
}

type flushableStateStore struct {
	saved      PersistentState
	source     StateSaveSource
	flushCount int
}

func (store *flushableStateStore) LoadState(context.Context, string) (PersistentState, bool, error) {
	return PersistentState{}, false, nil
}

func (store *flushableStateStore) SaveState(_ context.Context, _ string, state PersistentState, source StateSaveSource) error {
	store.saved = state
	store.source = source
	return nil
}

func (store *flushableStateStore) Flush(context.Context) error {
	store.flushCount++
	return nil
}

type idempotentStateStore struct {
	saved   PersistentState
	records map[string]StoredActionResult
}

func newIdempotentStateStore() *idempotentStateStore {
	return &idempotentStateStore{records: map[string]StoredActionResult{}}
}

func (store *idempotentStateStore) LoadState(context.Context, string) (PersistentState, bool, error) {
	return PersistentState{}, false, nil
}

func (store *idempotentStateStore) SaveState(_ context.Context, _ string, state PersistentState, source StateSaveSource) error {
	store.saved = state
	if source.IdempotencyKey != "" && source.ActionResult != nil {
		store.records[source.IdempotencyKey] = StoredActionResult{
			ActionID:     source.ActionID,
			RequestHash:  source.RequestHash,
			ActionResult: *source.ActionResult,
		}
	}

	return nil
}

func (store *idempotentStateStore) LoadActionResult(_ context.Context, _ string, idempotencyKey string) (StoredActionResult, bool, error) {
	record, ok := store.records[idempotencyKey]
	return record, ok, nil
}

type failingAfterSeedStore struct {
	saveCount int
}

func (store *failingAfterSeedStore) LoadState(context.Context, string) (PersistentState, bool, error) {
	return PersistentState{}, false, nil
}

func (store *failingAfterSeedStore) SaveState(_ context.Context, _ string, _ PersistentState, _ StateSaveSource) error {
	store.saveCount++
	if store.saveCount == 1 {
		return nil
	}

	return errTestPersistenceFailed
}
