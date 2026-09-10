package database

import (
	"context"
	"database/sql"
	"io/fs"
	"net/url"
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/player"
	"github.com/hamzasnc/mythwake/backend/internal/store/postgres"
)

// Opt-in acceptance against an EMPTY isolated cluster/database. This never
// reads the normal MYTHWAKE_DATABASE_URL or removes an existing database.
func TestKaelPostgresMigrationAndReload(t *testing.T) {
	databaseURL := os.Getenv("MYTHWAKE_KAEL_TEST_DATABASE_URL")
	if databaseURL == "" {
		t.Skip("set MYTHWAKE_KAEL_TEST_DATABASE_URL to an empty isolated loopback database")
	}
	parsed, err := url.Parse(databaseURL)
	if err != nil || parsed.Hostname() != "127.0.0.1" || parsed.Port() != "18434" || !strings.HasPrefix(parsed.Path, "/kael_migration_test_") {
		t.Fatal("refusing migration test outside isolated 127.0.0.1:18434/kael_migration_test_* database")
	}
	ctx, cancel := context.WithTimeout(context.Background(), 45*time.Second)
	defer cancel()
	db, err := Open(ctx, databaseURL)
	if err != nil {
		t.Fatal(err)
	}
	defer db.Close()
	var tableCount int
	if err := db.QueryRowContext(ctx, `SELECT count(*) FROM information_schema.tables WHERE table_schema IN ('account', 'player', 'common')`).Scan(&tableCount); err != nil || tableCount != 0 {
		t.Fatalf("refusing non-empty migration database: tables=%d err=%v", tableCount, err)
	}
	applyBeforeKael(t, ctx, db)
	definitionStore := postgres.NewDefinitionStore(db)
	before, err := definitionStore.Snapshot(ctx, "kael-integration-test")
	if err != nil || len(before.Heroes) != 7 {
		t.Fatalf("pre-Kael definition snapshot failed: heroes=%d err=%v", len(before.Heroes), err)
	}
	stateStore := postgres.NewPlayerStateStore(db)
	const playerID = "kael_migration_existing_player"
	oldManager := player.NewManager(stateStore, player.WithBalanceCatalog(player.NewSnapshotBalanceCatalog(before)))
	if _, err := oldManager.ServiceForPlayer(ctx, playerID); err != nil {
		t.Fatal(err)
	}
	oldState, found, err := stateStore.LoadState(ctx, playerID)
	if err != nil || !found {
		t.Fatalf("old player was not persisted: found=%v err=%v", found, err)
	}
	oldRevision := oldState.Revision
	oldState.Revision++
	oldState.UpdatedAt = time.Now().UTC()
	oldState.HeroLevels["hero_astra"] = 42
	oldState.HeroShards["hero_astra"] = 27
	oldState.HeroAscensions["hero_astra"] = 3
	oldState.HeroStars["hero_astra"] = 2
	oldState.PlayerState.Gold = 4321
	oldState.PlayerState.CampaignStage = 32
	if err := stateStore.SaveState(ctx, playerID, oldState, player.StateSaveSource{ActionID: "player_state_flush", ExpectedRevision: oldRevision}); err != nil {
		t.Fatal(err)
	}
	if err := Migrate(ctx, db); err != nil {
		t.Fatalf("migration 0034 failed: %v", err)
	}
	after, err := definitionStore.Snapshot(ctx, "kael-integration-test")
	if err != nil || len(after.Heroes) != 8 {
		t.Fatalf("post-Kael definition snapshot failed: heroes=%d err=%v", len(after.Heroes), err)
	}
	for index, previous := range before.Heroes {
		if !reflect.DeepEqual(previous, after.Heroes[index]) {
			t.Fatalf("migration changed existing hero definition %s", previous.HeroID)
		}
	}
	kael := after.Heroes[7]
	if kael.HeroID != "hero_kael" || !kael.StarterOwned || kael.SortOrder != 80 || kael.BaseAttack != 18 || kael.BaseHealth != 150 || kael.AttackPerLevel != 5 || kael.HealthPerLevel != 28 || kael.AttackPerAscension != 11 || kael.HealthPerAscension != 70 || kael.MaxLevel != 100 || kael.MaxAscension != 10 {
		t.Fatalf("SQL Kael definition differs from agreed balance: %#v", kael)
	}
	var shards, rotation int
	if err := db.QueryRowContext(ctx, `SELECT shard_amount, rotation_order FROM common.summon_pool_definitions WHERE banner_id='hero_shard_standard' AND hero_id='hero_kael'`).Scan(&shards, &rotation); err != nil || shards != 1 || rotation != 80 {
		t.Fatalf("SQL Kael shard pool is wrong: shards=%d rotation=%d err=%v", shards, rotation, err)
	}
	// Check both the actual migration runner's ledger replay and the additive
	// SQL itself; neither may duplicate rows or replace existing content.
	if err := Migrate(ctx, db); err != nil {
		t.Fatalf("repeat migration failed: %v", err)
	}
	kaelSQL, err := migrationFiles.ReadFile("migrations/0034_kael_hero_definition.sql")
	if err != nil {
		t.Fatal(err)
	}
	if _, err := db.ExecContext(ctx, string(kaelSQL)); err != nil {
		t.Fatalf("repeat additive SQL failed: %v", err)
	}
	repeated, err := definitionStore.Snapshot(ctx, "kael-integration-test")
	if err != nil || !reflect.DeepEqual(after, repeated) {
		t.Fatalf("repeated migration changed definition snapshot: %v", err)
	}
	var migrationCount int
	if err := db.QueryRowContext(ctx, `SELECT count(*) FROM common.schema_migrations`).Scan(&migrationCount); err != nil || migrationCount != 34 {
		t.Fatalf("expected all 34 migrations: count=%d err=%v", migrationCount, err)
	}
	manager := player.NewManager(stateStore, player.WithBalanceCatalog(player.NewSnapshotBalanceCatalog(after)))
	service, err := manager.ServiceForPlayer(ctx, playerID)
	if err != nil {
		t.Fatal(err)
	}
	snapshot := service.GetSnapshot()
	assertKaelReloadProgress(t, snapshot, 1)
	level := service.LevelHero("hero_kael")
	if !level.Success {
		t.Fatalf("starter Kael could not level through normal persistence: %#v", level)
	}
	if err := db.Close(); err != nil {
		t.Fatal(err)
	}
	reopened, err := Open(ctx, databaseURL)
	if err != nil {
		t.Fatal(err)
	}
	defer reopened.Close()
	newManager := player.NewManager(postgres.NewPlayerStateStore(reopened), player.WithBalanceCatalog(player.NewSnapshotBalanceCatalog(after)))
	reloaded, err := newManager.ServiceForPlayer(ctx, playerID)
	if err != nil {
		t.Fatal(err)
	}
	assertKaelReloadProgress(t, reloaded.GetSnapshot(), 2)
	t.Log("PASS: PostgreSQL 18 applied 0001-0034; Kael/shard pool exact; repeated migration unchanged; old progress preserved; Kael level persisted across connection and service reload")
}

