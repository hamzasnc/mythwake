package player

import (
	"context"
	"errors"
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

func (manager *Manager) FlushAll(ctx context.Context) error {
	manager.mu.Lock()
	services := make([]*Service, 0, len(manager.services))
	for _, service := range manager.services {
		services = append(services, service)
	}
	manager.mu.Unlock()

	var flushError error
	for _, service := range services {
		if err := ctx.Err(); err != nil {
			return errors.Join(flushError, err)
		}
		if err := service.FlushState(ctx); err != nil {
			flushError = errors.Join(flushError, fmt.Errorf("flush player %s: %w", service.PlayerID(), err))
		}
	}

	return flushError
}

func (manager *Manager) FlushPlayerIfLoaded(ctx context.Context, playerID string) (bool, error) {
	playerID = strings.TrimSpace(playerID)
	if playerID == "" {
		return false, fmt.Errorf("player id is required")
	}

	manager.mu.Lock()
	service, ok := manager.services[playerID]
	manager.mu.Unlock()
	if !ok {
		return false, nil
	}

	if err := service.FlushState(ctx); err != nil {
		return true, fmt.Errorf("flush player %s: %w", service.PlayerID(), err)
	}

	return true, nil
}

func (manager *Manager) LoadedPlayerCount() int {
	manager.mu.Lock()
	defer manager.mu.Unlock()

	return len(manager.services)
}
