-- Guilds (doc: "Guild System") and Guardian Raids (doc: "Guardian Raid System"). See docs/systems/GUILDS.md.
create table guilds (
    id          uuid primary key,
    name        text not null,
    name_key    text not null unique,
    leader      uuid not null references accounts (id),
    exp         bigint not null default 0,
    raid_level  int not null default 1,
    raid_hp     bigint not null,
    created_at  timestamptz not null default now()
);

create table guild_members (
    account_id   uuid primary key references accounts (id) on delete cascade,
    guild_id     uuid not null references guilds (id) on delete cascade,
    name         text not null,
    role         text not null default 'member',
    joined_at    timestamptz not null default now(),
    donate_day   bigint not null default -1,
    raid_day     bigint not null default -1,
    raid_used    int not null default 0,
    raid_damage  bigint not null default 0,
    -- raid kill rewards earned while away; paid on the member's next guild visit
    pending      json not null default '[]'
);
create index guild_members_guild on guild_members (guild_id);

create table guild_messages (
    id          bigserial primary key,
    guild_id    uuid not null references guilds (id) on delete cascade,
    account_id  uuid not null references accounts (id) on delete cascade,
    name        text not null,
    text        text not null,
    created_at  timestamptz not null default now()
);
create index guild_messages_guild on guild_messages (guild_id, id);
