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
- Topic progression is a clear-gated chain: every category unlocks only after its prerequisite category is cleared (topic stage 4, mastery >= 0.65). The chain is add → sub/moji → kazu/clock/story (after sub), kokugo and bun (after moji), goi (after bun), dokkai (after kokugo) → measure/chart (after kazu) → shape (after measure), and for grade 2+ hissan (after kazu) → mul (after hissan) → frac (after mul), with div at grade 3+ (after mul). Every category must be cleared to open the full set.
- Every topic uses the same four mastery stages (`<0.25`, `<0.45`, `<0.65`, then stage 4). Stage-based generators choose the current stage about 75% of the time and earlier material about 25% of the time, so new content is introduced gradually without dropping review questions. Existing saved mastery and unlock progress are preserved.
- Topics cover the MEXT grade 1-3 curriculum: mental/written arithmetic, multiplication tables, division with remainders (わりざん), large numbers and place value (かず), shapes (かたち: まる/さんかく → 正方形/長方形/直角三角形/はこの形 → 二等辺/正三角形/円/球), fractions and decimals including decimal composition (ぶんすう), pictograph/bar-chart reading (グラフ), word problems and □-equations (ぶんしょうだい), clock/time, measurement units, kanji reading/writing choice, and hiragana/katakana/alphabet/romaji.
- Japanese language coverage beyond kanji/kana lives in three dedicated topics: ぶん (bun: particles は・を・へ, punctuation and quotation marks, katakana loanword/onomatopoeia spelling, subject/predicate at grade 2+, modifiers at grade 3+), ことば (goi: antonyms and word groups at all grades, proverbs/idioms and dictionary gojuon ordering at grade 3+), and よみとり (dokkai: 1-3 sentence reading-comprehension passages staged by grade).
- An English topic えいご (eigo) matches the MEXT grade 3-4 foreign language activities: colors and numbers 1-10 both directions (English→Japanese and Japanese→English), everyday words (animals, food, school items) at stage 2+, and greetings/basic phrases (Hello, Good morning, Thank you, What is your name?) at stage 3+. Japanese→English spelling choices and English phrase choices each have an independent pronunciation button. It uses the browser's English (`en-US`) speech voice at a child-friendly rate, never submits an answer, stops the previous utterance before replaying, and is disabled when audio is muted or speech synthesis is unavailable.
- Multiplication starts with facts such as `1 × 2`, `2 × 1`, and `2 × 2`, then adds the 2/5 tables, the 1-5/10 tables, and finally the 6-9 tables. Division starts with `2 ÷ 2`, `4 ÷ 2`, and `6 ÷ 2`, then introduces small divisors, exact table facts, and only at stage 4 introduces remainders and larger dividends.
- Written arithmetic (hissan) starts with no-carry addition, then introduces carrying, borrowing/two-digit work, and only at its top stage larger written calculations or multiplication when the grade permits.
- Each learning session asks 20 questions and requires 15 correct answers to pass.
- Non-hissan addition and subtraction progress from totals within 10, to totals within 18, to tens/two-digit-with-one-digit questions without regrouping, and finally regrouping or three-term questions. Larger written-calculation shapes belong to the hissan topic.
- A dedicated たんい (measurement) topic follows the MEXT curriculum: grade 1 gets visual direct comparison (which is longer / holds more / is wider, counted in arbitrary units), grade 2 gets mm/cm/m and mL/dL/L conversions plus same-unit arithmetic, and grade 3 adds g/kg, km, and composite conversions. Time units (hour/minute/second, elapsed time, 1 day = 24 hours) stay inside the clock topic.
- Non-hissan arithmetic shows level-aligned visual aids under the question: concrete/ten-frame dots for addition, crossed-out dots for subtraction, equal groups for multiplication, and equal-share groups for division. These hints (and the kokugo meaning hint box) are adaptive scaffolds: they appear only while the topic's mastery stage is 1-2 (mastery < 0.45) and disappear once the child reaches stage 3, reappearing automatically if mastery drops after mistakes. Visuals that ARE the question content (clock face, charts, shapes, measure comparison, fraction diagrams, position squares) always stay visible, and hissan's per-step hint after a wrong digit remains mistake-triggered.
- Initial emergency unlock password is `1234`. It can be changed from the parent control page.
- The window runs fullscreen, topmost, and blocks normal close shortcuts until completion.
- Clicking the existing `パソコンを つかう` completion control closes the app.
- The default executable mode is a task tray resident updater. Use the tray menu or run `KidsTraining.App.exe --training` to start fullscreen learning.
- The tray app also serves a LAN parent control page on `http://<PCのIP>:44567/` when the port is available. The page can start fullscreen learning or return the PC screen from another device on the same private network.
- The parent control page can change the four-digit parent password after the current password is entered. The password is saved under `%LOCALAPPDATA%\KidsTraining\parent-settings.json` and synced into the WebView storage when learning starts.
- The tray menu includes `保護者画面を開く` and `保護者画面URLをコピー` for finding the parent control URL.
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
- Parent remote control page: https://github.com/tsuyoshi-otake/kids-traning/issues/10
- Mental arithmetic operand limits: https://github.com/tsuyoshi-otake/kids-traning/issues/11
- Topic progression and multiplication difficulty: https://github.com/tsuyoshi-otake/kids-traning/issues/12
- Session length and pass threshold: https://github.com/tsuyoshi-otake/kids-traning/issues/13
- Four-stage curriculum review and English pronunciation choices: https://github.com/tsuyoshi-otake/kids-traning/issues/20
