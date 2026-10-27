# UI style – as close to AFK Arena as possible, with our own art

Studied on 27.10.2026 from real AFK Arena screens (campaign, hero detail, formation, hero list, AFK chest, inbox,
bounty board, shop, monthly deals, battle HUD, temple, crystal). The goal is AFK Arena's look and structure; every
picture is still our own (no copied assets, logos or layouts pixel by pixel).

## Materials (only three)

| Material | Use | Colours |
|---|---|---|
| Dark wood | top bar, bottom dock, title plates, frames of popups and cards, back button | #3b2414 / #5a3a20 / #8a5a2b, thin highlight on top edges |
| Parchment | every content surface: popups, list rows, panels | #ecd9b0 → #d9bf8f, faint paper grain, dark brown text #4a2e14 |
| Teal-cyan | the one primary action per screen ("Kampf", "Level Up", "Abholen", "Kaufen") | #47e3f0 → #17a9c9, soft glow, white text with thin outline |

Gold is only a thin edge (1–3 px) and the stars; never a filled surface. Secondary buttons are rounded wood with cream
text ("Abbrechen", "Schließen", "Alle löschen").

## Components

- **Top bar:** slim dark-wood strip. Left: round avatar with a gold ring, name, level, thin XP bar, power (fist icon +
  yellow number). Right: currencies as dark translucent pills (icon left, white number, gold "+" square for gems).
- **Bottom dock:** dark wood, five tiles. Icons are one-tone sepia relief illustrations with a short white outlined
  label; the active tile is lighter wood with a warm glow and slightly larger icon.
- **Back button:** always bottom-left, a small wooden square with a curved arrow. Popups close by tapping outside or
  with "Schließen".
- **Popup:** parchment card with a carved wood frame and small corner ornaments; the title sits on a dark wood plate that
  sticks out at the top centre; section headers are a thin dark band with arrow tips (◄ title ►); buttons at the bottom:
  wood (cancel) left, teal (action) right.
- **List row:** parchment card, item square left, title in dark brown bold, stars or progress (teal bar) below, teal
  action button right; finished rows fade.
- **Item slot:** rounded square, rarity frame (grey/green/blue/purple/gold/red), count bottom right, white with dark
  outline; new items get a red dot.
- **Hero card:** portrait in a rarity frame, level on top ("Lv. 30"), faction gem top-left, stars below; in the hero
  list portraits are tall banners.
- **Filter row:** round faction gems in a dark pill bar.
- **Full screens** (temple, shop, crystal): an illustrated header picture with the title on a wood plate, content on
  parchment below, back button bottom-left.

## Screens

- **Campaign:** the world full-screen; side columns of round icon buttons with small labels (left: quests, events;
  right: bag, mail, friends); chest with a coin pile under the team (AFK rewards); "Kampf" teal at the bottom centre;
  world map button bottom-left.
- **Hero detail:** themed stage background; rarity crest + title + name top centre; skills as round icons left and
  right of the hero; arrows to switch heroes; one band with power and stats (level, HP, ATK, DEF icons + numbers);
  "Level Up" teal with costs above; side buttons bottom-right (Held, Fähigkeiten).
- **Battle:** round buttons top (pause, x2, Auto), hero cards bottom in tall rarity frames with HP and energy bars;
  a ready ultimate glows.
- **Results:** dark illustrated backdrop, glowing VICTORY title, rewards in item slots, quest progress lines, "Tippen
  zum Fortfahren".

## Type

Titles: bold rounded display font (Lilita One), white or gold gradient with a dark outline on art, dark brown on
parchment. Body: Baloo 2, small, high contrast.
