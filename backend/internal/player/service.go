package player

import (
	"context"
	"errors"
	"fmt"
	"strings"
	"sync"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

const (
	defaultPlayerID  = "dev-player-1"
	goldDungeonID    = balance.DungeonGold
	essenceDungeonID = balance.DungeonEssence
	gearDungeonID    = balance.DungeonGear
	heroBannerID     = balance.BannerHeroShardStandard
	weaponID         = balance.EquipmentWeapon
	armorID          = balance.EquipmentArmor
)

type StateStore interface {
	LoadState(ctx context.Context, playerID string) (PersistentState, bool, error)
	SaveState(ctx context.Context, playerID string, state PersistentState, source StateSaveSource) error
}

type ActionResultStore interface {
	LoadActionResult(ctx context.Context, playerID string, idempotencyKey string) (StoredActionResult, bool, error)
}

type ActionResultWriter interface {
	SaveActionResult(ctx context.Context, playerID string, source StateSaveSource) error
}

type StateFlusher interface {
	Flush(ctx context.Context) error
}

type PersistentState struct {
	PlayerState        api.PlayerState
	HeroLevels         map[string]int
	HeroShards         map[string]int
	HeroAscensions     map[string]int
	EquipmentLevels    map[string]int
	AccessoryInventory map[string]int
	AccessoryLevels    map[string]int
	EquippedAccessory  map[string]string
	ClaimedDaily       map[string]bool
	ClaimedBattlePass  map[string]bool
	SummonCount        int
	LastAFKClaimedAt   time.Time
	DailyDate          string
	DailyFightCount    int
	DailyStageClears   int
	DailySummonCount   int
}

type StateSaveSource struct {
	ActionID       string
	RewardID       string
	IdempotencyKey string
	RequestHash    string
	CurrencyDelta  api.Reward
	ActionResult   *api.ActionResult
}

type ActionRequest struct {
	IdempotencyKey string
	RequestHash    string
}

type StoredActionResult struct {
	ActionID     string
	RequestHash  string
	ActionResult api.ActionResult
}

func (request ActionRequest) HasIdempotency() bool {
	return request.IdempotencyKey != "" && request.RequestHash != ""
}

func ClonePersistentState(state PersistentState) PersistentState {
	return PersistentState{
		PlayerState:        state.PlayerState,
		HeroLevels:         cloneIntMap(state.HeroLevels),
		HeroShards:         cloneIntMap(state.HeroShards),
		HeroAscensions:     cloneIntMap(state.HeroAscensions),
		EquipmentLevels:    cloneIntMap(state.EquipmentLevels),
		AccessoryInventory: cloneIntMap(state.AccessoryInventory),
		AccessoryLevels:    cloneIntMap(state.AccessoryLevels),
		EquippedAccessory:  cloneStringMap(state.EquippedAccessory),
		ClaimedDaily:       cloneBoolMap(state.ClaimedDaily),
		ClaimedBattlePass:  cloneBoolMap(state.ClaimedBattlePass),
		SummonCount:        state.SummonCount,
		LastAFKClaimedAt:   state.LastAFKClaimedAt,
		DailyDate:          state.DailyDate,
		DailyFightCount:    state.DailyFightCount,
		DailyStageClears:   state.DailyStageClears,
		DailySummonCount:   state.DailySummonCount,
	}
}

type Service struct {
	mu                 sync.Mutex
	playerID           string
	stateStore         StateStore
	actionResultStore  ActionResultStore
	balanceCatalog     BalanceCatalog
	campaignActions    campaignActions
	dungeonActions     dungeonActions
	heroActions        heroProgressionActions
	equipmentActions   equipmentActions
	accessoryActions   accessoryActions
	summonActions      summonActions
	missionActions     missionActions
	afkActions         afkActions
	state              api.PlayerState
	heroLevels         map[string]int
	heroShards         map[string]int
	heroAscensions     map[string]int
	equipmentLevels    map[string]int
	accessoryInventory map[string]int
	accessoryLevels    map[string]int
	equippedAccessory  map[string]string
	claimedDaily       map[string]bool
	claimedBattlePass  map[string]bool
	summonCount        int
	lastAFKClaimedAt   time.Time
	dailyDate          string
	dailyFightCount    int
	dailyStageClears   int
	dailySummonCount   int
	now                func() time.Time
}

type ServiceOption func(*Service)

func withServiceBalanceCatalog(catalog BalanceCatalog) ServiceOption {
	return func(service *Service) {
		if catalog != nil {
			service.balanceCatalog = catalog
		}
	}
}

func NewService() *Service {
	return NewServiceForPlayer(defaultPlayerID)
}

func NewServiceForPlayer(playerID string, options ...ServiceOption) *Service {
	playerID = strings.TrimSpace(playerID)
	if playerID == "" {
		playerID = defaultPlayerID
	}

	service := &Service{
		playerID:       playerID,
		balanceCatalog: StaticBalanceCatalog{},
		now:            time.Now,
		state: api.PlayerState{
			SaveVersion:         1,
			Gold:                0,
			Gems:                35,
			MythEssence:         20,
			PassXP:              0,
			CampaignStage:       1,
			GoldDungeonFloor:    1,
			EssenceDungeonFloor: 1,
			GearDungeonFloor:    1,
			TeamPower:           148,
			TeamAttack:          96,
			TeamHealth:          780,
		},
		heroLevels:         map[string]int{},
		heroShards:         map[string]int{},
		heroAscensions:     map[string]int{},
		equipmentLevels:    map[string]int{weaponID: 0, armorID: 0},
		accessoryInventory: map[string]int{},
		accessoryLevels:    map[string]int{},
		equippedAccessory:  map[string]string{},
		claimedDaily:       map[string]bool{},
		claimedBattlePass:  map[string]bool{},
	}
	for _, option := range options {
		option(service)
	}
	service.seedInitialHeroes()
	service.lastAFKClaimedAt = service.now().UTC()
	service.dailyDate = dailyDateKey(service.now())
	service.configureDomainServices()
	return service
}

func (service *Service) seedInitialHeroes() {
	for _, definition := range service.balanceCatalog.HeroDefinitions() {
		if definition.ID == "" {
			continue
		}
		if definition.StarterOwned {
			service.heroLevels[definition.ID] = max(1, service.heroLevels[definition.ID])
		}
		if _, ok := service.heroShards[definition.ID]; !ok {
			service.heroShards[definition.ID] = 0
		}
	}
}

func (service *Service) UseStateStore(ctx context.Context, store StateStore) error {
	service.mu.Lock()
	defer service.mu.Unlock()

	service.stateStore = store
	if actionResultStore, ok := store.(ActionResultStore); ok {
		service.actionResultStore = actionResultStore
	}
	persistedState, found, err := store.LoadState(ctx, service.playerID)
	if err != nil {
		return err
	}
	if found {
		service.applyPersistentState(persistedState)
		return nil
	}

	return store.SaveState(ctx, service.playerID, service.persistentState(), StateSaveSource{ActionID: gameplay.ActionPlayerStateSeed})
}

func (service *Service) PlayerID() string {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.playerID
}

func (service *Service) GuestAuth(sessionToken string) api.GuestAuthResponse {
	service.mu.Lock()
	defer service.mu.Unlock()

	service.ensureDailyWindow()
	return api.GuestAuthResponse{
		PlayerID:       service.playerID,
		SessionToken:   sessionToken,
		PlayerState:    service.state,
		PlayerSnapshot: service.snapshot(),
	}
}

func (service *Service) GetState() api.PlayerState {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.state
}

func (service *Service) GetSnapshot() api.PlayerSnapshot {
	service.mu.Lock()
	defer service.mu.Unlock()

	service.ensureDailyWindow()
	return service.snapshot()
}

func (service *Service) FlushState(ctx context.Context) error {
	service.mu.Lock()
	store := service.stateStore
	if store == nil {
		service.mu.Unlock()
		return nil
	}

	playerID := service.playerID
	service.ensureDailyWindow()
	state := service.persistentState()
	service.mu.Unlock()

	if err := store.SaveState(ctx, playerID, state, StateSaveSource{ActionID: gameplay.ActionPlayerStateFlush}); err != nil {
		return err
	}

	flusher, ok := store.(StateFlusher)
	if !ok {
		return nil
	}

	return flusher.Flush(ctx)
}

type actionOutcome struct {
	success   bool
	persist   bool
	errorCode string
	message   string
	reward    api.Reward
	combat    *api.CombatResult
}

func actionSuccess(message string, reward api.Reward) actionOutcome {
	return actionOutcome{
		success: true,
		message: message,
		reward:  reward,
	}
}

func actionSuccessWithCombat(message string, reward api.Reward, combat api.CombatResult) actionOutcome {
	outcome := actionSuccess(message, reward)
	outcome.combat = &combat
	return outcome
}

func actionFailure(errorCode string, message string) actionOutcome {
	return actionOutcome{
		errorCode: errorCode,
		message:   message,
	}
}

func actionFailureWithCombat(errorCode string, message string, combat api.CombatResult, persist bool) actionOutcome {
	return actionOutcome{
		persist:   persist,
		errorCode: errorCode,
		message:   message,
		combat:    &combat,
	}
}

func (service *Service) executeAction(ctx context.Context, request ActionRequest, actionID string, run func() actionOutcome) api.ActionResult {
	if result, handled := service.replayedActionResult(ctx, request, actionID); handled {
		return result
	}

	service.ensureDailyWindow()
	beforeState := service.persistentState()
	outcome := run()
	result := service.newActionResult(outcome.success, actionID, request.IdempotencyKey, false, outcome.errorCode, outcome.message, outcome.reward, outcome.combat)
	if !outcome.success && !outcome.persist {
		return result
	}

	delta := economy.Delta(beforeState.PlayerState, service.state)
	if err := service.saveState(ctx, request, actionID, outcome.reward, delta, result); err != nil {
		service.applyPersistentState(beforeState)
		return service.newActionResult(false, actionID, request.IdempotencyKey, false, "persistence_failed", fmt.Sprintf("Action could not be saved and was rolled back: %v", err), api.Reward{}, nil)
	}

	return result
}

func (service *Service) replayedActionResult(ctx context.Context, request ActionRequest, actionID string) (api.ActionResult, bool) {
	if !request.HasIdempotency() || service.actionResultStore == nil {
		return api.ActionResult{}, false
	}

	record, found, err := service.actionResultStore.LoadActionResult(ctx, service.playerID, request.IdempotencyKey)
	if err != nil {
		return service.newActionResult(false, actionID, request.IdempotencyKey, false, "idempotency_lookup_failed", fmt.Sprintf("Could not verify idempotency key: %v", err), api.Reward{}, nil), true
	}
	if !found {
		return api.ActionResult{}, false
	}
	if record.ActionID != actionID || record.RequestHash != request.RequestHash {
		return service.newActionResult(false, actionID, request.IdempotencyKey, false, "idempotency_conflict", "Idempotency key was already used for a different action request.", api.Reward{}, nil), true
	}

	result := record.ActionResult
	result.IdempotencyKey = request.IdempotencyKey
	result.Replay = true
	return result, true
}

func (service *Service) newActionResult(success bool, actionID string, idempotencyKey string, replay bool, errorCode string, message string, reward api.Reward, combat *api.CombatResult) api.ActionResult {
	return api.ActionResult{
		Success:        success,
		ActionID:       actionID,
		IdempotencyKey: idempotencyKey,
		Replay:         replay,
		ErrorCode:      errorCode,
		Message:        message,
		PlayerState:    service.state,
		PlayerSnapshot: service.snapshot(),
		Reward:         reward,
		Combat:         combat,
	}
}

func (service *Service) spendCurrency(currencyID string, amount int) (actionOutcome, bool) {
	if err := economy.Spend(&service.state, currencyID, amount); err != nil {
		var insufficient economy.InsufficientCurrencyError
		if errors.As(err, &insufficient) {
			return actionFailure("insufficient_currency", insufficient.Message()), false
		}

		return actionFailure("invalid_currency", err.Error()), false
	}

	return actionOutcome{}, true
}