func assertKaelReloadProgress(t *testing.T, snapshot api.PlayerSnapshot, kaelLevel int) {
	t.Helper()
	heroes := make(map[string]api.HeroState, len(snapshot.Heroes))
	for _, hero := range snapshot.Heroes {
		heroes[hero.HeroID] = hero
	}
	if len(heroes) != 8 || heroes["hero_kael"].Level != kaelLevel || heroes["hero_astra"].Level != 42 || heroes["hero_astra"].Ascension != 3 || heroes["hero_astra"].StarLevel != 2 || snapshot.State.Gold != 4321 || snapshot.State.CampaignStage != 32 {
		t.Fatalf("SQL migration/reload lost existing or Kael progress: %#v", snapshot)
	}
	for _, shards := range snapshot.HeroShards {
		if shards.HeroID == "hero_astra" && shards.Shards == 27 {
			return
		}
	}
	t.Fatal("SQL migration/reload lost existing hero shards")
}

func applyBeforeKael(t *testing.T, ctx context.Context, db *sql.DB) {
	t.Helper()
	tx, err := db.BeginTx(ctx, nil)
	if err != nil {
		t.Fatal(err)
	}
	defer tx.Rollback()
	if _, err := tx.ExecContext(ctx, `CREATE SCHEMA common; CREATE TABLE common.schema_migrations (version text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())`); err != nil {
		t.Fatal(err)
	}
	files, err := fs.Glob(migrationFiles, "migrations/*.sql")
	if err != nil {
		t.Fatal(err)
	}
	for _, name := range files {
		if filepath.Base(name) >= "0034_" {
			continue
		}
		source, err := migrationFiles.ReadFile(name)
		if err != nil {
			t.Fatal(err)
		}
		if _, err := tx.ExecContext(ctx, string(source)); err != nil {
			t.Fatalf("apply %s: %v", name, err)
		}
		if _, err := tx.ExecContext(ctx, `INSERT INTO common.schema_migrations(version) VALUES ($1)`, strings.TrimSuffix(filepath.Base(name), ".sql")); err != nil {
			t.Fatal(err)
		}
	}
	if err := tx.Commit(); err != nil {
		t.Fatal(err)
	}
}
