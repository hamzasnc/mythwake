package player

import (
	"context"
	"fmt"
	"strings"
	"sync"
)

type Manager struct {
	mu         sync.Mutex
	stateStore StateStore
	services   map[string]*Service
}

func NewManager(stateStore StateStore) *Manager {
	return &Manager{
		stateStore: stateStore,
		services:   map[string]*Service{},
	}
}

func (manager *Manager) ServiceForPlayer(ctx context.Context, playerID string) (*Service, error) {
	playerID = strings.TrimSpace(playerID)
	if playerID == "" {
		return nil, fmt.Errorf("player id is required")
	}

	manager.mu.Lock()
	if service, ok := manager.services[playerID]; ok {
		manager.mu.Unlock()
		return service, nil
	}
	manager.mu.Unlock()

	service := NewServiceForPlayer(playerID)
	if manager.stateStore != nil {
		if err := service.UseStateStore(ctx, manager.stateStore); err != nil {
			return nil, err
		}
	}

	manager.mu.Lock()
	defer manager.mu.Unlock()

	if existing, ok := manager.services[playerID]; ok {
		return existing, nil
	}

	manager.services[playerID] = service
	return service, nil
}
