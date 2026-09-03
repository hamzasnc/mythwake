package player

import "github.com/hamzasnc/mythwake/backend/internal/balance"

func (service *Service) normalizeTowerProgress() {
	definition, ok := service.balanceCatalog.TowerDefinitionByID(towerDungeonID)
	if !ok {
		definition = balance.TowerDefinition{MaxFloor: 1000, SectionSize: 100}
	}

	maxFloor := max(1, definition.MaxFloor)
	sectionSize := max(1, definition.SectionSize)
	service.towerHighestClearedFloor = min(max(0, service.towerHighestClearedFloor), maxFloor)
	service.towerHighestUnlockedFloor = min(max(max(1, service.towerHighestUnlockedFloor), service.towerHighestClearedFloor+1), maxFloor)
	if service.towerHighestClearedFloor >= maxFloor {
		service.towerHighestUnlockedFloor = maxFloor
	}
	service.towerSelectedFloor = min(max(1, service.towerSelectedFloor), service.towerHighestUnlockedFloor)
	service.towerSectionStartFloor = towerSectionStart(service.towerSelectedFloor, sectionSize, maxFloor)
}

func towerSectionStart(floor int, sectionSize int, maxFloor int) int {
	floor = min(max(1, floor), max(1, maxFloor))
	sectionSize = max(1, sectionSize)
	return ((floor-1)/sectionSize)*sectionSize + 1
}
