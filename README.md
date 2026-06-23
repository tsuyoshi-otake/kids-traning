# Kids Training WebView2 App

This project wraps `kids-training.html` in a fullscreen Windows WebView2 app and builds a per-user MSI installer.

## Build

```powershell
rtk dotnet publish src/KidsTraining.App/KidsTraining.App.csproj -c Release -r win-x64 --self-contained true
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-msi.ps1
```

The MSI is written to `artifacts\KidsTraining.msi`.

## Runtime Behavior

- The app loads `assets\kids-training.html` in WebView2.
- The training page uses `localStorage` for data (`kt_profiles_v1`, `kt_settings_v1`, `kt_muted_v1`), physically under `%LOCALAPPDATA%\KidsTraining\WebView2UserData`.
- The wrapper loads a runtime-patched copy of the bundled HTML so the profile selection screen is skipped.
- The profile store is normalized to a single current Windows user profile at startup, so bundled samples and the temporary `キッズ` profile are removed while first-profile progress is preserved.
- Emergency unlock PIN is `1234`.
- The window runs fullscreen, topmost, and blocks normal close shortcuts until completion.
- Clicking the existing `パソコンを つかう` completion control closes the app.
- The MSI installs under `%LOCALAPPDATA%\KidsTraining` and registers HKCU login startup.

Tracking issue: https://github.com/tsuyoshi-otake/kids-traning/issues/1
