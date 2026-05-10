package economy

import (
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
)

const (
	CurrencyGold        = "gold"
	CurrencyGems        = "gems"
	CurrencyMythEssence = "myth_essence"
	CurrencyPassXP      = "pass_xp"
	CurrencyHeroShards  = "hero_shards"
)

type CurrencyDefinition struct {
	ID          string
	DisplayName string
	IsPremium   bool
}

type InsufficientCurrencyError struct {
	CurrencyID string
	Required   int
	Available  int
}

var currencyDefinitions = []CurrencyDefinition{
	{ID: CurrencyGold, DisplayName: "Gold"},
	{ID: CurrencyGems, DisplayName: "Gems", IsPremium: true},
	{ID: CurrencyMythEssence, DisplayName: "Myth Essence"},
	{ID: CurrencyPassXP, DisplayName: "Pass XP"},
	{ID: CurrencyHeroShards, DisplayName: "Hero Shards"},
}

func (err InsufficientCurrencyError) Error() string {
	return err.Message()
}

func (err InsufficientCurrencyError) Message() string {
	return fmt.Sprintf("Need %d %s.", err.Required, DisplayName(err.CurrencyID))
}

func DisplayName(currencyID string) string {
	for _, definition := range currencyDefinitions {
		if definition.ID == currencyID {
			return definition.DisplayName
		}
	}

	return currencyID
}

func CurrencyDefinitions() []CurrencyDefinition {
	definitions := make([]CurrencyDefinition, len(currencyDefinitions))
	copy(definitions, currencyDefinitions)
	return definitions
}

func Amount(state api.PlayerState, currencyID string) (int, bool) {
	switch currencyID {
	case CurrencyGold:
		return state.Gold, true
	case CurrencyGems:
		return state.Gems, true
	case CurrencyMythEssence:
		return state.MythEssence, true
	case CurrencyPassXP:
		return state.PassXP, true
	default:
		return 0, false
	}
}

func Spend(state *api.PlayerState, currencyID string, amount int) error {
	if amount < 0 {
		return fmt.Errorf("currency spend must not be negative: %d", amount)
	}

	available, ok := Amount(*state, currencyID)
	if !ok {
		return fmt.Errorf("unknown currency: %s", currencyID)
	}
	if available < amount {
		return InsufficientCurrencyError{
			CurrencyID: currencyID,
			Required:   amount,
			Available:  available,
		}
	}

	switch currencyID {
	case CurrencyGold:
		state.Gold -= amount
	case CurrencyGems:
		state.Gems -= amount
	case CurrencyMythEssence:
		state.MythEssence -= amount
	case CurrencyPassXP:
		state.PassXP -= amount
	}

	return nil
}

func Grant(state *api.PlayerState, reward api.Reward) {
	state.Gold += reward.Gold
	state.Gems += reward.Gems
	state.MythEssence += reward.MythEssence
	state.PassXP += reward.PassXP
}

func Delta(before api.PlayerState, after api.PlayerState) api.Reward {
	return api.Reward{
		Gold:        after.Gold - before.Gold,
		Gems:        after.Gems - before.Gems,
		MythEssence: after.MythEssence - before.MythEssence,
		PassXP:      after.PassXP - before.PassXP,
	}
}
