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

Curriculum scope, stable unit IDs, assessment modes, and official references are listed in [the elementary grade 1 through junior-high grade 3 curriculum alignment](docs/curriculum-alignment.md).

- The app builds `assets\kids-training.runtime.html` from the source template and learning app definition, then maps the local assets to the constrained virtual HTTPS host `learning.kidstraining.local` in WebView2 while keeping all learning content local.
- Learning content is split into `index.template.html`, `app/learning-app.dc.html`, external CSS and JavaScript, and WOFF2 files under `fonts/`. During the secure-origin transition, the app reads only the four legacy learning keys from the earlier `file://` origin and copies each one only when the HTTPS-side key is absent, so existing progress is retained without overwriting newer data.
- The training page uses `localStorage` for data (`kt_profiles_v1`, `kt_settings_v1`, `kt_muted_v1`, and the resumable `kt_session_checkpoint_v1`), physically under `%LOCALAPPDATA%\KidsTraining\WebView2UserData`.
- The wrapper loads a runtime-patched copy of the bundled HTML so the profile selection screen is skipped.
- The profile store is normalized to a single current Windows user profile at startup, so bundled samples and the temporary `キッズ` profile are removed while first-profile progress is preserved.
- New profiles normally begin with every enabled elementary grade-1 unit at beginner confidence (`.05`), regardless of the registered school grade. Parents can optionally prefer the registered grade so the first active grade is the registered grade; this is not an upper limit, does not erase lower-grade progress, and can be turned off to restore the grade-1 frontier. Schema-v5 migration preserves stars, XP, history, historical clear flags, and legacy topic evidence while adding stable per-unit `unitStats`; rerunning the migration is idempotent.
- Math, Japanese, life/social/science, English, technology/home economics, moral education, information/integrated study, special activities, and keyboard practice retain their subject ordering through junior-high grade 3, but progression is gated as one grade cohort. Completing difficulty 5 starts retention for that unit; the next grade becomes available only after every enabled unit in the current grade has entered retention. Historical upper-grade evidence and saved sessions cannot skip this gate, and disabled topics do not block promotion.
- Every topic keeps separate counts for attempts, independent correct answers, assisted answers, revealed answers, and errors. Difficulty levels 1-2 advance after at least 3 independent successes in 4 same-level attempts, levels 3-4 after 4 in 5, and level 5 after 5 in 6. This uses stage-scoped cumulative evidence rather than a moving average, and a topic advances by at most one level at a time. Helped or revealed answers never count as independent evidence.
- Qualifying at difficulty 5 starts stage 6, a delayed retention stage, and allows the next curriculum topic to begin. A retention topic is removed from ordinary mixed practice until review is due, and only a scheduled difficulty-5 review can advance its confirmation count. Independent successes after approximately 1, 3, and 7 days are all required before the topic earns its sticky historical achievement (`★`); a failed, helped, or revealed retention review resets the current confirmation count and schedules another check after one day without erasing an achievement already earned. After mastery, 21-day maintenance reviews continue, so a due topic can keep its achievement while current readiness returns to review.
- Topic prerequisites are centralized in an acyclic graph. Only an upper topic with `attempts > 0` and either confidence below `.50` or a due review triggers remediation; the deepest available unmet prerequisites are then favored, while an unattempted upper topic keeps the normal curriculum flow.
- Placement checks ask three questions for each of six grade-relevant core skills. Untested skills start conservatively at `.05`; a single lucky answer cannot place a learner near mastery.
- Each session keeps a visible learning sequence: scheduled review, the next curriculum-frontier target, earlier mixed work, and a final target exit check. Questions are generated immediately before display so newly earned stages affect the next question, assisted/revealed/incorrect outcomes temporarily step that topic down by one stage until an independent success, and entering retention moves later target slots to the next curriculum topic. Every quiz and placement-check question displays its generated grade, category, and actual question difficulty from 1 through 5; stage 6 is shown as the separate retention state rather than a nonexistent difficulty 6. The metadata wraps on narrow screens. Exact question content is de-duplicated with bounded retries and a least-repeated fallback when a small question pool is exhausted. Passing requires both the configured global score and at least 70% independent success on four or more target questions.
- Curriculum progression uses indexed maps and insertion-ordered sets, linear minimum selection, one memoized progression scope per generated question, and constant-work stage overlays. A deterministic one-question bank no longer repeats the same candidate up to 24 times. Profile and checkpoint persistence are coalesced outside the synchronous button-update path and both are force-flushed before pause or unmount. Learning-page preparation starts in parallel with the independent WebView2 environment initialization, shortening the serial startup span while keeping cancellation and failure terminals explicit.
- The learning experience does not request camera or microphone access and performs no camera-based attention monitoring.
- The catalog covers elementary grades 1-6 and junior-high grades 1-3 in mathematics, Japanese, science, social studies, English, technology/home economics, moral education, integrated study/information, and special activities. Music, art, and health/physical education remain excluded. Activities that cannot be judged objectively on screen—experiments, observation, speaking, making, cooking, sewing, and collaboration—use activity cards and reflection; the app does not claim to replace school instruction or practical work.
- The independent キーボード topic progresses through five stages: a random single letter from `a`-`z`, then Japanese words whose romaji answers are exactly two, three, four, and five letters. Hiragana is shown with every word from stage 2 onward. A display-only QWERTY guide in stages 1-2 uses the same green, red, blue, and yellow touch-typing finger groups as the learner's color-coded physical keyboard; the next key keeps its finger color and also receives a raised white-and-dark outline so the cue does not depend on color alone. The guide is hidden in stages 3-5. Physical `A`-`Z` input is case-insensitive, ignores held-key repeats and IME composition/process events, and works the same with JIS and US layouts because no digits, punctuation, or layout-specific keys are used.
- Japanese language coverage separates kanji reading from choosing and writing kanji in context. The canonical bank contains the exact MEXT allocations of 80, 160, 200, 202, 193, and 191 characters for grades 1-6 (1,026 unique characters total), with startup validation for count and duplication. Romaji display follows the current Cabinet-notification spelling, while common equivalent IME spellings remain accepted for typing.
- English is listening/speaking-centered foreign-language activity in grade 4 and covers the five domains in grades 5-6. Audio, word order, letters, expressions, reading, writing, and activity cards are supported; pronunciation is not automatically graded.
- Multiplication starts with facts such as `1 × 2`, `2 × 1`, and `2 × 2`, then adds small pairs, the 1-5/10 tables, and finally the 6-9 tables, commutativity, and word problems at level 5. Division starts with visual equal sharing and making equal groups, then introduces the two interpretations, exact table facts, multiplicative comparison/two-step work, and finally remainders at level 5.
- Written arithmetic (hissan) starts with no-carry addition, then introduces carrying, borrowing/two-digit work, and only at its top stage larger written calculations or multiplication when the grade permits. Grade 3 includes three-digit-by-one-digit and two-digit-by-two-digit multiplication. In the addition and subtraction lanes, two-digit-by-two-digit questions with carrying or borrowing are also routed through the two-step written-arithmetic UI while evidence remains assigned to the original addition or subtraction topic.
- Across elementary grade 1 through junior-high grade 3, mathematical prompts, choices, feedback, and explanations render fractions, operators, signs, exponents, radicals, and ratios as structured notation while preserving their canonical grading values. Problems intended for written calculation use aligned vertical addition, subtraction, multiplication, or long-division layouts at the grade and difficulty where that method is appropriate; ordinary facts and fraction exercises stay in their natural inline form. Existing answer markers are retained without appending a duplicate `= ?`.
- Each learning session asks a fixed 10-30 questions selected by the parent. The default is 20 questions with 15 points required, plus the target-skill evidence gate described above. A correct answer is worth `1`, `0.5`, or `0.25` point after zero, one, or two mistakes. A third mistake awards `0` and automatically advances after showing the correct answer. The app does not extend the session after it starts. Legacy saved settings are clamped into the supported range without resetting progress.
- Addition progresses from totals within 10, to totals within 18, to tens/two-digit-with-one-digit questions without regrouping, and finally regrouping or three-term questions. Grade-1 subtraction stays within 20 across all five stages: subtraction within 10, simple teen subtraction, borrowing within 20, missing-number variants, and bounded three-term or mixed work. Subtracting zero appears only as an approximately 8% stage-1 concept review instead of a high-difficulty staple. General two-digit subtraction and written borrowing remain grade-2-or-later work. Both operations include generated □-operand variants. Fixed coverage places `3 + 7` and `7 + 3` in add stage 1; `10 + 2` and `10 + 5` in add stage 3; `58 + 29`, `68 + 22`, `35 + 25`, and `19 + 43` in add stage 5 as written-arithmetic questions; `9 - 3 - 2` in sub stage 4; `16 - 6 + 7` in sub stage 5; and the adult/children and stamp/envelope questions in story. Story generation draws from 12 object/unit combinations. Larger written-calculation shapes belong to the hissan topic.
- A dedicated たんい (measurement) topic follows the MEXT curriculum: grade 1 gets visual direct comparison (which is longer / holds more / is wider, counted in arbitrary units), grade 2 gets mm/cm/m and mL/dL/L conversions plus same-unit arithmetic, and grade 3 adds g/kg, km, and composite conversions. Time units (hour/minute/second, elapsed time, 1 day = 24 hours) and weekday-name/order/offset questions stay inside the clock topic.
- Non-hissan arithmetic shows level-aligned visual aids under the question: concrete/ten-frame dots for addition, crossed-out dots for subtraction, equal groups for multiplication/division, coin layouts, counted objects, tables, and tape diagrams. Visuals that are the question content always stay visible. Choosing an answer after hints is recorded as assisted evidence rather than an independent success.
- Question text uses context-aware ruby without annotating answer-revealing reading targets. Reading passages start `もんだい：` on a new line, and word-problem prompts do not append a synthetic `= ?`.
- Question typography responds to narrow or short screens while preserving ruby and natural wrapping. Click-style controls receive button semantics, Enter/Space activation, visible focus, and live feedback announcements; reduced-motion preferences suppress nonessential animation. A `わからない` action records a revealed outcome and shows the answer/explanation without inflating the score.
- Initial emergency unlock password is `1234`. It can be changed from the parent control page.
- The window runs fullscreen, topmost, and blocks normal close shortcuts until completion.
- Clicking the existing `パソコンを つかう` completion control closes the app.
- The default executable mode is a task tray resident updater. Use the tray menu or run `KidsTraining.App.exe --training` to start fullscreen learning.
- The tray app also serves a LAN parent control page on `http://<PCのIP>:44567/` when the port is available. The page can start fullscreen learning, return the PC screen and discard the current checkpoint, or pause an active session from another device on the same private network. Pause writes the exact quiz/feedback state before closing; the next launch restores the question index, generated questions, entered answer, mistake counts, written-arithmetic step, score, combo, and feedback state.
- The parent control page can change the four-digit parent password after the current password is entered. The password is saved under `%LOCALAPPDATA%\KidsTraining\parent-settings.json` and synced into the WebView storage when learning starts.
- Both PIN-protected in-app settings and the LAN parent page can set school grade from elementary grade 1 through junior-high grade 3, optionally prefer that registered grade as the starting frontier, choose a fixed question count from 10 through 30, and set a passing score from 1 through that selected count. The values share the atomic parent-settings store and live synchronization. Invalid values are rejected, missing legacy values default to grade 1 with grade preference off, and grade changes preserve all progress and affect display from the next session rather than the active question.
- Both the in-app and LAN parent dashboards offer PIN-confirmed reset choices. `学習履歴のみリセット` clears streak, mastery, five difficulty-level counters, sixth-stage retention evidence, achievements, review dates, and any active checkpoint while preserving level/XP/stars; `すべてリセット` additionally clears XP and stars. Both preserve the learner name, selected grade, profile color, parent PIN, question count, pass line, enabled topics, and mute setting. A LAN reset is applied immediately when the learning WebView is active or stored as a pending reset for the next launch.
- The tray menu includes `保護者画面を開く` and `保護者画面URLをコピー` for finding the parent control URL.
- Login startup is registered as `KidsTraining.App.exe --auto-training`, so the tray resident app starts and immediately opens fullscreen learning after user login.
- The tray app checks GitHub Releases once per hour. If a newer `KidsTraining.msi` is attached to the latest non-prerelease release, it downloads the MSI under `%LOCALAPPDATA%\KidsTraining\Updates`, starts a copied update runner, exits, and lets `msiexec` perform a quiet per-user reinstall without update-start notifications.
- The MSI installs under `%LOCALAPPDATA%\KidsTraining` and registers HKCU login startup for tray residency plus automatic fullscreen learning.
- Start Menu includes a tray shortcut and a direct learning-mode shortcut.

