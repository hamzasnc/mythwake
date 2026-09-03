# Mythwake Internal Testbuild Checklist

Last updated: 2026-09-03

Use this checklist for small 20-30 minute internal feedback runs. The goal is not final balance yet; testers should be able to understand the early loop, notice what feels rewarding, and report where the prototype feels confusing or stuck.

## Fresh Start

- Start from a fresh local save or reset the local/dev player.
- Confirm the app opens to Home and the version label is visible.
- Read the Home `Next Goal` hint before tapping anything.
- Open the current Stage Detail from the Home map and check whether the status, power, reward, and Battle action are understandable.

## Core Loop

- Enter Formation from the current campaign stage.
- Check that deployed heroes are visible, slots are tappable, and `Begin Battle` starts one fight.
- Finish the first campaign fight and press `Continue`.
- Confirm the result reward and the new `Next:` line make the next action clear.
- Repeat early campaign stages until a power/resource gap appears.
- Try `Auto Battle` once and stop when it reaches a wall or feels unclear.

## Upgrade Loop

- Open Heroes and level a starter hero if Myth Essence is available.
- Open Gear and inspect Weapon, Armor, and accessory actions.
- Run Gold Dungeon, Essence Dungeon, and Gear Dungeon floor 1.
- Equip the first Gear Dungeon accessory drop and check whether Team Power visibly changes.
- Return to Home and verify the `Next Goal` changes after upgrades.

## Tower Server Slice

- Log in with an Email account, enable Server Mode, and open Dungeons → Tower Dungeon.
- Confirm the card and floor detail show `Highest Unlocked F1`, `Highest Cleared 0`, boss/stat preview, and an enabled `Start F1` action.
- Enter Formation, start the floor, watch the visible server combat result, and confirm the next unlocked floor is shown after Continue.
- Retry the cleared floor with a new idempotency key and confirm the server rejects it without another reward.
- Retry the same floor request with the original idempotency key and confirm the response is a replay.
- Restart the app, use the same Email account, and confirm Tower progress returns from the server snapshot.

## Village And Rewards

- Open Village, pick an empty plot, and build one early building when affordable.
- Open the built building detail and check whether current/next bonuses are understandable.
- Open Fast Rewards and check stored time, reward estimate, Village bonus copy, and Claim/Redeem state.

## Summon

- Open Summon and inspect banner, costs, rates, carousel, and summon count.
- Spend the starter one-pull if Gems are available.
- In the result popup, verify the drawn hero, shard count, shard hint, repeat buttons, Auto-Summon checkbox, disabled states, and Close button.
- Return to Home and check whether the next goal still makes sense after spending Gems.

## Feedback Questions

- What was your first goal, in your own words?
- Where did you first feel blocked or unsure what to do next?
- Which reward felt useful: campaign Essence, dungeon resources, Gear drops, Village bonus, or Summon shards?
- Did `Continue`, `Begin Battle`, `Auto Battle`, and `Close` react immediately?
- Did any text feel too small, cut off, or hidden behind another UI element?
- Did any screen feel too dense for phone portrait?

## Build 0.1 Focus Addendum

- On Formation, confirm the enemy power/stage area is readable without the FPS overlay covering it.
- On Dungeons, read the flow hint and check whether Gold, Essence, and Gear routes are understandable.
- On Hero Detail and Gear, inspect empty slots, equipped items, missing copies, and fuse requirements; disabled buttons should explain why they cannot be used.
- On Home, check whether the current-stage preview's `Ziel:` line makes the next route clearer.
- Keep watching Result `Continue`, auto-next, and longer fights for delayed or surprising behavior.

## Future Account Need

- Internal testers will soon need durable accounts so repeated builds do not force everyone back to zero.
- Planned first slice: Email + Password registration/login.
- Later slice: Google Login through Play Store / Google Play Services.
- The current prototype should not build the full login system yet; keep documenting test-save pain points so the account slice can target real tester needs.
