package database

import (
	"context"
	"database/sql"
	"embed"
	"fmt"
	"io/fs"
	"path/filepath"
	"sort"
	"strings"
)

//go:embed migrations/*.sql
var migrationFiles embed.FS

func Migrate(ctx context.Context, db *sql.DB) error {
	tx, err := db.BeginTx(ctx, nil)
	if err != nil {
		return err
	}
	defer tx.Rollback()

	if _, err := tx.ExecContext(ctx, `
		CREATE SCHEMA IF NOT EXISTS common
	`); err != nil {
		return err
	}

	if _, err := tx.ExecContext(ctx, `
		CREATE TABLE IF NOT EXISTS common.schema_migrations (
			version text PRIMARY KEY,
			applied_at timestamptz NOT NULL DEFAULT now()
		)
	`); err != nil {
		return err
	}

	files, err := fs.Glob(migrationFiles, "migrations/*.sql")
	if err != nil {
		return err
	}
	sort.Strings(files)

	for _, file := range files {
		version := strings.TrimSuffix(filepath.Base(file), ".sql")
		applied, err := migrationApplied(ctx, tx, version)
		if err != nil {
			return err
		}
		if applied {
			continue
		}

		statement, err := migrationFiles.ReadFile(file)
		if err != nil {
			return err
		}
		if _, err := tx.ExecContext(ctx, string(statement)); err != nil {
			return fmt.Errorf("apply migration %s: %w", version, err)
		}
		if _, err := tx.ExecContext(ctx, `INSERT INTO common.schema_migrations (version) VALUES ($1)`, version); err != nil {
			return err
		}
	}

	return tx.Commit()
}

func migrationApplied(ctx context.Context, tx *sql.Tx, version string) (bool, error) {
	var applied bool
	err := tx.QueryRowContext(ctx, `SELECT EXISTS (SELECT 1 FROM common.schema_migrations WHERE version = $1)`, version).Scan(&applied)
	return applied, err
}
