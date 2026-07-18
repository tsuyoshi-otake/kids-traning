# Kids Training WebView2 App

This project wraps the split learning content under `kids-training/` in a fullscreen Windows WebView2 app and builds a per-user MSI installer.

## Build

```powershell
rtk dotnet publish src/KidsTraining.App/KidsTraining.App.csproj -c Release -r win-x64 --self-contained true
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-msi.ps1
```

The MSI is written to `artifacts\KidsTraining.msi`.

## Architecture

The application shell follows an inward dependency direction:

```text
Presentation/WinForms -> Application <- Infrastructure
                              |
                            Domain
```

- `Domain` contains stable values and terminal result types such as parent PINs, release versions, and update outcomes.
- `Application` contains learning-page, parent-password, and update use cases plus their external ports. It does not access WinForms, WebView2, files, HTTP, or processes.
- `Infrastructure` implements those ports with split learning assets, JSON settings, GitHub Releases, MSI launching, and the LAN parent-control adapter.
- `Presentation/WinForms` owns the tray and fullscreen UI and receives its use cases from `Program.cs`, the composition root.
- Runtime markup patches are ordered in `Application/Learning/Markup` and fail explicitly when a required anchor is missing.

Run the dependency and use-case checks with:

```powershell
rtk dotnet run --project tests\KidsTraining.ArchitectureTests\KidsTraining.ArchitectureTests.csproj -c Release -- .
```

## Runtime Behavior

