# Guilds and Guardian Raids – concept

Source: design doc, "Guild System" (guild chat, missions, donations, shop, ranking, shared raid progress, research,
buffs, cooperative events; later a Guild Castle) and "Guardian Raid System" (guilds fight massive corrupted Guardians
over multiple attempts, deal as much damage as possible; rewards by individual and guild damage, boss level,
participation; bosses Celestial Turtle, Dragon of the Sky, Hollowed Leviathan, Six-Legged Worldbearer, Craterborn
Guardian; rewards Guardian Sigils, Legendary Relics, awakening materials, Guild Coins). Numbers are first-pass
balancing (`Content/Data/guilds.json`).

## Where it lives

Guilds are shared between players, so the **server owns them** (tables `guilds`, `guild_members`,
`guild_messages`). The player's save only knows its guild id and its Guild Coins; both change only through
server-only commands (`SetGuild`, `PayGuildCreation`, `DonateToGuild`, `GrantRewards`). Guilds are online only.

## Guild

- **Found** a guild for 300 Gems (name 3–20 characters, unique), or **search** and **join** one. Up to 20 members,
  +1 per guild level. The founder leads; when the leader leaves, the longest member takes over; an empty guild is
  removed.
- **Chat**: messages up to 200 characters, newest 50 shown, refreshed every 5 s while open.
- **Donations**: once per day, Gold (20 000) or Gems (100) → guild EXP and Guild Coins for the donor. Guild levels
  come from total EXP (600, 1500, 3000, … for level 2, 3, 4, …).
- **Guild Shop** (Guild Coins): Guardian Sigils, Relic materials, Summon Scrolls, Upgrade stones, a weekly Legendary
  gear piece.

## Guardian Raid

- The guild fights the doc's five Guardians in order (then again, stronger). Raid level 1: Celestial Turtle with
  3 000 000 HP; every level +35 % HP and +6 enemy levels.
- Every member has **2 attempts per day**. An attempt is a 60 s fight of the member's formation against the Guardian;
  the server simulates it, the client replays it. All damage dealt counts against the **shared HP** (a Guardian's own
  regeneration does not undo it).
- Per attempt: 30 Guild Coins plus 2 Guild Coins and 500 Gold per 50 000 damage.
- When the Guardian falls, **every member** gets the kill rewards (Guardian Sigils, Gems, Guild Coins, growing with
  the raid level); members who were away get them on their next guild visit. The next Guardian appears.
- Members are listed by their total raid damage (the guild-internal ranking).

## Not yet

Guild missions, research, buffs, cooperative events, guild ranking across guilds, roles beyond leader/member, kicking,
join requests, and the Guild Castle (doc: depends on how much time players should spend in guild management).
