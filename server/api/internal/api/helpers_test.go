package api

import (
	"testing"

	"mythwake/api/internal/rules"
)

var testArena = rules.ArenaConfig{
	StartRating: 1000, SeasonDays: 14, ResetHourUtc: 4,
	Ranks: []rules.ArenaRank{{Name: "Bronze", MinRating: 0}, {Name: "Silver", MinRating: 1100}, {Name: "Shattered King", MinRating: 2000}},
}

func TestRankOf(t *testing.T) {
	for rating, want := range map[int]string{0: "Bronze", 1099: "Bronze", 1100: "Silver", 2500: "Shattered King"} {
		if got := rankOf(testArena, rating); got != want {
			t.Errorf("rating %d: %q, want %q", rating, got, want)
		}
	}
}

func TestDayIndexUsesTheResetHour(t *testing.T) {
	const day = int64(20_000) * dayMs
	if dayIndex(testArena, day+3*3_600_000) != dayIndex(testArena, day-1) {
		t.Error("03:00 UTC still belongs to the previous day")
	}
	if dayIndex(testArena, day+4*3_600_000) != dayIndex(testArena, day-1)+1 {
		t.Error("04:00 UTC starts a new day")
	}
}

func TestRateLimiter(t *testing.T) {
	l := newRateLimiter(1, 3)
	for i := 0; i < 3; i++ {
		if !l.allow("a") {
			t.Fatalf("request %d within the burst rejected", i)
		}
	}
	if l.allow("a") {
		t.Fatal("burst exceeded but allowed")
	}
	if !l.allow("b") {
		t.Fatal("buckets must be per account")
	}
}

func TestHighestClearedReadsTheSave(t *testing.T) {
	if got := highestCleared(`{"version":1,"campaign":{"highestCleared":17}}`); got != 17 {
		t.Fatalf("got %d", got)
	}
}

func TestNewUUIDIsV4(t *testing.T) {
	id := newUUID()
	if !uuidPattern.MatchString(id) || id[14] != '4' {
		t.Fatalf("not a v4 uuid: %s", id)
	}
}

func TestStampCommandsKeepsClientTimeInsideTheWindow(t *testing.T) {
	const now = 1_000_000_000
	cmds := []rules.Command{{AtMs: now - 3_000}, {AtMs: now - 60_000}, {AtMs: now + 60_000}, {AtMs: now - 5_000}}
	last := stampCommands(cmds, now, 0)
	want := []int64{now - 3_000, now, now, now}
	for i, c := range cmds {
		if c.AtMs != want[i] {
			t.Errorf("command %d: %d, want %d", i, c.AtMs, want[i])
		}
	}
	if last != now {
		t.Errorf("last %d", last)
	}
	if got := stampCommands([]rules.Command{{AtMs: now - 1}}, now, now+500); got != now+500 {
		t.Errorf("time must not go backwards: %d", got)
	}
}

func TestSeasonsCountFromTheEpoch(t *testing.T) {
	c := rules.ArenaConfig{SeasonDays: 14, SeasonEpochMs: 1_767_571_200_000}
	s := &Server{}
	if got := s.season(c, c.SeasonEpochMs); got != 1 {
		t.Fatalf("first day: season %d", got)
	}
	end := s.seasonEndMs(c, 1)
	if s.season(c, end-1) != 1 || s.season(c, end) != 2 {
		t.Fatal("season 2 starts exactly at the end of season 1")
	}
}
