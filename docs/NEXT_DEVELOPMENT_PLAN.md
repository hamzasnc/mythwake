# Mythwake Next Development Plan

Last updated: 2026-09-03

## Objective

Turn the current `0.2.176` prototype and the unfinished generated Bag UI into a clean internal-alpha candidate. Continue autonomously until the remaining uncertainty requires a substantial manual test by the project owner on a real device or emulator.

The target is not more feature breadth. The target is a stable, testable vertical slice with one coherent Bag implementation, a smaller client architecture seam, server-authoritative Tower progression, and a reproducible Android handoff.

## Current Baseline

- Unity client version: `0.2.176`.
- Backend version: `0.2.62`.
- Local save version: `2`.
- Current branch before this plan: `codex/batch-1-stabilize-prototype`.
- Backend tests and `go vet ./...` pass.
- Runtime and Editor C# projects compile with existing serialized-field warnings.
- `git diff --check` passes.
- Unity Current Slice validation is presently blocked by a missing local Unity Editor license, not by a confirmed validation failure.
- No Android device/emulator is currently attached and Docker is not available on `PATH`.
- The working Bag implementation contains two generated asset approaches. Runtime and validation prefer `Assets/Art/UI/BagGenerated`; the older `Assets/_Mythwake/Resources/Mythwake/UI/Bag` and `Assets/_Mythwake/Prefabs/Bag` path remains as a fallback/draft.

## Working Rules

- Keep the project buildable after every commit.
- Prefer small, reviewable commits grouped by one outcome.
- Do not perform a big-bang rewrite of `IdlePrototypeController`.
- Extract one feature seam at a time and preserve behavior through validators/tests.
- Server Mode is authoritative for account-bound progression. Local Mode remains a development/offline fallback and must not silently diverge in player-facing rules.
- Do not add PvP, guilds, monetization, events, more heroes, or unrelated gameplay breadth in this plan.
- Do not rewrite Git history. Introduce repository hygiene for future assets without endangering existing work.
- Do not claim a release/test candidate until the required automated checks pass or an environmental blocker is documented precisely.

## Work Package 1: Finish And Consolidate The Bag

1. Inventory every Bag sprite, prefab, catalog, generator, and runtime fallback reference.
2. Choose `Assets/Art/UI/BagGenerated` plus `BagGeneratedSpriteCatalog` as the canonical generated asset path unless inspection finds a concrete blocker.
3. Remove the unused parallel Bag builder/assets only after proving they have no required references.
4. Ensure generated files have stable Unity `.meta` files and regeneration is deterministic.
5. Keep the visible Bag layout aligned with `Pictures/bag_mockup_reference.png`:
   - coherent header and category tabs;
   - ten-slot grid without ghost/overlapping panels;
   - selected-item detail panel;
   - quantity selection with Use 1, minus, plus, All, and final Use;
   - modal reward presentation with an unambiguous close/OK flow;
   - readable English and German copy;
   - safe mobile touch targets and no click-through.
6. Verify local and Server Mode Hero Shard Chest usage, inventory refresh, reward application, and error states.
7. Extend validators only where they protect player-visible behavior or asset integrity.
8. Bump the prototype/build version when the Bag slice becomes a handoff candidate and update the relevant release/status docs.

Done when:

- exactly one Bag asset pipeline remains;
- Runtime and Editor C# compile;
- `git diff --check` passes;
- Bag validation passes when Unity licensing is available;
- an APK can be produced, or the exact environment blocker is recorded;
- the Bag has a focused manual-test checklist.

## Work Package 2: Establish A Maintainable Client Seam

Use the Bag as the first extraction from the monolithic `IdlePrototypeController`.

1. Separate pure inventory view data/filtering/quantity rules from Unity object creation.
2. Introduce focused components such as an inventory presenter/controller and a Bag view binding without changing unrelated screens.
3. Move reusable UI creation/binding into dedicated Bag code or prefabs rather than adding more Bag-specific methods to the main controller.
4. Keep persistence and backend actions behind the existing service boundaries.
5. Add EditMode tests for pure inventory rules where practical.
6. Add assembly definitions only if they can be introduced without destabilizing Spine, TextMeshPro, or existing editor tooling.
7. Reduce or eliminate newly exposed compiler warnings in touched code. Do not spend the batch cleaning every historical warning.

