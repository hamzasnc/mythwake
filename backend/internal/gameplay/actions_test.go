package gameplay

import "testing"

func TestActionCatalogIDsAreUnique(t *testing.T) {
	seen := map[string]bool{}
	for _, definition := range ActionCatalog() {
		if definition.ID == "" {
			t.Fatalf("action definition has empty id: %#v", definition)
		}
		if seen[definition.ID] {
			t.Fatalf("duplicate action id %q", definition.ID)
		}
		seen[definition.ID] = true
	}
}

func TestActionCatalogRequiresIdempotencyForGameplayMutations(t *testing.T) {
	for _, definition := range ActionCatalog() {
		if !definition.RequiresIdempotency {
			t.Fatalf("gameplay action %q must require idempotency", definition.ID)
		}
	}
}

func TestActionDefinitionByID(t *testing.T) {
	definition, found := ActionDefinitionByID(ActionHeroLevel)
	if !found {
		t.Fatalf("expected %q to exist in action catalog", ActionHeroLevel)
	}
	if definition.Domain != "hero" {
		t.Fatalf("expected hero domain, got %#v", definition)
	}
}
