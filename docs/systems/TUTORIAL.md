# Intro and tutorial – concept

Source: design doc, "Lore sneak-peak" (the story of the intro) and "Quest System" (Main / Village Quests guide the
player). The doc has no tutorial section; this is the minimal first-time guidance an AFK Arena–style idle RPG needs.

## Intro story

- A new player (nothing cleared, intro not seen) opens the game on the intro instead of the campaign hub.
- Five pages, one picture each (`Content/Story/story_1…5.png`, Codex art via
  `Tools/ContentPipeline/backgrounds/make_background_jobs.py <dir> <style> story`). The text is the doc's lore
  sneak-peek in German with the doc's English terms (strings `story.1`–`story.5`):
  1. Atheris is broken, the Guardians vanished, the Fragments scarred the world, the Shattered Realms bleed in.
  2. Dawnhollow, the quiet border village that thought it was safe.
  3. The ground under Mossveil Shrine cracks, a Rift awakens, Dawnhollow falls into chaos.
  4. The Fragment chooses you and becomes the Mythwake Core (stabilizes corruption, awakens heroes, calls Echoes).
  5. Dawn over the ruins: the survivors look to you. "Your village has fallen. The world is remembering."
- Tap moves on, "Überspringen" ends it. The player character is never shown from the front (no defined look).

## Coach marks

A gold ring pulses around one button with a short line of text; the button keeps working as usual and tapping it
finishes the step. Steps follow the player's progress, so anyone past a step (older saves included) never sees it.

| Step | Where | When | Points at |
|---|---|---|---|
| first_fight | Campaign hub | nothing cleared | Kampf |
| formation_fight | Formation (campaign) | nothing cleared | Kampf |
| ultimate | First stage battle | nothing cleared | the ultimate portraits (hint with OK) |
| afk_claim | Campaign hub | stage 1 cleared and idle rewards waiting | Einsammeln |
| rebuild_gate | Campaign hub | Summoning Gate can be rebuilt | Dawnhollow tab |
| gate_plot | Dawnhollow | Summoning Gate can be rebuilt | the Summoning Gate ruin |
| summon | Campaign hub, Dawnhollow | Gate rebuilt, never summoned | Beschwörung tab |
| level_up | Campaign hub | stage 3 cleared, never levelled a hero | Helden tab |

The Main / Village Quest chain (`docs/systems/QUESTS.md`) carries the guidance from there on.

## State

`PlayerState.TutorialDone` lists finished step ids (`CompleteTutorial`, only ids from `Tutorial.Steps`), saved on the
server like everything else, so a second device does not replay the intro.
