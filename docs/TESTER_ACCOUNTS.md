# Mythwake Tester Accounts

Last updated: 2026-05-30

## Current Stand

Mythwake currently has two state modes:

- Local Mode stores a single local prototype save on the device through PlayerPrefs. The main save is a versioned JSON blob at `Mythwake.Prototype.SaveJson`; older scalar PlayerPrefs keys are migrated on load.
- Server Mode stores gameplay state under the backend `player_id`. The Unity client caches the Bearer session token, `player_id`, state revision, and definition cache in PlayerPrefs, then reuses `/client/bootstrap` on startup.

Guest auth already creates a real backend player and a random session token. With PostgreSQL enabled, the server stores only the session token hash and persists player state in normalized `player.*` tables plus the snapshot/action-result recovery path. With no database, Guest auth is only runtime memory and is not durable across API restarts.

Email + Password auth now exists on the backend and is exposed in the Unity Shop -> Backend panel:

- `POST /auth/email/register`
- `POST /auth/email/login`
- Email field
- Password field
- `Register`
- `Login`
- `Logout`

Both endpoints issue the same Bearer session shape as Guest auth. Passwords are stored as salted PBKDF2-SHA256 hashes in `account.player_email_credentials`; raw passwords are never stored. After Register/Login, Unity caches the session, marks it as an Email Account, enables Server Mode, calls `/client/bootstrap`, and applies the returned player snapshot. Logout clears the cached session and turns Server Mode off, but it does not delete the account's server progress.

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
- Make Local Mode, Server Mode, Guest Session, Email Account, Player ID, and reset/logout actions visible enough for testers.
- Keep reset/dev reset actions deliberate and clearly named.

Medium term:

- Let a tester log back into the same backend `player_id` on a new install or after clearing local app data.
- Keep Guest auth working for smoke tests and fast local development.
- Decide whether existing Guest progress can be linked into Email, or whether Email accounts should be created before meaningful Server Mode testing.

Later:

- Add Google Login through Play Store / Google Play Services.
- Link provider identities to the same backend player where product rules allow it.
- Add Apple login for iOS when that platform path becomes relevant.

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
- Logout clears only the cached session. Login again with the same Email + Password to restore the same server `player_id`.
- Treat `Dev Reset` as a destructive server-player reset for the active tester account.
- If a tester reports "I started at zero", capture whether they were in Local or Server Mode, whether status showed Guest or Email Account, the visible Player ID, whether app data was cleared, and whether the backend was restarted without PostgreSQL.

## Open Risks

- The Unity Email UI is intentionally MVP-only inside the Backend panel, not a polished first-run login screen.
- Existing Guest accounts are not automatically linked to email accounts.
- Password reset, email verification, account deletion, and provider-link conflict rules are not implemented.
- Google Login needs a platform-provider token validation design and should not be faked client-side.
- No production secret/abuse hardening pass has happened beyond current session hashing, password hashing, and rate-limit plumbing.

## Next Technical Step

- Use APK `Builds\Android\Mythwake-0.2.166-email-login.apk` for the first Email UI smoke; it has been built, installed, and cold-launched in MuMuPlayer.
- Exercise Register -> Server Mode progress -> app restart -> cached session bootstrap -> Logout -> Login -> same Player ID/progress against a PostgreSQL-backed API.
- Decide Guest-to-Email linking behavior before asking testers to make meaningful progress as Guest.
- Keep Google Play login for a later provider-token validation slice.
