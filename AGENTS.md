# Mythwake v2

Mobile idle RPG in Unity 6 (URP, UI Toolkit, spine-unity 4.2), set in the world of Atheris. This branch
(`rebuild/v2`) is a ground-up rebuild; nothing from the v1 code base is referenced or copied.

## Game design – source of truth

See `docs/GAME_DESIGN.md`.

## Rules for lore, names and design

- `docs/GAME_DESIGN.md` is the team's design document (Corex [HS]). Everything about the world, lore, races,
  factions, Fragment Aspects, buildings, systems, bosses and enemies comes from there.
- Do NOT invent new world/lore terms (names of aspects, races, factions, gods, places, currencies, systems).
  Use only what the doc names. If something is missing (e.g. the doc says "seven Fragment Aspects" but names
  only Ravak, Somnara, Envyra, Gorath, Veloria), say so and ask the team instead of filling the gap.
- Character names for new heroes are fine to propose, but they must fit the races and factions of the doc.
- When writing prompts, docs or in-game text, use the doc's wording and terms (English terms stay English).

## Code

- Architecture and folder layout: `docs/ARCHITECTURE.md`. Combat rules: `docs/COMBAT.md`. Progress and open points: `docs/STATUS.md`.
- Game rules live in `Mythwake.Core` (pure C#, no Unity, deterministic integer math). Unity layers only present
  state and forward input.
- Content is JSON under `Assets/Mythwake/Content`; heroes are `Content/Heroes/<id>/<id>.hero.json` next to their
  Spine export. Unknown JSON fields are load errors.
- Fast test loop outside Unity: `dotnet test Tools/CoreTests`. Run it after every change to Core or content.
