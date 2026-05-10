package economy

import (
	"errors"
	"testing"

	"github.com/hamzasnc/mythwake/backend/internal/api"
)

func TestSpendCurrency(t *testing.T) {
	state := api.PlayerState{Gold: 120}

	if err := Spend(&state, CurrencyGold, 75); err != nil {
		t.Fatalf("spend gold: %v", err)
	}
	if state.Gold != 45 {
		t.Fatalf("expected 45 gold after spend, got %d", state.Gold)
	}
}

func TestSpendCurrencyReturnsInsufficientError(t *testing.T) {
	state := api.PlayerState{MythEssence: 20}

	err := Spend(&state, CurrencyMythEssence, 35)
	var insufficient InsufficientCurrencyError
	if !errors.As(err, &insufficient) {
		t.Fatalf("expected insufficient currency error, got %v", err)
	}
	if insufficient.Available != 20 || insufficient.Required != 35 {
		t.Fatalf("unexpected insufficient currency data: %#v", insufficient)
	}
	if state.MythEssence != 20 {
		t.Fatalf("spend should not mutate failed state, got %d", state.MythEssence)
	}
}

func TestGrantReward(t *testing.T) {
	state := api.PlayerState{Gold: 10, Gems: 1, MythEssence: 5, PassXP: 2}

	Grant(&state, api.Reward{Gold: 3, Gems: 4, MythEssence: 7, PassXP: 9})

	if state.Gold != 13 || state.Gems != 5 || state.MythEssence != 12 || state.PassXP != 11 {
		t.Fatalf("unexpected state after grant: %#v", state)
	}
}

func TestDelta(t *testing.T) {
	before := api.PlayerState{Gold: 100, Gems: 20, MythEssence: 50, PassXP: 5}
	after := api.PlayerState{Gold: 80, Gems: 25, MythEssence: 75, PassXP: 5}

	delta := Delta(before, after)

	if delta.Gold != -20 || delta.Gems != 5 || delta.MythEssence != 25 || delta.PassXP != 0 {
		t.Fatalf("unexpected delta: %#v", delta)
	}
}
