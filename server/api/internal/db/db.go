// Package db opens the PostgreSQL pool and applies the embedded migrations.
package db

import (
	"context"
	"embed"
	"fmt"
	"io/fs"
	"sort"
	"strconv"
	"strings"

	"github.com/jackc/pgx/v5/pgxpool"
)

//go:embed migrations/*.sql
var migrations embed.FS

// migrationLock is the advisory lock id that serialises migrations across API instances.
const migrationLock = 7_310_001

func Open(ctx context.Context, url string) (*pgxpool.Pool, error) {
	pool, err := pgxpool.New(ctx, url)
	if err != nil {
		return nil, err
	}
	if err := pool.Ping(ctx); err != nil {
		pool.Close()
		return nil, fmt.Errorf("database not reachable: %w", err)
	}
	return pool, nil
}

// Migrate applies every migrations/NNN_name.sql not applied yet, in order, each in its own transaction.
func Migrate(ctx context.Context, pool *pgxpool.Pool) (applied []string, err error) {
	conn, err := pool.Acquire(ctx)
	if err != nil {
		return nil, err
	}
	defer conn.Release()
	if _, err := conn.Exec(ctx, "select pg_advisory_lock($1)", migrationLock); err != nil {
		return nil, err
	}
	defer conn.Exec(context.Background(), "select pg_advisory_unlock($1)", migrationLock)

	if _, err := conn.Exec(ctx, `create table if not exists schema_migrations (
		version int primary key, name text not null, applied_at timestamptz not null default now())`); err != nil {
		return nil, err
	}
	files, err := fs.Glob(migrations, "migrations/*.sql")
	if err != nil {
		return nil, err
	}
	sort.Strings(files)
	for _, file := range files {
		name := strings.TrimSuffix(strings.TrimPrefix(file, "migrations/"), ".sql")
		version, err := strconv.Atoi(strings.SplitN(name, "_", 2)[0])
		if err != nil {
			return applied, fmt.Errorf("migration %s: name must start with a number", file)
		}
		var done bool
		if err := conn.QueryRow(ctx, "select exists(select 1 from schema_migrations where version = $1)", version).Scan(&done); err != nil {
			return applied, err
		}
		if done {
			continue
		}
		sql, err := migrations.ReadFile(file)
		if err != nil {
			return applied, err
		}
		tx, err := conn.Begin(ctx)
		if err != nil {
			return applied, err
		}
		if _, err := tx.Exec(ctx, string(sql)); err != nil {
			tx.Rollback(ctx)
			return applied, fmt.Errorf("migration %s: %w", name, err)
		}
		if _, err := tx.Exec(ctx, "insert into schema_migrations (version, name) values ($1, $2)", version, name); err != nil {
			tx.Rollback(ctx)
			return applied, err
		}
		if err := tx.Commit(ctx); err != nil {
			return applied, err
		}
		applied = append(applied, name)
	}
	return applied, nil
}
