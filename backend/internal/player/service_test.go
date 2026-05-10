package player

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
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

func TestServiceSeedsStarterHeroesFromBalanceCatalog(t *testing.T) {
	catalog := NewSnapshotBalanceCatalog(api.DefinitionSnapshot{
		Heroes: []api.HeroDefinition{
			{HeroID: "hero_custom_starter", StarterOwned: true, MaxLevel: 100, MaxAscension: 10, BaseAttack: 20, AttackPerLevel: 4, AttackPerAscension: 9, BaseHealth: 200, HealthPerLevel: 15, HealthPerAscension: 60},
			{HeroID: "hero_custom_locked", StarterOwned: false, MaxLevel: 100, MaxAscension: 10, BaseAttack: 10, AttackPerLevel: 3, AttackPerAscension: 8, BaseHealth: 100, HealthPerLevel: 12, HealthPerAscension: 50},
		},
	})
	service := NewServiceForPlayer("hero-seed-player", withServiceBalanceCatalog(catalog))

	if service.heroLevels["hero_custom_starter"] != 1 {
		t.Fatalf("expected custom starter hero at level 1, got %#v", service.heroLevels)
	}
	if _, ok := service.heroLevels["hero_custom_locked"]; ok {
		t.Fatalf("expected locked hero to stay unowned, got %#v", service.heroLevels)
	}
	if _, ok := service.heroLevels["hero_astra"]; ok {
		t.Fatalf("expected static starter heroes to be replaced by snapshot catalog, got %#v", service.heroLevels)
	}
	if _, ok := service.heroShards["hero_custom_locked"]; !ok {
		t.Fatalf("expected custom hero shard row to be initialized, got %#v", service.heroShards)
	}
	if service.state.TeamAttack != 20 || service.state.TeamHealth != 200 {
		t.Fatalf("expected starter team stats from hero definition, got state=%#v", service.state)
	}
}

func TestHeroProgressionRespectsDefinitionCaps(t *testing.T) {
	catalog := NewSnapshotBalanceCatalog(api.DefinitionSnapshot{
		Heroes: []api.HeroDefinition{
			{HeroID: "hero_capped", StarterOwned: true, MaxLevel: 1, MaxAscension: 1, BaseAttack: 20, AttackPerLevel: 4, AttackPerAscension: 9, BaseHealth: 200, HealthPerLevel: 15, HealthPerAscension: 60},
		},
	})
	service := NewServiceForPlayer("hero-cap-player", withServiceBalanceCatalog(catalog))
	service.state.MythEssence = 100000
	service.heroShards["hero_capped"] = 100000

	levelResult := service.LevelHero("hero_capped")
	if levelResult.Success || levelResult.ErrorCode != "max_level" {
		t.Fatalf("expected max_level, got %#v", levelResult)
	}

	ascendResult := service.AscendHero("hero_capped")
	if !ascendResult.Success {
		t.Fatalf("expected first ascension to succeed, got %#v", ascendResult)
	}
	secondAscend := service.AscendHero("hero_capped")
	if secondAscend.Success || secondAscend.ErrorCode != "max_ascension" {
		t.Fatalf("expected max_ascension, got %#v", secondAscend)
	}
}

func TestAccessoryEquipUsesDefinitionSlot(t *testing.T) {
	service := NewService()
	service.accessoryInventory["accessory_necklace_r0"] = 1
	attackBefore := service.state.TeamAttack

	result := service.EquipAccessory("accessory_necklace_r0")
	if !result.Success {
		t.Fatalf("expected accessory equip to succeed, got %#v", result)
	}
	if service.equippedAccessory["necklace"] != "accessory_necklace_r0" {
		t.Fatalf("expected necklace slot to be equipped from definition, got %#v", service.equippedAccessory)
	}
	if service.accessoryLevels["accessory_necklace_r0"] != 1 {
		t.Fatalf("expected equipped accessory to start at level 1, got %d", service.accessoryLevels["accessory_necklace_r0"])
	}
	if service.state.TeamAttack <= attackBefore {
		t.Fatalf("expected equipped accessory stats to increase team attack, before=%d after=%d", attackBefore, service.state.TeamAttack)
	}
}

