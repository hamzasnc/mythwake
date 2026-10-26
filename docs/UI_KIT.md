# UI kit

Painted UI in the style of 7DS Idle Adventure / AFK Arena: hand-painted icons with a dark outline and cel shading,
gold-trimmed frames, painted screen backgrounds. All pieces are Codex art made from the game's own item icons as
style reference, so they match the gear and Relic icons.

## Files

`Assets/Mythwake/Content/UI/`

| Folder | What | Used by |
|---|---|---|
| `Icons/` | currencies, navigation, menu modes, controls (back, close, info …), Fragment Aspects, roles, stats, statuses, factions, races – 256 px | `Mythwake.uss` (`.icon--<currency>`, `.nav__icon--<tab>`, `.icon-btn--<name>`, `.aspect-dot.aspect--<aspect>`, …) |
| `Chrome/` | 9-slice buttons, panels, popup, plate, hero card frames per grade, bars, chip, ribbon, badge, nav/top bar | `Mythwake.uss` (`.btn`, `.panel`, `.popup`, `.hero-card__frame`, …) |
| `Backgrounds/` | painted screen backgrounds (summon, heroes, shop, guild, neutral menu, splash) | `.screen--solid` and `.screen--<name>`, `.boot` |

Stylesheets reference the files directly (`url("project://database/…")`), so a new picture with the same name replaces
the old one without code changes.

## Pipeline

1. `Tools/ContentPipeline/ui/make_ui_jobs.py <dir> <style reference>` writes the Codex jobs: icon sheets with 12
   motifs in a fixed grid (one picture = one consistent style), frame sheets, backgrounds. `cells.json` per job lists
   the grid and the file name of every cell.
2. `run_jobs.sh` runs them with Codex (8 in parallel).
3. `Tools/ContentPipeline/ui/import_ui.py <dir>` cuts out the white background, assigns every painted blob to its grid
   cell, clears white enclosed by frames (card frames get a transparent window) and writes `_import_check.png` for
   review.

## Rules for new UI art

- Same style block (`STYLE` in `make_ui_jobs.py`) and the same style reference for every sheet.
- No text in pictures: labels stay UI text (localized).
- Frames: decorated corners only; straight edges uniform so 9-slicing can stretch them.
