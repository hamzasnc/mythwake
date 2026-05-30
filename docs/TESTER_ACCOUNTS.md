# Mythwake Tester Accounts

Last updated: 2026-05-31

## Current Stand

Mythwake currently has two state modes:

- Local Mode stores a single local prototype save on the device through PlayerPrefs. The main save is a versioned JSON blob at `Mythwake.Prototype.SaveJson`; older scalar PlayerPrefs keys are migrated on load.
- Server Mode stores gameplay state under the backend `player_id`. The Unity client caches the Bearer session token, `player_id`, state revision, and definition cache in PlayerPrefs, then reuses `/client/bootstrap` when the Account Start screen continues a cached server session.

Guest auth already creates a real backend player and a random session token. With PostgreSQL enabled, the server stores only the session token hash and persists player state in normalized `player.*` tables plus the snapshot/action-result recovery path. With no database, Guest auth is only runtime memory and is not durable across API restarts.

Email + Password auth now exists on the backend and is exposed in two Unity places:

- The runtime Account Start screen shown before Home.
- The Shop -> Backend panel for smoke/dev testing.

- `POST /auth/email/register`
- `POST /auth/email/login`
- Email field
- Password field
- Startscreen `Continue`
- Startscreen `Play as Guest`
- Startscreen `Email Login`
- Startscreen `Register`
- `Register`
- `Login`
- `Logout`

Both endpoints issue the same Bearer session shape as Guest auth. Passwords are stored as salted PBKDF2-SHA256 hashes in `account.player_email_credentials`; raw passwords are never stored. After Register/Login, Unity caches the session, marks it as an Email Account, enables Server Mode, calls `/client/bootstrap`, and applies the returned player snapshot. The Account Start screen's preferred tester order is now `Continue`, `Login with Email`, `Create Account`, then `Play as Guest`, so testers are nudged toward durable Email progress before Guest smoke. If no server session exists but a local save exists, `Continue` opens the local save without touching server progress. Logout clears the cached session, turns Server Mode off, and returns to the Account Start screen, but it does not delete the account's server progress.

Email auth errors are structured for tester-facing UI: missing email, invalid email, missing password, weak password, duplicate email, and invalid credentials are separate backend error codes. Unknown email and wrong password intentionally share `invalid_credentials` so the API does not leak which accounts exist.

The Account Start screen maps those backend errors into tester-readable copy: duplicate Email points testers to Login, wrong password/unknown Email asks them to check both fields, backend/network failures explain that Email progress needs the server, and local validation catches invalid Email or passwords shorter than 8 characters before sending a request.

Android local tester builds point at `http://127.0.0.1:8080`; run `adb reverse tcp:8080 tcp:8080` before MuMuPlayer/emulator/USB account tests against the Windows backend. Backend requests also have a client-side timeout guard so a missing server returns a readable message instead of leaving the Start screen in a loading state.

Starting with Prototype `0.2.170`, Android tester builds use package `com.xmiepsen.mythwake`, Version Name `0.2.170`, and Version Code `2170`. Older local saves from APKs with Unity's previous default package identity may not appear after installing this package, because Android treats it as a different app. Email Login can still recover server progress when the backend uses PostgreSQL.

`scripts/check-postgres-e2e.cmd` now uses a unique Email account for the durable PostgreSQL path: Register, protected state, gameplay progress, flush, backend restart, same-session state reload, Logout, Email Login again, and `/client/bootstrap` restoring the same `player_id` and progress. The same smoke also checks Guest auth still reaches and revokes a protected backend player.

Google Login is intentionally not implemented in this slice. It should be added later through the Play Store / Google Play Services provider flow once the Email + Password path and account-linking rules are stable.

## Why A Tester Can Start At Zero

A tester can appear to restart from zero in these cases:

- They reinstall the app or clear app data while only using Local Mode. Android removes the PlayerPrefs save.
- They press the local prototype reset path. This deletes known local prototype save keys and writes a fresh save.
- They use Server Mode as Guest, then lose the cached backend session token before registering or logging into Email. A new Guest login creates a new `player_id`.
- The backend runs without PostgreSQL. In that mode, sessions and state are memory-only and disappear when the API restarts.
- A dev-only backend reset is used. `/dev/player/reset` intentionally wipes the active server player progression while keeping the account/session for local smoke tests.
- They switch between Local Mode and Server Mode without realizing these are different state sources.

Normal app restarts should not reset progress when the same PlayerPrefs save or the same cached backend session is present.

## Target Model

