package e2e

import (
	"encoding/json"
	"testing"
)

// The intro / coach steps are saved on the server, so a second device does not replay them (docs/systems/TUTORIAL.md).
func TestTutorialProgressIsSaved(t *testing.T) {
	c, _ := newPlayer(t)
	var r commandsResult
	done := []command{{ID: uuid(), Type: "CompleteTutorial", Data: map[string]any{"id": "intro"}}}
	if s := c.do("POST", "/v1/commands", map[string]any{"baseVersion": 1, "commands": done}, &r); s != 200 || r.Version != 2 {
		t.Fatalf("complete intro: %d %+v", s, r)
	}
	unknown := []command{{ID: uuid(), Type: "CompleteTutorial", Data: map[string]any{"id": "free_gems"}}}
	if s := c.do("POST", "/v1/commands", map[string]any{"baseVersion": 2, "commands": unknown}, &r); s != 422 || r.Error != "unknown_tutorial" {
		t.Fatalf("unknown step: want 422 unknown_tutorial, got %d %+v", s, r)
	}
	var p player
	c.do("GET", "/v1/player", nil, &p)
	var state struct {
		TutorialDone []string `json:"tutorialDone"`
	}
	_ = json.Unmarshal(p.State, &state)
	if len(state.TutorialDone) != 1 || state.TutorialDone[0] != "intro" {
		t.Fatalf("saved steps: %v", state.TutorialDone)
	}
}
