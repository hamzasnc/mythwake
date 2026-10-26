# Mythwake v2 – Combat rules

The rules as `Mythwake.Core.Combat.BattleSimulator` implements them. Hero kits live in
`Assets/Mythwake/Content/Heroes/<id>/<id>.hero.json`. All percentages are stored as **permille** (1000 = 100 %).

## Time and determinism

- Fixed 50 ms steps, battles end after 90 s (timeout = defeat).
- Integer math only; one seeded PCG32 per battle. Same `BattleSetup` → identical event log on every platform.
- Each step: due hits land → statuses tick (DoT, regen, expiry, periodic passives) → every living, non-busy,
  non-disabled unit picks an action (in unit order: allies by slot, then enemies by slot).

## Formation

5 slots per side: 0–1 front row, 2–4 back row (doc: 2 frontline, 3 backline). How many slots a player has unlocked is
a progression rule, not a combat rule.

## Actions

Priority each time a unit is free:

1. **Battle start** skills (once, at the first free moment; continue on their cooldown if they have one).
2. **Ultimate** when energy is full. Enemies and allies with **Auto** on cast at once; with Auto off an ally waits for
   the player's tap (`UltimateRequest`).
3. **Special Skills** whose trigger is ready: cooldown, own HP below X, an ally below X, an enemy below X.
4. **Normal Skill** on the attack interval. Chance-on-attack and every-Nth-attack specials replace the Normal Skill
   when they proc.

A cast picks its targets, makes the caster busy for the action length and schedules each hit at its hit time.
Hit times come from the Spine `hit` events, so damage lands on the frame the animation shows. Stun, sleep and
interrupts cancel a running non-ultimate action; ultimates cannot be interrupted.

Attack interval = base interval × 1000 / (1000 + attack speed), at least 400 ms. Attack speed = AttackSpeedUp − Slow
+ passives, clamped to −600…+2000.

## Energy

0–1000 per hero. +100 per cast (scaled by Energy Regen), +40 per hit taken (not from damage over time), +150 per kill.
The ultimate empties the bar.

## Damage

```
base     = ATK × multiplier × hit portion          (or caster / target max HP for HP-scaled effects)
bonus    = execute + bonus vs status + missing-HP bonus
DEF      = 1000 / (1000 + DEF × (1 + DefenseUp − DefenseDown))
crit     = 1 + CritDamage − CritDefense             (base CritDamage 50 %, no cap)
modifier = aspect (+25 % / −15 %) + DamageTakenUp − DamageTakenDown − damage reduction passives (floor 10 %)
variance = 95 … 105 %
```

- **Hit chance** = accuracy / evasion, clamped 20–100 %. Aspect advantage adds +15 % accuracy, AccuracyDown subtracts.
  Buffs, heals and shields never miss.
- **Crit chance** = CritRate + CritRateUp + passives − CritResistance.
- **Multi-hit**: the last hit carries 40 %, the others share 60 %. Non-damage effects apply once (first hit).
- Order on the target: Evade charges → hit roll → dodge passive → shield → HP. Immune takes no damage and no debuffs.
- Sleep breaks on damage.

### Fragment Aspects

Ravak → Gorath → Somnara → Veloria → Envyra → Ravak (each beats the next). Advantage: +25 % damage, +15 % accuracy.
Disadvantage: −15 % damage. The doc mentions seven Aspects; the team decided to stay at these five (docs/DECISIONS.md).

## Targeting

| Selector | Picks |
|---|---|
| enemyFront / enemyBack | front-most unit of that row, else the other row (`targetCount` > 1 continues in that order) |
| enemyLowestHp / enemyHighestAttack / enemyRandom | by HP ratio / attack / random, `targetCount` units |
| enemyFrontRow / enemyBackRow | the whole row, else the other row |
| enemyCircle | the front-most target and its neighbours in the same row |
| enemyAll, allyAll, allyFrontRow, allyBackRow, allyLowestHp, allyHighestEnergy, self | as named |

Single-target enemy selectors obey, in this order: **Confuse** (random unit, any side) → **Provoke** (on the attacker:
hit the status source) → **Taunt** (on a defender: everyone hits it). **Charm** swaps the sides.
Single targets that died before a later hit lands are re-picked.

## Statuses

One instance per kind per unit; reapplying refreshes the duration and keeps the higher value.

| Kind | Value |
|---|---|
| stun, sleep | – (cannot act; sleep breaks on damage) |
| taunt, provoke, charm, confuse | – |
| burn, poison | damage per second, permille of the source's ATK |
| regen | heal per second, permille of max HP |
| attackUp/Down, defenseUp/Down, damageTakenUp/Down, attackSpeedUp, slow, accuracyDown, critRateUp | permille |
| immune | – (no damage, no new debuffs) |
| evade | number of hits dodged |

Debuffs (cleansed by `cleanse`): stun, sleep, provoke, charm, confuse, burn, poison, attackDown, defenseDown,
damageTakenUp, slow, accuracyDown. Everything else is a buff (stolen by `stealBuff`).

## Effects

damage, heal, shield (absorbs before HP, expires), applyStatus, gainEnergy, drainEnergy, cleanse (count, 0 = all),
stealBuff (count), lifesteal (permille of this skill's damage), delayAction (pushes next attack and cooldowns back),
interrupt. Any effect can override the skill's target (`target`) and roll a `chance`.

## Passives

A passive is a list of components. `team: true` makes a component an aura on every ally (it stops when its owner
dies). `belowHp` gates a component on the affected unit's HP. `growth` adds value every `growthIntervalMs` of battle
time, up to `cap`.

damageBonus, attackSpeed, critRate, damageReduction, healingBonus, regen, lifesteal, dodge, stackingAttackSpeed
(stacks on hit, optional regen per stack), onHit (effect on the hit target; `chance`, `requireCrit`, `everyN`
Normal Skills), onHitTaken (effect on the attacker), onKill (effect on the victim), periodic (effect every
`intervalMs`), battleStart (effect once), leaderAura (Hero Leader: team ATK / HP / healing).

## Team building

`TeamBuilder` turns owned heroes into battle stats:

- Level: +8 % of base HP/ATK/DEF per level (`growthPermille`). Stars: +15 % per star above 1.
- Faction synergy (doc: 2 / 3 / 5 heroes): +5 % / +10 % / +15 % ATK and HP.
- Race synergy: +3 % ATK and HP for 2+ heroes of the same race.
- Hero Leader (doc: does not fight): its `leaderAura` applies to the whole team and grows with its level and stars.
  Its Special Skills are **leader abilities**: the leader stands off the field (never targeted, not counted for the
  battle's end) and casts them on their triggers; events use the index after the last enemy. Arena and the Trial of
  Fragments fight without a leader.

## Open points

- Kit mechanics that are approximated are listed per hero in `designNotes` (slot swap, summons, copying skills,
  boss sleep immunity, rank-gated skills).
- Base stats per role and grade are a first pass (`Tools/ContentPipeline/migrate_kits.py`) and need balancing.