Short term:

- Keep Guest Session persistence from resetting unnecessarily.
- Make Local Mode, Server Mode, Guest Session, Email Account, Player ID, and reset/logout actions visible enough for testers on the Account Start screen and Backend panel.
- Keep reset/dev reset actions deliberate and clearly named.

Medium term:

- Let a tester log back into the same backend `player_id` on a new install or after clearing local app data.
- Keep Guest auth working for smoke tests and fast local development.
- Decide whether existing Guest progress can be linked into Email, or whether Email accounts should be created before meaningful Server Mode testing.

Later:

- Add Google Login through Play Store / Google Play Services.
- Link provider identities to the same backend player where product rules allow it.
- Add Apple login for iOS when that platform path becomes relevant.

## Multi-Tester Convention

For the next small tester build, use one Email account per tester or per device. Example pattern:

- `mythwake+tester01@example.com`
- `mythwake+tester02@example.com`
- `mythwake+redmagic01@example.com`

Each Email account should map to one backend `player_id`. A tester who changes device, reinstalls, or clears app data should use `Login with Email` to recover the same server progress. Feedback should include the visible Prototype version, Local/Server mode, session kind, and Player ID from Management -> Account or the Account Start screen.

Do not ask testers to use personal production passwords. Current password rule is only "8 or more characters"; password reset and email verification are not implemented yet.

## Current Storage Map

Client PlayerPrefs:

- `Mythwake.Prototype.SaveJson`: local gameplay save.
- `Mythwake.Prototype.SaveVersion`: local save schema marker.
- `Mythwake.Backend.SessionToken`: cached backend Bearer session.
- `Mythwake.Backend.PlayerId`: cached backend player ID.
- `Mythwake.Backend.AccountKind`: cached `guest` or `email` label for safer UI/session behavior.
- `Mythwake.Backend.StateRevision`: last known backend state revision.
- `Mythwake.Backend.Definitions.*`: cached server definition snapshot metadata/body.
- `Mythwake.Backend.GameplayEnabled`: Local/Server Mode preference.

Backend PostgreSQL:

- `account.players`: durable player/account row.
- `account.player_auth_identities`: provider identities for guest, email, Google, and Apple.
- `account.player_sessions`: hashed session tokens and revocation/expiry data.
- `account.player_email_credentials`: normalized email and password hash for Email + Password auth.
- `player.*`: normalized gameplay state, revisions, action results, AFK, daily progress, village, gear, heroes, and currencies.
- `logs.*`: economy/action history.
- `debug.*`: Navicat-friendly account and persistence views.

## Tester Rules To Avoid Data Loss

- For local-only APK feedback, do not clear app data unless a fresh run is intended.
- For Server Mode feedback, keep the backend on PostgreSQL and use Email Register/Login when progress must survive app-data clears or installs on another device.
- On the Account Start screen, use `Continue` when a cached Email/Guest session or local save should be preserved.
- `Play as Guest` creates or refreshes a Guest backend session when the backend is reachable; if it cannot reach the backend, the client starts local guest mode instead.
- Logout clears only the cached session. Login again with the same Email + Password to restore the same server `player_id`.
- Treat `Dev Reset` as a destructive server-player reset for the active tester account.
- If a tester reports "I started at zero", capture whether they were in Local or Server Mode, whether status showed Guest or Email Account, the visible Player ID, whether app data was cleared, and whether the backend was restarted without PostgreSQL.

## Open Risks

- The Account Start screen is still an MVP runtime flow, not a polished final login screen.
- Existing Guest accounts are not automatically linked to email accounts.
- Password reset, email verification, account deletion, and provider-link conflict rules are not implemented.
- Google Login needs a platform-provider token validation design and should not be faked client-side.
- No production secret/abuse hardening pass has happened beyond current session hashing, password hashing, and rate-limit plumbing.

## Next Technical Step

- Use APK `Builds\Android\Mythwake-0.2.170-tester-release.apk` plus `docs\TESTER_RELEASE_PROCESS.md` / `docs\TESTER_BUILD_NOTES.md` for the next Account Start smoke.
- Exercise Startscreen Register -> Server Mode progress -> app restart -> cached session Continue -> Logout -> Login -> same Player ID/progress against a PostgreSQL-backed API.
- Exercise `Play as Guest` from the Account Start screen and confirm Guest auth still reaches a server `player_id`.
- Decide Guest-to-Email linking behavior before asking testers to make meaningful progress as Guest.
- Keep Google Play login for a later provider-token validation slice.
