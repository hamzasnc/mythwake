-- Accounts and device logins.
create table accounts (
    id          uuid primary key,
    created_at  timestamptz not null default now()
);

create table devices (
    device_id    text primary key,
    account_id   uuid not null references accounts (id) on delete cascade,
    secret_hash  text not null,
    created_at   timestamptz not null default now(),
    last_seen_at timestamptz not null default now()
);
create index devices_account on devices (account_id);

-- One saved game per account. `state` is `json` (not jsonb) on purpose: the text must stay byte-identical to what the
-- rules service wrote, because client and server compare SHA-256 hashes of it.
create table players (
    account_id      uuid primary key references accounts (id) on delete cascade,
    display_name    text not null,
    state           json not null,
    state_hash      text not null,
    version         bigint not null,
    content_version text not null,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

-- Every applied command (audit, support, idempotent retries).
create table command_log (
    id          bigserial primary key,
    account_id  uuid not null references accounts (id) on delete cascade,
    command_id  uuid not null,
    version     bigint not null,
    type        text not null,
    data        jsonb not null,
    created_at  timestamptz not null default now(),
    unique (account_id, command_id)
);
