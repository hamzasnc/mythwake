# Login and shop presentation

The login uses the existing account controller and its Continue, email login,
registration, guest, validation, error and localization flows. `MythwakeLoginUI`
supplies the artwork and styling. `LoginDesignScale` fits a fixed 1080 × 1920
design into the available portrait area, keeping artwork and controls together.

## Assets and slicing

- `Assets/_Mythwake/Resources/Mythwake/UI/Login/login_background.png`: three
  runtime UV slices, design rows 0–720, 720–1640, and 1640–1920.
- `Assets/_Mythwake/Resources/Mythwake/UI/Login/login_button.png`: transparent
  source loaded as a regular texture. Runtime `RawImage` controls crop the
  painted button and remain the live click targets. This avoids Android falling
  back to the brown default button when a generated multi-sprite cannot load.
  The legacy `Image` component is disabled, and account buttons are excluded
  from the global runtime button skin so the old brown layer cannot reappear.
- Headings use [Cinzel Bold](https://github.com/google-fonts-bower/cinzel-bower),
  with its OFL license bundled beside the font in `UI/Fonts/OFL.txt`.

Both login images were generated using the built-in Imagegen tool. Final prompts:

Background:
> Create a production mobile fantasy RPG login background asset for Mythwake, portrait 1080x1920. Painterly premium game UI style: antique gold carved border, deep midnight navy, cyan magical spiral medallion in upper center around y=320, atmospheric dark forest and distant ruined arches at edges. Very dark clean empty central panel spanning x=130..950 y=620..1620, reserved for runtime login controls. Subtle gold filigree corners, teal mist, restrained magical particles. No text, no letters, no buttons, no input fields. Entire composition is a finished edge-to-edge background, elegant readable negative space. Gold and cyan match a high quality fantasy shop interface.

Button:
> Production fantasy RPG UI button sprite, single wide horizontal button centered on genuinely transparent background. Wide 6:1 button ratio, canvas tightly framing it with minimal transparent padding. Ornate antique gold beveled slim frame, finely engraved corners, tiny cyan gemstone at left and right tips. Deep midnight teal subtle inset center, clean empty space for a text label rendered later. Premium painterly realistic gold and magical cyan style, elegant and restrained, dark forest fantasy. No words, no letters, no numbers, no symbols in center. Straight continuous edges with corner decoration restricted to outer 10 percent so it works as Unity 9-sliced sprite.

## Shop

The Featured artwork remains the common header and navigation on all tabs.
The active tab frame is sampled directly from Featured, with a live label.
Battle Pass artwork and its interactions live inside a clipped body viewport;
its separate baked header and footer are no longer displayed. Its content fits
inside the viewport, including the premium reward row.

The shared resource bar, tabs, menu and bottom navigation use a dedicated hit
layer above secondary tab content. The gem and gold plus controls select the
Crystals and Bundles tabs; the crest, management menu and every bottom navigation
destination remain usable from Crystals, Bundles, Battle Pass and Dev.

## Verification

Unity menu: **Mythwake → Validate Login and Shop Presentation**.
Batch method: `LoginShopPresentationValidation.Run`.

Runs the existing account validation, checks common shop geometry through
Featured → Crystals → Bundles → Battle Pass → Featured, verifies that shop
decoration is hidden after leaving, and renders previews in
`docs/screenshots/login-shop/`.

The Android APK was built successfully on 2026-09-05 and its APK v2 signature
verified. Package: `com.xmiepsen.mythwake`, version `0.2.176` (2176), ARM64.
Output: `C:/Users/March/.codex/worktrees/5a03/mythwake/Builds/Android/Mythwake-0.2.176-login-shop-ui.apk`.
The APK was not installed on a device during these checks. No real account was
registered or live purchase made.
