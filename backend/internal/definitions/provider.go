package definitions

import (
	"context"

	"github.com/hamzasnc/mythwake/backend/internal/api"
)

type SnapshotProvider interface {
	Snapshot(ctx context.Context, apiVersion string) (api.DefinitionSnapshot, error)
}

type StaticSnapshotProvider struct{}

func NewStaticSnapshotProvider() StaticSnapshotProvider {
	return StaticSnapshotProvider{}
}

func (StaticSnapshotProvider) Snapshot(_ context.Context, apiVersion string) (api.DefinitionSnapshot, error) {
	return Snapshot(apiVersion), nil
}
