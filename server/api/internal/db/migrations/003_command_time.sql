-- Time of the player's last command: client timestamps must never go backwards.
alter table players add column last_command_ms bigint not null default 0;