- The app builds `assets\kids-training.runtime.html` from the source template and learning app definition, then loads it in WebView2. The runtime path stays compatible with earlier releases so the existing WebView `file://` storage origin is preserved.
- Learning content is split into `index.template.html`, `app/learning-app.dc.html`, external CSS and JavaScript, and WOFF2 files under `fonts/`. This keeps the repository source maintainable without changing the existing `file://` origin used by WebView2 and `localStorage`.
- The training page uses `localStorage` for data (`kt_profiles_v1`, `kt_settings_v1`, `kt_muted_v1`), physically under `%LOCALAPPDATA%\KidsTraining\WebView2UserData`.
- The wrapper loads a runtime-patched copy of the bundled HTML so the profile selection screen is skipped.
- The profile store is normalized to a single current Windows user profile at startup, so bundled samples and the temporary `キッズ` profile are removed while first-profile progress is preserved.
- New or unstarted profiles start at grade 1, level 1, and beginner confidence (`.05`). Profiles saved by earlier releases are migrated in place: stars, streaks, mastery values, and historical clear flags are retained while explicit five-stage progress, stage evidence counters, and review dates are added.
- Math, Japanese, and supplementary English are independent ordered learning lanes. There is no add→Japanese or Japanese→math unlock chain, but each lane introduces only its next unfinished topic. The default grade 1 math order starts with numbers and shapes before addition/subtraction and later money/equal-group foundations; grade 2 starts with charts, clocks, arithmetic and measurement before written arithmetic, calculation order and multiplication; grade 3 starts with multiplication, division, shapes and advanced written arithmetic. The order and missing practice elements were informed by the [grade 1](https://print-kids.net/print/1nensei.html), [grade 2](https://print-kids.net/print/2nensei.html), [grade 3](https://print-kids.net/print/3nensei.html), and [calculation-order](https://print-kids.net/print/sansuu/keisan-no-junjo/) references. The setup UI intentionally offers only grades 1-3.
- Every topic keeps separate counts for attempts, independent correct answers, assisted answers, revealed answers, and errors. A topic advances by at most one of its five stages after at least three same-stage attempts with at least 67% independent accuracy. Historical achievement (`★`) is sticky, while current readiness requires stage 5, at least 8 independent correct answers across 10 attempts, at least 80% independent accuracy, confidence of at least `.80`, and no overdue review. Helped or revealed answers never count as independent evidence.
- Correct independent work schedules review after approximately 1, 3, 7, and 21 days. Assisted, revealed, and incorrect work becomes due immediately. A due topic can therefore keep its achievement while returning to the review state.
- Topic prerequisites are centralized in an acyclic graph. Only an upper topic with `attempts > 0` and either confidence below `.50` or a due review triggers remediation; the deepest available unmet prerequisites are then favored, while an unattempted upper topic keeps the normal curriculum flow.
- Placement checks ask three questions for each of six grade-relevant core skills. Untested skills start conservatively at `.05`; a single lucky answer cannot place a learner near mastery.
- Each session keeps a visible learning sequence: scheduled review, the next curriculum-frontier target, earlier mixed work, and a final target exit check. Passing requires both the configured global score and at least 70% independent success on four or more target questions.
- The implemented grade 1-3 practice scope includes mental/written arithmetic, money, equal-group foundations, parentheses and inequality signs, multiplication tables, equal-sharing/equal-grouping division and remainders, place value through `1億`, up-to-four-digit addition/subtraction, two-digit multiplication at the advanced grade-3 stage, flat/solid shapes and compass basics, fractions/decimals, tables/scaled chart reading, tape-diagram word problems and □-equations, clock/time, measurement units, kanji reading/selection, and kana/romaji. This is practice software, not a claim of complete textbook, handwriting, speaking-assessment, or school-curriculum equivalence.
- Japanese language coverage beyond kanji/kana lives in three dedicated topics: ぶん (bun: particles は・を・へ, punctuation and quotation marks, katakana loanword/onomatopoeia spelling, subject/predicate at grade 2+, modifiers at grade 3+), ことば (goi: antonyms and word groups at all grades, proverbs/idioms and dictionary gojuon ordering at grade 3+), and よみとり (dokkai: 1-3 sentence reading-comprehension passages staged by grade).
- The grade-3 English topic is explicitly supplementary practice inspired by foreign-language activities: colors and numbers 1-10, everyday words, greetings, and basic phrases. Pronunciation buttons use the browser's English (`en-US`) voice and the screen asks the learner to repeat aloud before choosing; the app does not assess pronunciation or replace interactive speaking with a teacher or partner.
- Multiplication starts with facts such as `1 × 2`, `2 × 1`, and `2 × 2`, then adds small pairs, the 1-5/10 tables, and finally the 6-9 tables, commutativity, and word problems at level 5. Division starts with visual equal sharing and making equal groups, then introduces the two interpretations, exact table facts, multiplicative comparison/two-step work, and finally remainders at level 5.
- Written arithmetic (hissan) starts with no-carry addition, then introduces carrying, borrowing/two-digit work, and only at its top stage larger written calculations or multiplication when the grade permits.
- Each learning session asks at least 20 questions. The default global threshold is 15 independent correct answers, with the additional target-skill evidence gate described above. Legacy saved settings migrate without resetting progress.
- Non-hissan addition and subtraction progress from totals within 10, to totals within 18, to tens/two-digit-with-one-digit questions without regrouping, and finally regrouping or three-term questions. Fixed coverage places `3 + 7` and `7 + 3` in add stage 1; `10 + 2` and `10 + 5` in add stage 3; `58 + 29`, `68 + 22`, `35 + 25`, and `19 + 43` in add stage 5; `9 - 3 - 2` in sub stage 4; `16 - 6 + 7` in sub stage 5; and the adult/children and stamp/envelope questions in story. Larger written-calculation shapes belong to the hissan topic.
- A dedicated たんい (measurement) topic follows the MEXT curriculum: grade 1 gets visual direct comparison (which is longer / holds more / is wider, counted in arbitrary units), grade 2 gets mm/cm/m and mL/dL/L conversions plus same-unit arithmetic, and grade 3 adds g/kg, km, and composite conversions. Time units (hour/minute/second, elapsed time, 1 day = 24 hours) stay inside the clock topic.
- Non-hissan arithmetic shows level-aligned visual aids under the question: concrete/ten-frame dots for addition, crossed-out dots for subtraction, equal groups for multiplication/division, coin layouts, counted objects, tables, and tape diagrams. Visuals that are the question content always stay visible. Choosing an answer after hints is recorded as assisted evidence rather than an independent success.
- Question text uses context-aware ruby without annotating answer-revealing reading targets. Reading passages start `もんだい：` on a new line, and word-problem prompts do not append a synthetic `= ?`.
- Question typography responds to narrow or short screens while preserving ruby and natural wrapping. Click-style controls receive button semantics, Enter/Space activation, visible focus, and live feedback announcements; reduced-motion preferences suppress nonessential animation. A `わからない` action records a revealed outcome and shows the answer/explanation without inflating the score.
- Initial emergency unlock password is `1234`. It can be changed from the parent control page.
- The window runs fullscreen, topmost, and blocks normal close shortcuts until completion.
- Clicking the existing `パソコンを つかう` completion control closes the app.
- The default executable mode is a task tray resident updater. Use the tray menu or run `KidsTraining.App.exe --training` to start fullscreen learning.
- The tray app also serves a LAN parent control page on `http://<PCのIP>:44567/` when the port is available. The page can start fullscreen learning or return the PC screen from another device on the same private network.
- The parent control page can change the four-digit parent password after the current password is entered. The password is saved under `%LOCALAPPDATA%\KidsTraining\parent-settings.json` and synced into the WebView storage when learning starts.
- The parent control page can also set 20-40 questions per session and a passing score from 1 through the selected question count. These values share the atomic parent-settings store, are validated by the application service, and are synchronized into WebView storage for the next learning session.
- The in-app parent dashboard includes a two-step learning-progress reset. It clears stars, XP, streak, mastery, five-stage evidence, achievements, review dates, and any active session while preserving the learner name, selected grade, profile color, parent PIN, question count, pass line, enabled topics, and mute setting. The reset is persisted immediately and cannot run unless the confirmation panel is open.
- The tray menu includes `保護者画面を開く` and `保護者画面URLをコピー` for finding the parent control URL.
- Login startup is registered as `KidsTraining.App.exe --auto-training`, so the tray resident app starts and immediately opens fullscreen learning after user login.
- The tray app checks GitHub Releases once per hour. If a newer `KidsTraining.msi` is attached to the latest non-prerelease release, it downloads the MSI under `%LOCALAPPDATA%\KidsTraining\Updates`, starts a copied update runner, exits, and lets `msiexec` perform a quiet per-user reinstall without update-start notifications.
- The MSI installs under `%LOCALAPPDATA%\KidsTraining` and registers HKCU login startup for tray residency plus automatic fullscreen learning.
- Start Menu includes a tray shortcut and a direct learning-mode shortcut.

## Release Updates

Build new releases with a version that matches the release tag:

```powershell
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-msi.ps1 -Version 1.15.0
```

Publish a GitHub Release such as `v1.15.0` and attach `artifacts\KidsTraining.msi`. Anonymous update checks require the repository/releases to be public, or GitHub will return a private-repository access error.

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
- Twenty-question migration and five-level mastery: https://github.com/tsuyoshi-otake/kids-traning/issues/21
- Split learning HTML assets: https://github.com/tsuyoshi-otake/kids-traning/issues/22
- Clean Architecture refactoring: https://github.com/tsuyoshi-otake/kids-traning/issues/23
- Evidence-based curriculum progression: https://github.com/tsuyoshi-otake/kids-traning/issues/24
- Ordered grade 1-3 curriculum coverage: https://github.com/tsuyoshi-otake/kids-traning/issues/25
- Protected learning-progress reset: https://github.com/tsuyoshi-otake/kids-traning/issues/26
- Question coverage, furigana, prerequisite remediation, and typography: https://github.com/tsuyoshi-otake/kids-traning/issues/27
- LAN parent-page session scoring settings: https://github.com/tsuyoshi-otake/kids-traning/issues/28
- Runtime ownership and lifecycle hardening: https://github.com/tsuyoshi-otake/kids-traning/issues/29
