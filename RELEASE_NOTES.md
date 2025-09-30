# AI Mate Android (Flutter Wrapper) – Internal Test Release Notes

- Version: 1.0.0 (1)
- Package: `com.nickphinesme.aimate`
- Bundle: `ai_mate_wrapper/build/app/outputs/bundle/release/app-release.aab`
- Wrapper: Flutter WebView (`ai_mate_wrapper/lib/main.dart`)
- Release URL: https://ai-mate.nickphinesme.com

## What’s New
- Flutter WebView wrapper for the AI Mate PWA with dark theme and copper accent.
- Pull-to-refresh, linear loading indicator, and app bar with title.
- Production-only HTTPS URL for release builds; cleartext disabled in main manifest.
- Debug builds allow cleartext via a dedicated debug manifest for loading the local dev server.

## Technical Changes
- Android `applicationId`/`namespace` set to `com.nickphinesme.aimate` in `ai_mate_wrapper/android/app/build.gradle.kts`.
- `AndroidManifest.xml` (main): label set to "AI Mate", `usesCleartextTraffic` removed.
- `AndroidManifest.xml` (debug): `usesCleartextTraffic="true"` added for dev-only HTTP.
- `MainActivity.kt` moved/created under `com.nickphinesme.aimate`.
- Conditional release signing via `key.properties` (gitignored). Falls back to debug keys only when missing.

## Known Issues / Limitations
- Navigation is driven by the PWA. Hardware back behaves like browser back.
- Offline capability depends on the PWA’s service worker and cached assets.
- No native Android permissions required in this build.

## Upgrade Notes
- Increment `version` in `ai_mate_wrapper/pubspec.yaml` for every Play upload (e.g., `1.0.1+2`).
- Rebuild with `flutter build appbundle --release`.

## Play Console Notes
- Use Google Play App Signing.
- Upload the `.aab` to Internal testing and add testers.

## Short Release Notes (for Play Console)
- Initial Internal Test release of AI Mate (Flutter WebView wrapper).
- Loads the production PWA at https://ai-mate.nickphinesme.com.
- Includes pull-to-refresh and a loading indicator.
- No native permissions requested in this build.
