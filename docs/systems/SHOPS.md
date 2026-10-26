# Shops – concept

Source: design doc, "Shop System" (General, Arena, Guild, Event, Fragment, Village and Hunt Shop; refresh daily,
weekly or after events; some unlocked through buildings or reputation; the cash shop is its own system). Numbers are
first-pass balancing. Data: `Content/Data/shops.json` (the Hunt Shop stays with the Hunt Board, `hunts.json`).

| Shop | Opens | Offers |
|---|---|---|
| General Shop | always | Hero EXP, Gear materials, Fragment Dust for Gold; a Summon Scroll and a weekly Shattered Realm Key for Gems |
| Village Shop | Survivor Camp | Wood and Stone for Gold, a weekly rebuilding bundle for Gems |
| Fragment Shop | Fragment Shrine | Core/Aspect Fragments (Rare) and an Echo Fragment (Epic) weekly, Relic materials – all for Fragment Dust |
| Arena Shop | stage 30 | Hero shards of a Legendary and an Epic hero that change every day, Summon Scrolls and Epic gear – for Arena Coins |
| Hunt Shop | Hunter's Hut | see HUNT_BOARD.md |

- Limits per offer reset daily (04:00 UTC) or weekly (Monday), never stack.
- Not yet: Guild Shop (needs guilds), Event Shop (needs Event Portals), reputation unlocks (needs faction reputation),
  the cash shop (doc: its own system).
- UI: Dawnhollow → **Shop** button next to the town name; one tab per shop.
