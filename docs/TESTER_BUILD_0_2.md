# Mythwake Tester Build 0.2

Last updated: 2026-05-30

## Build

- APK target: `Builds/Android/Mythwake-0.2.169-account-tester-build.apk`
- Prototype: `0.2.169`
- Backend: `0.2.60`
- Save version: `2`
- Branch: `codex/batch-1-stabilize-prototype`
- Scope: small internal tester build with multiple Email accounts.

## What This Build Tests

- First-run Account Start screen.
- Email + Password registration and login.
- Cached `Continue` with the same backend session.
- Logout followed by Login with the same Email account.
- Guest flow remains available for quick smoke tests.
- Local saves remain separate from server account progress.

Google Play login is not part of this build. It remains a later Play Store / Google Play Services provider task.

## Install And Start

1. Install the APK on MuMuPlayer or an Android device.
2. Start the app and confirm the visible version reads Prototype `0.2.169`.
3. On the Account Start screen, prefer `Create Account` for a new tester or `Login with Email` for an existing tester.
4. Use `Continue` when the app already shows a cached server account or local save.
5. Use `Play as Guest` only for a short smoke test where durable progress is not important.

For a local Windows backend test, start the API on `localhost:8080` and run this before launching the APK:

```powershell
adb reverse tcp:8080 tcp:8080
```

The Android tester APK points to `http://127.0.0.1:8080`, so `adb reverse` lets MuMuPlayer, Android emulators, and USB devices reach the Windows backend. Without a reachable backend, Email auth should fail with a readable server/network message instead of hanging.

## Account Setup

Recommended tester email convention:

- `mythwake+tester01@example.com`
- `mythwake+tester02@example.com`
- `mythwake+device-redmagic@example.com`

Every Email account owns its own backend `player_id` and server player state. Testers should keep their Email and password for follow-up builds. Clearing app data or reinstalling the APK removes the cached session from the device, but logging in with the same Email restores the same server progress when the backend uses PostgreSQL.

Password rule for this MVP: at least 8 characters. There is no password reset or email verification yet, so do not use real personal passwords.

## Normal Tester Flow

1. Start app with no saved session.
2. Tap `Create Account`.
3. Enter tester Email and 8+ character password.
4. Confirm the game opens in Server Mode.
5. Make visible progress, for example clear one Campaign fight or run one Dungeon.
6. Close and reopen the app.
7. Tap `Continue`.
8. Confirm the same Player ID and progress return.
9. Logout from the Backend/Account controls.
10. Return to Account Start and use `Login with Email`.
11. Confirm the same Player ID and progress return again.

## Guest Flow

`Play as Guest` still creates a backend Guest session when the backend is reachable. If the backend is down, the client falls back to local guest mode. Guest is useful for smoke testing, but testers should not use it for meaningful long-running progress until Guest-to-Email linking rules are designed.

## Reporting Feedback

Every feedback note should include:

- Device or emulator name.
- Android version if known.
- APK/prototype version.
- Account mode shown in Management -> Account: Local or Server.
- Session kind: Email account, Guest session, or none.
- Visible Player ID.
- What button was tapped and what happened.
- Screenshot if UI text overlaps, buttons miss taps, or the wrong account/progress appears.

The Management -> Account panel now shows the build, mode, session, and Player ID specifically so testers can report account problems without digging through logs.

## Backend Down Behavior

When Email auth or `Continue` cannot reach the backend, the app should show a readable server/network message instead of a raw debug-looking failure or an endless loading state. Email progress requires the backend. Guest can fall back to local mode, but local guest progress stays on the device.

## What Not To Test Yet

- Google Play login.
- Password reset.
- Email verification.
- Account deletion.
- Guest-to-Email linking.
- Production abuse/security hardening beyond the current tester MVP.
- Payments, monetization, or Play Store internal-track delivery.

## Known Risks

- Email + Password is a tester MVP, not a final public account system.
- Password recovery does not exist yet.
- Unknown Email and wrong password intentionally share one safe invalid-credentials error.
- Existing Guest progress is not linked to Email accounts.
- Dev Reset intentionally wipes the active server player and should not be used by normal testers.
- Backend must use PostgreSQL for durable multi-tester progress.

## Feedback Questions

- Was it obvious whether to use Continue, Login, Create Account, or Guest?
- Did an Email account keep the same Player ID after restart and logout/login?
- Were wrong password, duplicate Email, backend-down, and validation errors understandable?
- Did the Account/Management status give enough information for a bug report?
- Did any text clip or feel too small on the phone screen?
- Did Logout feel clearly different from Reset?

## Verification Notes

- Unity validators cover the Account Start overlay, Email-first button order, masked password field, friendly local validation errors, EN/DE text fit, Google-later hint, and no Reset button on the main account flow.
- Backend PostgreSQL E2E already covers durable Email register/login, duplicate Email, invalid Email, wrong password, Logout revocation, restart/re-login progress recovery, and Guest auth smoke.
- Client backend requests have an extra Unity-side timeout guard so server-down cases return to the UI with an understandable failure.
- Android/MuMuPlayer smoke passed for APK `Builds/Android/Mythwake-0.2.169-account-tester-build.apk`.

## Android Smoke

- Built APK: `Builds/Android/Mythwake-0.2.169-account-tester-build.apk`.
- Installed in MuMuPlayer with `adb install -r`.
- Local backend path used `adb reverse tcp:8080 tcp:8080` against the Windows PostgreSQL-backed API.
- Startscreen rendered with Prototype `0.2.169`, Email-first order, non-prominent Google Play later hint, and no Reset trap.
- Registered `mw169test001207@example.com`, entered Server Mode, and saw backend player `player_PgHUwAfmphG5XwZusJLf3A`.
- Started a Server Mode Campaign battle, cleared Stage 1-1, and returned to Home at Stage 1-2.
- Force-stopped/restarted the app; Account Start showed the same cached Email Account and Player ID.
- `Continue` loaded the same Stage 1-2 server progress.
- `Logout` returned to Account Start and cleared only the cached session.
- Wrong password showed `Email or password is wrong. Check both fields and try again.`
- Duplicate Email showed `This email already has an account. Use Login with Email.`
- Removing `adb reverse` made Email Login show `Server is not reachable...` instead of staying stuck in a loading state.
- `Play as Guest` with the backend unreachable fell back into local play.
- Filtered Logcat found no Mythwake/Unity crash, ANR, `NullReference`, missing asset, or `libunity` failure; remaining line was MuMu/Android telephony-service noise.

Local screenshots from this pass are under `Builds/Android/` with names beginning `Mythwake-0.2.169-account-tester-build-`.
