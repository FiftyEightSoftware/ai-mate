# AI Mate Android Internal Test Plan

- App: AI Mate (Flutter WebView wrapper)
- Package: `com.nickphinesme.aimate`
- Build: `.aab` at `ai_mate_wrapper/build/app/outputs/bundle/release/app-release.aab`
- Release URL: https://ai-mate.nickphinesme.com
- Debug URLs (reference): `http://10.0.2.2:5173` (Android emulator), `http://localhost:5173` (others)

## Scope
Validate first internal release for install, launch, navigation, refresh, connectivity, and stability.

## Devices
- At least one physical Android 10+ phone.
- Optionally an Android emulator (API 30+).

## Preconditions
- Tester is added to the Play Internal testing track and has accepted the opt-in link.

## Test Cases

1. Installation
- Install from Internal test link.
- Verify app label is "AI Mate" and icon appears.

2. First Launch
- App opens without crash.
- Linear progress indicator appears while loading.
- Home content from `https://ai-mate.nickphinesme.com` loads.

3. Navigation
- Navigate across PWA sections (Home, Jobs, Quotes, Invoices, Expenses, Clients, Assistant, Settings).
- Use hardware back: confirm it navigates back within the app or exits at root.

4. Pull-to-Refresh
- Swipe down to refresh; confirm page reloads.

5. External Links
- If the PWA has external links, verify expected behavior (in-app vs external browser) is consistent.

6. Rotation & UI
- Rotate device; app remains usable without layout breakage.

7. Connectivity
- Go offline (Airplane Mode). Observe behavior (cached content, offline message).
- Restore connectivity; content recovers on next load/refresh.

8. Performance & Stability
- Scroll across long lists; no severe jank or stutter.
- No crashes after 10+ minutes of general use.

9. Uninstall/Reinstall
- Uninstall the app, reinstall from Internal test link, verify launch and load.

10. Upgrade (future builds)
- Accept an update when available; verify app launches and loads normally post-update.

## Reporting
For each issue, provide:
- Reproduction steps
- Device model and Android version
- Screenshots/screen recording
- Logcat snippet if a crash occurs

## Pass/Fail Criteria
- Pass: All critical paths above work without crash; any issues are minor or cosmetic.
- Fail: Crash on launch, content never loads, or major navigation/refresh/connectivity failures.
