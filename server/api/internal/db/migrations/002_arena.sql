-- Arena: one row per player and season-independent defense snapshot. Bots have no device and is_bot = true.
create table arena_players (
    account_id   uuid primary key references accounts (id) on delete cascade,
    name         text not null,
    is_bot       boolean not null default false,
    season       int not null,
    rating       int not null,
    defense      json not null,
    power        bigint not null,
    wins         int not null default 0,
    losses       int not null default 0,
    fights_day   bigint not null default -1,
    fights_used  int not null default 0,
    updated_at   timestamptz not null default now()
);
create index arena_players_rating on arena_players (season, rating);

create table arena_battles (
    id             bigserial primary key,
    season         int not null,
    attacker       uuid not null references accounts (id) on delete cascade,
    defender       uuid not null references accounts (id) on delete cascade,
    seed           bigint not null,
    victory        boolean not null,
    rating_change  int not null,
    created_at     timestamptz not null default now()
);
create index arena_battles_attacker on arena_battles (attacker, created_at desc);
create index arena_battles_defender on arena_battles (defender, created_at desc);
