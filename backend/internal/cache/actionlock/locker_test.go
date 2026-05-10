package actionlock

import (
	"context"
	"testing"
	"time"
)

func TestMemoryLockerBlocksUntilReleaseOrExpiry(t *testing.T) {
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	locker := NewMemoryLocker()
	locker.now = func() time.Time { return now }
	ctx := context.Background()

	lock, ok, err := locker.Acquire(ctx, "player:one", time.Second)
	if err != nil {
		t.Fatalf("acquire lock: %v", err)
	}
	if !ok {
		t.Fatal("expected first lock to be acquired")
	}

	if _, ok, err := locker.Acquire(ctx, "player:one", time.Second); err != nil || ok {
		t.Fatalf("expected second lock to be blocked, ok=%v err=%v", ok, err)
	}

	if err := lock.Release(ctx); err != nil {
		t.Fatalf("release lock: %v", err)
	}
	if _, ok, err := locker.Acquire(ctx, "player:one", time.Second); err != nil || !ok {
		t.Fatalf("expected lock after release, ok=%v err=%v", ok, err)
	}

	now = now.Add(2 * time.Second)
	if _, ok, err := locker.Acquire(ctx, "player:one", time.Second); err != nil || !ok {
		t.Fatalf("expected expired lock to be replaceable, ok=%v err=%v", ok, err)
	}
}
