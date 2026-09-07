# Internal Alpha Release Gates

Before an internal candidate is called ready:

- Backend: `go test ./...`, `go vet ./...`, `gofmt -l .` returns no files, and `git diff --check` is clean. The same test/vet/format/whitespace checks run in `.github/workflows/backend-checks.yml`.
- Unity: run `scripts/check-unity-csharp.ps1` and `scripts/check-unity-current-slice.ps1`. Warnings from existing serialized optional fields are allowed; compile errors and validator exceptions are not.
- Packaging: set `IdlePrototypeController.PrototypeVersion`, `ProjectSettings.asset` `bundleVersion`, and the derived Android Version Code together. Build both APK and AAB with `scripts/build-android.cmd`.
- Metadata: verify package `com.xmiepsen.mythwake`, Version Name `0.2.177`, Version Code `2177`, label `Mythwake`, and the expected signing boundary with `aapt`/`jarsigner` when available.
- Manual: run `docs/ALPHA_TEST_PLAN.md`, including Server Mode Tower duplicate/restart/relogin and manual visual-stop cases.

Play Console upload, production signing, real-device coverage, and external service setup remain explicit handoff steps; they are not silently treated as local validation.
