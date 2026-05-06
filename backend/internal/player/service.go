package player

import "github.com/hamzasnc/mythwake/backend/internal/api"

type Service struct {
	state api.PlayerState
}

func NewService() *Service {
	return &Service{
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
	}
}

func (service *Service) GetState() api.PlayerState {
	return service.state
}