## Release Updates

Build new releases with a version that matches the release tag:

```powershell
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-msi.ps1 -Version 1.27.0
```

Publish a GitHub Release such as `v1.20.0` and attach `artifacts\KidsTraining.msi`. Anonymous update checks require the repository/releases to be public, or GitHub will return a private-repository access error.

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
- Five-stage physical keyboard practice: https://github.com/tsuyoshi-otake/kids-traning/issues/30
- Protected learning-progress reset: https://github.com/tsuyoshi-otake/kids-traning/issues/26
- Question coverage, furigana, prerequisite remediation, and typography: https://github.com/tsuyoshi-otake/kids-traning/issues/27
- LAN parent-page session scoring settings: https://github.com/tsuyoshi-otake/kids-traning/issues/28
- Adaptive non-repeating session questions: https://github.com/tsuyoshi-otake/kids-traning/issues/31
- Expanded problem variety, weekday practice, kanji material, and 10-30 question settings: https://github.com/tsuyoshi-otake/kids-traning/issues/32
- Per-question grade, category, and difficulty labels: https://github.com/tsuyoshi-otake/kids-traning/issues/33
- Complete grade 1-3 assigned-kanji coverage: https://github.com/tsuyoshi-otake/kids-traning/issues/34
- Runtime ownership and lifecycle hardening: https://github.com/tsuyoshi-otake/kids-traning/issues/29
- Remaining UI and learning-system audit: https://github.com/tsuyoshi-otake/kids-traning/issues/35
- Parent learning-history and full resets: https://github.com/tsuyoshi-otake/kids-traning/issues/36
- Three-attempt fractional scoring: https://github.com/tsuyoshi-otake/kids-traning/issues/37
- Pause and resume an in-progress session: https://github.com/tsuyoshi-otake/kids-traning/issues/38
- Grade-appropriate subtraction and finger-colored keyboard guidance: https://github.com/tsuyoshi-otake/kids-traning/issues/39
- Answer feedback within a 768px viewport: https://github.com/tsuyoshi-otake/kids-traning/issues/40
- Centered feedback layout and drawn hanamaru/batsu marks: https://github.com/tsuyoshi-otake/kids-traning/issues/44
- Visible session pass conditions and a way out of the retry loop: https://github.com/tsuyoshi-otake/kids-traning/issues/43
- Sixth-stage delayed retention: https://github.com/tsuyoshi-otake/kids-traning/issues/46
- Camera-based attention monitoring removal: https://github.com/tsuyoshi-otake/kids-traning/issues/50
- Grade 1-6 mastery-linked curriculum and school-grade display attribute: https://github.com/tsuyoshi-otake/kids-traning/issues/48
- Junior-high grade 1-3 mastery curriculum: https://github.com/tsuyoshi-otake/kids-traning/issues/49
- Startup and learning interaction performance: https://github.com/tsuyoshi-otake/kids-traning/issues/51
- Second-wave algorithmic performance optimization: https://github.com/tsuyoshi-otake/kids-traning/issues/52