func TestAccessoryFuseUsesDefinitionTargetAndCopyCost(t *testing.T) {
	service := NewService()
	service.accessoryInventory["accessory_earrings_r0"] = 3

	result := service.FuseAccessory("accessory_earrings_r0")
	if !result.Success {
		t.Fatalf("expected accessory fuse to succeed, got %#v", result)
	}
	if service.accessoryInventory["accessory_earrings_r0"] != 0 || service.accessoryInventory["accessory_earrings_r1"] != 1 {
		t.Fatalf("expected r0 copies spent into r1, inventory=%#v", service.accessoryInventory)
	}
}

func TestAccessoryFuseStopsAtDefinitionMaxRarity(t *testing.T) {
	service := NewService()
	service.accessoryInventory["accessory_earrings_r4"] = 3

	result := service.FuseAccessory("accessory_earrings_r4")
	if result.Success || result.ErrorCode != "max_rarity" {
		t.Fatalf("expected max_rarity, got %#v", result)
	}
}

func TestAccessoryLevelRespectsDefinitionMaxLevel(t *testing.T) {
	service := NewService()
	service.state.Gold = 100000
	service.accessoryInventory["accessory_earrings_r0"] = 1
	service.accessoryLevels["accessory_earrings_r0"] = 20

	result := service.LevelAccessory("accessory_earrings_r0")
	if result.Success || result.ErrorCode != "max_level" {
		t.Fatalf("expected max_level, got %#v", result)
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

	service.dailyFightCount = 15
	result := service.ClaimDailyMission("daily_battles_15")
	if !result.Success {
		t.Fatalf("expected daily claim to succeed, got %#v", result)
	}

	if !store.saved.ClaimedDaily["daily_battles_15"] {
		t.Fatalf("expected daily_battles_15 claim to be saved")
	}
}

func TestDailyMissionClaimRequiresProgress(t *testing.T) {
	service := NewService()

	result := service.ClaimDailyMission("daily_battles_15")
	if result.Success || result.ErrorCode != "not_complete" {
		t.Fatalf("expected not_complete, got %#v", result)
	}
}

func TestCampaignFightAdvancesDailyProgress(t *testing.T) {
	service := NewService()
	service.state.TeamPower = 9999

	result := service.FightCampaign()
	if !result.Success {
		t.Fatalf("expected campaign fight to succeed, got %#v", result)
	}
	if result.Combat == nil || !result.Combat.Won || result.Combat.ElapsedSeconds <= 0 || result.Combat.MaxSeconds != 30 {
		t.Fatalf("expected successful combat result, got %#v", result.Combat)
	}

	if result.PlayerSnapshot.DailyDate == "" {
		t.Fatal("expected snapshot to include daily date")
	}
	progress := dailyProgressByMission(result.PlayerSnapshot.DailyProgress, "daily_stage_clears_3")
	if progress.Progress != 1 || progress.Target != 3 {
		t.Fatalf("expected daily stage progress 1/3, got %#v", progress)
	}
	if service.dailyFightCount != 1 || service.dailyStageClears != 1 {
		t.Fatalf("expected daily counters to advance, fights=%d clears=%d", service.dailyFightCount, service.dailyStageClears)
	}
}

func TestCombatLossReturnsResultAndPersistsFightProgress(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	service.state.TeamAttack = 1
	service.state.TeamHealth = 1
	service.state.TeamPower = 1

	result := service.FightCampaign()
	if result.Success || result.ErrorCode != "combat_lost" {
		t.Fatalf("expected combat loss, got %#v", result)
	}
	if result.Combat == nil || result.Combat.Won || result.Combat.DamageTaken <= 0 {
		t.Fatalf("expected failed combat result, got %#v", result.Combat)
	}
	if store.saved.DailyFightCount != 1 {
		t.Fatalf("expected failed combat attempt to persist daily fight progress, got %d", store.saved.DailyFightCount)
	}
	if store.saved.PlayerState.CampaignStage != 1 {
		t.Fatalf("expected failed combat to keep campaign stage 1, got %d", store.saved.PlayerState.CampaignStage)
	}
}

func TestDailyWindowResetsClaimsAndProgress(t *testing.T) {
	service := NewService()
	now := time.Date(2026, 5, 10, 23, 59, 0, 0, time.UTC)
	service.now = func() time.Time { return now }
	service.dailyDate = dailyDateKey(now)
	service.dailyFightCount = 15
	service.claimedDaily["daily_battles_15"] = true

	now = now.Add(2 * time.Minute)
	snapshot := service.GetSnapshot()

	if snapshot.DailyDate != "2026-05-11" {
		t.Fatalf("expected daily date 2026-05-11, got %s", snapshot.DailyDate)
	}
	progress := dailyProgressByMission(snapshot.DailyProgress, "daily_battles_15")
	if progress.Progress != 0 || progress.Claimed {
		t.Fatalf("expected reset daily progress, got %#v", progress)
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

func dailyProgressByMission(progress []api.DailyProgress, missionID string) api.DailyProgress {
	for _, item := range progress {
		if item.MissionID == missionID {
			return item
		}
	}

	return api.DailyProgress{}
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

func TestEquipmentProgressionUsesDefinitions(t *testing.T) {
	catalog := NewSnapshotBalanceCatalog(api.DefinitionSnapshot{
		Heroes: []api.HeroDefinition{
			{HeroID: "hero_custom_starter", StarterOwned: true, MaxLevel: 100, MaxAscension: 10, BaseAttack: 20, AttackPerLevel: 4, AttackPerAscension: 9, BaseHealth: 200, HealthPerLevel: 15, HealthPerAscension: 60},
		},
		Equipment: []api.EquipmentDefinition{
			{EquipmentID: "equipment_custom", StarterOwned: true, MaxLevel: 1, AttackPerLevel: 13, HealthPerLevel: 70},
		},
		ProgressionCosts: []api.ProgressionCostDefinition{
			{Domain: "equipment", TargetID: "equipment_custom", CostCurrencyID: "gold", BaseAmount: 10, AmountPerLevel: 0},
		},
	})
	service := NewServiceForPlayer("equipment-definition-player", withServiceBalanceCatalog(catalog))
	service.state.Gold = 100
	beforeAttack := service.state.TeamAttack
	beforeHealth := service.state.TeamHealth

	result := service.LevelEquipment("equipment_custom")
	if !result.Success {
		t.Fatalf("expected equipment level to succeed, got %#v", result)
	}
	if service.state.TeamAttack != beforeAttack+13 || service.state.TeamHealth != beforeHealth+70 {
		t.Fatalf("expected equipment stats from definition, before attack=%d health=%d after=%#v", beforeAttack, beforeHealth, service.state)
	}

	second := service.LevelEquipment("equipment_custom")
	if second.Success || second.ErrorCode != "max_level" {
		t.Fatalf("expected max_level, got %#v", second)
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
	if first.PlayerSnapshot.Revision != 2 || first.Receipt.StateRevision != first.PlayerSnapshot.Revision {
		t.Fatalf("expected first action to advance revision and receipt, got %#v", first)
	}

	replay := service.FightCampaignWithRequest(context.Background(), request)
	if !replay.Success || !replay.Replay {
		t.Fatalf("expected idempotent replay, got %#v", replay)
	}
	if replay.PlayerSnapshot.Revision != first.PlayerSnapshot.Revision || replay.Receipt.StateRevision != first.Receipt.StateRevision {
		t.Fatalf("expected replay to return original revision, first=%#v replay=%#v", first.Receipt, replay.Receipt)
	}
	if replay.Combat == nil || first.Combat == nil || replay.Combat.ElapsedSeconds != first.Combat.ElapsedSeconds {
		t.Fatalf("expected replay to include the original combat result, first=%#v replay=%#v", first.Combat, replay.Combat)
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

func TestExpectedRevisionRejectsStaleAction(t *testing.T) {
	service := NewService()

	first := service.FightCampaignWithRequest(context.Background(), ActionRequest{ExpectedRevision: 1})
	if !first.Success || first.PlayerSnapshot.Revision != 2 {
		t.Fatalf("expected first campaign fight to advance revision, got %#v", first)
	}

	stale := service.FightCampaignWithRequest(context.Background(), ActionRequest{ExpectedRevision: 1})
	if stale.Success || stale.ErrorCode != "stale_player_state" {
		t.Fatalf("expected stale revision rejection, got %#v", stale)
	}
	if stale.PlayerSnapshot.Revision != first.PlayerSnapshot.Revision || stale.PlayerState.CampaignStage != first.PlayerState.CampaignStage {
		t.Fatalf("expected stale rejection to return current state without mutation, first=%#v stale=%#v", first, stale)
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
