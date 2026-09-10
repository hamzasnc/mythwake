package player

import "fmt"

const combatFormationSlots = 7

// resolveCombatFormation is called inside the action lock. It copies the
// selection so retries and simulation cannot observe a changed caller slice.
func (service *Service) resolveCombatFormation(requested []string) ([]string, error) {
	if requested == nil {
		selected := make([]string, 0, combatFormationSlots)
		for _, definition := range service.balanceCatalog.HeroDefinitions() {
			// Older clients have seven slots and no Kael assets. A new starter
			// grant must not silently insert an eighth combatant in their team.
			if definition.ID == "hero_kael" || service.heroLevels[definition.ID] <= 0 {
				continue
			}
			selected = append(selected, definition.ID)
			if len(selected) == combatFormationSlots {
				break
			}
		}
		if len(selected) == 0 {
			return nil, fmt.Errorf("formation has no owned heroes")
		}
		return selected, nil
	}
	if len(requested) < 1 || len(requested) > combatFormationSlots {
		return nil, fmt.Errorf("formation must contain 1 to %d heroes", combatFormationSlots)
	}
	selected := make([]string, len(requested))
	seen := make(map[string]bool, len(requested))
	for index, heroID := range requested {
		if _, ok := service.balanceCatalog.HeroDefinitionByID(heroID); !ok || service.heroLevels[heroID] <= 0 {
			return nil, fmt.Errorf("unknown or unowned formation hero: %s", heroID)
		}
		if seen[heroID] {
			return nil, fmt.Errorf("duplicate formation hero: %s", heroID)
		}
		seen[heroID] = true
		selected[index] = heroID
	}
	return selected, nil
}

// Combat stats are request-local. Account-wide power is still used by other
// progression systems and must not be replaced by a temporary formation.
func (service *Service) combatTeamStats(heroIDs []string) (int, int) {
	attack, health := 0, 0
	for _, heroID := range heroIDs {
		definition, ok := service.balanceCatalog.HeroDefinitionByID(heroID)
		if !ok || service.heroLevels[heroID] <= 0 {
			continue
		}
		level := clampHeroLevel(service.heroLevels[heroID], definition.MaxLevel)
		ascension := clampHeroAscension(service.heroAscensions[heroID], definition.MaxAscension)
		stars := service.heroStars[heroID]
		attack += heroAttackFromDefinition(definition, level, ascension) + heroStarAttackBonus(definition, stars)
		health += definition.BaseHealth + (level-1)*definition.HealthPerLevel + ascension*definition.HealthPerAscension + heroStarHealthBonus(definition, stars)
	}
	equipmentAttack, equipmentHealth, _ := service.equipmentStatBonuses()
	accessoryAttack, accessoryHealth := service.accessoryStatBonuses()
	villageAttack, villageHealth := service.villageStatBonuses()
	return max(1, attack+equipmentAttack+accessoryAttack+villageAttack), max(1, health+equipmentHealth+accessoryHealth+villageHealth)
}