Done when:

- the main controller no longer owns the majority of Bag layout and interaction details;
- inventory filtering and quantity rules can be tested without constructing the whole runtime screen;
- existing Hero Progression and Current Slice behavior remains compatible.

## Work Package 3: Improve Repository And Test Hygiene

1. Add a lightweight CI workflow for `go test ./...`, `go vet ./...`, and whitespace validation.
2. Add the Unity C# compile check to CI only if the required Unity/.NET references can be made reliable; otherwise document it as a local required check.
3. Configure Git LFS for future large binary art types if the environment supports it, without rewriting existing history. If LFS cannot be safely enabled now, document the exact follow-up.
4. Stop committing redundant contact sheets, raw variants, and runtime copies without an explicit source/runtime policy.
5. Keep APK/AAB artifacts ignored.
6. Refresh `CURRENT_STATUS.md`, `NEXT_CHAT_CONTEXT.md`, and tester notes so they agree with the code.

Done when:

- pushes receive an automatic backend-quality signal;
- future binary-asset policy is explicit;
- documentation names one current candidate and one known-good packaged fallback.

## Work Package 4: Make Tower Progression Server-Authoritative

1. Add stable Tower definition rows and PostgreSQL migration(s).
2. Persist highest unlocked, highest cleared, selected/relevant floor state, and any needed run metadata per player.
3. Move reward and enemy scaling to the active backend definition/catalog boundary.
4. Add authenticated, idempotent Tower action(s) with replay-safe results and action-ledger coverage.
5. Expose Tower state and definitions through bootstrap/snapshot contracts.
6. Enable the Tower UI in Server Mode and remove the local-only block once parity is proven.
7. Keep local formulas aligned as an explicit fallback or consume the shared snapshot definitions.
8. Add service/router tests and PostgreSQL restart/re-login E2E coverage.
9. Verify that failed/manual-ended Tower runs cannot duplicate or lose accepted rewards.

Done when:

- an Email account retains Tower progress through flush, API restart, logout, and login;
- duplicate request replay cannot duplicate rewards;
- Unity previews match authoritative server definitions;
- Local and Server Mode communicate their state source clearly.

## Work Package 5: Prepare The Large Manual Test Candidate

1. Run all locally available automated checks.
2. Produce a new versioned APK and, if signing/tooling permits, an AAB.
3. Update build notes with exact versions, known issues, and test-account instructions.
4. Prepare one concise manual test route covering:
   - fresh install and Account Start;
   - Email registration/login and Guest fallback;
   - Campaign Formation/Fight/Result;
   - Bag categories and Hero Shard Chest quantity/reward flow;
   - Hero Star/Awakening progression;
   - Shard Rift reward retention;
   - Tower progression in Server Mode;
   - Village/Fast Rewards;
   - app restart, cached Continue, Logout/Login, and same Player ID/progress;
   - German and English UI spot checks;
   - tall-phone safe area and visible touch targets;
   - crash/ANR/error-log inspection.
5. Stop autonomous implementation when this candidate is ready and report exactly what the owner must test, how to run it, and what evidence to return.

## Required Commit Checkpoints

Prefer these checkpoints, adjusted only when the code makes a different split safer:

1. Consolidate generated Bag assets.
2. Stabilize Bag interaction and localization.
3. Extract Bag presentation from the main controller.
4. Add test/repository hygiene.
5. Add backend Tower definitions and persistence.
6. Integrate Unity Server Mode Tower.
7. Prepare the internal-alpha test candidate and documentation.

Push completed checkpoints to a `codex/` branch. Do not merge to `main` or publish externally without an explicit user request.

## Manual-Test Handoff Threshold

The next substantial user test is justified when all implementation work that can be verified automatically is complete and the remaining questions are genuinely experiential or device-specific: visual quality, touch behavior, safe areas, first-hour pacing, account recovery on a real installation, and overall comprehension.
