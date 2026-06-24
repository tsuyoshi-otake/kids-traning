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
- New or unstarted profiles start at grade 1, level 1, and beginner mastery (`.05`). Real progress is kept when stars, streaks, or mastery have changed.
- Difficulty is staged from easiest first-grade addition, then subtraction, then clock/kokugo, and finally multiplication/hissan as stars and mastery rise.
- Non-hissan arithmetic shows level-aligned visual aids under the question: concrete/ten-frame dots for addition, crossed-out dots for subtraction, and equal groups for multiplication.
- Emergency unlock PIN is `1234`.
- The window runs fullscreen, topmost, and blocks normal close shortcuts until completion.
- Clicking the existing `パソコンを つかう` completion control closes the app.
- The default executable mode is a task tray resident updater. Use the tray menu or run `KidsTraining.App.exe --training` to start fullscreen learning.
- Login startup is registered as `KidsTraining.App.exe --auto-training`, so the tray resident app starts and immediately opens fullscreen learning after user login.
- The tray app checks GitHub Releases once per hour. If a newer `KidsTraining.msi` is attached to the latest non-prerelease release, it downloads the MSI under `%LOCALAPPDATA%\KidsTraining\Updates`, starts a copied update runner, exits, and lets `msiexec` perform a quiet per-user reinstall without update-start notifications.
- The MSI installs under `%LOCALAPPDATA%\KidsTraining` and registers HKCU login startup for tray residency plus automatic fullscreen learning.
- Start Menu includes a tray shortcut and a direct learning-mode shortcut.

## Release Updates

Build new releases with a version that matches the release tag:

```powershell
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-msi.ps1 -Version 1.1.3
```

Publish a GitHub Release such as `v1.1.3` and attach `artifacts\KidsTraining.msi`. Anonymous update checks require the repository/releases to be public, or GitHub will return a private-repository access error.

Tracking issues:
- Initial app and installer: https://github.com/tsuyoshi-otake/kids-traning/issues/1
- Tray updater: https://github.com/tsuyoshi-otake/kids-traning/issues/2
- Login fullscreen startup: https://github.com/tsuyoshi-otake/kids-traning/issues/3
- Level 1 beginner startup: https://github.com/tsuyoshi-otake/kids-traning/issues/4
- Level-aligned arithmetic visuals: https://github.com/tsuyoshi-otake/kids-traning/issues/5
