package player

type campaignActions struct {
	service *Service
}

type dungeonActions struct {
	service *Service
}

type heroProgressionActions struct {
	service *Service
}

type equipmentActions struct {
	service *Service
}

type accessoryActions struct {
	service *Service
}

type summonActions struct {
	service *Service
}

type missionActions struct {
	service *Service
}

func (service *Service) configureDomainServices() {
	service.campaignActions = campaignActions{service: service}
	service.dungeonActions = dungeonActions{service: service}
	service.heroActions = heroProgressionActions{service: service}
	service.equipmentActions = equipmentActions{service: service}
	service.accessoryActions = accessoryActions{service: service}
	service.summonActions = summonActions{service: service}
	service.missionActions = missionActions{service: service}
}
