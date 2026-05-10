package player

import (
	"context"
	"errors"
	"fmt"
	"strings"
	"sync"
	"time"
)

type Manager struct {
	mu             sync.Mutex
	stateStore     StateStore
	balanceCatalog BalanceCatalog
	services       map[string]managedService
	now            func() time.Time
}

type managedService struct {
	service    *Service
	lastUsedAt time.Time
}

type ManagerOption func(*Manager)

func WithBalanceCatalog(catalog BalanceCatalog) ManagerOption {
	return func(manager *Manager) {
		if catalog != nil {
			manager.balanceCatalog = catalog
		}
	}
}

func WithClock(now func() time.Time) ManagerOption {
	return func(manager *Manager) {
		if now != nil {
			manager.now = now
		}
	}
}

func NewManager(stateStore StateStore, options ...ManagerOption) *Manager {
	manager := &Manager{
		stateStore:     stateStore,
		balanceCatalog: StaticBalanceCatalog{},
		services:       map[string]managedService{},
		now:            func() time.Time { return time.Now().UTC() },
	}
	for _, option := range options {
		option(manager)
	}
	return manager
}

func (manager *Manager) ServiceForPlayer(ctx context.Context, playerID string) (*Service, error) {
	playerID = strings.TrimSpace(playerID)
	if playerID == "" {
		return nil, fmt.Errorf("player id is required")
	}

	manager.mu.Lock()
	if managed, ok := manager.services[playerID]; ok {
		managed.lastUsedAt = manager.now()
		manager.services[playerID] = managed
		manager.mu.Unlock()
		return managed.service, nil
	}
	manager.mu.Unlock()

	service := NewServiceForPlayer(playerID, withServiceBalanceCatalog(manager.balanceCatalog))
	if manager.stateStore != nil {
		if err := service.UseStateStore(ctx, manager.stateStore); err != nil {
			return nil, err
		}
	}

	manager.mu.Lock()
	defer manager.mu.Unlock()

	if existing, ok := manager.services[playerID]; ok {
		existing.lastUsedAt = manager.now()
		manager.services[playerID] = existing
		return existing.service, nil
	}

	manager.services[playerID] = managedService{service: service, lastUsedAt: manager.now()}
	return service, nil
}

func (manager *Manager) FlushAll(ctx context.Context) error {
	manager.mu.Lock()
	services := make([]*Service, 0, len(manager.services))
	for _, managed := range manager.services {
		services = append(services, managed.service)
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
	managed, ok := manager.services[playerID]
	manager.mu.Unlock()
	if !ok {
		return false, nil
	}

	service := managed.service
	if err := service.FlushState(ctx); err != nil {
		return true, fmt.Errorf("flush player %s: %w", service.PlayerID(), err)
	}

	return true, nil
}

func (manager *Manager) ResetPlayer(ctx context.Context, playerID string) (*Service, error) {
	playerID = strings.TrimSpace(playerID)
	if playerID == "" {
		return nil, fmt.Errorf("player id is required")
	}

	if manager.stateStore != nil {
		resetter, ok := manager.stateStore.(StateResetter)
		if !ok {
			return nil, fmt.Errorf("state store does not support reset")
		}
		if err := resetter.ResetState(ctx, playerID); err != nil {
			return nil, err
		}
	}

	manager.mu.Lock()
	delete(manager.services, playerID)
	manager.mu.Unlock()

	service := NewServiceForPlayer(playerID, withServiceBalanceCatalog(manager.balanceCatalog))
	if manager.stateStore != nil {
		if err := service.UseStateStore(ctx, manager.stateStore); err != nil {
			return nil, err
		}
	}

	manager.mu.Lock()
	manager.services[playerID] = managedService{service: service, lastUsedAt: manager.now()}
	manager.mu.Unlock()

	return service, nil
}

func (manager *Manager) FlushIdle(ctx context.Context, maxIdle time.Duration) (int, error) {
	if maxIdle <= 0 {
		return 0, nil
	}

	cutoff := manager.now().Add(-maxIdle)

	manager.mu.Lock()
	candidates := make(map[string]managedService)
	for playerID, managed := range manager.services {
		if managed.lastUsedAt.Before(cutoff) {
			candidates[playerID] = managed
		}
	}
	manager.mu.Unlock()

	var unloaded int
	var flushError error
	for playerID, managed := range candidates {
		if err := ctx.Err(); err != nil {
			return unloaded, errors.Join(flushError, err)
		}
		if err := managed.service.FlushState(ctx); err != nil {
			flushError = errors.Join(flushError, fmt.Errorf("flush idle player %s: %w", managed.service.PlayerID(), err))
			continue
		}

		manager.mu.Lock()
		current, ok := manager.services[playerID]
		if ok && current.service == managed.service && current.lastUsedAt.Equal(managed.lastUsedAt) {
			delete(manager.services, playerID)
			unloaded++
		}
		manager.mu.Unlock()
	}

	return unloaded, flushError
}

func (manager *Manager) LoadedPlayerCount() int {
	manager.mu.Lock()
	defer manager.mu.Unlock()

	return len(manager.services)
}
